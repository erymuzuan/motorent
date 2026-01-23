# Exchange Rate Provider Integration Plan

## Overview
Port IExchangeRateService from forex project to MotoRent cashier-till, enabling denomination-specific rates from MamyExchange and SuperRich providers with delta adjustments per shop.

## Requirements Summary
- Denomination-specific rates (USD 100 at 31.50, USD 50 at 31.45)
- Both buy and sell rates (buy for transactions, sell for reference)
- Direct update with audit trail (no approval workflow)
- Organization default provider + shop-level delta overrides

---

## Phase 1: Domain Entities

### 1.1 New Entity: RawExchangeRate (DTO - not persisted)
**File:** `src/MotoRent.Domain/Entities/RawExchangeRate.cs`

```csharp
public class RawExchangeRate
{
    public string BaseCurrency { get; set; } = "THB";
    public string Provider { get; set; } = "";
    public string Currency { get; set; } = "";
    public DateTimeOffset UpdatedOn { get; set; }
    public decimal Buying { get; set; }
    public decimal Selling { get; set; }
    public List<decimal> Denominations { get; set; } = [];
}
```

### 1.2 New Entity: DenominationRate
**File:** `src/MotoRent.Domain/Entities/DenominationRate.cs`

Stores rates with denomination granularity:
- `DenominationRateId`, `ShopId?` (null = org default)
- `Currency`, `Denomination` (e.g., 100, 50, 20)
- `BuyRate`, `SellRate` (adjusted rates)
- `ProviderCode`, `ProviderBuyRate`, `ProviderSellRate` (original)
- `BuyDelta`, `SellDelta` (saved adjustments, e.g., -0.06)
- `EffectiveDate`, `ExpiresOn`, `IsActive`

### 1.3 New Entity: RateDelta
**File:** `src/MotoRent.Domain/Entities/RateDelta.cs`

Persists delta configuration for reapplication:
- `RateDeltaId`, `ShopId?`
- `Currency`, `Denomination?` (null = all denominations)
- `BuyDelta`, `SellDelta`
- `IsActive`

### 1.4 New Constants: RateProviderCodes
**File:** `src/MotoRent.Domain/Entities/RateProviderCodes.cs`

```csharp
public static class RateProviderCodes
{
    public const string MamyExchange = "MamyExchange";
    public const string SuperRich = "SuperRich";
    public const string Manual = "Manual";
}
```

### 1.5 Update Entity.cs
Add JSON polymorphism for new entities:
```csharp
[JsonDerivedType(typeof(DenominationRate), nameof(DenominationRate))]
[JsonDerivedType(typeof(RateDelta), nameof(RateDelta))]
```

---

## Phase 2: Database Schema

### 2.1 DenominationRate Table
**File:** `database/tables/MotoRent.DenominationRate.sql`

```sql
CREATE TABLE [<schema>].[DenominationRate]
(
    [DenominationRateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [Denomination] AS CAST(JSON_VALUE([Json], '$.Denomination') AS DECIMAL(18,2)),
    [BuyRate] AS CAST(JSON_VALUE([Json], '$.BuyRate') AS DECIMAL(18,4)),
    [ProviderCode] AS CAST(JSON_VALUE([Json], '$.ProviderCode') AS NVARCHAR(50)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [EffectiveDate] DATETIMEOFFSET NULL,
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
CREATE INDEX IX_DenominationRate_Currency ON [<schema>].[DenominationRate]([Currency], [IsActive])
CREATE INDEX IX_DenominationRate_Shop ON [<schema>].[DenominationRate]([ShopId], [Currency])
```

### 2.2 RateDelta Table
**File:** `database/tables/MotoRent.RateDelta.sql`

```sql
CREATE TABLE [<schema>].[RateDelta]
(
    [RateDeltaId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [Denomination] AS CAST(JSON_VALUE([Json], '$.Denomination') AS DECIMAL(18,2)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
CREATE INDEX IX_RateDelta_Shop_Currency ON [<schema>].[RateDelta]([ShopId], [Currency])
```

---

## Phase 3: Rate Providers

### 3.1 Interface: IExchangeRateProvider
**File:** `src/MotoRent.Services/ExchangeRateProviders/IExchangeRateProvider.cs`

```csharp
public interface IExchangeRateProvider
{
    string Name { get; }
    string Code { get; }
    string BaseCurrency { get; }
    string? IconUrl { get; }
    DateTimeOffset? LastUpdatedOn { get; }

    Task<RawExchangeRate[]> GetRatesAsync();
    Task<bool> IsAvailableAsync();
}
```

### 3.2 MamyExchangeProvider
**File:** `src/MotoRent.Services/ExchangeRateProviders/MamyExchangeProvider.cs`

Port from forex project with simplifications:
- Fetch from `https://mamyexchange.com/wp-json/currencyxrates/v1/rates`
- Parse denomination ranges from API
- Use configured denominations per currency

### 3.3 SuperRichProvider
**File:** `src/MotoRent.Services/ExchangeRateProviders/SuperRichProvider.cs`

Port from forex project:
- Fetch from `https://www.superrichthailand.com/web/api/v1/rates`
- 30-minute cache TTL
- Parse denomination ranges

