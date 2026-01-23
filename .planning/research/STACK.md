# Technology Stack: Cashier Till Multi-Currency Support

**Project:** MotoRent Cashier Till Enhancement
**Researched:** 2026-01-19
**Overall Confidence:** HIGH

## Executive Summary

The MotoRent codebase already has a **well-designed till system** with TillSession, TillTransaction, Receipt entities, and a comprehensive TillService. Multi-currency support is partially implemented via `ReceiptPayment` (with `Currency`, `ExchangeRate`, `AmountInBaseCurrency` fields). The enhancement needed is to **extend the existing till to track cash by currency** and provide **real-time exchange rate management**.

**Key finding:** No external libraries are needed. The existing .NET 10 + Blazor Server + SQL Server stack fully supports all required features.

---

## Recommended Stack

### Core Framework (Already in Place)
| Technology | Version | Purpose | Why |
|------------|---------|---------|-----|
| .NET 10 | Latest | Runtime | Already in use, provides decimal precision for financial calculations |
| Blazor Server | .NET 10 | UI Framework | Already in use, enables real-time UI updates via SignalR |
| SQL Server | 2019+ | Database | Already in use, JSON columns with computed columns pattern |
| System.Text.Json | Built-in | Serialization | Already configured with decimal converters |

### Multi-Currency Support (Extend Existing)
| Technology | Purpose | Rationale |
|------------|---------|-----------|
| `decimal` type | Currency amounts | Native .NET type with 28-29 significant digits, no floating-point errors |
| `SupportedCurrencies` class | Currency codes | Already defined in `ReceiptPayment.cs` with THB, USD, EUR, GBP, CNY, JPY, AUD, RUB |
| Custom `ExchangeRateService` | Rate management | Build using existing repository pattern, no external library needed |

### Real-Time Updates (Already Available)
| Technology | Purpose | Rationale |
|------------|---------|-----------|
| SignalR | Real-time notifications | Already configured (`CommentHub.cs`), extend pattern for till updates |
| Blazor `StateHasChanged()` | UI refresh | Standard pattern used throughout the codebase |

### Receipt Generation (Already Implemented)
| Technology | Purpose | Rationale |
|------------|---------|-----------|
| `ReceiptService` | Receipt management | Already handles multi-currency payments via `ReceiptPayment` |
| HTML/CSS receipts | Print-ready output | `ReceiptDocument.razor` already supports exchange rate display |

---

## What NOT to Use

### External Currency Libraries
| Library | Why NOT |
|---------|---------|
| `NodaMoney` | Overkill for this use case. The codebase already has `decimal` + currency code pattern working |
| `Money.NET` | Adds abstraction layer that conflicts with existing JSON serialization approach |
| `CurrencyConverter.NET` | External API dependency for rates; shop owners should set their own rates |

**Rationale:** The existing `ReceiptPayment` class already implements the exact pattern needed:
```csharp
public decimal Amount { get; set; }
public string Currency { get; set; } = SupportedCurrencies.THB;
public decimal ExchangeRate { get; set; } = 1.0m;
public decimal AmountInBaseCurrency { get; set; }
```
This is simpler and more maintainable than introducing a money library.

### External Exchange Rate APIs
| Service | Why NOT |
|---------|---------|
| Open Exchange Rates | Internet dependency at point of sale is risky |
| Fixer.io | Same concern; also has rate limits |
| XE API | Expensive for small rental shops |

**Rationale:** Tourist rental shops need to set their own **buy rates** that include margin. External APIs provide mid-market rates that aren't useful for cash exchange. Build a simple rate management UI instead.

### Complex Event Sourcing
| Pattern | Why NOT |
|---------|---------|
| Event Sourcing | The existing `TillTransaction` table is already an append-only transaction log |
| CQRS | Unnecessary complexity; the read/write patterns are simple |

---

## Design Patterns to Implement

### 1. Multi-Currency Till Balance Tracking

**Pattern:** Extend `TillSession` to track cash balances per currency.

**Current state:**
```csharp
public decimal TotalCashIn { get; set; }
public decimal TotalCashOut { get; set; }
public decimal ExpectedCash { get; set; }  // THB only
```

