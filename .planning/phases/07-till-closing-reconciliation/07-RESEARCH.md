# Phase 7: Till Closing and Reconciliation - Research

**Researched:** 2026-01-21
**Domain:** Till session close workflow with per-currency variance tracking
**Confidence:** HIGH

## Summary

This phase extends the existing till closing workflow to add per-currency variance tracking, summary review before confirm, and accountability logging. The foundation already exists from Phase 6 (denomination counting) - this phase wires up the close workflow with variance persistence.

The existing `TillCloseSessionDialog` already integrates `ClosingCountPanel` which captures denomination counts and calculates variance per currency. The main gaps are:
1. Adding a summary review step before confirm
2. Implementing `ForceCloseSessionAsync` method (called but not implemented)
3. Extending `CloseSessionAsync` to persist per-currency variances
4. Adding close metadata fields to TillSession entity (ClosedByUserId)

**Primary recommendation:** Extend TillSession entity with close metadata, add summary step to close dialog, implement ForceCloseSessionAsync, update CloseSessionAsync to accept multi-currency variances.

## Current State Analysis

### TillSession Entity

**Location:** `src/MotoRent.Domain/Entities/TillSession.cs`

**Existing Fields:**
```csharp
// Opening
public decimal OpeningFloat { get; set; }
public DateTimeOffset OpenedAt { get; set; }
public string? OpeningNotes { get; set; }

// Running totals (per session, denormalized)
public decimal TotalCashIn { get; set; }
public decimal TotalCashOut { get; set; }
public decimal TotalDropped { get; set; }
public decimal TotalToppedUp { get; set; }

// Currency tracking
public Dictionary<string, decimal> CurrencyBalances { get; set; }  // Expected per currency

// Closing (THB only currently)
public decimal ActualCash { get; set; }       // THB actual from count
public decimal Variance { get; set; }          // THB variance only
public DateTimeOffset? ClosedAt { get; set; }
public string? ClosingNotes { get; set; }

// Manager verification (Phase 8 scope)
public string? VerifiedByUserName { get; set; }
public DateTimeOffset? VerifiedAt { get; set; }
public string? VerificationNotes { get; set; }

// Computed
public decimal ExpectedCash => OpeningFloat + TotalCashIn - TotalCashOut - TotalDropped + TotalToppedUp;
```

**Missing Fields (per CONTEXT.md decisions):**
- `ClosedByUserId` / `ClosedByUserName` - Staff who closed (for accountability)
- Per-currency variance tracking - Currently only tracks THB variance
- `VarianceNote` - Optional note when closing with variance
- `IsForceClose` - Flag if manager force-closed

**Confidence:** HIGH - Directly verified from entity source

### TillSessionStatus Enum

**Location:** `src/MotoRent.Domain/Entities/TillEnums.cs`

**Existing States:**
```csharp
public enum TillSessionStatus
{
    Open,              // Active, accepting transactions
    Reconciling,       // Staff counting (optional - not currently used)
    Closed,            // Closed with no variance
    ClosedWithVariance,// Closed but had variance
    PendingVerification, // Awaiting manager (not currently used)
    Verified           // Manager approved
}
```

**State Transitions (per CONTEXT.md):**
- `Open` -> `Closed` (no variance)
- `Open` -> `ClosedWithVariance` (has variance)
- Once closed, session is immutable - no new transactions allowed

**Confidence:** HIGH - Directly verified from enum source

### TillCloseSessionDialog (Existing)

**Location:** `src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor`

**Current Workflow:**
1. Session summary card (Opening, CashIn, CashOut)
2. `ClosingCountPanel` for denomination entry (THB + foreign currencies with balance)
3. Variance acknowledgment checkbox (if variance exists)
4. Closing notes textarea
5. Manager Force Close button (calls `ForceCloseSessionAsync` - NOT IMPLEMENTED)
6. Close Session button

**Already Implemented:**
- Integration with `ClosingCountPanel`
- `GetExpectedBalances()` - Returns `TillSession.CurrencyBalances`
- `OnBreakdownsChanged()` - Updates `m_currencyBreakdowns` list
- `HasVariance` - Checks if any currency has variance
- `CanClose` - Requires entered amount + variance acknowledgment
- `SaveAsync()` - Calls `CloseSessionAsync` then `SaveDenominationCountAsync`

