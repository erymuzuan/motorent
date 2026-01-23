# Phase 2: Multi-Currency Till Operations - Research

**Researched:** 2026-01-20
**Domain:** Multi-currency cash handling in POS till system
**Confidence:** HIGH

## Summary

Phase 2 extends the existing till system to support per-currency balance tracking. The current implementation tracks all amounts in THB only. This phase adds currency-specific tracking so staff can see how much USD, EUR, and CNY they have in the drawer, while still converting everything to THB for reconciliation purposes.

The existing architecture is well-suited for this extension. `TillSession` already has denormalized totals that get updated atomically with each transaction. `TillTransaction` can be extended with currency and exchange rate fields. The `ExchangeRateService` from Phase 1 provides conversion functions. The key challenge is maintaining backward compatibility - THB-only sessions must continue to work.

**Primary recommendation:** Extend `TillSession` with a `Dictionary<string, decimal>` for per-currency balances. Extend `TillTransaction` with `Currency`, `ExchangeRate`, `AmountInBaseCurrency` fields. Create new UI components for denomination entry and currency balance display. Follow the existing patterns from Phase 1 (ExchangeRate entity) and the current till implementation.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET 10 | 10.x | Runtime | Project standard |
| Blazor Server | .NET 10 | UI framework | Existing project architecture |
| System.Text.Json | Built-in | JSON serialization | Already used with polymorphic Entity pattern |
| SQL Server | 2019+ | Database | Existing infrastructure, JSON columns |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| ExchangeRateService | Phase 1 | Currency conversion | All foreign currency transactions |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Dictionary for balances | Separate CurrencyBalance entity | Dictionary is simpler, embedded in JSON column; separate entity adds complexity without benefit |
| Denomination tracking in Phase 2 | Defer to Phase 3 | Context decision - denomination entry is UI-only for Phase 2, actual counting/verification is Phase 3 |

**Installation:**
No new packages required. All dependencies exist in the solution.

## Architecture Patterns

### Recommended Project Structure
```
src/
├── MotoRent.Domain/
│   └── Entities/
│       ├── TillSession.cs           # Extend with CurrencyBalances
│       ├── TillTransaction.cs       # Extend with Currency, ExchangeRate fields
│       └── CurrencyDenominations.cs # NEW - denomination definitions
├── MotoRent.Services/
│   └── TillService.cs               # Extend methods for multi-currency
├── MotoRent.Client/
│   └── Pages/Staff/
│       ├── TillOpenSessionDialog.razor    # Already THB-only (no change)
│       ├── TillReceivePaymentDialog.razor # Extend with currency selection
│       ├── TillCashDropDialog.razor       # Extend for multi-currency drops
│       └── Till.razor                     # Add currency balance display
│   └── Components/Till/
│       ├── CurrencyBalancePanel.razor     # NEW - collapsible balance display
│       └── DenominationEntryPanel.razor   # NEW - denomination input
database/
└── tables/
    └── (no new tables - extend existing TillSession, TillTransaction)
```

### Pattern 1: Extending TillSession for Multi-Currency
**What:** Add per-currency balance tracking to existing entity
**When to use:** Session needs to track amounts in multiple currencies
**Example:**
```csharp
// Source: Existing TillSession.cs pattern
public class TillSession : Entity
{
    // ... existing properties ...

    /// <summary>
    /// Opening float amount in THB (base currency only).
    /// </summary>
    public decimal OpeningFloat { get; set; }  // THB only, unchanged

    /// <summary>
    /// Current balance per currency. Key is currency code (THB, USD, EUR, CNY).
    /// Updated atomically with each transaction.
    /// </summary>
    public Dictionary<string, decimal> CurrencyBalances { get; set; } = new()
    {
        [SupportedCurrencies.THB] = 0m  // Initialize with THB
    };

    /// <summary>
    /// Total value in THB (opening float + all currency balances converted).
    /// </summary>
    public decimal ExpectedCashInThb => OpeningFloat + TotalCashIn - TotalCashOut - TotalDropped + TotalToppedUp;

    /// <summary>
    /// Gets the balance for a specific currency. Returns 0 if currency not tracked.
    /// </summary>
    public decimal GetCurrencyBalance(string currency)
        => CurrencyBalances.TryGetValue(currency, out var balance) ? balance : 0m;
}
```

