# Phase 5: Refunds & Corrections - Context

**Gathered:** 2026-01-20
**Status:** Ready for planning

<domain>
## Phase Boundary

Staff can process security deposit refunds at check-out, overpayment refunds, and void transactions with manager PIN approval. All refunds are issued in THB cash. Voided transactions are preserved for audit trail with compensating entries.

</domain>

<decisions>
## Implementation Decisions

### Refund Workflow
- Refunds initiated from original transaction (not a separate "Issue Refund" action)
- Overpayment refund amount is auto-calculated (not editable by staff)
- Summary dialog shows: original payment breakdown, refund amount, reason — confirm or cancel
- Reason required for manual refunds (overpayment), auto-reason for security deposit refunds
- Staff can issue refunds directly (no manager approval needed)
- Security deposit refund is part of the check-out flow, not a separate action after
- All refunds issued as THB cash only
- Warning shown if till has insufficient THB cash, but refund still allowed (till may go negative)

### Void Authorization
- Manager enters PIN via touch-friendly numeric keypad dialog
- 3 wrong attempts locks the void request for 5 minutes
- Manager PIN stored in user profile (each manager has their own PIN)

### Audit Trail Display
- Voided transactions shown with strikethrough text + "VOID" badge, inline in transaction lists
- Summary of original transaction preserved: original amount, type, plus void metadata (who voided, when, why, who approved)
- Reports show breakdown: Gross, Voids, Net (voided transactions shown separately, not hidden)
- Only managers can view void history and approvals

### Currency Handling
- Refunds use exchange rate from original payment (rate already stored on payment record)
- Split payments (e.g., THB + USD): sum all to THB equivalent, refund total in THB
- Voiding foreign currency payment: reverse exact currencies from till (if $50 USD received, void removes $50 USD)
- Staff sees original payment breakdown in refund dialog: "Original: 1,000 + $50 (=1,775) → Refund: 2,775 THB"

### Claude's Discretion
- PIN keypad visual design (number layout, size)
- Exact void badge styling
- Lockout UI feedback
- Refund dialog layout details

</decisions>

<specifics>
## Specific Ideas

- Exchange rate provider for MVP: mock rate provider returning buy rates based on denominations (rates differ by denomination)
- This is separate from refund logic — refunds use the rate already stored on the original payment

</specifics>

<deferred>
## Deferred Ideas

- Denomination-based exchange rate provider (mentioned during discussion, separate from this phase)
- Card refund to original card (all refunds are THB cash for now)
- Threshold-based manager approval for large refunds

</deferred>

---

## Requirements Reference

| ID | Requirement | Coverage |
|----|-------------|----------|
| REFUND-01 | Security deposit refunds at check-out | EXISTING - CheckOutDialog.razor already implements this via RecordDepositRefundToTillAsync |
| REFUND-02 | Overpayment refunds when customer pays too much | Plan 05-05: OverpaymentRefundDialog + TillService.RecordOverpaymentRefundAsync |
| VOID-01 | Transaction reversals require manager approval | Plans 05-03 to 05-05: ManagerPinDialog + VoidTransactionDialog |
| VOID-02 | Manager can approve via PIN entry or session authentication | Plan 05-02: ManagerPinService, Plan 05-03: ManagerPinDialog |
| VOID-03 | Voided transactions preserved for audit trail | Plan 05-01: TillTransaction.IsVoided and void metadata fields |

---

## Existing Implementation Notes

### REFUND-01: Security Deposit Refunds (Already Implemented)

Per research, `CheckOutDialog.razor` already implements security deposit refunds during the check-out flow:
- Calls `TillService.RecordDepositRefundToTillAsync`
- Integrated into the check-out workflow
- No additional work required for this requirement

This phase focuses on:
1. Overpayment refunds (REFUND-02) - NEW
2. Transaction voids with manager approval (VOID-01, VOID-02, VOID-03) - NEW

---

*Phase: 05-refunds-corrections*
*Context gathered: 2026-01-20*
