# Phase 3: Transaction Search & Item Confirmation - Research

**Researched:** 2026-01-20
**Domain:** Till Transaction Flow, Booking/Rental Search, Line Item Management
**Confidence:** HIGH

## Summary

Phase 3 builds on the existing Till infrastructure (Phase 2) to enable staff to search for bookings/rentals and review/edit line items before payment. The codebase already has robust patterns for:

1. **Booking/Rental Search** - `TillBookingDepositDialog.razor` demonstrates the search-then-select pattern with customer name, reference, and phone search
2. **Fullscreen Dialogs** - The custom modal system (`ModalContainer.razor`, `ModalFluent.cs`) fully supports `ModalSize.Fullscreen`
3. **Entity Structure** - `Booking`, `Rental`, `Accessory`, and `Insurance` entities have complete structures with pricing fields
4. **Transaction Recording** - `TillService` and `ReceiptService` handle transaction/receipt creation

**Key Finding:** The existing `TillBookingDepositDialog.razor` (296 lines) is a working search-and-payment dialog that can serve as a template. However, it only handles booking deposits - Phase 3 needs to expand this to support all three transaction types (Booking Payment, Check-In, Check-Out) with item editing capabilities.

**Primary recommendation:** Create a new unified `TillTransactionDialog.razor` that combines search, transaction type auto-detection, and fullscreen item confirmation. Leverage existing search patterns from `TillBookingDepositDialog.razor` and dialog infrastructure from `ModalFluent.cs`.

## Standard Stack

The established libraries/tools for this domain:

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| Tabler CSS | (bundled) | Modal dialogs with `modal-fullscreen` class | Already used in `ModalContainer.razor` |
| Custom Modal System | N/A | `ModalFluent<T>`, `ModalSize.Fullscreen` | Project-specific, fully supports fullscreen |
| Bootstrap 5 | (bundled) | Grid layout for responsive two-column | Already used throughout project |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `DialogService` | N/A | Fluent dialog creation | `.Create<T>().WithFullscreen().ShowDialogAsync()` |
| `BookingService` | N/A | Booking search and management | Search by ref, customer, phone |
| `RentalService` | N/A | Rental search and management | Search active rentals |
| `AccessoryService` | N/A | Load available accessories | Add accessories to transaction |
| `InsuranceService` | N/A | Load insurance packages | Change insurance selection |
| `ReceiptService` | N/A | Generate receipts | After payment confirmation |

### No Alternatives Needed
The project has established patterns - no need to evaluate alternatives.

## Architecture Patterns

### Recommended Dialog Structure
```
TillTransactionDialog.razor (Fullscreen)
├── Search Step (reuse TillBookingDepositDialog pattern)
│   ├── Search input with Enter key support
│   ├── Recent items list
│   └── Search results grouped by type
├── Item Confirmation Step (new)
│   ├── Left Column: Customer/Vehicle/Dates summary
│   ├── Right Column: Line items with edit capabilities
│   └── Footer: Total and "Proceed to Payment" button
└── Mobile: Single column, stacked layout
```

### Pattern 1: Two-Step Dialog with Selection State
**What:** Dialog progresses from search to item confirmation using `m_selectedBooking`/`m_selectedRental` state
**When to use:** When user must first find an entity, then work with it
**Example (from TillBookingDepositDialog.razor):**
```csharp
// State for two-step flow
private Booking? m_selectedBooking;
private List<Booking> m_searchResults = [];

// Step 1: Search/Select
@if (m_selectedBooking is null)
{
    // Search UI
}
else
{
    // Item confirmation UI (Step 2)
}
```

### Pattern 2: Search with Debounce and Enter Key
**What:** Search input with Enter key trigger and optional debounce
**When to use:** For text search fields
**Example (from TillBookingDepositDialog.razor):**
```csharp
private async Task OnSearchKeyDown(KeyboardEventArgs e)
{
    if (e.Key == "Enter")
    {
        await SearchBookingsAsync();
    }
}
```

### Pattern 3: Fullscreen Dialog Invocation
**What:** Using fluent API to create fullscreen dialogs
**When to use:** For complex workflows requiring focused attention
**Example (from Till.razor):**
```csharp
var result = await this.DialogService.Create<TillBookingDepositDialog>(Localizer["BookingDeposit"])
    .WithParameter(x => x.SessionId, this.m_session.TillSessionId)
    .WithParameter(x => x.ShopId, this.m_shopId)
    .WithFullscreen()
    .ShowDialogAsync();
```