### Pattern 2: Extending TillTransaction for Currency Tracking
**What:** Add currency and exchange rate fields to transaction entity
**When to use:** Every transaction that involves foreign currency
**Example:**
```csharp
// Source: Existing TillTransaction.cs pattern
public class TillTransaction : Entity
{
    // ... existing properties ...

    /// <summary>
    /// Currency code of the transaction (THB, USD, EUR, CNY).
    /// Default is THB for backward compatibility.
    /// </summary>
    public string Currency { get; set; } = SupportedCurrencies.THB;

    /// <summary>
    /// Exchange rate used (THB per 1 unit of foreign currency).
    /// 1.0 for THB transactions.
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1.0m;

    /// <summary>
    /// Amount converted to base currency (THB).
    /// For THB transactions, equals Amount.
    /// </summary>
    public decimal AmountInBaseCurrency { get; set; }

    /// <summary>
    /// Source of the exchange rate (Manual, API, Adjusted, Base).
    /// </summary>
    public string? ExchangeRateSource { get; set; }

    /// <summary>
    /// Reference to the ExchangeRate entity used (null for THB).
    /// </summary>
    public int? ExchangeRateId { get; set; }
}
```

### Pattern 3: Currency Denomination Definitions
**What:** Define bill and coin denominations for each supported currency
**When to use:** Denomination entry UI needs to know what denominations exist
**Example:**
```csharp
// Source: New file, follows existing pattern
public static class CurrencyDenominations
{
    public static readonly decimal[] THB = [1000, 500, 100, 50, 20, 10, 5, 2, 1];
    public static readonly decimal[] USD = [100, 50, 20, 10, 5, 1];
    public static readonly decimal[] EUR = [500, 200, 100, 50, 20, 10, 5];
    public static readonly decimal[] CNY = [100, 50, 20, 10, 5, 1];

    public static decimal[] GetDenominations(string currency) => currency switch
    {
        SupportedCurrencies.THB => THB,
        SupportedCurrencies.USD => USD,
        SupportedCurrencies.EUR => EUR,
        SupportedCurrencies.CNY => CNY,
        _ => []
    };
}
```

### Pattern 4: Multi-Currency Payment Recording
**What:** Service method that records payment in foreign currency
**When to use:** Staff accepts foreign currency payment
**Example:**
```csharp
// Source: TillService.cs pattern extension
public async Task<SubmitOperation> RecordForeignCurrencyPaymentAsync(
    int sessionId,
    TillTransactionType type,
    string currency,
    decimal foreignAmount,
    string description,
    string username,
    int? rentalId = null,
    string? notes = null)
{
    var session = await GetSessionByIdAsync(sessionId);
    if (session == null)
        return SubmitOperation.CreateFailure("Session not found");

    // Get current exchange rate
    var conversion = await m_exchangeRateService.ConvertToThbAsync(currency, foreignAmount);
    if (conversion == null)
        return SubmitOperation.CreateFailure($"No exchange rate configured for {currency}");

    var transaction = new TillTransaction
    {
        TillSessionId = sessionId,
        TransactionType = type,
        Direction = TillTransactionDirection.In,
        Amount = foreignAmount,
        Currency = currency,
        ExchangeRate = conversion.RateUsed,
        AmountInBaseCurrency = conversion.ThbAmount,
        ExchangeRateSource = conversion.RateSource,
        ExchangeRateId = conversion.ExchangeRateId,
        Description = description,
        RentalId = rentalId,
        TransactionTime = DateTimeOffset.Now,
        RecordedByUserName = username,
        Notes = notes
    };

    // Update session totals (always in THB for reconciliation)
    if (transaction.AffectsCash)
        session.TotalCashIn += conversion.ThbAmount;

    // Update per-currency balance
    if (!session.CurrencyBalances.ContainsKey(currency))
        session.CurrencyBalances[currency] = 0m;
    session.CurrencyBalances[currency] += foreignAmount;

    using var persistenceSession = Context.OpenSession(username);
    persistenceSession.Attach(transaction);
    persistenceSession.Attach(session);
    return await persistenceSession.SubmitChanges("RecordForeignCurrencyPayment");
}
```

