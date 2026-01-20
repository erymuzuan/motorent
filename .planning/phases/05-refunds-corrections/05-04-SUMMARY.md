---
phase: 05
plan: 04
subsystem: void-ui
tags: [blazor, void-dialog, till-transaction, localization]
dependency-graph:
  requires:
    - 05-02 (TillService.VoidTransactionAsync)
  provides:
    - VoidTransactionDialog (transaction display and reason collection)
    - VoidTransactionResult (transfer object for void workflow)
  affects:
    - 05-05 (Overpayment Refund) - may integrate void workflow
tech-stack:
  added: []
  patterns:
    - LocalizedDialogBase for dialog components
    - Code-behind for shared result class
    - Effect preview showing balance impact
key-files:
  created:
    - src/MotoRent.Client/Components/Till/VoidTransactionDialog.razor
    - src/MotoRent.Client/Components/Till/VoidTransactionDialog.razor.cs
    - src/MotoRent.Client/Components/Till/VoidTransactionDialog.razor.css
    - src/MotoRent.Client/Resources/Components/Till/VoidTransactionDialog.resx
    - src/MotoRent.Client/Resources/Components/Till/VoidTransactionDialog.en.resx
    - src/MotoRent.Client/Resources/Components/Till/VoidTransactionDialog.th.resx
  modified: []
decisions:
  - key: code-behind-for-result-class
    choice: VoidTransactionResult in separate .cs file
    rationale: Generic base class needs type at compile time, classes defined in razor not visible
  - key: effect-preview
    choice: Show balance impact before confirmation
    rationale: Staff must understand void effect on till before proceeding
  - key: minimum-reason-length
    choice: 5 characters minimum
    rationale: Prevent meaningless reasons while not being overly restrictive
metrics:
  duration: ~10 minutes
  completed: 2026-01-20
---

# Phase 5 Plan 4: Void Transaction Dialog Summary

Dialog for staff to initiate void of a till transaction with reason entry and original transaction display.

## One-Liner

VoidTransactionDialog showing original transaction details, collecting void reason, and previewing balance impact.

## Key Deliverables

| Deliverable | Description | Lines |
|-------------|-------------|-------|
| VoidTransactionDialog.razor | Dialog showing original transaction and collecting void reason | 210 |
| VoidTransactionDialog.razor.cs | VoidTransactionResult class for void workflow | 19 |
| VoidTransactionDialog.razor.css | Scoped CSS for dialog styling | 31 |
| Localization (3 files) | English and Thai translations | 129 each |

## Implementation Details

### VoidTransactionDialog Component

The dialog shows:
1. **Header** - Danger-styled icon with "Void Transaction" title
2. **Original Transaction Card** - Type, direction, amount, time, description
3. **Warning Alert** - Manager approval required notice
4. **Reason Entry** - Required textarea with 5-character minimum
5. **Effect Summary** - Shows how till balance will change
6. **Action Buttons** - Cancel and Void Transaction

### Foreign Currency Display

For non-THB transactions, the dialog shows both:
- Original amount in foreign currency
- THB equivalent based on recorded exchange rate

### Validation

- Reason is required
- Minimum 5 characters
- Error message displayed inline

### Result Object

```csharp
public class VoidTransactionResult
{
    public int TransactionId { get; set; }
    public string Reason { get; set; } = "";
}
```

The parent component receives this result and handles:
1. Showing ManagerPinDialog for approval
2. Calling TillService.VoidTransactionAsync
3. Displaying success/error feedback

## Task Commits

| Task | Description | Commit |
|------|-------------|--------|
| 1 | Create VoidTransactionDialog component | 1326924 |
| 2 | Add VoidTransactionDialog CSS | ad2de44 |
| 3 | Add localization resources | 8131245 |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] VoidTransactionResult class in code-behind file**
- **Found during:** Task 1
- **Issue:** Class defined in razor file not visible to generic base class at compile time
- **Fix:** Created VoidTransactionDialog.razor.cs with VoidTransactionResult class
- **Files modified:** VoidTransactionDialog.razor.cs (new file)
- **Commit:** 1326924

## Verification Results

1. `dotnet build src/MotoRent.Client` - 0 errors, 78 warnings (pre-existing)
2. VoidTransactionDialog.razor exists with 210 lines (min: 80)
3. VoidTransactionDialog.razor.css has styling
4. All localization files (default, en, th) exist in Resources/Components/Till/
5. Dialog shows original transaction details
6. Reason validation implemented (required, min 5 chars)

## Next Phase Readiness

Ready for Plan 05-05: Overpayment Refund Dialog
- VoidTransactionDialog complete for void workflow
- ManagerPinDialog ready for approval flow
- TillService has all void/refund operations