### Pattern 4: Transaction Type Auto-Detection
**What:** Determine transaction type from entity status
**When to use:** When loading booking/rental for transaction
**Example (new pattern to implement):**
```csharp
private TillTransactionType DetectTransactionType(Booking? booking, Rental? rental)
{
    if (booking is not null)
    {
        return booking.Status switch
        {
            BookingStatus.Pending => TillTransactionType.BookingDeposit,
            BookingStatus.Confirmed => TillTransactionType.CheckIn,
            _ => TillTransactionType.BookingDeposit
        };
    }

    if (rental is not null && rental.Status == "Active")
    {
        return TillTransactionType.CheckOut;
    }

    throw new InvalidOperationException("Unable to detect transaction type");
}
```

### Pattern 5: Line Item Model
**What:** Use `ReceiptItem` as the line item model for transactions
**When to use:** Building transaction line items before payment
**Example (existing ReceiptItem structure):**
```csharp
public class ReceiptItem
{
    public string Category { get; set; }      // "Rental", "Insurance", "Accessory", "Deposit", "Discount"
    public string Description { get; set; }   // "Honda PCX 160", "Basic Insurance", "Helmet"
    public string? Detail { get; set; }       // "3 days @ 500/day"
    public decimal Quantity { get; set; }     // Days or units
    public decimal UnitPrice { get; set; }    // Per day/unit
    public decimal Amount { get; set; }       // Total for line
    public bool IsDeduction { get; set; }     // For discounts
    public int SortOrder { get; set; }
}
```

### Anti-Patterns to Avoid
- **Multiple entry points for transactions:** Use single "New Transaction" button, not separate buttons per type
- **Manual transaction type selection:** Auto-detect from entity status instead
- **Building item logic in the dialog:** Extract to a service/helper for testability
- **Ignoring existing TillBookingDepositDialog patterns:** Reuse the search/select UI instead of reinventing

## Don't Hand-Roll

Problems that look simple but have existing solutions:

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Dialog fullscreen | Custom fullscreen CSS | `ModalSize.Fullscreen` in `ModalOptions` | Already implemented in `ModalContainer.razor` |
| Search bookings | New search service | `BookingService.GetBookingsAsync()` with `searchTerm` | Supports customer name search |
| Search by ref | New lookup method | `BookingService.GetBookingByRefAsync()` | Exact ref match |
| Load accessories | Direct DB query | `AccessoryService.GetAvailableAccessoriesAsync()` | Returns available items for shop |
| Load insurance | Direct DB query | `InsuranceService.GetActiveInsurancesAsync()` | Returns active packages |
| Line item structure | New DTO | `ReceiptItem` class | Already has all fields needed |
| Receipt categories | Hardcoded strings | `ReceiptItemCategory` constants | `Rental`, `Insurance`, `Accessory`, `Discount` |
| Transaction types | New enum | `TillTransactionType` enum | `BookingDeposit`, `RentalPayment`, etc. |

**Key insight:** The existing receipt infrastructure (`Receipt`, `ReceiptItem`, `ReceiptPayment`) is designed for exactly this use case. Build line items using `ReceiptItem` and create receipts via `ReceiptService`.

## Common Pitfalls

### Pitfall 1: Ignoring Existing Dialog Patterns
**What goes wrong:** Creating inconsistent dialog UX
**Why it happens:** Not reviewing existing Till dialogs before building new ones
**How to avoid:** Study `TillBookingDepositDialog.razor` as the template
**Warning signs:** Dialog looks different from Phase 2 dialogs

### Pitfall 2: Forgetting Mobile Layout
**What goes wrong:** Two-column layout breaks on mobile
**Why it happens:** Only testing on desktop
**How to avoid:** Use Bootstrap responsive classes: `col-md-6` for desktop columns, stacks on mobile
**Warning signs:** Layout specified without responsive breakpoints

### Pitfall 3: Not Using Existing Entity Calculations
**What goes wrong:** Duplicate calculation logic for totals
**Why it happens:** Not noticing calculated properties on entities
**How to avoid:** Use `Booking.Days`, `Booking.BalanceDue`, `BookingItem.ItemTotal`
**Warning signs:** Manual date subtraction, manual total calculation

### Pitfall 4: Hardcoding Transaction Types
**What goes wrong:** Logic duplicated across conditions
**Why it happens:** Not using the enum properly
**How to avoid:** Use `TillTransactionType` enum and switch expressions
**Warning signs:** String comparisons for "BookingDeposit", "CheckIn", etc.

### Pitfall 5: Discount Without Reason
**What goes wrong:** Discounts applied without audit trail
**Why it happens:** Skipping the reason field
**How to avoid:** Make discount reason required in UI
**Warning signs:** Discount line items without notes/description

### Pitfall 6: Modifying Booking/Rental Directly
**What goes wrong:** Changes not persisted or audited
**Why it happens:** Modifying entity properties without using service
**How to avoid:** Use `BookingService` methods that add to `ChangeHistory`
**Warning signs:** Direct property assignment without service call

