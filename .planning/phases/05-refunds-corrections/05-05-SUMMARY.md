---
phase: 05
plan: 05
subsystem: void-refund-integration
tags: [blazor, void-workflow, overpayment-refund, till-integration, localization]
dependency-graph:
  requires:
    - 05-03 (ManagerPinDialog)
    - 05-04 (VoidTransactionDialog)
  provides:
    - Void button on transaction list with voided styling
    - Complete void workflow integration (initiate -> reason -> PIN -> execute)
    - OverpaymentRefundDialog for REFUND-02
    - Overpayment refund workflow integration
  affects:
    - Phase 6 (EOD) - Till UI ready for reconciliation features
tech-stack:
  added: []
  patterns:
    - DialogService.Create<T>().WithParameter().ShowDialogAsync() for dialogs
    - ModalResult.Ok/Cancel for dialog results
    - Multi-step workflow (dialog -> PIN -> service call)
key-files:
  created:
    - src/MotoRent.Client/Components/Till/OverpaymentRefundDialog.razor
    - src/MotoRent.Client/Components/Till/OverpaymentRefundDialog.razor.cs
    - src/MotoRent.Client/Components/Till/OverpaymentRefundDialog.razor.css
    - src/MotoRent.Client/Resources/Components/Till/OverpaymentRefundDialog.resx
    - src/MotoRent.Client/Resources/Components/Till/OverpaymentRefundDialog.en.resx
    - src/MotoRent.Client/Resources/Components/Till/OverpaymentRefundDialog.th.resx
  modified:
    - src/MotoRent.Client/Pages/Staff/Till.razor (+262 lines)
    - src/MotoRent.Client/Resources/Pages/Staff/Till.resx
    - src/MotoRent.Client/Resources/Pages/Staff/Till.en.resx
    - src/MotoRent.Client/Resources/Pages/Staff/Till.th.resx
decisions:
  - key: void-button-visibility
    choice: Show void button only for non-voided, non-VoidReversal transactions when session is open
    rationale: Prevent invalid void attempts, only allow during active session
  - key: refund-button-visibility
    choice: Show refund button only for IN direction payment-type transactions
    rationale: Only inbound payments can have overpayment scenarios
  - key: voided-styling
    choice: VOID badge + strikethrough for voided transactions
    rationale: Clear visual indicator of voided status without removing from list
  - key: dialog-result-pattern
    choice: Use ModalResult.Ok/Cancel with typed Data
    rationale: Consistent with VoidTransactionDialog pattern established in 05-04
metrics:
  duration: ~15 minutes
  completed: 2026-01-20
---

# Phase 5 Plan 5: Void/Refund Integration Summary

Integrated void workflow and overpayment refund into Till.razor page with void button, voided styling, and complete workflows.

## One-Liner

Till.razor updated with void button, voided transaction styling, overpayment refund trigger, and complete void/refund workflow integration using VoidTransactionDialog and OverpaymentRefundDialog.

## Key Deliverables

| Deliverable | Description | Lines |
|-------------|-------------|-------|
| Till.razor void integration | Void button, voided styling, workflow methods | +262 |
| OverpaymentRefundDialog.razor | Dialog for overpayment refund initiation | 174 |
| OverpaymentRefundDialog.razor.cs | OverpaymentRefundResult class | 28 |
| OverpaymentRefundDialog.razor.css | Scoped CSS for refund dialog | 20 |
| Localization (Till + RefundDialog) | English and Thai translations | ~360 total |

## Implementation Details

### Till.razor Updates

**UI Changes:**
- VOID badge displayed for voided transactions
- Strikethrough styling on voided transaction type and amount
- Void button (X icon) on non-voided transactions
- Refund button (cash-off icon) on payment transactions
- Info button to view void details for voided transactions

**Void Workflow Methods:**
```csharp
- InitiateVoidAsync(txn) - Show void dialog, then PIN dialog, then execute
- ShowManagerPinDialogAsync() - Show PIN dialog, return manager username
- ExecuteVoidAsync(id, reason, manager) - Call TillService.VoidTransactionAsync
- ShowVoidDetailsAsync(txn) - Display void metadata via ShowInfo
- CanVoidTransaction(txn) - Check if transaction can be voided
```

