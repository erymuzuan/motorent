# Phase 3: Transaction Search & Item Confirmation

**Created:** 2026-01-20
**Status:** Not Started

---

## Goal

Staff can start a transaction by searching for a booking/rental and reviewing/editing items before payment.

---

## Background

### 3 Core Desk Operations

The till supports three primary transaction types based on the booking/rental lifecycle:

1. **Booking Payment** — Customer makes or pays deposit on a new or existing booking
2. **Check-In** — Customer picks up vehicle, add accessories/insurance/extras
3. **Check-Out** — Customer returns vehicle, refunds, damage charges

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| Scoped cart | 1 booking OR 1 rental per receipt (not general POS) |
| Single entry point | "New Transaction" button initiates search |
| Auto-detect type | System determines transaction type from entity status |
| Fullscreen dialog | Dedicated focus for item review before payment |
| Two-column layout | Tablet/PC shows side-by-side; mobile stacks vertically |

### Transaction Type Auto-Detection

| Entity Status | Transaction Type | Line Items |
|---------------|------------------|------------|
| Booking (Reserved) | Booking Deposit | Deposit amount |
| Booking (Confirmed) | Check-In | Rental charges, security deposit, accessories, insurance |
| Rental (Active) | Check-Out | Final charges, damage, refunds |

---

## Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| TXSEARCH-01 | Staff can search for bookings or rentals by reference, customer name, or phone | Must |
| TXSEARCH-02 | System auto-detects transaction type from entity status | Must |
| ITEMS-01 | Staff sees full summary (customer, vehicle, dates, line items) in fullscreen dialog | Must |
| ITEMS-02 | Staff can add/remove accessories from the transaction | Must |
| ITEMS-03 | Staff can change insurance package | Must |
| ITEMS-04 | Staff can apply percentage or fixed discounts with reason | Must |
| ITEMS-05 | Responsive layout (two columns tablet/PC, stacked mobile) | Must |

---

## Success Criteria

1. Staff clicks "New Transaction", searches "John", finds his Reserved booking
2. System shows check-in items (rental charges, security deposit, optional accessories)
3. Staff adds a helmet (฿50/day x 3 days = ฿150) to the line items
4. Staff applies 10% discount with reason "Returning customer"
5. Total updates and staff can proceed to payment

---

## Existing Components to Leverage

### From Phase 2
- `TillSession` - Active session required for transactions
- `TillService` - Transaction recording
- `Till.razor` - Main till page with session management

### Domain Entities
- `Booking` - Reservation with customer, vehicle, dates, status
- `Rental` - Active rental with charges, deposits
- `Renter` - Customer information
- `Motorbike` - Vehicle details
- `Accessory` - Add-on items (helmets, phone holders)
- `Insurance` - Insurance packages

### Existing Search Patterns
- Review `src/MotoRent.Client/Pages/Rentals/` for rental search patterns
- Review `src/MotoRent.Client/Pages/Bookings/` for booking search patterns

---

## UI Components Needed

### New Transaction Search Dialog
- Search input with type-ahead
- Search by: reference number, customer name, phone number
- Results grouped by type (Bookings, Rentals)
- Recent transactions for quick selection

### Item Confirmation Dialog (Fullscreen)
- **Left Column (Tablet/PC)**: Customer info, vehicle, dates
- **Right Column (Tablet/PC)**: Line items, accessories, discounts, total
- **Mobile**: Single column, stacked sections
- Action buttons: "Back", "Add Item", "Apply Discount", "Proceed to Payment"

### Line Item Editor
- Show existing items from booking/rental
- Add accessories from inventory
- Change insurance package
- Apply discount (% or fixed amount with reason)
- Running total with THB formatting

---

## Data Flow

```
[New Transaction Button]
        |
        v
[Search Dialog] -- search --> [Booking/Rental Search Service]
        |
        v
[Select Result] -- load --> [Item Confirmation Dialog]
        |
        v
[Edit Items] -- modify --> [Transaction Line Items]
        |
        v
[Proceed to Payment] --> Phase 4 Payment Terminal
```

---

## Dependencies

- Phase 2 complete (till session infrastructure)
- Booking entity with customer, vehicle, dates
- Rental entity with charges, deposits
- Accessory inventory for add-on items
- Insurance packages for selection

---

## Research Needed

- Search result presentation patterns in MudBlazor
- Fullscreen dialog patterns (MudBlazor `FullScreen` option)
- Responsive two-column layouts with MudGrid

---

## Notes

- Walk-in sales (no booking/rental) deferred to future
- Receipt customization deferred to future
- Focus on core booking/rental transaction flow

---

*Last updated: 2026-01-20*