## Code Examples

Verified patterns from official sources:

### Search Input with Enter Key (from TillBookingDepositDialog.razor)
```html
<div class="input-group">
    <input type="text" class="form-control"
           placeholder="@Localizer["BookingRefOrCustomer"]"
           @bind="m_searchTerm" @bind:event="oninput"
           @onkeydown="OnSearchKeyDown"/>
    <button type="button" class="btn btn-primary" @onclick="SearchBookingsAsync"
            disabled="@m_searching">
        @if (m_searching)
        {
            <span class="spinner-border spinner-border-sm"></span>
        }
        else
        {
            <i class="ti ti-search"></i>
        }
    </button>
</div>
```

### Fullscreen Dialog Launch (from Till.razor)
```csharp
var result = await this.DialogService.Create<TillBookingDepositDialog>(Localizer["BookingDeposit"])
    .WithParameter(x => x.SessionId, this.m_session.TillSessionId)
    .WithParameter(x => x.ShopId, this.m_shopId)
    .WithFullscreen()
    .ShowDialogAsync();
```

### Selection State with Clear Button (from TillBookingDepositDialog.razor)
```html
<div class="d-flex justify-content-between align-items-center">
    <div>
        <span class="badge bg-purple me-2">@m_selectedBooking.BookingRef</span>
        <span class="fw-semibold">@m_selectedBooking.CustomerName</span>
    </div>
    <button type="button" class="btn btn-sm btn-ghost-secondary" @onclick="ClearSelection">
        <i class="ti ti-x"></i>
    </button>
</div>
```

### Responsive Two-Column Layout
```html
<div class="row">
    <div class="col-12 col-md-5">
        <!-- Customer/Vehicle Summary (left on desktop, top on mobile) -->
    </div>
    <div class="col-12 col-md-7">
        <!-- Line Items (right on desktop, bottom on mobile) -->
    </div>
</div>
```

### Building Line Items from Booking
```csharp
private List<ReceiptItem> BuildLineItemsFromBooking(Booking booking)
{
    var items = new List<ReceiptItem>();
    var days = booking.Days;
    int sortOrder = 0;

    foreach (var item in booking.Items)
    {
        // Vehicle rental
        items.Add(new ReceiptItem
        {
            Category = ReceiptItemCategory.Rental,
            Description = item.VehicleDisplayName ?? "Vehicle",
            Detail = $"{days} days @ {item.DailyRate:N0}/day",
            Quantity = days,
            UnitPrice = item.DailyRate,
            Amount = item.DailyRate * days,
            SortOrder = sortOrder++
        });

        // Insurance
        if (item.InsuranceId.HasValue && item.InsuranceRate > 0)
        {
            items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.Insurance,
                Description = item.InsuranceName ?? "Insurance",
                Detail = $"{days} days @ {item.InsuranceRate:N0}/day",
                Quantity = days,
                UnitPrice = item.InsuranceRate,
                Amount = item.InsuranceRate * days,
                SortOrder = sortOrder++
            });
        }

        // Deposit (for check-in)
        if (item.DepositAmount > 0)
        {
            items.Add(new ReceiptItem
            {
                Category = ReceiptItemCategory.Deposit,
                Description = "Security Deposit",
                Amount = item.DepositAmount,
                SortOrder = sortOrder++
            });
        }
    }

    return items;
}
```

### Adding Discount Line Item
```csharp
private void ApplyDiscount(decimal amount, bool isPercent, string reason)
{
    decimal discountAmount;
    string detail;

    if (isPercent)
    {
        var subtotal = m_lineItems.Where(i => !i.IsDeduction).Sum(i => i.Amount);
        discountAmount = subtotal * (amount / 100m);
        detail = $"{amount}% discount";
    }
    else
    {
        discountAmount = amount;
        detail = $"Fixed discount";
    }

    m_lineItems.Add(new ReceiptItem
    {
        Category = ReceiptItemCategory.Discount,
        Description = reason,
        Detail = detail,
        Amount = -discountAmount, // Negative for discount
        IsDeduction = true,
        SortOrder = 999
    });

    RecalculateTotal();
}
```

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Separate dialogs per txn type | Unified transaction dialog | Phase 3 | Single entry point, auto-detection |
| Manual line item entry | Build from booking/rental | Phase 3 | Reduces errors, faster workflow |
| Fixed modal sizes only | Fullscreen option | Already implemented | Better for complex item editing |

**Already implemented:**
- Fullscreen modal support via `ModalSize.Fullscreen`
- Search patterns in `TillBookingDepositDialog.razor`
- Line item structure via `ReceiptItem`
- Receipt generation via `ReceiptService`