### Pattern 5: Multi-Currency Cash Drop
**What:** Service method that records cash drop with multiple currencies
**When to use:** Staff drops multiple currencies to safe in one operation
**Example:**
```csharp
// Source: TillService.cs pattern extension
public class CurrencyDropAmount
{
    public string Currency { get; set; } = SupportedCurrencies.THB;
    public decimal Amount { get; set; }
}

public async Task<SubmitOperation> RecordMultiCurrencyDropAsync(
    int sessionId,
    List<CurrencyDropAmount> drops,
    string username,
    string? notes = null)
{
    var session = await GetSessionByIdAsync(sessionId);
    if (session == null)
        return SubmitOperation.CreateFailure("Session not found");

    using var persistenceSession = Context.OpenSession(username);
    decimal totalThbDropped = 0m;

    foreach (var drop in drops.Where(d => d.Amount > 0))
    {
        // Validate sufficient balance
        var currentBalance = session.GetCurrencyBalance(drop.Currency);
        if (drop.Amount > currentBalance)
            return SubmitOperation.CreateFailure($"Insufficient {drop.Currency} balance");

        // Get THB equivalent for non-THB currencies
        decimal thbAmount = drop.Amount;
        decimal rate = 1.0m;
        string rateSource = "Base";
        int? rateId = null;

        if (drop.Currency != SupportedCurrencies.THB)
        {
            var conversion = await m_exchangeRateService.ConvertToThbAsync(drop.Currency, drop.Amount);
            if (conversion == null)
                return SubmitOperation.CreateFailure($"No exchange rate for {drop.Currency}");
            thbAmount = conversion.ThbAmount;
            rate = conversion.RateUsed;
            rateSource = conversion.RateSource;
            rateId = conversion.ExchangeRateId;
        }

        var transaction = new TillTransaction
        {
            TillSessionId = sessionId,
            TransactionType = TillTransactionType.Drop,
            Direction = TillTransactionDirection.Out,
            Amount = drop.Amount,
            Currency = drop.Currency,
            ExchangeRate = rate,
            AmountInBaseCurrency = thbAmount,
            ExchangeRateSource = rateSource,
            ExchangeRateId = rateId,
            Description = $"Cash drop to safe - {drop.Currency}",
            TransactionTime = DateTimeOffset.Now,
            RecordedByUserName = username,
            Notes = notes
        };

        // Update currency balance
        session.CurrencyBalances[drop.Currency] -= drop.Amount;
        totalThbDropped += thbAmount;

        persistenceSession.Attach(transaction);
    }

    // Update session total dropped (in THB)
    session.TotalDropped += totalThbDropped;
    persistenceSession.Attach(session);

    return await persistenceSession.SubmitChanges("RecordMultiCurrencyDrop");
}
```

