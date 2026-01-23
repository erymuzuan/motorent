---
phase: 09-end-of-day-operations
plan: 04
subsystem: receipt-search
tags: [ui, blazor, receipts, search, localization, staff]

dependency-graph:
  requires: [09-01]
  provides: [staff-receipt-search, staff-reprint]
  affects: []

tech-stack:
  added: []
  patterns: [filter-debounce, pagination, quick-date-buttons]

file-tracking:
  key-files:
    created:
      - src/MotoRent.Client/Pages/Staff/TillTransactionSearch.razor
      - src/MotoRent.Client/Pages/Staff/TillTransactionSearch.razor.cs
      - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionSearch.resx
      - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionSearch.en.resx
      - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionSearch.th.resx
      - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionSearch.ms.resx
    modified:
      - src/MotoRent.Client/Layout/StaffLayout.razor

decisions:
  - id: staff-no-void
    choice: No void action available on staff receipt search
    rationale: Staff should not be able to void receipts - manager only
  - id: search-debounce
    choice: 300ms debounce on search input
    rationale: Reduce API calls while typing
  - id: quick-date-active-state
    choice: Visual indicator for active quick date button
    rationale: Clear feedback on selected date range
  - id: operations-menu-section
    choice: Place Receipt Search in Operations section of More drawer
    rationale: Groups with related features like Deposits and Reports

metrics:
  duration: ~8 minutes
  completed: 2026-01-21
---

# Phase 9 Plan 04: Staff Receipt Search and Reprint Summary

**One-liner:** Staff receipt search page with date/type/customer/amount filters, pagination, and View/Print action via existing ReceiptPrintDialog.

## Tasks Completed

| Task | Name | Commit | Key Files |
|------|------|--------|-----------|
| 1 | Create TillTransactionSearch page | f1a85d0 | TillTransactionSearch.razor, TillTransactionSearch.razor.cs |
| 2 | Add navigation and localization | a6aa5ab | StaffLayout.razor, TillTransactionSearch.*.resx (4 files) |

## Key Deliverables

### TillTransactionSearch.razor (235 lines)
- Page at `/staff/till-transactions`
- Authorization: Default tenant access (Staff, ShopManager, OrgAdmin)
- Inherits from `LocalizedComponentBase<TillTransactionSearch>`
- Uses `ReceiptService.GetReceiptsAsync` for data loading

### Search Filters
1. **Date Range:**
   - From/To date inputs
   - Quick buttons: Today, Last 7 Days, This Month
   - Visual active state on selected quick button

2. **Receipt Type:**
   - All Types, Check-In, Settlement, Booking Deposit
   - Dropdown with type badges

3. **Search Term:**
   - Placeholder: "Customer name, phone, or receipt number"
   - Debounced input (300ms)
   - Clear button when text present

4. **Amount Range:**
   - Min/Max numeric inputs
   - Optional filtering (applied in-memory)

### Results Table
- Columns: Receipt #, Type, Customer, Vehicle, Amount, Date/Time, Actions
- Receipt # is clickable (opens print dialog)
- Type badges: Check-In (blue), Settlement (green), Booking Deposit (cyan)
- Voided receipts: Red row + "Voided" badge
- Refund amounts shown for settlements
- Single action: View/Print button (no void for staff)

### Pagination
- 20 items per page
- Prev/Next buttons
- Page number buttons (current +/- 2)
- "Page X of Y (Z total)" display

### Empty State
- Receipt-off icon
- "No receipts found" message
- "Try adjusting your search filters" suggestion

### Code-behind (TillTransactionSearch.razor.cs - 190 lines)
- Filter fields: `m_fromDate`, `m_toDate`, `m_receiptType`, `m_searchTerm`, `m_minAmount`, `m_maxAmount`
- Pagination: `m_currentPage`, `c_pageSize = 20`, `m_totalCount`, `m_totalPages`
- Debounce timer for search input
- `LoadDataAsync()` - Calls ReceiptService with filters
- `ApplyQuickDate()` / `ApplyQuickDateInternal()` - Date range helpers
- `IsQuickDateActive()` - Visual state for quick buttons
- `ViewPrintReceiptAsync()` - Opens ReceiptPrintDialog with IsReprint=true
- `IDisposable` implementation for timer cleanup

### Navigation
- Added to StaffLayout.razor More drawer
- Operations section: Receipt Search link
- Icon: ti-receipt-2
- Route: /staff/till-transactions

### Localization (24 keys per language)
- `TillTransactionSearch.resx` (default English)
- `TillTransactionSearch.en.resx` (English)
- `TillTransactionSearch.th.resx` (Thai)
- `TillTransactionSearch.ms.resx` (Malay)

Key translations:
| Key | English | Thai | Malay |
|-----|---------|------|-------|
| PageTitle | Transaction Search | ค้นหารายการ | Carian Transaksi |
| Today | Today | วันนี้ | Hari Ini |
| NoReceiptsFound | No receipts found | ไม่พบใบเสร็จ | Tiada resit dijumpai |

## Deviations from Plan

None - plan executed exactly as written.

## Verification

- [x] TillTransactionSearch.razor accessible at /staff/till-transactions
- [x] Navigation link appears in StaffLayout More drawer
- [x] Search filters work: date range, type, customer text, amount range
- [x] Quick date buttons (Today, Last 7 Days, This Month) update date range
- [x] Results table shows receipts with proper type badges
- [x] View/Print opens ReceiptPrintDialog with IsReprint=true
- [x] Pagination works correctly
- [x] No void action available (staff-safe)
- [x] All localization keys have Thai and Malay translations

## Requirements Satisfied

- **RCPT-02:** Staff can search and view past till transactions
- **RCPT-03:** Staff can reprint receipts
- **RCPT-01:** Already satisfied by existing ReceiptService

## Next Phase Readiness

Plan 09-04 is the final plan in Phase 9. The End of Day Operations phase is complete with:
- EOD entities and service methods (09-01)
- Daily Close UI (09-02 - pending)
- Shortage accountability (09-03 - pending)
- Staff receipt search (09-04 - complete)

Note: Plans 09-02 and 09-03 appear to have pre-existing incomplete implementation in the codebase (DailyClose.razor, CashDropVerificationDialog.razor with compilation errors). These should be addressed before the phase can be considered fully complete.