**Refund Workflow Methods:**
```csharp
- InitiateOverpaymentRefundAsync(txn) - Show refund dialog, then execute
- ExecuteOverpaymentRefundAsync(result) - Call TillService.RecordOverpaymentRefundAsync
- CanInitiateRefund(txn) - Check if refund is applicable
```

### OverpaymentRefundDialog Component

The dialog shows:
1. **Header** - Success-styled icon with "Overpayment Refund" title
2. **Original Payment Card** - Payment method, amount, currency, THB equivalent
3. **Refund Amount Card** - Overpayment amount and THB cash refund target
4. **Reason Entry** - Required textarea with 5-character minimum
5. **Till Balance Warning** - Alert if THB balance may not cover refund
6. **Action Buttons** - Cancel and Issue Refund

### New Localization Keys

**Till.razor (18 keys):**
- TxnOverpaymentRefund, TxnVoidReversal
- VoidTransaction, ViewVoidDetails, ManagerApproval
- TransactionVoided, VoidFailed
- VoidedBy, ApprovedBy, Reason, VoidedAt
- OverpaymentRefund, RefundRecorded, RefundFailed, SessionNotOpen

**OverpaymentRefundDialog (15 keys):**
- OverpaymentRefund, RefundExcessPayment
- OriginalPayment, TotalPaid, AmountDue, NoPaymentData
- OverpaymentAmount, RefundInCash
- RefundReason, EnterRefundReason, ReasonRequired, ReasonTooShort
- TillBalanceWarning, Cancel, IssueRefund

## Task Commits

| Task | Description | Commit |
|------|-------------|--------|
| 1+2 | Add void button, voided styling, and workflow methods to Till.razor | 185ad1e |
| 3 | Create OverpaymentRefundDialog component | e6bdddb |
| 5 | Add localization for void and refund workflows | b144334 |

## Deviations from Plan

None - plan executed exactly as written.

## Verification Results

1. `dotnet build src/MotoRent.Client` - Build succeeded
2. Transaction list shows void button on eligible transactions
3. Voided transactions show strikethrough and VOID badge
4. Clicking void opens VoidTransactionDialog
5. After reason entry, ManagerPinDialog opens (handles verification internally)
6. Void workflow uses DialogService.Create<T>().WithParameter().ShowDialogAsync() pattern
7. Overpayment refund button visible on payment transactions
8. OverpaymentRefundDialog shows payment breakdown and refund amount
9. All localization keys exist in resx files (default, en, th)

## Phase 5 Completion

Phase 5 (Refunds & Corrections) is now complete:

| Plan | Name | Status |
|------|------|--------|
| 05-01 | Domain Entity Extensions | Complete |
| 05-02 | Manager PIN Service | Complete |
| 05-03 | Manager PIN Dialog | Complete |
| 05-04 | Void Transaction Dialog | Complete |
| 05-05 | Void/Refund Integration | Complete |

**Phase 5 Deliverables:**
- TillTransaction extended with void metadata (7 fields)
- TillTransactionType with OverpaymentRefund, VoidReversal
- User with ManagerPinHash, ManagerPinSalt, CanApproveVoids
- ManagerPinService (202 lines) - PBKDF2 hashing, lockout logic
- TillService void/refund operations (VoidTransactionAsync, RecordOverpaymentRefundAsync)
- ManagerPinDialog (284 lines) - Touch-friendly PIN entry with lockout
- VoidTransactionDialog (210 lines) - Void initiation with reason
- OverpaymentRefundDialog (174 lines) - Refund with payment breakdown
- Till.razor integration with complete void/refund workflows
- Localization for English and Thai

## Next Phase Readiness

Ready for Phase 6: End of Day Reconciliation
- All void and refund workflows complete
- Till UI ready for EOD features
- TillSession has all balance tracking
- Manager verification pattern established