### Anti-Patterns to Avoid
- **Separate CurrencyBalance table:** Adding a separate entity for balances creates complexity without benefit. Use embedded Dictionary in JSON.
- **Converting all amounts to THB immediately:** Lose visibility of actual foreign currency in drawer. Track both original currency and THB equivalent.
- **Overwriting existing totals:** Existing TotalCashIn/TotalCashOut must remain in THB for backward compatibility and reconciliation.
- **Breaking existing THB-only flow:** All existing functionality must continue to work. New currency fields default to THB.

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Currency conversion | Manual multiplication | `ExchangeRateService.ConvertToThbAsync()` | Already handles rate lookup, source tracking, audit trail |
| Currency codes | String literals | `SupportedCurrencies` class | Already defined in `ReceiptPayment.cs` |
| Transaction recording | Custom insert logic | Extend existing `RecordCashInAsync` pattern | Maintains atomic session total updates |
| Rate source tracking | Custom string | `ExchangeConversionResult` from Phase 1 | Already returns RateSource, RateId |

**Key insight:** Phase 1 established the conversion infrastructure. Phase 2 uses it - don't re-implement.

## Common Pitfalls

### Pitfall 1: Breaking Backward Compatibility
**What goes wrong:** Existing THB-only sessions fail to load or display incorrectly
**Why it happens:** New required fields without defaults
**How to avoid:**
- All new fields have sensible defaults (Currency = "THB", ExchangeRate = 1.0)
- Dictionary initializes with THB key
- Existing ExpectedCash calculation unchanged
**Warning signs:** Errors loading historical sessions

### Pitfall 2: Race Condition on Balance Update
**What goes wrong:** Concurrent transactions cause incorrect balance
**Why it happens:** Read-modify-write without atomic update
**How to avoid:**
- Update session totals in same database transaction as TillTransaction
- Follow existing pattern: load session, modify, attach both, single SubmitChanges
**Warning signs:** Balance doesn't match sum of transactions

### Pitfall 3: Stale Exchange Rate During Payment
**What goes wrong:** Rate changes between display and confirmation
**Why it happens:** Rate fetched on page load, not at commit time
**How to avoid:**
- Fetch rate at commit time, not page load
- Display rate in confirmation, allow staff to proceed or refresh
- Store the exact rate used on transaction for audit
**Warning signs:** Displayed change amount differs from recorded amount

### Pitfall 4: Insufficient Currency Balance for Drop
**What goes wrong:** Staff drops more than they have
**Why it happens:** No validation against per-currency balance
**How to avoid:**
- Validate drop amount against `session.GetCurrencyBalance(currency)`
- Return clear error: "Insufficient USD balance"
- UI shows available balance for each currency
**Warning signs:** Negative currency balances

### Pitfall 5: Inconsistent Denomination Entry
**What goes wrong:** Different UX for opening, payments, drops, closing
**Why it happens:** Building each flow independently
**How to avoid:**
- Create reusable `DenominationEntryPanel` component
- Use same component in all flows with different currencies enabled
- Consistent layout: denomination values on left, count input on right, total below
**Warning signs:** Staff confused by different layouts

## Code Examples

Verified patterns from official sources:

### Extending Entity with Dictionary Property
```csharp
// Source: Existing Entity.cs, JSON serialization pattern
// Dictionary<string, decimal> serializes naturally with System.Text.Json
public Dictionary<string, decimal> CurrencyBalances { get; set; } = new()
{
    [SupportedCurrencies.THB] = 0m
};
```

### Currency Selection Button Row
```razor
@* Source: Context decision - currency buttons for mobile-first UI *@
<div class="d-flex gap-2 mb-3">
    @foreach (var currency in new[] { "THB", "USD", "EUR", "CNY" })
    {
        <button type="button"
                class="btn @(m_selectedCurrency == currency ? "btn-primary" : "btn-outline-secondary")"
                @onclick="@(() => SelectCurrency(currency))">
            @currency
        </button>
    }
</div>
```

