# Phase 9: End of Day Operations - Research

**Researched:** 2026-01-21
**Domain:** End-of-day reconciliation, daily close, receipt search/reprint
**Confidence:** HIGH

## Summary

Phase 9 completes the cashier till system by adding end-of-day (EOD) operations. The existing codebase provides substantial infrastructure that can be extended rather than built from scratch:

1. **TillService** already has manager verification methods (`VerifySessionAsync`, `GetDailySummaryAsync`, `GetSessionsForVerificationAsync`) which provide a foundation for cash drop verification.

2. **Receipt entity and ReceiptService** are fully implemented with search, generation, and reprint capabilities. The existing `/finance/receipts` page provides a manager-focused view; staff need a simplified search/reprint interface.

3. **DailyClose and ShortageLog** are new entities that must be created to track day-level locking and variance accountability.

**Primary recommendation:** Extend existing TillService and ReceiptService rather than building parallel infrastructure. Add DailyClose and ShortageLog entities for daily state management and variance tracking.

## Standard Stack

The established libraries/tools for this domain:

### Core (Already in Use)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| MudBlazor | 6.x | UI components | Project standard, already configured |
| Tabler CSS | 1.x | Layout, styling | Project standard for non-MudBlazor components |
| System.Text.Json | .NET 10 | JSON serialization | Project standard, polymorphism configured |

### Supporting (Already in Use)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IJSRuntime | .NET 10 | Browser print() | HandoverReportDialog pattern for print |
| IModalService | Blazored.Modal | Dialogs | All existing dialogs use this |
| ILocalizer | .NET | Localization | All components inherit LocalizedComponentBase |

### New Patterns (Project-Specific)
| Pattern | Source | Purpose |
|---------|--------|---------|
| `LocalizedDialogBase<TEntity, TComponent>` | Project | Dialog with entity binding |
| `RequireTenantManager` policy | Program.cs | Authorization for manager pages |
| `SubmitOperation` | Domain | Async operation result pattern |

**Installation:** No new packages required.

## Architecture Patterns

### Recommended Project Structure

```
src/MotoRent.Client/Pages/Manager/
    DailyClose.razor             # Daily close page (EOD-02)
    CashDropVerificationDialog.razor  # Per-session drop verification (EOD-01)
    DailySummaryReportDialog.razor    # Printable daily summary (EOD-03)
    ShortageLogDialog.razor      # Variance log entry dialog
    StaffShortagesTable.razor    # Staff shortage history view

src/MotoRent.Client/Pages/Staff/
    TillTransactionSearch.razor  # Receipt search for staff (RCPT-02)
    (TillReceiptsDialog.razor already exists for session-based receipt view)

src/MotoRent.Domain/Entities/
    DailyClose.cs               # New entity for day-level close state
    ShortageLog.cs              # New entity for variance accountability

src/MotoRent.Services/
    TillService.eod.cs          # New partial file for EOD methods
    (ReceiptService.cs already handles receipt search/reprint)

database/tables/
    MotoRent.DailyClose.sql     # New table
    MotoRent.ShortageLog.sql    # New table
```

### Pattern 1: Daily Close Entity Design

**What:** DailyClose tracks whether a day is closed and records the manager who performed the close.

**When to use:** For EOD-02 (daily sales close) and EOD-04 (preventing transactions on closed days).

**Example:**
```csharp
// Source: Project pattern from TillSession.cs
public class DailyClose : Entity
{
    public int DailyCloseId { get; set; }
    public int ShopId { get; set; }
    public DateTime Date { get; set; } // Date only, no time
    public DailyCloseStatus Status { get; set; } = DailyCloseStatus.Open;

    // Close details
    public DateTimeOffset? ClosedAt { get; set; }
    public string? ClosedByUserName { get; set; }

    // Summary totals (denormalized for quick access)
    public decimal TotalCashIn { get; set; }
    public decimal TotalCashOut { get; set; }
    public decimal TotalDropped { get; set; }
    public decimal TotalVariance { get; set; }
    public decimal TotalElectronicPayments { get; set; }
    public int SessionCount { get; set; }
    public int SessionsWithVariance { get; set; }

    // Re-open tracking (soft block with override)
    public bool WasReopened { get; set; }
    public string? ReopenReason { get; set; }
    public DateTimeOffset? ReopenedAt { get; set; }
    public string? ReopenedByUserName { get; set; }

    public override int GetId() => DailyCloseId;
    public override void SetId(int value) => DailyCloseId = value;
}

public enum DailyCloseStatus
{
    Open,       // Day is open, transactions allowed
    Closed,     // Day is closed, transactions blocked (soft)
    Reconciled  // Day is fully reconciled and verified
}
```

