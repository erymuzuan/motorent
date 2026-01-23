# Phase 1: Exchange Rate Foundation - Research

**Researched:** 2026-01-20
**Domain:** Exchange rate management for multi-currency tourist rental POS
**Confidence:** HIGH

## Summary

Phase 1 establishes the exchange rate infrastructure that underlies all multi-currency operations in the MotoRent cashier till. The existing codebase provides solid foundations to build on: `ReceiptPayment` already has `Currency`, `ExchangeRate`, and `AmountInBaseCurrency` fields; `SupportedCurrencies` defines THB, USD, EUR, CNY, GBP, JPY, AUD, RUB; and the Setting entity supports organization-scoped key-value storage with caching.

The key deliverable is a new `ExchangeRate` entity with buy rates per currency (shop buys foreign currency from tourists), plus an `ExchangeRateService` to manage rates. The "Forex POS API" mentioned in requirements refers to the organization's existing forex business system - not an external public API. This phase builds the manual rate management capability first; API integration can be added later as a rate source option.

**Primary recommendation:** Create an `ExchangeRate` entity with `Currency`, `BuyRate`, `Source` (Manual/API), and `EffectiveDate`. Use the existing `Setting` pattern for caching. Follow existing service patterns (`TillService`, `ReceiptService`) for the new `ExchangeRateService`.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 10 | 10.x | Runtime | Project standard, supports decimal precision |
| Blazor Server | .NET 10 | UI framework | Existing project architecture |
| System.Text.Json | Built-in | JSON serialization | Already used with polymorphic Entity pattern |
| SQL Server | 2019+ | Database | Existing infrastructure, JSON columns |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IHttpClientFactory | Built-in | HTTP clients | For future Forex POS API integration |
| Microsoft.Extensions.Caching | Built-in | In-memory caching | Rate caching (see `SettingConfigService` pattern) |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Custom Entity | Settings table | Settings work for simple key-value but don't support effective dates, audit trail, or buy/sell rate pairs |
| External exchange rate API | Manual rate entry | Research shows shops set their own rates with margin; external APIs provide mid-market rates unsuitable for cash exchange |
| Real-time rate updates | Daily manual updates | Cash exchange business doesn't need real-time; staff need predictable rates for a shift |

**Installation:**
No new packages required. All dependencies already exist in the solution.

## Architecture Patterns

### Recommended Project Structure
```
src/
├── MotoRent.Domain/
│   └── Entities/
│       └── ExchangeRate.cs           # New entity
├── MotoRent.Services/
│   └── ExchangeRateService.cs        # New service
├── MotoRent.Client/
│   └── Pages/
│       └── Settings/
│           └── ExchangeRateSettings.razor   # Manager rate management
│   └── Components/
│       └── Staff/
│           └── ExchangeRatePanel.razor      # Staff rate display/calculator
database/
└── tables/
    └── MotoRent.ExchangeRate.sql     # New table
```

### Pattern 1: Entity Design (ExchangeRate)
**What:** New entity following existing Entity base class pattern
**When to use:** Any new domain object requiring persistence with audit trail
**Example:**
```csharp
// Source: Existing Entity.cs pattern, ReceiptPayment.cs for currency fields
public class ExchangeRate : Entity
{
    public int ExchangeRateId { get; set; }

    /// <summary>
    /// Currency code (USD, EUR, CNY, etc.) - base is always THB
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.USD;

    /// <summary>
    /// Buy rate: How many THB per 1 unit of foreign currency.
    /// E.g., 35.50 means we give 35.50 THB for 1 USD received.
    /// This is the rate staff use when tourists pay with foreign currency.
    /// </summary>
    public decimal BuyRate { get; set; }

    /// <summary>
    /// Rate source for audit: "Manual", "API", or "Adjusted"
    /// </summary>
    public string Source { get; set; } = ExchangeRateSources.Manual;

    /// <summary>
    /// When this rate becomes effective.
    /// </summary>
    public DateTimeOffset EffectiveDate { get; set; }

    /// <summary>
    /// Optional: When this rate expires (null = valid until replaced)
    /// </summary>
    public DateTimeOffset? ExpiresOn { get; set; }

    /// <summary>
    /// Optional: Reference to API rate if adjusted from API
    /// </summary>
    public decimal? ApiRate { get; set; }

    /// <summary>
    /// Optional: Notes about this rate (e.g., "High season adjustment")
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this rate is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public override int GetId() => ExchangeRateId;
    public override void SetId(int value) => ExchangeRateId = value;
}

public static class ExchangeRateSources
{
    public const string Manual = "Manual";
    public const string API = "API";
    public const string Adjusted = "Adjusted";
}
```