---

## Phase 4: Service Layer

### 4.1 Enhanced ExchangeRateService
**File:** `src/MotoRent.Services/ExchangeRateService.cs`

New methods:
```csharp
// Provider operations
Task<RawExchangeRate[]> FetchFromProviderAsync(string providerCode);
Task<FetchRatesResult> ApplyAndSaveRatesAsync(string providerCode, int? shopId, string username);

// Denomination rates
Task<DenominationRate?> GetDenominationRateAsync(string currency, decimal denomination, int? shopId);
Task<List<DenominationRate>> GetAllDenominationRatesAsync(string currency, int? shopId);

// Delta management
Task<SubmitOperation> SaveDeltaAsync(RateDelta delta, string username);
Task<List<RateDelta>> GetDeltasAsync(string currency, int? shopId);
Task<SubmitOperation> ApplyDeltasToRatesAsync(int? shopId, string username);

// Enhanced conversion
Task<ExchangeConversionResult?> ConvertWithDenominationAsync(
    string currency, decimal amount, decimal? denomination, int? shopId);
```

### 4.2 Partial Classes
Split for organization:
- `ExchangeRateService.Providers.cs` - Provider fetch logic
- `ExchangeRateService.Deltas.cs` - Delta CRUD and application
- `ExchangeRateService.Denominations.cs` - Denomination rate queries

---

## Phase 5: UI Components

### 5.1 Enhanced ExchangeRateSettings Page
**File:** `src/MotoRent.Client/Pages/Settings/ExchangeRateSettings.razor`

Redesign with sections:
1. **Provider Selection** - Dropdown (SuperRich/MamyExchange/Manual), Fetch button
2. **Rate Table** - Expandable rows per currency showing denominations
3. **Delta Inputs** - Inline editing of buy/sell deltas per denomination
4. **Shop Override** - Toggle for shop-specific settings

### 5.2 New Dialog: RateProviderPreviewDialog
**File:** `src/MotoRent.Client/Pages/Settings/RateProviderPreviewDialog.razor`

Shows preview before applying:
- Currency list with buy/sell from provider
- Denomination breakdown
- Compare with current rates
- Apply button

### 5.3 Localization Files
- `Resources/Pages/Settings/ExchangeRateSettings.resx` (+ .en.resx, .th.resx)
- `Resources/Pages/Settings/RateProviderPreviewDialog.resx` (+ .en.resx, .th.resx)

---

## Phase 6: Integration

### 6.1 Update TillService.currency.cs
Modify foreign currency conversion to use denomination-aware rates:
```csharp
// Add optional denomination parameter
public async Task<SubmitOperation> RecordForeignCurrencyPaymentAsync(
    ..., decimal? denomination = null, ...)
```

### 6.2 Update TillTransaction Entity
Add fields:
```csharp
public decimal? Denomination { get; set; }
public int? DenominationRateId { get; set; }
```

### 6.3 DI Registration in Program.cs
```csharp
// HttpClient for providers
builder.Services.AddHttpClient<MamyExchangeProvider>();
builder.Services.AddHttpClient<SuperRichProvider>();

// Register providers
builder.Services.AddScoped<IExchangeRateProvider, MamyExchangeProvider>();
builder.Services.AddScoped<IExchangeRateProvider, SuperRichProvider>();

// Repository registration
builder.Services.AddScoped<IRepository<DenominationRate>, SqlJsonRepository<DenominationRate>>();
builder.Services.AddScoped<IRepository<RateDelta>, SqlJsonRepository<RateDelta>>();
```

---

## Critical Files

| File | Action |
|------|--------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add JsonDerivedType for new entities |
| `src/MotoRent.Services/ExchangeRateService.cs` | Major expansion with provider/delta logic |
| `src/MotoRent.Client/Pages/Settings/ExchangeRateSettings.razor` | Redesign UI |
| `src/MotoRent.Server/Program.cs` | DI registration for providers |
| `database/tables/MotoRent.DenominationRate.sql` | New table |
| `database/tables/MotoRent.RateDelta.sql` | New table |

---

## Verification

1. **Provider connectivity**: Fetch rates from MamyExchange and SuperRich APIs
2. **Delta persistence**: Save deltas, refresh rates, verify deltas reapply
3. **Denomination lookup**: Query correct rate for specific denomination
4. **Cashier workflow**: Record foreign payment with denomination, verify rate used
5. **Shop override**: Verify shop-specific deltas override org defaults
6. **EOD audit**: Confirm rate source and delta tracked in transactions

---

## Implementation Order

1. **Wave 1**: Domain entities (RawExchangeRate DTO, DenominationRate, RateDelta, RateProviderCodes)
2. **Wave 2**: Database scripts (DenominationRate, RateDelta tables)
3. **Wave 3**: Provider interface and implementations (MamyExchange, SuperRich)
4. **Wave 4**: ExchangeRateService expansion (partial classes)
5. **Wave 5**: UI (ExchangeRateSettings redesign, preview dialog)
6. **Wave 6**: Integration (TillService, DI registration)
7. **Wave 7**: Testing and localization