### Pattern 2: Shortage Log Entity Design

**What:** ShortageLog records variance entries posted during daily close, creating an accountability trail.

**When to use:** When a session has variance and manager chooses to post it as shortage.

**Example:**
```csharp
// Source: Project pattern from TillTransaction.cs
public class ShortageLog : Entity
{
    public int ShortageLogId { get; set; }
    public int ShopId { get; set; }
    public int TillSessionId { get; set; }
    public int? DailyCloseId { get; set; } // Links to the daily close operation

    // Staff accountability
    public string StaffUserName { get; set; } = string.Empty;
    public string StaffDisplayName { get; set; } = string.Empty;

    // Variance details - per currency
    public string Currency { get; set; } = SupportedCurrencies.THB;
    public decimal Amount { get; set; }
    public decimal AmountInThb { get; set; } // Converted at time of logging

    // Manager explanation
    public string Reason { get; set; } = string.Empty;
    public string LoggedByUserName { get; set; } = string.Empty;
    public DateTimeOffset LoggedAt { get; set; }

    public override int GetId() => ShortageLogId;
    public override void SetId(int value) => ShortageLogId = value;
}
```

### Pattern 3: Cash Drop Verification Per Session

**What:** Verification compares session drops (by currency) against manager's safe count.

**When to use:** EOD-01 when manager verifies drops from each till session.

**Example:**
```csharp
// TillService.eod.cs - New method
public async Task<List<TillTransaction>> GetDropsForSessionAsync(int sessionId)
{
    var result = await Context.LoadAsync(
        Context.CreateQuery<TillTransaction>()
            .Where(t => t.TillSessionId == sessionId)
            .Where(t => t.TransactionType == TillTransactionType.Drop)
            .Where(t => !t.IsVoided)
            .OrderBy(t => t.TransactionTime),
        page: 1, size: 1000, includeTotalRows: false);
    return result.ItemCollection.ToList();
}

// Returns drops grouped by currency
public async Task<Dictionary<string, decimal>> GetDropTotalsByCurrencyAsync(int sessionId)
{
    var drops = await GetDropsForSessionAsync(sessionId);
    return drops
        .GroupBy(d => d.Currency)
        .ToDictionary(g => g.Key, g => g.Sum(d => d.Amount));
}
```

### Pattern 4: Daily Summary Report (Browser Print)

**What:** Extends HandoverReportDialog pattern to create a daily summary printable via browser print.

**When to use:** EOD-03 for daily summary report generation.

**Example:**
```razor
@* Source: HandoverReportDialog.razor pattern *@
@inject IJSRuntime JSRuntime

@* Screen preview (hidden when printing) *@
<div class="modal-body p-0 d-print-none" style="max-height: 70vh; overflow-y: auto;">
    @RenderDailySummary()
</div>

@* Print-only version *@
<div class="d-none d-print-block p-4">
    @RenderDailySummary()
</div>

<div class="modal-footer d-print-none">
    <button type="button" class="btn btn-ghost-secondary" @onclick="Cancel">
        @CommonLocalizer["Close"]
    </button>
    <button type="button" class="btn btn-primary" @onclick="PrintAsync">
        <i class="ti ti-printer me-1"></i>
        @Localizer["Print"]
    </button>
</div>

@code {
    private async Task PrintAsync()
    {
        await JSRuntime.InvokeVoidAsync("print");
    }
}
```

### Anti-Patterns to Avoid