### Pattern 2: Service Pattern (ExchangeRateService)
**What:** Business logic service following existing TillService/ReceiptService patterns
**When to use:** Any domain-specific operations beyond basic CRUD
**Example:**
```csharp
// Source: TillService.cs pattern
public class ExchangeRateService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;

    /// <summary>
    /// Gets the current buy rate for a currency.
    /// Returns null if no active rate exists.
    /// </summary>
    public async Task<ExchangeRate?> GetCurrentRateAsync(string currency)
    {
        var now = DateTimeOffset.Now;
        return await Context.LoadOneAsync<ExchangeRate>(r =>
            r.Currency == currency &&
            r.IsActive &&
            r.EffectiveDate <= now &&
            (r.ExpiresOn == null || r.ExpiresOn > now));
    }

    /// <summary>
    /// Gets current rates for all configured currencies.
    /// </summary>
    public async Task<List<ExchangeRate>> GetAllCurrentRatesAsync()
    {
        var now = DateTimeOffset.Now;
        var query = Context.CreateQuery<ExchangeRate>()
            .Where(r => r.IsActive)
            .Where(r => r.EffectiveDate <= now)
            .OrderBy(r => r.Currency);

        var result = await Context.LoadAsync(query, page: 1, size: 100, includeTotalRows: false);

        // Filter expired rates in memory (complex OR condition)
        return result.ItemCollection
            .Where(r => r.ExpiresOn == null || r.ExpiresOn > now)
            .ToList();
    }

    /// <summary>
    /// Sets a new exchange rate (deactivates previous rate for same currency).
    /// </summary>
    public async Task<SubmitOperation> SetRateAsync(
        string currency,
        decimal buyRate,
        string source,
        string username,
        decimal? apiRate = null,
        string? notes = null)
    {
        // Deactivate current rate for this currency
        var currentRate = await GetCurrentRateAsync(currency);

        var newRate = new ExchangeRate
        {
            Currency = currency,
            BuyRate = buyRate,
            Source = source,
            EffectiveDate = DateTimeOffset.Now,
            ApiRate = apiRate,
            Notes = notes,
            IsActive = true
        };

        using var session = Context.OpenSession(username);

        if (currentRate != null)
        {
            currentRate.IsActive = false;
            currentRate.ExpiresOn = DateTimeOffset.Now;
            session.Attach(currentRate);
        }

        session.Attach(newRate);
        return await session.SubmitChanges("SetExchangeRate");
    }

    /// <summary>
    /// Converts foreign currency amount to THB using current buy rate.
    /// </summary>
    public async Task<(decimal ThbAmount, decimal RateUsed)?> ConvertToThbAsync(
        string currency,
        decimal foreignAmount)
    {
        if (currency == SupportedCurrencies.THB)
            return (foreignAmount, 1.0m);

        var rate = await GetCurrentRateAsync(currency);
        if (rate == null)
            return null;

        var thbAmount = foreignAmount * rate.BuyRate;
        return (thbAmount, rate.BuyRate);
    }
}
```

### Pattern 3: SQL Table with JSON Column
**What:** Standard MotoRent table pattern with computed columns for indexing
**When to use:** All new entities
**Example:**
```sql
-- Source: Existing MotoRent.TillSession.sql pattern
CREATE TABLE [<schema>].[ExchangeRate]
(
    [ExchangeRateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [BuyRate] AS CAST(JSON_VALUE([Json], '$.BuyRate') AS DECIMAL(18,4)),
    [Source] AS CAST(JSON_VALUE([Json], '$.Source') AS NVARCHAR(20)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [EffectiveDate] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.EffectiveDate'), 127) PERSISTED,
    [ExpiresOn] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ExpiresOn'), 127) PERSISTED,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_ExchangeRate_Currency_Active ON [<schema>].[ExchangeRate]([Currency], [IsActive])
CREATE INDEX IX_ExchangeRate_EffectiveDate ON [<schema>].[ExchangeRate]([EffectiveDate])
```

### Pattern 4: Blazor Settings Page
**What:** Manager-facing settings page following OrganizationSettings.razor pattern
**When to use:** Configuration pages requiring tabbed panels and forms
**Example structure:**
```csharp
// Source: OrganizationSettings.razor pattern
@page "/settings/exchange-rates"
@attribute [Authorize(Roles = "OrgAdmin,ShopManager")]
@inherits LocalizedComponentBase<ExchangeRateSettings>
@inject ExchangeRateService ExchangeRateService

// Panel navigation on left (col-lg-3)
// Rate management on right (col-lg-9)
// Table showing: Currency, Current Rate, Source, Last Updated
// Edit button to override rate
// Save button per row or batch save
```

