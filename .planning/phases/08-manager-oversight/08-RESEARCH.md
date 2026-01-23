# Phase 8: Manager Oversight - Research

**Researched:** 2026-01-21
**Domain:** Till session dashboard, verification workflow, alerts, shift handover reports
**Confidence:** HIGH

## Summary

Phase 8 provides manager oversight capabilities for till sessions built on top of the existing Phase 7 infrastructure. The TillSession entity already contains all necessary verification fields (VerifiedByUserName, VerifiedAt, VerificationNotes). The TillService already has manager verification methods (VerifySessionAsync, GetSessionsForVerificationAsync, GetDailySummaryAsync). The existing EndOfDay.razor page provides the verification workflow template.

Key additions needed:
1. New manager dashboard page at `/manager/till-dashboard` with active sessions cards + closed sessions table
2. Variance threshold setting in Organization Settings
3. Alert indicator component (bell icon with badge)
4. Shift handover report with sales clearing journal format

**Primary recommendation:** Leverage existing TillService.manager.cs methods and EndOfDay.razor patterns. Add new dashboard page with cards-and-table layout, extend SettingKeys with variance threshold, and create journal-style handover report component using browser print.

## Standard Stack

### Core (Already in Project)
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Blazor Server | .NET 10 | Component framework | Project standard |
| Tabler CSS | 1.0+ | UI framework | Project standard (cards, badges, tables) |
| Tabler Icons | 3.x | Icon library | ti-* icon classes throughout |
| Browser print | native | PDF/print output | Existing pattern in ReceiptPrintDialog.razor |

### Supporting (Already Available)
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| IModalService | in-project | Dialog management | Verification dialog, session details |
| ISettingConfig | in-project | Tenant settings | Variance threshold configuration |
| ExchangeRateService | in-project | Currency conversion | THB-equivalent variance calculation |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Browser print | QuestPDF | Browser print simpler, no server dependency; QuestPDF better for precise layouts |
| Real-time alerts | SignalR push | Polling/page-load is simpler, real-time adds complexity |

## Architecture Patterns

### Recommended Project Structure
```
src/MotoRent.Client/
├── Pages/Manager/
│   ├── TillDashboard.razor           # NEW: Main dashboard page
│   ├── TillDashboard.razor.cs        # Code-behind (optional)
│   └── HandoverReportDialog.razor    # NEW: Report preview dialog
├── Components/Till/
│   ├── ActiveSessionCard.razor       # NEW: Card for open sessions
│   └── VarianceAlertBadge.razor      # NEW: Bell icon with count
├── Resources/Pages/Manager/
│   ├── TillDashboard.resx            # Default resources
│   ├── TillDashboard.en.resx         # English
│   ├── TillDashboard.th.resx         # Thai
│   └── TillDashboard.ms.resx         # Malay
└── Resources/Components/Till/
    ├── ActiveSessionCard.*.resx
    └── VarianceAlertBadge.*.resx

src/MotoRent.Services/
└── TillService.manager.cs            # EXTEND: Add dashboard queries

src/MotoRent.Domain/
└── Settings/SettingKeys.cs           # EXTEND: Add variance threshold key
```

### Pattern 1: Manager Dashboard Layout
**What:** Two-section layout with cards above and table below
**When to use:** Manager oversight dashboards with active items + history
**Example:**
```razor
// Source: AssetDashboard.razor pattern
<div class="container-xl py-4">
    @* Section 1: Active Sessions Cards *@
    <div class="row g-3 mb-4">
        @foreach (var session in m_activeSessions)
        {
            <div class="col-md-4 col-lg-3">
                <ActiveSessionCard Session="@session"
                    OnVerify="@(() => ViewSession(session))" />
            </div>
        }
    </div>

    @* Section 2: Closed Sessions Table *@
    <div class="card">
        <div class="card-header">
            <h4 class="card-title">@Localizer["RecentClosed"]</h4>
            <div class="card-actions">
                @* Filter controls *@
            </div>
        </div>
        <div class="table-responsive">
            <table class="table table-vcenter card-table">
                @* Table content *@
            </table>
        </div>
    </div>
</div>
```

