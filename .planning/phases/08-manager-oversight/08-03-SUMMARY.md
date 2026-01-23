---
phase: 08-manager-oversight
plan: 03
subsystem: manager-dashboard
tags: [handover-report, sales-clearing-journal, print, localization]

dependency-graph:
  requires: [08-01]
  provides: [handover-report-dialog, sales-clearing-journal]
  affects: [manager-dashboard-integration]

tech-stack:
  added: []
  patterns: [print-preview-dialog, ledger-style-table, currency-formatting]

key-files:
  created:
    - src/MotoRent.Client/Pages/Manager/HandoverReportDialog.razor
    - src/MotoRent.Client/Resources/Pages/Manager/HandoverReportDialog.resx
    - src/MotoRent.Client/Resources/Pages/Manager/HandoverReportDialog.en.resx
    - src/MotoRent.Client/Resources/Pages/Manager/HandoverReportDialog.th.resx
    - src/MotoRent.Client/Resources/Pages/Manager/HandoverReportDialog.ms.resx
  modified:
    - src/MotoRent.Services/TillService.transaction.cs

decisions:
  - GetTransactionsForSessionAsync orders chronologically for handover report display
  - Cash payment types aggregated (RentalPayment, BookingDeposit, CheckIn, etc.) per currency
  - Ledger format with icon + description + Credit/Debit columns matches reference design
  - Print via browser print() for PDF generation
  - Variance breakdown shown only when multiple currencies have variance

metrics:
  duration: ~15 minutes
  completed: 2026-01-21
---

# Phase 8 Plan 3: Handover Report Dialog Summary

**One-liner:** Sales clearing journal dialog with ledger-style Credit/Debit columns, multi-currency support, and browser print for PDF

## Key Deliverables

### HandoverReportDialog.razor (474 lines)
- **Sales clearing journal format** matching sc.png reference design
- **Header section:** Report title, staff name, date, open/close times
- **Journal table:** Description (with icons), Credit column, Debit column
- **Opening Float** as first credit entry
- **Credits (inflows):**
  - Cash payments per currency (THB, USD, EUR, CNY)
  - Card payments
  - PromptPay
  - Bank transfers
  - Float top ups
- **Debits (outflows):**
  - Cash shortages per currency
  - Customer refunds (deposit + overpayment)
  - Cash drops to safe
  - Expenses (petty cash, fuel, commission)
  - Change given
- **Totals row:** Sum of credits and debits
- **Closing summary:** Expected vs Actual balance
- **Variance display:** Alert with color coding (info/warning based on over/short)
- **Multi-currency variance breakdown** when applicable
- **Verification status badge** for verified sessions
- **Print button:** Triggers browser print dialog for PDF export
- **Screen/print separation:** `d-print-none` and `d-print-block` classes

### TillService.GetTransactionsForSessionAsync
- Dedicated method for handover report data loading
- Orders transactions chronologically (ascending by time)
- Distinct from GetTransactionsAsync which orders descending

### Localization (27 keys x 4 files)
- **English:** Default values
- **Thai:** Full translation
- **Malay:** Full translation
- Key terms: SalesClearingJournal, ShiftHandoverReport, CashPayment, CardPayments, CashShortage, CustomerRefunds, CashDropToSafe, etc.

## Commits

| Task | Commit | Description |
|------|--------|-------------|
| Task 2 | 54bb2a6 | Add GetTransactionsForSessionAsync method |
| Task 1 | 38bb568 | Add HandoverReportDialog with sales clearing journal |

## Technical Notes

### Print Architecture
- Browser native print dialog via `JSRuntime.InvokeVoidAsync("print")`
- Two render fragments: screen preview (d-print-none) and print version (d-print-block)
- No external PDF library required - browser handles PDF generation

### Currency Formatting
- THB: Integer format (N0)
- Foreign currencies: Two decimal places (N2)

### Transaction Type Mapping
Cash payments include: RentalPayment, BookingDeposit, CheckIn, SecurityDeposit, DamageCharge, LateFee, Surcharge, MiscellaneousIncome

## Verification

- [x] `dotnet build` succeeds
- [x] HandoverReportDialog component created (474 lines)
- [x] Ledger-style table with Description, Credit, Debit columns
- [x] All credit/debit categories implemented
- [x] Print button triggers browser print
- [x] Localization: English, Thai, Malay (27 keys each)

## Deviations from Plan

None - plan executed exactly as written.

## Next Phase Readiness

Ready for Plan 08-02 (Manager Dashboard UI) if not yet complete, or integration testing.
Manager can now view detailed shift handover reports for any closed session.