## Data Model Summary

### Entity Status to Transaction Type Mapping
| Entity | Status | Transaction Type | Primary Line Items |
|--------|--------|------------------|-------------------|
| Booking | Pending | BookingDeposit | Deposit amount only |
| Booking | Confirmed | CheckIn | Rental + Deposit + Insurance + Accessories |
| Rental | Active | CheckOut | Final charges, damage, refunds |

### Key Entity Properties

#### Booking
- `BookingRef` - 6-char alphanumeric for search
- `CustomerName`, `CustomerPhone` - for search
- `Status` - Pending/Confirmed/CheckedIn/Completed/Cancelled
- `Items` - List<BookingItem> with vehicle, insurance, accessories
- `Days` - Calculated rental days
- `TotalAmount`, `AmountPaid`, `BalanceDue` - Pricing
- `CanReceivePayment` - Status != Cancelled && Status != Completed && BalanceDue > 0
- `CanCheckIn` - Status == Pending || Status == Confirmed

#### BookingItem
- `VehicleDisplayName`, `VehicleGroupKey` - Vehicle info
- `InsuranceId`, `InsuranceName`, `InsuranceRate` - Insurance
- `AccessoryIds`, `AccessoriesTotal` - Accessories
- `DailyRate`, `DepositAmount`, `ItemTotal` - Pricing
- `ItemStatus` - Pending/CheckedIn/Cancelled

#### Rental
- `RentalId` - for search
- `RenterName`, `VehicleName` - denormalized for display
- `Status` - Reserved/Active/Completed/Cancelled
- `StartDate`, `ExpectedEndDate`, `RentalDays` - Duration
- `TotalAmount`, `RentalRate` - Pricing
- `InsuranceId`, `DepositId` - Related entities
- `BookingId` - Link to booking if created from booking

#### Accessory
- `AccessoryId`, `ShopId`, `Name`, `DailyRate`
- `QuantityAvailable` - Stock count
- `IsIncluded` - Free with rental

#### Insurance
- `InsuranceId`, `Name`, `Description`
- `DailyRate`, `MaxCoverage`, `Deductible`
- `IsActive` - Can be selected

## Open Questions

Things that couldn't be fully resolved:

1. **Check-Out Line Items for Active Rentals**
   - What we know: `Rental.TotalAmount` exists, but no breakdown in entity
   - What's unclear: How to build check-out items (extra days, damage charges)
   - Recommendation: Build line items from rental properties, add damage/extra as separate items

2. **Discount Persistence**
   - What we know: `ReceiptItemCategory.Discount` exists in constants
   - What's unclear: Where discounts are stored between item confirmation and payment
   - Recommendation: Store in dialog state (`m_lineItems`), persist in receipt on payment

3. **Unified Search Across Booking and Rental**
   - What we know: BookingService and RentalService have separate search methods
   - What's unclear: Best UX for unified search results
   - Recommendation: Search both in parallel, group results by type in UI

## Sources

### Primary (HIGH confidence)
- `src/MotoRent.Client/Pages/Staff/TillBookingDepositDialog.razor` - Working search/payment dialog
- `src/MotoRent.Client/Pages/Staff/Till.razor` - Fullscreen dialog invocation pattern
- `src/MotoRent.Client/Services/ModalFluent.cs` - Dialog builder with fullscreen support
- `src/MotoRent.Client/Controls/ModalContainer.razor` - Fullscreen CSS class implementation
- `src/MotoRent.Domain/Entities/Booking.cs` - Complete booking entity structure
- `src/MotoRent.Domain/Entities/Rental.cs` - Complete rental entity structure
- `src/MotoRent.Domain/Entities/ReceiptItem.cs` - Line item model
- `src/MotoRent.Domain/Entities/ReceiptStatus.cs` - Category constants including Discount
- `src/MotoRent.Services/BookingService.cs` - Search and payment methods
- `src/MotoRent.Services/AccessoryService.cs` - Accessory lookup methods
- `src/MotoRent.Services/InsuranceService.cs` - Insurance package lookup

### Secondary (MEDIUM confidence)
- `.planning/phases/03-transaction-search-items/03-CONTEXT.md` - Phase requirements and decisions

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - Using existing project patterns and services
- Architecture: HIGH - TillBookingDepositDialog provides proven template
- Entity structure: HIGH - Verified from source code
- Line item patterns: HIGH - ReceiptItem model verified
- Discount handling: MEDIUM - Category exists, persistence unclear
- Check-out flow: MEDIUM - Rental entity less detailed than Booking

**Research date:** 2026-01-20
**Valid until:** 30 days (stable domain, no external dependencies)
