---
phase: 3
plan: 1
subsystem: till
tags: [blazor, search, dialog, fullscreen, transaction-type]
dependency-graph:
  requires: [phase-2]
  provides: [transaction-search-dialog, transaction-entity-types]
  affects: [03-02]
tech-stack:
  added: []
  patterns: [search-then-select, fullscreen-dialog]
key-files:
  created:
    - src/MotoRent.Client/Pages/Staff/TillTransactionDialog.razor
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.en.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.th.resx
    - src/MotoRent.Domain/Models/TransactionSearchResult.cs
  modified:
    - src/MotoRent.Domain/Entities/TillEnums.cs
    - src/MotoRent.Services/RentalService.cs
    - src/MotoRent.Client/Pages/Staff/Till.razor
    - src/MotoRent.Client/Resources/Pages/Staff/Till.resx
    - src/MotoRent.Client/Resources/Pages/Staff/Till.en.resx
    - src/MotoRent.Client/Resources/Pages/Staff/Till.th.resx
decisions:
  - id: d3-01-1
    summary: Search-then-select pattern with grouped results
    context: Need to support finding bookings and rentals for transaction processing
    choice: Group results into Bookings and Active Rentals sections
    rationale: Clearer UX than mixed results, matches mental model of transaction types
metrics:
  duration: 11m
  completed: 2026-01-20
---

# Phase 3 Plan 1: Transaction Search UI Foundation Summary

**One-liner:** Search-then-select fullscreen dialog with booking/rental grouping and auto-detected transaction types

## What Was Built

### TillTransactionDialog Component
A fullscreen dialog for searching and selecting transactions to process:
- Search by booking reference, customer name, or phone number
- Search active rentals by renter name or rental ID
- Results grouped into **Bookings** and **Active Rentals** sections
- Auto-detects transaction type based on entity status:
  - Booking with balance due -> BookingDeposit
  - Confirmed booking fully paid -> CheckIn
  - Active rental -> RentalPayment (check-out context)
- Shows key details: dates, amounts, balance due, status badges
- Returns `TransactionSearchResult` with entity and detected type

### Domain Model
- `TransactionSearchResult` class to encapsulate search selection
- `TransactionEntityType` enum (Booking, Rental)
- Added `CheckIn` value to `TillTransactionType` enum

### Service Extension
- `SearchActiveRentalsAsync(searchTerm, shopId?)` in RentalService
- Searches active rentals by renter name or rental ID

### Till Page Integration
- Prominent "New Transaction" button above Quick Payments section
- Launches TillTransactionDialog fullscreen
- Transaction type mapping for CheckIn in display methods

## Design Decisions

1. **Search-then-select pattern**: User searches first, then selects from grouped results. This is clearer than a paginated list when staff knows the booking ref or customer name.

2. **Grouped results**: Bookings and Active Rentals shown separately rather than mixed. Matches mental model of "am I processing a booking deposit/check-in?" vs "am I processing a check-out?".

3. **Auto-detect transaction type**: Dialog determines the likely transaction type from entity status (balance due = deposit, confirmed = check-in). Plan 2 will allow override if needed.

## Files Created

| File | Purpose |
|------|---------|
| `TillTransactionDialog.razor` | Search-then-select fullscreen dialog |
| `TillTransactionDialog.resx` | Default localization |
| `TillTransactionDialog.en.resx` | English localization |
| `TillTransactionDialog.th.resx` | Thai localization |
| `TransactionSearchResult.cs` | Result model for dialog selection |

## Files Modified

| File | Changes |
|------|---------|
| `TillEnums.cs` | Added `CheckIn` to `TillTransactionType` |
| `RentalService.cs` | Added `SearchActiveRentalsAsync` method |
| `Till.razor` | Added New Transaction button and dialog method |
| `Till.resx` (all cultures) | Added `NewTransaction`, `TxnCheckIn` keys |

## Deviations from Plan

None - plan executed exactly as written.

## Commits

| Hash | Message |
|------|---------|
| f1a2d6e | feat(03-01): add TillTransactionDialog for transaction search |
| 2f2bc6b | feat(03-01): extend RentalService and TillTransactionType for search |
| 4b96ee3 | feat(03-01): wire TillTransactionDialog to Till page |
| 6c64209 | feat(03-01): add CheckIn transaction type mapping |

## Next Phase Readiness

**Ready for Plan 03-02:** Item confirmation panel

Plan 03-02 will:
- Take `TransactionSearchResult` from this dialog
- Display line items for confirmation
- Allow adjustments to individual items
- Pass confirmed data to payment terminal (Phase 4)

**Dependencies satisfied:**
- Search dialog returns selected entity with transaction type
- CheckIn transaction type now exists for check-in flows
- Service methods available for rental searches

**No blockers identified.**