### Collapsible Balance Panel
```razor
@* Source: Context decision - collapsed by default, show THB total *@
<div class="mr-balance-panel">
    <button type="button" class="mr-balance-header" @onclick="ToggleExpanded">
        <span>@Localizer["TillBalance"]</span>
        <span class="ms-auto fw-semibold">@TotalThbEquivalent.ToString("N0") THB</span>
        <i class="ti ti-chevron-@(m_expanded ? "up" : "down") ms-2"></i>
    </button>
    @if (m_expanded)
    {
        <div class="mr-balance-details">
            @foreach (var kvp in CurrencyBalances.Where(b => b.Value > 0))
            {
                <div class="mr-balance-row">
                    <span>@kvp.Key</span>
                    <span>@kvp.Value.ToString("N2")</span>
                    @if (kvp.Key != SupportedCurrencies.THB)
                    {
                        <span class="text-muted small">
                            (@ConvertToThb(kvp.Key, kvp.Value).ToString("N0") THB)
                        </span>
                    }
                </div>
            }
        </div>
    }
</div>
```

### Change Display Breakdown
```razor
@* Source: Context decision - full breakdown for foreign currency payments *@
@if (m_currency != SupportedCurrencies.THB && m_amountReceived > 0)
{
    <div class="mr-change-breakdown">
        <div class="mr-change-row">
            <span>@Localizer["AmountReceived"]</span>
            <span>@m_amountReceived.ToString("N2") @m_currency</span>
        </div>
        <div class="mr-change-row">
            <span>@Localizer["ExchangeRate"]</span>
            <span>1 @m_currency = @m_exchangeRate.ToString("N2") THB</span>
        </div>
        <div class="mr-change-row">
            <span>@Localizer["ThbEquivalent"]</span>
            <span>@m_thbEquivalent.ToString("N0") THB</span>
        </div>
        <hr />
        <div class="mr-change-row fw-bold">
            <span>@Localizer["ChangeToGive"]</span>
            <span class="text-success">@m_changeAmount.ToString("N0") THB</span>
        </div>
    </div>
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| THB-only session totals | Per-currency balances | Phase 2 | Staff can see actual foreign currency in drawer |
| No exchange rate on transaction | Currency, ExchangeRate, AmountInBaseCurrency | Phase 2 | Full audit trail for foreign currency payments |
| Simple amount entry | Denomination-based entry | Phase 2 | Reduces counting errors, consistent UX |

**Deprecated/outdated:**
- None - this is an extension, not a replacement

## Open Questions

Things that couldn't be fully resolved:

1. **Denomination entry performance**
   - What we know: Staff enters count for each denomination
   - What's unclear: Should total update on every keystroke or on blur?
   - Recommendation: Update on input (oninput) for real-time feedback, debounce if performance issues

2. **Balance display refresh timing**
   - What we know: Real-time updates required (per context decision)
   - What's unclear: SignalR push vs polling interval
   - Recommendation: Update after each transaction completes (no polling needed if same session)

3. **Handling very large denomination counts**
   - What we know: Tourist shops rarely handle large volumes
   - What's unclear: Maximum practical count per denomination
   - Recommendation: No hard limit, validate against reasonable session totals

## Sources

### Primary (HIGH confidence)
- Existing codebase: `TillSession.cs`, `TillTransaction.cs`, `TillService.cs`
- Existing codebase: `ExchangeRate.cs`, `ExchangeRateService.cs` (Phase 1)
- Existing codebase: `ReceiptPayment.cs` with Currency, ExchangeRate fields
- Phase 1 research: `.planning/phases/01-exchange-rate-foundation/01-RESEARCH.md`
- Context document: `.planning/phases/02-multi-currency-till-operations/02-CONTEXT.md`

### Secondary (MEDIUM confidence)
- Architecture research: `.planning/research/ARCHITECTURE.md` (existing patterns)

### Tertiary (LOW confidence)
- None - all patterns verified against existing codebase

## Metadata

**Confidence breakdown:**
- Entity extension: HIGH - follows existing patterns exactly
- Service extension: HIGH - existing RecordCashInAsync pattern
- UI patterns: HIGH - decisions documented in CONTEXT.md
- Denomination definitions: MEDIUM - reasonable values but may need adjustment

**Research date:** 2026-01-20
**Valid until:** 60 days (stable patterns, no external dependencies)