- **Creating separate ReceiptSearch service:** ReceiptService already has `GetReceiptsAsync` with full search capability
- **Building custom print infrastructure:** Browser print with CSS `@media print` is the established pattern
- **Blocking daily close on open sessions:** Decision specifies soft block with override, not hard block
- **Aggregating drops by staff:** Decision specifies per-session verification, not per-staff aggregate

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Receipt search | Custom search endpoint | `ReceiptService.GetReceiptsAsync()` | Already supports date range, type, status, customer, amount |
| Receipt reprint | Custom reprint flow | `ReceiptPrintDialog` with `IsReprint=true` | Already increments `ReprintCount` |
| Session summary | Custom aggregation | `TillService.GetDailySummaryAsync()` | Already calculates all totals |
| Manager auth check | Custom role check | `[Authorize(Policy = "RequireTenantManager")]` | Policy already defined in Program.cs |
| Session verification | Custom verify logic | `TillService.VerifySessionAsync()` | Already prevents self-verification |
| Cash flow display | Custom formatting | Reuse `FormatCurrency()` from existing components | Consistent number formatting |

**Key insight:** Most "new" functionality is extension of existing methods, not new systems.

## Common Pitfalls

### Pitfall 1: Forgetting Multi-Currency Drops

**What goes wrong:** Treating drops as single-currency when sessions can have THB, USD, EUR, CNY drops.
**Why it happens:** TotalDropped on TillSession is THB-equivalent; actual drops are per-currency.
**How to avoid:** Always query TillTransaction with `TransactionType == Drop` and group by `Currency`.
**Warning signs:** Drop verification shows one total instead of per-currency breakdown.

### Pitfall 2: Hard-Blocking Closed Days

**What goes wrong:** Making daily close an irreversible hard block.
**Why it happens:** Natural assumption that "closed" means "locked."
**How to avoid:** Implement soft block with manager override (reopen with reason).
**Warning signs:** No reopen mechanism in DailyClose entity.

### Pitfall 3: Verifying Own Sessions

**What goes wrong:** Manager can verify their own sessions.
**Why it happens:** Not checking if manager == session staff.
**How to avoid:** `TillService.VerifySessionAsync` already checks this - don't bypass.
**Warning signs:** No "cannot verify own session" error in UI.

### Pitfall 4: Missing Variance Resolution Choice

**What goes wrong:** Forcing all variances to shortage log.
**Why it happens:** Not implementing the suspend vs. post choice.
**How to avoid:** Dialog must offer "Suspend session" (leave unresolved) OR "Post shortage" (log it).
**Warning signs:** No choice presented to manager during verification.

### Pitfall 5: Receipt Search Without TillSession Link

**What goes wrong:** Staff search returns receipts from other staff's sessions.
**Why it happens:** Not filtering by current staff's sessions.
**How to avoid:** For staff search, filter by current user's sessions OR make search organization-wide (context decision).
**Warning signs:** Staff sees all shop receipts instead of their own.

## Code Examples

Verified patterns from official sources (existing project code):