### Pattern 5: Floating Action Panel
**What:** On-demand panel for staff rate viewing during payment
**When to use:** Quick access tools that should be always available
**Example concept:**
```razor
@* Floating button (always visible on till screen) *@
<button class="mr-rate-fab" @onclick="ToggleRatePanel">
    <i class="ti ti-currency-dollar"></i>
</button>

@* Slide-out panel with rates and calculator *@
@if (m_showRatePanel)
{
    <div class="mr-rate-panel">
        <div class="mr-rate-panel-header">
            <h4>@Localizer["ExchangeRates"]</h4>
            <button @onclick="ToggleRatePanel"><i class="ti ti-x"></i></button>
        </div>
        <div class="mr-rate-panel-body">
            @foreach (var rate in m_rates)
            {
                <div class="mr-rate-row">
                    <span class="mr-rate-currency">@rate.Currency</span>
                    <span class="mr-rate-value">@rate.BuyRate.ToString("N2")</span>
                </div>
            }
        </div>
        <div class="mr-rate-calculator">
            <input type="number" @bind="m_calcAmount" placeholder="Amount" />
            <select @bind="m_calcCurrency">
                @foreach (var c in SupportedCurrencies.All.Where(c => c != "THB"))
                {
                    <option value="@c">@c</option>
                }
            </select>
            <div class="mr-calc-result">
                = @(m_calcAmount * GetRate(m_calcCurrency)):N0 THB
            </div>
        </div>
    </div>
}
```

### Anti-Patterns to Avoid
- **Fetching rates on every transaction:** Cache rates at service level or use in-memory dictionary. Rates change daily, not per-transaction.
- **Storing sell rates when only buy rates needed:** Tourist shops only BUY foreign currency. Don't add unnecessary complexity.
- **Using decimal(18,2) for rates:** Rates like 0.0285 for CNY/THB need more precision. Use decimal(18,4) minimum.
- **Making rates organization-wide when shops may differ:** Current design assumes organization-level rates. If shops need different rates, add ShopId later.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Currency constants | String literals | `SupportedCurrencies` class | Already defined in `ReceiptPayment.cs` with THB, USD, EUR, etc. |
| Rate caching | Custom dictionary | Follow `SettingConfigService` pattern | Existing pattern handles tenant-scoped caching with expiration |
| JSON serialization | Manual JsonSerializer | Entity base class | Polymorphic serialization already configured in `Entity.cs` |
| Audit trail | Custom timestamp tracking | Entity audit fields | `CreatedBy`, `ChangedBy`, timestamps handled by repository |
| HTTP client creation | `new HttpClient()` | `IHttpClientFactory` | Already used in `DocumentOcrService.cs` for Gemini API |

**Key insight:** The codebase already has mature patterns for everything this phase needs. Follow existing conventions rather than inventing new approaches.

## Common Pitfalls

### Pitfall 1: Rate Precision Issues
**What goes wrong:** Storing rates as decimal(18,2) loses precision for currencies with low THB values (e.g., CNY ~5.0, JPY ~0.25)
**Why it happens:** Database columns default to 2 decimal places from money context
**How to avoid:** Use decimal(18,4) or decimal(18,6) for rate storage
**Warning signs:** Rounding errors accumulate on large transactions

### Pitfall 2: Race Condition on Rate Update
**What goes wrong:** Two managers update rate simultaneously, one overwrites the other
**Why it happens:** Read-modify-write pattern without locking
**How to avoid:** Use `IsActive` flag pattern - create new rate, deactivate old one atomically
**Warning signs:** Rate history shows gaps or overlaps

### Pitfall 3: Timezone Confusion on EffectiveDate
**What goes wrong:** Rate set in morning not effective because server timezone differs from shop timezone
**Why it happens:** Using `DateTime` instead of `DateTimeOffset`, or comparing without timezone context
**How to avoid:** Use `DateTimeOffset` consistently (existing pattern in codebase)
**Warning signs:** Rates "not working" for staff in morning, works after lunch

### Pitfall 4: Missing Rate for Currency
**What goes wrong:** Staff tries to accept USD but no USD rate configured
**Why it happens:** Initial setup incomplete, or rate expired
**How to avoid:**
- Show clear error message: "No exchange rate configured for USD. Contact manager."
- Manager settings page shows warning for missing currency rates
- Consider seed data for common currencies with placeholder rates
**Warning signs:** Null reference exceptions in payment flow