**Gaps:**
- No summary step before confirm (user decision: "Summary review then confirm")
- `ForceCloseSessionAsync` called but throws NotImplementedException
- No per-currency variance display in final confirmation

**Confidence:** HIGH - Directly verified from dialog source

### ClosingCountPanel (Existing)

**Location:** `src/MotoRent.Client/Components/Till/ClosingCountPanel.razor`

**Capabilities:**
- Shows currencies with expected balance > 0 (THB always shown)
- Denomination entry per currency (vertical list layout)
- Per-currency variance calculation and display
- Overall variance in THB (converted via exchange rate)
- Auto-save draft counts (debounced 2 seconds)
- Draft restoration on load
- Callback `OnBreakdownsChanged(List<CurrencyDenominationBreakdown>)`

**Output Data Structure:**
```csharp
public class CurrencyDenominationBreakdown
{
    public string Currency { get; set; }
    public Dictionary<decimal, int> Denominations { get; set; }  // Value -> Count
    public decimal Total { get; }  // Computed sum
    public decimal? ExpectedBalance { get; set; }
    public decimal? Variance { get; }  // Computed: Total - ExpectedBalance
}
```

**Confidence:** HIGH - Directly verified from component source

### TillService.CloseSessionAsync (Existing)

**Location:** `src/MotoRent.Services/TillService.cs`

**Current Signature:**
```csharp
public async Task<SubmitOperation> CloseSessionAsync(
    int sessionId,
    decimal actualCash,       // THB only
    string? notes,
    string username)
```

**Current Behavior:**
1. Validate session exists and is open
2. Validate user owns the session
3. Set `session.ActualCash = actualCash`
4. Set `session.Variance = actualCash - session.ExpectedCash` (THB only)
5. Set `session.ClosedAt = DateTimeOffset.Now`
6. Set `session.ClosingNotes = notes`
7. Set status to `Closed` or `ClosedWithVariance`

**Gaps:**
- Only accepts THB actual/variance
- Does not store per-currency variances
- Does not store who closed (StaffUserName is in session but ClosedByUserName not set)
- No force close tracking

**Confidence:** HIGH - Directly verified from service source

### ForceCloseSessionAsync (MISSING)

**Called from:** `TillCloseSessionDialog.razor` line 208

**Expected Signature (inferred from call):**
```csharp
public async Task<SubmitOperation> ForceCloseSessionAsync(
    int sessionId,
    string managerUserName,   // Manager who approved
    string? notes,
    string staffUserName)     // Staff requesting close
```

**Expected Behavior:**
- Manager approval bypasses denomination count requirement
- Sets ActualCash = ExpectedCash for each currency (zero variance)
- Marks session as force-closed for audit trail

**Confidence:** HIGH - Call site verified, method signature inferred

### TillDenominationCount Entity

**Location:** `src/MotoRent.Domain/Entities/TillDenominationCount.cs`

**Already Stores:**
- `TillSessionId` - Links to session
- `CountType` - Opening or Closing
- `CurrencyBreakdowns` - Full denomination breakdown per currency
- `TotalInThb` - Grand total converted
- `IsFinal` - Draft vs final flag
- `CountedAt`, `CountedByUserName`, `Notes`

**Note:** This entity already captures the detailed closing count. The gap is linking the variance summary to TillSession for quick access without loading denomination count.

**Confidence:** HIGH - Directly verified from entity source

### Manager PIN Dialog (Existing Pattern)

**Location:** `src/MotoRent.Client/Components/Auth/ManagerPinDialog.razor`

**Capabilities:**
- PIN pad entry (4 digits)
- Manager selection dropdown (if multiple)
- Lockout after failed attempts
- Returns manager username on success

**Usage Pattern:**
```csharp
var pinResult = await DialogService.Create<ManagerPinDialog>(Localizer["ManagerApproval"])
    .WithSize(ModalSize.Medium)
    .ShowDialogAsync();

if (pinResult is null or { Cancelled: true }) return;
var managerUserName = pinResult.Data as string;
```

**Confidence:** HIGH - Directly verified from dialog source

## Gap Analysis

### Must Implement

| Gap | Description | Priority |
|-----|-------------|----------|
| **ForceCloseSessionAsync** | Method called but not implemented | Critical |
| **Summary Step** | Add summary view before confirm (per user decision) | High |
| **Per-Currency Variance Storage** | Extend TillSession with variance breakdown | High |
| **Close Metadata** | Add ClosedByUserName, IsForceClose to TillSession | High |