**Required extension:**
```csharp
public class CurrencyBalance
{
    public string Currency { get; set; } = SupportedCurrencies.THB;
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal Balance => CashIn - CashOut;
}

// In TillSession:
public List<CurrencyBalance> CurrencyBalances { get; set; } = [];
```

**Why this pattern:**
- Aligns with existing JSON-in-SQL approach
- CurrencyBalances embedded in TillSession JSON
- No schema migration needed for new currencies

### 2. Exchange Rate Service Pattern

**Pattern:** Organization-scoped exchange rates with effective dates.

```csharp
public class ExchangeRate : Entity
{
    public int ExchangeRateId { get; set; }
    public string FromCurrency { get; set; } = string.Empty;
    public string ToCurrency { get; set; } = SupportedCurrencies.THB;
    public decimal BuyRate { get; set; }   // Rate when customer pays in foreign currency
    public decimal SellRate { get; set; }  // Rate when customer receives foreign currency (refunds)
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
    public bool IsActive { get; set; } = true;
}
```

**Why buy/sell rates:**
- Tourist pays USD -> shop uses **buy rate** (favorable to shop)
- Shop refunds in USD -> shop uses **sell rate** (favorable to shop)
- This is how every money changer operates

### 3. Currency Drawer Pattern

**Pattern:** Physical cash drawer slots mapped to currencies.

The till should display:
```
[Drawer Status]
THB: ฿15,000 (base currency)
USD: $200 (@32.50 = ฿6,500)
EUR: €150 (@36.80 = ฿5,520)
---
Total THB Equivalent: ฿27,020
```

**Implementation:**
- Each `TillTransaction` records `Currency` and `Amount`
- `TillSession.CurrencyBalances` maintains running totals
- UI shows drawer by currency with THB equivalents

### 4. Real-Time Balance Updates

**Pattern:** Extend existing SignalR infrastructure.

```csharp
public class TillHub : Hub
{
    public async Task TillUpdated(int tillSessionId, CurrencyBalance[] balances)
    {
        await Clients.Group($"till-{tillSessionId}")
            .SendAsync("OnTillUpdated", balances);
    }
}
```

**Why SignalR is sufficient:**
- Already configured in the project
- Blazor Server already uses SignalR for UI
- No additional infrastructure needed

---

## Data Model Extensions

### TillTransaction Enhancement
Add to existing `TillTransaction.cs`:
```csharp
/// <summary>
/// Currency of the transaction (default: THB)
/// </summary>
public string Currency { get; set; } = SupportedCurrencies.THB;

/// <summary>
/// Exchange rate applied (1.0 for THB)
/// </summary>
public decimal ExchangeRate { get; set; } = 1.0m;

/// <summary>
/// Amount converted to base currency (THB)
/// </summary>
public decimal AmountInBaseCurrency { get; set; }
```

### TillSession Enhancement
Add to existing `TillSession.cs`:
```csharp
/// <summary>
/// Cash balances by currency
/// </summary>
public List<CurrencyBalance> CurrencyBalances { get; set; } = [];

/// <summary>
/// Opening float by currency (for multi-currency drawer)
/// </summary>
public List<CurrencyBalance> OpeningFloatByCurrency { get; set; } = [];

/// <summary>
/// Actual cash counted at close, by currency
/// </summary>
public List<CurrencyBalance> ActualCashByCurrency { get; set; } = [];
```

### New Entity: ExchangeRate
```sql
CREATE TABLE [<schema>].[ExchangeRate]
(
    [ExchangeRateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [FromCurrency] AS CAST(JSON_VALUE([Json], '$.FromCurrency') AS CHAR(3)),
    [ToCurrency] AS CAST(JSON_VALUE([Json], '$.ToCurrency') AS CHAR(3)),
    [BuyRate] AS CAST(JSON_VALUE([Json], '$.BuyRate') AS DECIMAL(18,6)),
    [SellRate] AS CAST(JSON_VALUE([Json], '$.SellRate') AS DECIMAL(18,6)),
    [EffectiveFrom] AS CONVERT(DATE, JSON_VALUE([Json], '$.EffectiveFrom'), 127) PERSISTED,
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_ExchangeRate_Currency ON [<schema>].[ExchangeRate]([FromCurrency], [ToCurrency], [IsActive])
```

---

## Service Layer

