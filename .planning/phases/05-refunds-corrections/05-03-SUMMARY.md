---
phase: 05
plan: 03
subsystem: auth-ui
tags: [blazor, pin-dialog, manager-approval, localization]
dependency-graph:
  requires:
    - 05-02 (ManagerPinService)
  provides:
    - ManagerPinDialog (touch-friendly PIN entry)
    - Manager loading from CoreDataContext
    - Lockout display with countdown
  affects:
    - 05-04 (Void Dialog) - will use ManagerPinDialog for approval
    - 05-05 (Overpayment Refund) - will use ManagerPinDialog for approval
tech-stack:
  added: []
  patterns:
    - LocalizedDialogBase for dialog components
    - Inherited RequestContext from base
    - System.Timers.Timer for lockout countdown
key-files:
  created:
    - src/MotoRent.Client/Components/Auth/ManagerPinDialog.razor
    - src/MotoRent.Client/Components/Auth/ManagerPinDialog.razor.css
    - src/MotoRent.Client/Resources/Components/Auth/ManagerPinDialog.resx
    - src/MotoRent.Client/Resources/Components/Auth/ManagerPinDialog.en.resx
    - src/MotoRent.Client/Resources/Components/Auth/ManagerPinDialog.th.resx
  modified: []
decisions:
  - key: use-object-for-dialog-base
    choice: LocalizedDialogBase<object, T>
    rationale: String does not satisfy new() constraint required by base class
  - key: inherit-request-context
    choice: Use inherited RequestContext from base class
    rationale: MotoRentComponentBase already injects IRequestContext
  - key: timer-for-lockout
    choice: System.Timers.Timer for countdown
    rationale: Provides 1-second intervals for UI countdown
metrics:
  duration: ~15 minutes
  completed: 2026-01-20
---

# Phase 5 Plan 3: Manager PIN Dialog Summary

Touch-friendly PIN entry dialog for manager void approval with lockout handling.

## One-Liner

PIN keypad dialog with 4-dot display, manager selection, and lockout countdown using ManagerPinService.

## What Was Built

### ManagerPinDialog Component (284 lines)

**Location:** `src/MotoRent.Client/Components/Auth/ManagerPinDialog.razor`

Touch-friendly dialog for manager PIN entry:

- **4 PIN dots** - Visual feedback showing entry progress
- **Numeric keypad** - 3x4 grid of circular buttons (1-9, 0, backspace)
- **Manager selection** - Dropdown when multiple managers available
- **Single manager display** - Shows "Approving as: [name]" when only one
- **No managers warning** - Alert when no managers have PIN configured
- **Lockout display** - Shows countdown timer when locked out
- **Auto-verify** - Verifies PIN immediately when 4 digits entered

**Key implementation details:**

```csharp
// Loads managers with PIN configured and ManagementRoles
var usersResult = await CoreDataContext.LoadAsync(
    CoreDataContext.CreateQuery<User>()
        .Where(u => u.ManagerPinHash != null && u.ManagerPinHash != ""),
    page: 1, size: 50, includeTotalRows: false);

// Filters to ManagementRoles in current organization
m_availableManagers = usersResult.ItemCollection
    .Where(u => u.AccountCollection.Any(a =>
        a.AccountNo == accountNo &&
        a.Roles.Any(r => UserAccount.ManagementRoles.Contains(r))))
    .ToList();
```

### Scoped CSS (100 lines)

**Location:** `src/MotoRent.Client/Components/Auth/ManagerPinDialog.razor.css`

Styling consistent with ThbKeypadPanel:

- 3-column grid for numeric keypad
- Circular buttons with 60px minimum height
- Hover/active states for touch feedback
- PIN dots with filled/empty states
- Lockout pulse animation

### Localization Resources

**Locations:**
- `Resources/Components/Auth/ManagerPinDialog.resx` (default)
- `Resources/Components/Auth/ManagerPinDialog.en.resx` (English)
- `Resources/Components/Auth/ManagerPinDialog.th.resx` (Thai)

**10 localized strings:**
- ManagerApprovalRequired, EnterPinToApprove
- TooManyAttempts, SecondsRemaining
- SelectManager, NoManagerSelected, Cancel
- ApprovingAs, LoadingManagers, NoManagersWithPin

## Technical Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Dialog base class | `LocalizedDialogBase<object, T>` | `string` doesn't satisfy `new()` constraint |
| RequestContext | Use inherited from base | `MotoRentComponentBase` already injects it |
| Lockout timer | `System.Timers.Timer` | Provides reliable 1-second intervals for countdown |
| PIN length | Fixed 4 digits | Standard PIN length, matches service expectation |
| Auto-verify | Verify on 4th digit | Faster UX, no "Submit" button needed |

## Verification Results

| Check | Status |
|-------|--------|
| `dotnet build src/MotoRent.Client` compiles | Pass |
| ManagerPinDialog.razor > 100 lines | Pass (284 lines) |
| CSS contains `keypad-grid` | Pass |
| Localization files exist (default, en, th) | Pass |
| Lockout display with countdown | Pass |
| Manager loading from CoreDataContext | Pass |

## Commits

| Commit | Type | Description |
|--------|------|-------------|
| c4e84c0 | feat | Create ManagerPinDialog component |
| 1bad2fc | style | Add ManagerPinDialog scoped CSS |
| 5f20ed7 | chore | Add localization resources |

## Deviations from Plan

None - plan executed exactly as written.

## Usage Example

```csharp
// In calling dialog (e.g., VoidTransactionDialog)
private async Task<string?> RequestManagerApprovalAsync()
{
    var result = await ModalService.ShowAsync<ManagerPinDialog>(
        "Manager Approval",
        new ModalOptions { Size = ModalSize.Small });

    if (result.Cancelled)
        return null;

    return result.Data as string; // Manager username
}
```

## Next Phase Readiness

**Ready for 05-04 (Void Dialog & Workflow):**
- ManagerPinDialog is complete and tested
- Can be invoked via ModalService
- Returns manager username on success for audit trail
- Handles lockout gracefully

**Integration points:**
- `ManagerPinService.VerifyPin` - Direct service call
- `CoreDataContext.LoadAsync<User>` - Manager query
- `ModalService.Close(ModalResult.Ok())` - Returns result