### Already Done (Phase 6)

| Feature | Status |
|---------|--------|
| ClosingCountPanel component | Complete |
| Denomination breakdown entry | Complete |
| Per-currency variance calculation | Complete |
| Draft auto-save | Complete |
| TillDenominationCount persistence | Complete |

### Deferred (Phase 8)

| Feature | Why Deferred |
|---------|--------------|
| Session reopening | Manager oversight feature |
| Variance threshold alerts | Manager notification feature |
| Shift handover report | EOD reporting feature |

## Implementation Approach

### Task 1: Extend TillSession Entity

Add fields to `TillSession.cs`:

```csharp
// Additional close metadata
public string? ClosedByUserName { get; set; }
public bool IsForceClose { get; set; }
public string? ForceCloseApprovedBy { get; set; }

// Per-currency variance (stored as dictionary for flexibility)
// Key: currency code (THB, USD, EUR, CNY)
// Value: variance (positive = over, negative = short)
public Dictionary<string, decimal> ClosingVariances { get; set; } = new();

// Actual balances at close per currency
public Dictionary<string, decimal> ActualBalances { get; set; } = new();
```

**Note:** `CurrencyBalances` stores expected; `ActualBalances` stores counted; `ClosingVariances` stores difference.

### Task 2: Update Database Table

Add computed columns to `MotoRent.TillSession.sql` for indexing if needed (JSON column handles storage):

```sql
-- Optional: Add computed column for force close flag
[IsForceClose] AS CAST(JSON_VALUE([Json], '$.IsForceClose') AS BIT),
[ClosedByUserName] AS CAST(JSON_VALUE([Json], '$.ClosedByUserName') AS NVARCHAR(100)),
```

### Task 3: Add Summary Step to Dialog

**Pattern:** Two-step close workflow
1. Step 1: Denomination entry (ClosingCountPanel - already exists)
2. Step 2: Summary review (new)
3. Confirm button closes session

**Summary Step UI:**
```razor
@if (m_showSummary)
{
    <div class="card border-primary mb-3">
        <div class="card-header bg-primary text-white">
            <i class="ti ti-list-check me-2"></i>
            @Localizer["ClosingSummary"]
        </div>
        <div class="card-body">
            <table class="table table-sm">
                <thead>
                    <tr>
                        <th>@Localizer["Currency"]</th>
                        <th class="text-end">@Localizer["Expected"]</th>
                        <th class="text-end">@Localizer["Counted"]</th>
                        <th class="text-end">@Localizer["Variance"]</th>
                    </tr>
                </thead>
                <tbody>
                    @foreach (var breakdown in m_currencyBreakdowns)
                    {
                        var variance = breakdown.Variance ?? 0;
                        <tr class="@GetVarianceRowClass(variance)">
                            <td>@breakdown.Currency</td>
                            <td class="text-end">@FormatAmount(breakdown.ExpectedBalance, breakdown.Currency)</td>
                            <td class="text-end">@FormatAmount(breakdown.Total, breakdown.Currency)</td>
                            <td class="text-end @GetVarianceClass(variance)">
                                @FormatVariance(variance, breakdown.Currency)
                            </td>
                        </tr>
                    }
                </tbody>
            </table>

            @* Grand Total in THB *@
            <div class="border-top pt-2 mt-2">
                <div class="d-flex justify-content-between fw-bold">
                    <span>@Localizer["OverallVariance"] (THB)</span>
                    <span class="@GetVarianceClass(GetOverallVarianceThb())">
                        @FormatVariance(GetOverallVarianceThb(), SupportedCurrencies.THB)
                    </span>
                </div>
            </div>
        </div>
    </div>
}
```

### Task 4: Implement ForceCloseSessionAsync