### TillService Extension Pattern
```csharp
// Source: TillService.manager.cs lines 14-70
// Pattern: New partial file for EOD operations
// File: TillService.eod.cs

public partial class TillService
{
    /// <summary>
    /// Gets or creates the DailyClose record for a date.
    /// </summary>
    public async Task<DailyClose> GetOrCreateDailyCloseAsync(int shopId, DateTime date)
    {
        var existing = await Context.LoadOneAsync<DailyClose>(
            d => d.ShopId == shopId && d.Date == date.Date);

        if (existing != null)
            return existing;

        // Create new record in Open status
        var dailyClose = new DailyClose
        {
            ShopId = shopId,
            Date = date.Date,
            Status = DailyCloseStatus.Open
        };

        using var session = Context.OpenSession("system");
        session.Attach(dailyClose);
        await session.SubmitChanges("CreateDailyClose");

        return dailyClose;
    }

    /// <summary>
    /// Performs daily close with summary calculation.
    /// </summary>
    public async Task<SubmitOperation> PerformDailyCloseAsync(
        int shopId,
        DateTime date,
        string managerUserName)
    {
        var dailyClose = await GetOrCreateDailyCloseAsync(shopId, date);

        if (dailyClose.Status == DailyCloseStatus.Closed)
            return SubmitOperation.CreateFailure("Day is already closed");

        // Get daily summary for totals
        var summary = await GetDailySummaryAsync(shopId, date);

        // Populate denormalized totals
        dailyClose.TotalCashIn = summary.TotalCashIn;
        dailyClose.TotalCashOut = summary.TotalCashOut;
        dailyClose.TotalDropped = summary.TotalDropped;
        dailyClose.TotalVariance = summary.TotalVariance;
        dailyClose.TotalElectronicPayments = summary.TotalElectronicPayments;
        dailyClose.SessionCount = summary.TotalSessions;
        dailyClose.SessionsWithVariance = summary.SessionsWithVariance;
        dailyClose.Status = DailyCloseStatus.Closed;
        dailyClose.ClosedAt = DateTimeOffset.Now;
        dailyClose.ClosedByUserName = managerUserName;

        using var session = Context.OpenSession(managerUserName);
        session.Attach(dailyClose);
        return await session.SubmitChanges("PerformDailyClose");
    }

    /// <summary>
    /// Checks if a day is closed (for transaction blocking).
    /// </summary>
    public async Task<bool> IsDayClosedAsync(int shopId, DateTime date)
    {
        var dailyClose = await Context.LoadOneAsync<DailyClose>(
            d => d.ShopId == shopId && d.Date == date.Date);
        return dailyClose?.Status == DailyCloseStatus.Closed;
    }

    /// <summary>
    /// Re-opens a closed day with reason.
    /// </summary>
    public async Task<SubmitOperation> ReopenDayAsync(
        int shopId,
        DateTime date,
        string reason,
        string managerUserName)
    {
        var dailyClose = await Context.LoadOneAsync<DailyClose>(
            d => d.ShopId == shopId && d.Date == date.Date);

        if (dailyClose == null)
            return SubmitOperation.CreateFailure("Daily close record not found");

        if (dailyClose.Status != DailyCloseStatus.Closed)
            return SubmitOperation.CreateFailure("Day is not closed");

        dailyClose.Status = DailyCloseStatus.Open;
        dailyClose.WasReopened = true;
        dailyClose.ReopenReason = reason;
        dailyClose.ReopenedAt = DateTimeOffset.Now;
        dailyClose.ReopenedByUserName = managerUserName;

        using var session = Context.OpenSession(managerUserName);
        session.Attach(dailyClose);
        return await session.SubmitChanges("ReopenDay");
    }
}
```

### Shortage Log Recording Pattern
```csharp
// Source: TillService.transaction.cs pattern
// Pattern: Recording shortage entries during daily close

public async Task<SubmitOperation> LogShortageAsync(
    int shopId,
    int tillSessionId,
    int? dailyCloseId,
    string staffUserName,
    string staffDisplayName,
    string currency,
    decimal amount,
    string reason,
    string managerUserName)
{
    // Convert to THB if foreign currency
    decimal amountInThb = amount;
    if (currency != SupportedCurrencies.THB)
    {
        var conversion = await ExchangeRateService.ConvertToThbAsync(currency, amount);
        amountInThb = conversion?.ThbAmount ?? amount;
    }

    var shortageLog = new ShortageLog
    {
        ShopId = shopId,
        TillSessionId = tillSessionId,
        DailyCloseId = dailyCloseId,
        StaffUserName = staffUserName,
        StaffDisplayName = staffDisplayName,
        Currency = currency,
        Amount = Math.Abs(amount), // Store as positive
        AmountInThb = Math.Abs(amountInThb),
        Reason = reason,
        LoggedByUserName = managerUserName,
        LoggedAt = DateTimeOffset.Now
    };

    using var session = Context.OpenSession(managerUserName);
    session.Attach(shortageLog);
    return await session.SubmitChanges("LogShortage");
}
```