### Pattern 2: Session Card Component
**What:** Rich card displaying session status, staff info, currency balances
**When to use:** Displaying active session summaries
**Example:**
```razor
// Source: AssetDashboard.razor KPI cards pattern
<div class="card h-100 @(HasVariance ? "border-warning" : "")">
    <div class="card-body">
        <div class="d-flex align-items-start mb-3">
            <span class="avatar avatar-lg bg-primary-lt me-3">
                <i class="ti ti-user"></i>
            </span>
            <div class="flex-fill">
                <h4 class="mb-0">@Session.StaffDisplayName</h4>
                <small class="text-secondary">
                    <i class="ti ti-clock me-1"></i>
                    @FormatDuration(Session.OpenedAt)
                </small>
            </div>
            @if (HasVarianceAlert)
            {
                <span class="badge bg-warning">
                    <i class="ti ti-alert-triangle"></i>
                </span>
            }
        </div>

        @* Currency balances *@
        <div class="row g-2">
            @foreach (var balance in Session.CurrencyBalances)
            {
                <div class="col-6">
                    <small class="text-secondary d-block">@balance.Key</small>
                    <span class="fw-semibold">@balance.Value.ToString("N2")</span>
                </div>
            }
        </div>
    </div>
</div>
```

### Pattern 3: Authorization for Manager Pages
**What:** Policy-based authorization for manager-only pages
**When to use:** All pages in /manager/* path
**Example:**
```razor
// Source: EndOfDay.razor, AssetDashboard.razor
@page "/manager/till-dashboard"
@using Microsoft.AspNetCore.Authorization
@attribute [Authorize(Policy = "RequireTenantManager")]
@inherits LocalizedComponentBase<TillDashboard>
```

### Pattern 4: Variance Alert Badge
**What:** Bell icon with count badge showing sessions with variance issues
**When to use:** Header or sidebar to indicate pending attention items
**Example:**
```razor
// Source: StaffActionCard.razor BadgeCount pattern + AssetDashboard alert count
<a href="/manager/till-dashboard" class="nav-link position-relative">
    <i class="ti ti-bell"></i>
    @if (m_varianceCount > 0)
    {
        <span class="badge bg-danger position-absolute"
              style="top: 0; right: 0; transform: translate(25%, -25%);">
            @m_varianceCount
        </span>
    }
</a>
```

### Pattern 5: Sales Clearing Journal Report
**What:** Two-column ledger format for handover report
**When to use:** Shift handover, daily reconciliation
**Example:**
```razor
// Source: ReceiptDocument.razor print pattern + sc.png reference
<div class="handover-report" style="font-family: 'Courier New', monospace;">
    <h3 class="text-center mb-4">@Localizer["SalesClearingJournal"]</h3>
    <table class="table table-sm">
        <thead>
            <tr>
                <th>@Localizer["Description"]</th>
                <th class="text-end">@Localizer["Credit"]</th>
                <th class="text-end">@Localizer["Debit"]</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var item in m_journalItems)
            {
                <tr>
                    <td>
                        <i class="@item.Icon me-2"></i>
                        @item.Description
                    </td>
                    <td class="text-end">
                        @(item.IsCredit ? item.Amount.ToString("N2") : "")
                    </td>
                    <td class="text-end">
                        @(item.IsDebit ? item.Amount.ToString("N2") : "")
                    </td>
                </tr>
            }
        </tbody>
        <tfoot>
            <tr class="fw-bold">
                <td>@Localizer["Total"]</td>
                <td class="text-end">@m_totalCredit.ToString("N2")</td>
                <td class="text-end">@m_totalDebit.ToString("N2")</td>
            </tr>
        </tfoot>
    </table>
</div>
```

### Anti-Patterns to Avoid
- **Polling for alerts:** Don't poll for variance alerts; calculate on page load or navigation
- **Self-verification:** Staff cannot verify their own sessions; check `session.StaffUserName != currentUserName`
- **Hardcoded thresholds:** Use SettingKeys for variance threshold, not hardcoded values
- **Multi-query for currencies:** Don't load sessions multiple times for different currencies; use single load with CurrencyBalances dictionary

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Currency conversion | Custom conversion logic | ExchangeRateService | Already handles rate lookups, fallback rates |
| Session verification | Custom status update | TillService.VerifySessionAsync() | Already handles status, timestamp, validation |
| Daily summaries | Manual aggregation | TillService.GetDailySummaryAsync() | Already aggregates cash in/out/electronic |
| PDF generation | Server-side PDF library | Browser window.print() | Existing pattern in ReceiptPrintDialog.razor |
| Authorization check | Manual role check | [Authorize(Policy = "RequireTenantManager")] | Policy already defined in project |
| Settings storage | Custom config file | ISettingConfig / SettingKeys | Tenant-scoped, cached, typed access |

**Key insight:** TillService.manager.cs already has most query methods needed. The existing EndOfDay.razor page is essentially a prototype for this phase's dashboard.

## Common Pitfalls

### Pitfall 1: Self-Verification
**What goes wrong:** Manager verifies a session they operated
**Why it happens:** No validation preventing self-verification
**How to avoid:** Check `session.StaffUserName != managerUserName` before allowing verify action
**Warning signs:** Same username in StaffUserName and VerifiedByUserName

### Pitfall 2: Variance Threshold Currency Mismatch
**What goes wrong:** Threshold set in THB compared against USD variance
**Why it happens:** Variance stored per-currency but threshold is single value
**How to avoid:** Convert all currency variances to THB-equivalent using ExchangeRateService before comparison
**Warning signs:** Small USD variance triggers alert while large THB variance doesn't

### Pitfall 3: Stale Dashboard Data
**What goes wrong:** Dashboard shows outdated session data
**Why it happens:** Data loaded on init but not refreshed after verify action
**How to avoid:** Reload data after any mutating action (verify, force close)
**Warning signs:** Session still shows "Pending" after verification

### Pitfall 4: Missing Status Filter
**What goes wrong:** Closed sessions table shows all sessions including Open ones
**Why it happens:** Query doesn't filter by closed status
**How to avoid:** Filter sessions by status in GetRecentClosedSessionsAsync: `Closed, ClosedWithVariance, Verified`
**Warning signs:** Open sessions appear in "Recent Closed" section

### Pitfall 5: Report Not Printing Correctly
**What goes wrong:** Print preview shows UI chrome, navigation, etc.
**Why it happens:** Missing @media print CSS rules
**How to avoid:** Use `d-print-none` class on non-print elements, use `d-print-block` on report
**Warning signs:** Navbar appears in printed output

## Code Examples

### Loading Active Sessions for Dashboard
```csharp
// Source: TillService.session.cs GetActiveSessionsAsync pattern
// TillService.manager.cs - Add method
public async Task<List<TillSession>> GetAllActiveSessionsAsync(int shopId)
{
    var result = await this.Context.LoadAsync(
        this.Context.CreateQuery<TillSession>()
            .Where(s => s.ShopId == shopId)
            .Where(s => s.Status == TillSessionStatus.Open)
            .OrderBy(s => s.OpenedAt),
        page: 1, size: 100, includeTotalRows: false);
    return result.ItemCollection.ToList();
}
```

### Loading Recent Closed Sessions
```csharp
// TillService.manager.cs - Add method
public async Task<List<TillSession>> GetRecentClosedSessionsAsync(
    int shopId,
    int days = 7)
{
    var cutoffDate = DateTimeOffset.Now.AddDays(-days);

    var result = await this.Context.LoadAsync(
        this.Context.CreateQuery<TillSession>()
            .Where(s => s.ShopId == shopId)
            .Where(s => s.Status != TillSessionStatus.Open)
            .Where(s => s.Status != TillSessionStatus.Reconciling)
            .Where(s => s.ClosedAt >= cutoffDate)
            .OrderByDescending(s => s.ClosedAt),
        page: 1, size: 100, includeTotalRows: false);
    return result.ItemCollection.ToList();
}
```

### Calculating THB-Equivalent Variance
```csharp
// TillService.manager.cs - Add method
public async Task<decimal> GetTotalVarianceInThbAsync(TillSession session)
{
    if (session.ClosingVariances.Count == 0)
        return 0;

    decimal totalThb = 0;
    foreach (var (currency, variance) in session.ClosingVariances)
    {
        if (currency == SupportedCurrencies.THB)
        {
            totalThb += variance;
        }
        else
        {
            var rate = await ExchangeRateService.GetRateAsync(currency, SupportedCurrencies.THB);
            totalThb += variance * rate;
        }
    }
    return totalThb;
}
```

### Counting Sessions Exceeding Variance Threshold
```csharp
// TillService.manager.cs - Add method
public async Task<int> GetVarianceAlertCountAsync(
    int shopId,
    decimal thresholdThb)
{
    var activeSessions = await GetAllActiveSessionsAsync(shopId);
    var recentClosed = await GetRecentClosedSessionsAsync(shopId, days: 1);

    var allSessions = activeSessions.Concat(recentClosed)
        .Where(s => s.Status == TillSessionStatus.ClosedWithVariance ||
                    s.Status == TillSessionStatus.Closed);

    int count = 0;
    foreach (var session in allSessions.Where(s => s.ClosingVariances.Any()))
    {
        var totalVariance = await GetTotalVarianceInThbAsync(session);
        if (Math.Abs(totalVariance) > thresholdThb)
            count++;
    }
    return count;
}
```

### Settings Key for Variance Threshold
```csharp
// Source: SettingKeys.cs pattern
// Add to SettingKeys.cs under new #region Till Settings

#region Till Settings

/// <summary>
/// Variance threshold in THB for manager alerts (decimal).
/// Sessions with variance exceeding this amount trigger alerts.
/// </summary>
public const string Till_VarianceAlertThreshold = "Till.VarianceAlertThreshold";

/// <summary>
/// Default opening float amount in THB (decimal).
/// </summary>
public const string Till_DefaultOpeningFloat = "Till.DefaultOpeningFloat";

#endregion
```

### Verification with Self-Check
```csharp
// Source: TillService.manager.cs VerifySessionAsync - enhance
public async Task<SubmitOperation> VerifySessionAsync(
    int sessionId,
    string managerUserName,
    string? notes = null)
{
    var session = await this.GetSessionByIdAsync(sessionId);
    if (session is null)
        return SubmitOperation.CreateFailure("Session not found");

    if (session.Status is not TillSessionStatus.Closed and not TillSessionStatus.ClosedWithVariance)
        return SubmitOperation.CreateFailure("Session must be closed before verification");

    // Prevent self-verification
    if (session.StaffUserName == managerUserName)
        return SubmitOperation.CreateFailure("You cannot verify your own session");

    session.Status = TillSessionStatus.Verified;
    session.VerifiedByUserName = managerUserName;
    session.VerifiedAt = DateTimeOffset.Now;
    session.VerificationNotes = notes;

    using var persistenceSession = this.Context.OpenSession(managerUserName);
    persistenceSession.Attach(session);
    return await persistenceSession.SubmitChanges("VerifyTillSession");
}
```

### Print-Friendly Report Dialog
```razor
// Source: ReceiptPrintDialog.razor pattern
@inject IJSRuntime JSRuntime

<div class="modal-body p-0 d-print-none" style="max-height: 70vh; overflow-y: auto;">
    <HandoverReportContent Session="@m_session" />
</div>

@* Print-only version *@
<div class="d-none d-print-block">
    <HandoverReportContent Session="@m_session" />
</div>

<div class="modal-footer d-print-none">
    <button type="button" class="btn btn-ghost-secondary" @onclick="Cancel">
        @CommonLocalizer["Close"]
    </button>
    <button type="button" class="btn btn-primary" @onclick="PrintAsync">
        <i class="ti ti-printer me-1"></i>
        @Localizer["DownloadPdf"]
    </button>
</div>

@code {
    private async Task PrintAsync()
    {
        await JSRuntime.InvokeVoidAsync("print");
    }
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Single-currency variance | Per-currency variances (ClosingVariances dict) | Phase 7 | Already implemented, use directly |
| Global settings | Tenant-scoped ISettingConfig | Existing | Use SettingKeys for threshold |
| Server-side PDF | Browser print() | Project standard | Simpler, works everywhere |

**Deprecated/outdated:**
- Single ActualCash/Variance fields: Still populated for backward compatibility, but ClosingVariances dictionary is authoritative for multi-currency

## Open Questions

1. **Alert persistence:**
   - What we know: Variance alerts calculated on-demand from session data
   - What's unclear: Should dismissed alerts be tracked or just recalculate each time?
   - Recommendation: Recalculate on page load; don't persist dismissals (simpler)

2. **Report time range:**
   - What we know: Handover report for a session
   - What's unclear: Can manager generate multi-session report (e.g., full shift)?
   - Recommendation: Start with single-session report; add date range filter later if needed

3. **Bell icon placement:**
   - What we know: Need count badge on icon
   - What's unclear: Best location in existing navigation structure
   - Recommendation: Add to MainLayout header area near user dropdown (Claude's discretion per CONTEXT.md)

## Sources

### Primary (HIGH confidence)
- `src/MotoRent.Domain/Entities/TillSession.cs` - Entity fields verified
- `src/MotoRent.Services/TillService.manager.cs` - Existing verification methods
- `src/MotoRent.Services/TillService.session.cs` - Session query patterns
- `src/MotoRent.Client/Pages/Manager/EndOfDay.razor` - UI patterns for session table
- `src/MotoRent.Client/Pages/Finance/AssetDashboard.razor` - Dashboard layout patterns
- `src/MotoRent.Domain/Settings/SettingKeys.cs` - Settings key patterns
- `src/MotoRent.Services/Core/SettingConfigService.cs` - Settings service usage

### Secondary (MEDIUM confidence)
- `sc.png` - Sales clearing journal reference image for report format
- `src/MotoRent.Client/Components/Receipts/ReceiptPrintDialog.razor` - Print pattern

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - All libraries already in project, patterns verified
- Architecture: HIGH - Patterns extracted from existing codebase (EndOfDay, AssetDashboard)
- Pitfalls: HIGH - Based on actual entity fields and existing service methods

**Research date:** 2026-01-21
**Valid until:** 30 days (stable domain, existing patterns)