```csharp
public async Task<SubmitOperation> ForceCloseSessionAsync(
    int sessionId,
    string approvedByUserName,
    string? notes,
    string closedByUserName)
{
    var session = await GetSessionByIdAsync(sessionId);
    if (session == null)
        return SubmitOperation.CreateFailure("Session not found");

    if (session.Status != TillSessionStatus.Open)
        return SubmitOperation.CreateFailure("Session is not open");

    // Force close sets actual = expected (zero variance)
    session.ActualCash = session.ExpectedCash;
    session.Variance = 0;
    session.ClosedAt = DateTimeOffset.Now;
    session.ClosedByUserName = closedByUserName;
    session.ClosingNotes = notes;
    session.IsForceClose = true;
    session.ForceCloseApprovedBy = approvedByUserName;
    session.Status = TillSessionStatus.Closed;

    // Set per-currency actuals = expected (no variance)
    session.ActualBalances = new Dictionary<string, decimal>(session.CurrencyBalances);
    session.ClosingVariances = session.CurrencyBalances.ToDictionary(
        kvp => kvp.Key,
        kvp => 0m
    );

    using var persistenceSession = Context.OpenSession(closedByUserName);
    persistenceSession.Attach(session);
    return await persistenceSession.SubmitChanges("ForceCloseTillSession");
}
```

### Task 5: Update CloseSessionAsync

Update to accept per-currency data:

```csharp
public async Task<SubmitOperation> CloseSessionAsync(
    int sessionId,
    List<CurrencyDenominationBreakdown> breakdowns,
    string? notes,
    string closedByUserName)
{
    var session = await GetSessionByIdAsync(sessionId);
    if (session == null)
        return SubmitOperation.CreateFailure("Session not found");

    if (session.Status != TillSessionStatus.Open)
        return SubmitOperation.CreateFailure("Session is not open");

    if (session.StaffUserName != closedByUserName)
        return SubmitOperation.CreateFailure("You can only close your own session");

    // Calculate per-currency actuals and variances
    var actualBalances = new Dictionary<string, decimal>();
    var closingVariances = new Dictionary<string, decimal>();

    foreach (var breakdown in breakdowns)
    {
        actualBalances[breakdown.Currency] = breakdown.Total;
        closingVariances[breakdown.Currency] = breakdown.Variance ?? 0;
    }

    // Get THB values for backward compatibility
    var thbActual = actualBalances.GetValueOrDefault(SupportedCurrencies.THB, 0);
    var thbExpected = session.GetCurrencyBalance(SupportedCurrencies.THB);

    session.ActualCash = thbActual;
    session.Variance = thbActual - thbExpected;
    session.ActualBalances = actualBalances;
    session.ClosingVariances = closingVariances;
    session.ClosedAt = DateTimeOffset.Now;
    session.ClosedByUserName = closedByUserName;
    session.ClosingNotes = notes;
    session.IsForceClose = false;

    // Determine status based on ANY variance
    var hasAnyVariance = closingVariances.Values.Any(v => v != 0);
    session.Status = hasAnyVariance
        ? TillSessionStatus.ClosedWithVariance
        : TillSessionStatus.Closed;

    using var persistenceSession = Context.OpenSession(closedByUserName);
    persistenceSession.Attach(session);
    return await persistenceSession.SubmitChanges("CloseTillSession");
}
```

### Task 6: Update Dialog SaveAsync

Update `TillCloseSessionDialog.razor` to use new method signature:

```csharp
private async Task SaveAsync()
{
    if (!CanClose) return;

    try
    {
        Saving = true;

        // Close session with full breakdown
        var result = await TillService.CloseSessionAsync(
            Entity.TillSessionId,
            m_currencyBreakdowns,
            Notes,
            UserName);

        if (!result.Success)
        {
            ShowError(result.Message ?? "Failed to close session");
            return;
        }

        // Save denomination count (final)
        if (m_currencyBreakdowns.Count > 0)
        {
            var denominationResult = await TillService.SaveDenominationCountAsync(
                Entity.TillSessionId,
                DenominationCountType.Closing,
                m_currencyBreakdowns,
                UserName,
                isFinal: true,
                notes: Notes);

            if (!denominationResult.Success)
            {
                Logger.LogWarning("Failed to save closing denomination count: {Message}",
                    denominationResult.Message);
            }
        }

        ModalService.Close(ModalResult.Ok(result));
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "Error closing till session");
        ShowError("Failed to close session");
    }
    finally
    {
        Saving = false;
    }
}
```

## File Inventory

### Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Domain/Entities/TillSession.cs` | Add close metadata fields |
| `src/MotoRent.Services/TillService.cs` | Add ForceCloseSessionAsync, update CloseSessionAsync signature |
| `src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor` | Add summary step, update save logic |
| `database/tables/MotoRent.TillSession.sql` | Add computed columns for new fields |

### Files to Create

| File | Purpose |
|------|---------|
| None | All components exist from Phase 6 |

### Localization Resources