### Pitfall 5: API vs Manual Rate Source Confusion
**What goes wrong:** Manager overrides API rate, forgets, later complains "API rate is wrong"
**Why it happens:** No clear indicator of rate source
**How to avoid:**
- Always show source badge: "Manual" / "API" / "Adjusted"
- When displaying adjusted rate, show both manual rate and original API rate
- Log rate source changes with timestamp and user
**Warning signs:** Support tickets about "wrong rates"

## Code Examples

Verified patterns from official sources:

### Registering the Entity (Repository Pattern)
```csharp
// Source: Program.cs / ServicesExtensions pattern
// Add to DI registration
services.AddSingleton<IRepository<ExchangeRate>, Repository<ExchangeRate>>();
```

### Adding to Entity Polymorphism
```csharp
// Source: Entity.cs - add JsonDerivedType attribute
[JsonDerivedType(typeof(ExchangeRate), nameof(ExchangeRate))]
public abstract class Entity { ... }
```

### Loading with Complex Query
```csharp
// Source: TillService.cs patterns
var query = context.CreateQuery<ExchangeRate>()
    .Where(r => r.Currency == currency)
    .Where(r => r.IsActive)
    .Where(r => r.EffectiveDate <= DateTimeOffset.Now)
    .OrderByDescending(r => r.EffectiveDate);

var result = await context.LoadAsync(query, page: 1, size: 1, includeTotalRows: false);
return result.ItemCollection.FirstOrDefault();
```

### Settings Page Panel Pattern
```csharp
// Source: OrganizationSettings.razor
<div class="row">
    <div class="col-lg-3 col-md-4">
        <div class="card sticky-top" style="top: 80px;">
            <div class="list-group list-group-flush">
                <a href="/settings/exchange-rates" class="list-group-item list-group-item-action active">
                    <i class="ti ti-coin me-2"></i>@Localizer["ExchangeRates"]
                </a>
            </div>
        </div>
    </div>
    <div class="col-lg-9 col-md-8">
        @* Rate management content *@
    </div>
</div>
```

### Service Injection Pattern
```csharp
// Source: TillService.cs, ReceiptService.cs
public class ExchangeRateService(RentalDataContext context)
{
    private RentalDataContext Context { get; } = context;
    // ... methods
}

// Registration in Program.cs or ServicesExtensions.cs
services.AddScoped<ExchangeRateService>();
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Settings table for rates | Dedicated ExchangeRate entity | This phase | Enables rate history, source tracking, effective dates |
| Single rate per currency | Buy/Sell rate pair | This phase | Supports future sell rate if needed (buy-only for now) |

**Deprecated/outdated:**
- Using Settings for exchange rates - doesn't support audit trail, effective dates, or rate source tracking

## Open Questions

Things that couldn't be fully resolved:

1. **Forex POS API Details**
   - What we know: The organization has an existing forex POS system that could provide rates
   - What's unclear: API endpoint, authentication method, response format
   - Recommendation: Build manual rate entry first (RATE-02 before RATE-01). Add API fetch as enhancement when details available.

2. **Rate Expiration Policy**
   - What we know: Rates should be replaceable; shops may want to schedule future rates
   - What's unclear: Should rates auto-expire daily? Or remain valid until replaced?
   - Recommendation: Default to "valid until replaced" with optional ExpiresOn. Simpler for operators.

3. **Multi-Shop Rate Variance**
   - What we know: Current design is organization-scoped
   - What's unclear: Do different shops within an organization need different rates?
   - Recommendation: Start with organization-level. Add ShopId to ExchangeRate entity later if needed.

## Sources

### Primary (HIGH confidence)
- Existing codebase: `Entity.cs`, `ReceiptPayment.cs`, `TillService.cs`, `ReceiptService.cs`, `OrganizationSettings.razor`
- Existing codebase: `SupportedCurrencies` class (defines THB, USD, EUR, GBP, CNY, JPY, AUD, RUB)
- Existing codebase: `Setting.cs`, `SettingConfigService.cs` (caching pattern)
- Existing codebase: SQL table patterns (`MotoRent.TillSession.sql`, `MotoRent.TillTransaction.sql`)

### Secondary (MEDIUM confidence)
- `.planning/research/SUMMARY.md` - Research synthesis confirming approach
- `.planning/research/STACK.md` - Technology decision rationale
- `.planning/research/PITFALLS.md` - Common failure modes

### Tertiary (LOW confidence)
- "Forex POS API" - Mentioned in requirements but no API documentation found. Treat as future enhancement.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All patterns exist in codebase, no new dependencies
- Architecture: HIGH - Follows established Entity/Service/Repository patterns
- Pitfalls: HIGH - Based on existing codebase patterns and research documentation

**Research date:** 2026-01-20
**Valid until:** 60 days (stable patterns, no external dependencies)