### Receipt Search for Staff Pattern
```csharp
// Source: ReceiptService.cs GetReceiptsAsync() already provides this
// Pattern: Extend search to include amount range

public async Task<LoadOperation<Receipt>> SearchReceiptsAsync(
    int shopId,
    string? receiptNo = null,
    string? receiptType = null,
    string? customerNameOrPhone = null,
    decimal? minAmount = null,
    decimal? maxAmount = null,
    DateTimeOffset? fromDate = null,
    DateTimeOffset? toDate = null,
    int page = 1,
    int pageSize = 20)
{
    // Existing search logic extended
    var result = await GetReceiptsAsync(
        shopId,
        receiptType: receiptType,
        status: ReceiptStatus.Issued, // Staff only sees issued
        fromDate: fromDate,
        toDate: toDate,
        searchTerm: customerNameOrPhone,
        page: page,
        pageSize: pageSize);

    // Apply additional filters in memory
    var filtered = result.ItemCollection.AsEnumerable();

    if (!string.IsNullOrEmpty(receiptNo))
        filtered = filtered.Where(r => r.ReceiptNo.Contains(receiptNo, StringComparison.OrdinalIgnoreCase));

    if (minAmount.HasValue)
        filtered = filtered.Where(r => r.GrandTotal >= minAmount.Value);

    if (maxAmount.HasValue)
        filtered = filtered.Where(r => r.GrandTotal <= maxAmount.Value);

    result.ItemCollection = filtered.ToList();
    return result;
}
```

### Authorization Pattern
```razor
@* Source: TillDashboard.razor line 8 *@
@page "/manager/daily-close"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Policy = "RequireTenantManager")]
@inherits LocalizedComponentBase<DailyClose>
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Manager verification page only | TillDashboard + EOD page | Phase 8 | Already have verification, need EOD |
| Single-currency variance | Per-currency variance tracking | Phase 7 | ClosingVariances dict on TillSession |
| No receipt system | Full Receipt entity with service | Phase 8 | Receipt infrastructure complete |

**Deprecated/outdated:**
- None - this is a new feature area building on recent Phase 8 work.

## Open Questions

Things that couldn't be fully resolved:

1. **Staff receipt search scope**
   - What we know: Full search exists in `/finance/receipts` for managers
   - What's unclear: Should staff search see all shop receipts or only their own?
   - Recommendation: All shop receipts (more useful), but limited actions (view/print only, no void)

2. **Safe count entry method**
   - What we know: Cash drops are recorded per session with currency and amount
   - What's unclear: Does manager enter actual safe count, or just confirm drops are correct?
   - Recommendation: Context says "Manager confirms drops are correct, OR enters actual count if different and provides reason" - implement both options

3. **Daily close re-run**
   - What we know: Soft block with reopen capability
   - What's unclear: Can daily close be re-run after reopen to recalculate totals?
   - Recommendation: Yes, allow re-close after reopen with fresh calculation

## Sources

### Primary (HIGH confidence)
- `TillService.cs` and partial files - Complete till service implementation
- `TillSession.cs`, `TillTransaction.cs` - Entity definitions
- `ReceiptService.cs`, `Receipt.cs` - Receipt system implementation
- `TillDashboard.razor`, `HandoverReportDialog.razor` - Phase 8 UI patterns
- `EndOfDay.razor` - Existing EOD page (basic implementation)
- `EodSessionDetailDialog.razor` - Session detail dialog pattern
- `Program.cs` - Authorization policy definitions

### Secondary (MEDIUM confidence)
- `09-CONTEXT.md` - User decisions from `/gsd:discuss-phase`
- `REQUIREMENTS.md` - EOD and RCPT requirement definitions
- Existing `/finance/receipts` page for manager receipt search pattern

### Tertiary (LOW confidence)
- None - all research based on existing codebase

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in use, no new dependencies
- Architecture: HIGH - Extending existing patterns from Phase 8
- Entity design: HIGH - Following established TillSession/TillTransaction patterns
- Pitfalls: MEDIUM - Based on context decisions, some edge cases may emerge

**Research date:** 2026-01-21
**Valid until:** 2026-02-21 (30 days - stable domain, internal codebase)