| File | Add Keys |
|------|----------|
| `Resources/Pages/Staff/TillCloseSessionDialog.resx` | ClosingSummary, ReviewAndConfirm, BackToCount |
| `Resources/Pages/Staff/TillCloseSessionDialog.en.resx` | English translations |
| `Resources/Pages/Staff/TillCloseSessionDialog.th.resx` | Thai translations |
| `Resources/Pages/Staff/TillCloseSessionDialog.ms.resx` | Malay translations |

## Dependencies

### Prerequisites (Complete)

| Dependency | Status | Verified |
|------------|--------|----------|
| Phase 6 - Denomination Counting | Complete | Yes |
| ClosingCountPanel component | Exists | Yes |
| TillDenominationCount entity | Exists | Yes |
| ManagerPinDialog | Exists | Yes |
| TillService base methods | Exists | Yes |

### External Dependencies

| Dependency | Purpose | Status |
|------------|---------|--------|
| ExchangeRateService | Convert foreign variance to THB | Exists |
| ManagerPinService | PIN verification | Exists |

## Risk Areas

### Backward Compatibility

**Risk:** Existing close methods use THB-only parameters
**Mitigation:** Keep backward-compatible signature, add overload with multi-currency support
**Alternative:** Create new method `CloseSessionWithBreakdownsAsync` to avoid breaking changes

### Data Migration

**Risk:** Existing closed sessions don't have new fields
**Mitigation:** New fields are nullable or have defaults:
- `ClosedByUserName` nullable - can derive from `StaffUserName` if needed
- `ActualBalances` defaults to empty dict
- `ClosingVariances` defaults to empty dict
- `IsForceClose` defaults to false

### Pending Transactions Warning

**Risk:** Staff closes session with pending offline transactions
**Mitigation (per CONTEXT.md):** "Warning then allow" - show warning but don't block
**Implementation:** Check for unsynced transactions before close, show warning dialog

### Summary Step UX

**Risk:** Additional step may feel cumbersome
**Mitigation:**
- Make summary step clear and quick
- Single "Confirm" button (not multi-step wizard)
- Allow "Back to count" if adjustments needed

## Standard Stack

No new libraries required - uses existing Blazor/MudBlazor patterns.

| Component | Usage |
|-----------|-------|
| Blazor components | Dialog, forms |
| Tabler CSS | Styling |
| Tabler icons | Icons |
| System.Text.Json | Entity serialization |

## Code Examples

### Summary Table Pattern (from EodSessionDetailDialog)

```razor
<table class="table table-sm">
    <thead>
        <tr>
            <th>Currency</th>
            <th class="text-end">Expected</th>
            <th class="text-end">Actual</th>
            <th class="text-end">Variance</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var item in items)
        {
            <tr class="@GetRowClass(item.Variance)">
                <td>@item.Currency</td>
                <td class="text-end">@FormatCurrency(item.Expected)</td>
                <td class="text-end">@FormatCurrency(item.Actual)</td>
                <td class="text-end">
                    <span class="@GetVarianceClass(item.Variance)">
                        @(item.Variance >= 0 ? "+" : "")@FormatCurrency(item.Variance)
                    </span>
                </td>
            </tr>
        }
    </tbody>
</table>
```

### Variance Class Pattern (used throughout)

```csharp
private static string GetVarianceClass(decimal variance)
{
    if (variance == 0) return "text-success";
    if (variance > 0) return "text-info";     // Over
    return "text-danger";                      // Short
}
```

## Sources

### Primary (HIGH confidence)
- `src/MotoRent.Domain/Entities/TillSession.cs` - Entity structure
- `src/MotoRent.Services/TillService.cs` - Service methods
- `src/MotoRent.Client/Pages/Staff/TillCloseSessionDialog.razor` - Dialog implementation
- `src/MotoRent.Client/Components/Till/ClosingCountPanel.razor` - Counting component
- `.planning/phases/07-till-closing-reconciliation/07-CONTEXT.md` - User decisions

### Secondary (MEDIUM confidence)
- `.planning/phases/06-denomination-counting/06-03-PLAN.md` - Prior phase implementation

## Metadata

**Confidence breakdown:**
- Current state analysis: HIGH - All files verified directly
- Gap analysis: HIGH - Based on actual code vs requirements
- Implementation approach: HIGH - Follows existing patterns

**Research date:** 2026-01-21
**Valid until:** 2026-02-21 (stable domain)