### ExchangeRateService
```csharp
public class ExchangeRateService(RentalDataContext context)
{
    /// <summary>
    /// Gets the current buy rate for a currency pair.
    /// </summary>
    public async Task<decimal> GetBuyRateAsync(string fromCurrency, string toCurrency = "THB");

    /// <summary>
    /// Gets the current sell rate for a currency pair.
    /// </summary>
    public async Task<decimal> GetSellRateAsync(string fromCurrency, string toCurrency = "THB");

    /// <summary>
    /// Gets all active exchange rates.
    /// </summary>
    public async Task<List<ExchangeRate>> GetActiveRatesAsync();

    /// <summary>
    /// Sets a new exchange rate (creates new record, deactivates old).
    /// </summary>
    public async Task<SubmitOperation> SetRateAsync(
        string fromCurrency,
        decimal buyRate,
        decimal sellRate,
        string username);
}
```

### TillService Extensions
Add to existing `TillService.cs`:
```csharp
/// <summary>
/// Records a multi-currency cash-in transaction.
/// </summary>
public async Task<SubmitOperation> RecordMultiCurrencyCashInAsync(
    int sessionId,
    TillTransactionType type,
    decimal amount,
    string currency,
    decimal exchangeRate,
    string description,
    string username,
    int? paymentId = null,
    int? rentalId = null);

/// <summary>
/// Gets currency drawer balances for a session.
/// </summary>
public async Task<List<CurrencyBalance>> GetCurrencyBalancesAsync(int sessionId);
```

---

## UI Components to Build

### 1. Exchange Rate Management
`/settings/exchange-rates`
- Table showing current rates by currency
- Edit dialog for rate updates
- History of rate changes

### 2. Multi-Currency Payment Dialog
Extend existing `TillReceivePaymentDialog.razor`:
- Currency selector dropdown
- Amount in selected currency
- Real-time THB equivalent display
- Exchange rate used (from ExchangeRateService)

### 3. Currency Drawer Display
Extend existing `Till.razor`:
- Per-currency balance cards
- THB equivalent totals
- Visual drawer status

### 4. Multi-Currency Close Session
Extend existing `TillCloseSessionDialog.razor`:
- Count cash by currency
- Per-currency variance calculation
- Total variance in THB

---

## Confidence Assessment

| Area | Confidence | Rationale |
|------|------------|-----------|
| No external libraries needed | HIGH | Codebase already has all required patterns |
| Exchange rate entity design | HIGH | Standard forex pattern, aligns with existing Entity base class |
| TillSession extension | HIGH | JSON column approach allows schema evolution without migration |
| SignalR for real-time | HIGH | Already implemented for comments, same pattern applies |
| Receipt multi-currency | HIGH | Already supported in ReceiptPayment class |

---

## Implementation Checklist

**Phase 1: Data Layer**
- [ ] Create `ExchangeRate` entity
- [ ] Add `CurrencyBalance` class
- [ ] Extend `TillTransaction` with currency fields
- [ ] Extend `TillSession` with currency balances
- [ ] Create SQL migration script

**Phase 2: Service Layer**
- [ ] Create `ExchangeRateService`
- [ ] Extend `TillService` with multi-currency methods
- [ ] Update `ReceiptService` to use exchange rates from service

**Phase 3: UI Components**
- [ ] Build exchange rate management page
- [ ] Extend payment dialogs with currency selection
- [ ] Update Till.razor with currency drawer display
- [ ] Extend close session dialog with multi-currency counting

**Phase 4: Integration**
- [ ] Wire up SignalR for real-time balance updates
- [ ] Test end-to-end multi-currency payment flow
- [ ] Test reconciliation with multi-currency

---

## Sources

- Existing codebase analysis:
  - `TillSession.cs`, `TillTransaction.cs`, `TillEnums.cs` - Current till implementation
  - `ReceiptPayment.cs` - Multi-currency payment support already present
  - `SupportedCurrencies` class - Currency codes defined
  - `CommentHub.cs` - SignalR pattern for real-time updates
  - `Organization.cs` - Base currency configuration
  - `SettingKeys.cs` - Payment settings including default currency

- .NET documentation for `decimal` financial calculations (training knowledge, verified by codebase usage patterns)
