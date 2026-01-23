---
phase: 3
plan: 2
subsystem: till
tags: [blazor, item-confirmation, line-items, accessories, insurance, discount]
dependency-graph:
  requires: [03-01]
  provides: [item-confirmation-panel, transaction-line-items]
  affects: [04-01]
tech-stack:
  added: []
  patterns: [two-column-responsive, inline-editing, running-totals]
key-files:
  created:
    - src/MotoRent.Domain/Entities/TransactionLineItem.cs
  modified:
    - src/MotoRent.Client/Pages/Staff/TillTransactionDialog.razor
    - src/MotoRent.Domain/Models/TransactionSearchResult.cs
    - src/MotoRent.Domain/Entities/ReceiptStatus.cs
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.en.resx
    - src/MotoRent.Client/Resources/Pages/Staff/TillTransactionDialog.th.resx
decisions:
  - id: d3-02-1
    summary: Two-column responsive layout for item confirmation
    context: Need to show both summary and line items on tablet/desktop
    choice: Left column (5/12) for customer/vehicle summary, right column (7/12) for line items and actions
    rationale: Staff sees all relevant info at once without scrolling; stacks on mobile
  - id: d3-02-2
    summary: Inline editing panels instead of modal dialogs
    context: Need to allow quick accessory/insurance/discount changes
    choice: Show inline collapsible panels for each action type
    rationale: Faster workflow, no context switch, only one panel open at a time
  - id: d3-02-3
    summary: TransactionLineItem as working model separate from ReceiptItem
    context: Need editable line items during transaction before receipt creation
    choice: Create TransactionLineItem with edit-specific properties (CanRemove, AccessoryId, InsuranceId)
    rationale: Keeps ReceiptItem clean for final persistence, allows rich editing UX
metrics:
  duration: 8m
  completed: 2026-01-20
---

# Phase 3 Plan 2: Item Confirmation Panel Summary

**One-liner:** Two-column fullscreen item confirmation with inline accessory/insurance/discount editing and running totals

## What Was Built

### TillTransactionDialog - Step 2: Item Confirmation
Extended the dialog with a full item confirmation UI:

**Left Column - Customer & Booking Summary:**
- Transaction type badge (Booking Deposit / Check-In / Check-Out)
- Customer name and phone
- Vehicle name
- Rental period with day count
- Booking reference (for bookings)

**Right Column - Line Items & Actions:**
- Action buttons: Add Accessory, Change Insurance, Apply Discount
- Inline accessory selector from shop inventory
- Inline insurance selector with current selection highlighted
- Discount form with percentage/fixed toggle and required reason
- Line items table with category icons, descriptions, amounts
- Subtotal, Discount total, Grand Total display
- "Proceed to Payment" button shows grand total

### TransactionLineItem Model
Working model for transaction editing:
- `ItemId` - Unique identifier for list manipulation
- `Category` - Uses ReceiptItemCategory constants
- `Description`, `Detail` - Display text
- `Quantity`, `UnitPrice`, `Amount` - Pricing
- `AccessoryId`, `InsuranceId` - Foreign key tracking
- `CanRemove` - UI flag for removable items
- `DiscountReason` - Required for discount items
- `IsDeduction` - Negative amount flag

### TransactionSearchResult Extended
Added properties for passing confirmed items to payment:
- `LineItems` - List of confirmed TransactionLineItem
- `GrandTotal` - Calculated total after discounts

### Localization (EN/TH)
Added 24 new localization keys for item confirmation UI including:
- Customer, Vehicle, RentalPeriod, Days, BookingRef
- LineItems, Subtotal, Discount, Total
- AddAccessory, ChangeInsurance, ApplyDiscount
- SelectAccessory, SelectInsurance, NoInsurance
- Percentage, FixedAmount, DiscountReason, DiscountReasonRequired
- ProceedToPayment, Back, RemoveItem
- SecurityDeposit, DepositRefund

## Design Decisions

1. **Two-column responsive layout**: Summary on left (col-5), line items on right (col-7). Stacks to single column on mobile via Bootstrap grid.

2. **Inline editing panels**: Accessory, insurance, and discount forms appear inline rather than as separate modals. Only one panel can be open at a time, reducing visual clutter.

3. **Running totals**: Subtotal, discount, and grand total recalculate immediately on any change. Grand total shown in footer payment button.

4. **Category icons**: Each line item shows an icon based on category (motorbike, shield, helmet, wallet, etc.) for quick visual scanning.

5. **Removable items**: Only added accessories and discounts are removable. Core rental, original insurance, and deposit are not removable to prevent accidental errors.

## Files Created

| File | Purpose |
|------|---------|
| `TransactionLineItem.cs` | Working model for editable line items |

## Files Modified

| File | Changes |
|------|---------|
| `TillTransactionDialog.razor` | Added Step 2 UI (750+ new lines), line item building, editing methods |
| `TransactionSearchResult.cs` | Added LineItems and GrandTotal properties |
| `ReceiptStatus.cs` | Added LateFee constant to ReceiptItemCategory |
| `TillTransactionDialog.resx` | Added 24 localization keys |
| `TillTransactionDialog.en.resx` | Added 24 English translations |
| `TillTransactionDialog.th.resx` | Added 24 Thai translations |

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 1 - Bug] Rental entity property names**
- **Found during:** Task 2
- **Issue:** Plan assumed `DaysRented`, `DailyRate`, `InsuranceRate`, `InsuranceName`, `DepositAmount` properties on Rental that don't exist
- **Fix:** Calculate days from date range, use `RentalRate`, handle insurance as placeholder since rate not stored on rental
- **Commit:** 9a00ad9

**2. [Rule 3 - Blocking] ReceiptItemCategory already exists**
- **Found during:** Task 1
- **Issue:** Created duplicate ReceiptItemCategory.cs, but class already defined in ReceiptStatus.cs
- **Fix:** Removed duplicate file, added missing LateFee constant to existing class
- **Commit:** 626e3c6

## Commits

| Hash | Message |
|------|---------|
| 626e3c6 | feat(03-02): add TransactionLineItem model and LateFee category |
| 9a00ad9 | feat(03-02): add item confirmation UI with editing capabilities |

## Next Phase Readiness

**Ready for Phase 4:** Payment Terminal

Phase 4 will:
- Receive `TransactionSearchResult` with confirmed `LineItems` and `GrandTotal`
- Display payment terminal with THB keypad
- Handle foreign currency denomination counting
- Support split payments across methods/currencies
- Calculate change in THB

**Dependencies satisfied:**
- Item confirmation produces final line items list
- Grand total calculated for payment target
- Transaction type identified for receipt generation

**No blockers identified.**
