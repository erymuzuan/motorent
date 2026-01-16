# MotoRent Booking Module - Implementation Plan

## Overview
Comprehensive booking system with multi-vehicle support, flexible payment options, and Email + LINE notifications. Staff see expected customers with pre-filled data - no double entry at check-in.

## Requirements Summary
- **Channels**: Tourist portal + Staff-assisted booking
- **Payment**: Flexible (deposit/full prepay/pay at pickup) - shop configurable
- **Vehicle Selection**: Specific vehicle with cross-shop matching
- **Check-in**: Staff reviews/edits booking before creating rental
- **Cancellation**: Shop configurable policy with refund calculation
- **Statuses**: Pending → Confirmed → CheckedIn → Completed/Cancelled
- **Multi-vehicle**: Group bookings supported
- **Notifications**: Email + LINE
- **Reference Format**: 6-char alphanumeric (e.g., "ABC123")

---

## Phase 1: Domain Layer - Booking Entity ✓

### 1.1 Booking Entity
**File**: `src/MotoRent.Domain/Entities/Booking.cs`
- BookingId, BookingRef (6-char), Status
- Customer contact info (Name, Phone, Email, Passport, Hotel)
- Dates (StartDate, EndDate, PickupTime)
- Pricing (TotalAmount, DepositRequired, AmountPaid, PaymentStatus)
- Cancellation tracking (CancelledOn, CancellationReason, RefundAmount)
- Items collection for multi-vehicle support
- Change history for audit trail

### 1.2 BookingItem Model
**File**: `src/MotoRent.Domain/Entities/BookingItem.cs`
- VehicleGroupKey for cross-shop matching
- PreferredVehicleId, PreferredColor
- Insurance and accessories
- Pricing captured at booking time
- AssignedVehicleId set at check-in

### 1.3 BookingStatus Constants
**File**: `src/MotoRent.Domain/Entities/BookingStatus.cs`
- Pending, Confirmed, CheckedIn, Completed, Cancelled

---

## Phase 2: Shop Settings ✓

### Setting Keys Added
**File**: `src/MotoRent.Domain/Settings/SettingKeys.cs`
- `Booking_PaymentModel` - Flexible, DepositRequired, FullPrepay, PayAtPickup
- `Booking_DepositPercent` - int (e.g., 30 for 30%)
- `Booking_MinDepositAmount` - decimal
- `Booking_AllowMultiVehicle` - bool
- `Booking_CancellationPolicy` - Free, TimeBased, NonRefundable
- `Booking_FreeCancelHours` - int
- `Booking_LateCancelPenaltyPercent` - int
- `Booking_NoShowPenaltyPercent` - int

---

## Phase 3: Services Layer ✓

### 3.1 BookingService
**File**: `src/MotoRent.Services/BookingService.cs`

**Core Methods:**
- `CreateBookingAsync(CreateBookingRequest)` - Tourist/staff creates booking
- `GetBookingByRefAsync(bookingRef)` - Lookup by reference
- `GetBookingByIdAsync(bookingId)` - Lookup by ID
- `GetUpcomingBookingsAsync(date)` - Organization-wide upcoming
- `GenerateBookingRefAsync()` - Generate unique 6-char code

**Status Management:**
- `ConfirmBookingAsync(bookingId)` - Mark as confirmed
- `CancelBookingAsync(bookingId, reason)` - Cancel with policy
- `UpdateStatusAsync(bookingId, status, notes)` - Change status

**Payment:**
- `RecordPaymentAsync(bookingId, amount, method)` - Record payment
- `RecalculateTotalsAsync(bookingId)` - Recalculate after changes

**Tourist Portal:**
- `GetBookingsForTouristAsync(email, phone)` - Tourist lookup

---

## Phase 4: Staff UI ✓

### 4.1 BookingList.razor
- Route: `/bookings`
- Filters: Date range, Status, Shop
- Table: Ref, Customer, Pickup, Vehicles, Amount, Status, Actions
- Quick actions: View, Check-in

### 4.2 CreateBooking.razor
- Route: `/bookings/create`
- Multi-step wizard with MudStepper
- Customer info, Vehicle selection, Add-ons, Payment, Confirmation

### 4.3 BookingDetails.razor
- Route: `/bookings/{BookingRef}`
- Summary cards, Vehicle list, Payment tracking
- Status management, Schedule changes
- Change history timeline

### 4.4 Expected Arrivals Widget
- Dashboard component showing today's + tomorrow's bookings
- Quick action: Start Check-in

---

## Phase 5: Tourist Portal ✓

### 5.1 ReservationDialog Updates
- Creates Booking (not Rental) on submit
- Shows booking reference on confirmation
- Payment options based on shop settings

### 5.2 MyBooking.razor
**File**: `src/MotoRent.Client/Pages/Tourist/MyBooking.razor`
- Route: `/tourist/{AccountNo}/booking/{BookingRef}` or `/tourist/{AccountNo}/booking`
- Booking lookup by reference
- View status, vehicles, payment summary
- Cancel booking option (if policy allows)
- Contact information display

### 5.3 RentalHistory Updates
**File**: `src/MotoRent.Client/Pages/Tourist/RentalHistory.razor`
- Added "Upcoming Bookings" section
- Shows booking cards with status, vehicles, payment
- Cancel booking option
- Links to MyBooking page for details

---

## Phase 6: Notifications ✓

### 6.1 NotificationService
**File**: `src/MotoRent.Services/NotificationService.cs`

**Email Methods:**
- `SendBookingConfirmationAsync(booking)` - Confirmation email
- `SendPaymentReceiptAsync(booking, amount, method)` - Receipt email
- `SendBookingReminderAsync(booking)` - Day-before reminder
- `SendCancellationNoticeAsync(booking, refund)` - Cancellation email

**LINE Methods:**
- `SendLineBookingConfirmationAsync(booking, token)` - LINE confirmation
- `SendLinePaymentReceiptAsync(booking, amount, token)` - LINE receipt
- `SendLineBookingReminderAsync(booking, token)` - LINE reminder
- `SendLineCancellationNoticeAsync(booking, refund, token)` - LINE cancellation
- `SendLineStaffNotificationAsync(message, token)` - Staff notification

### 6.2 Email Templates
Built-in HTML templates with MotoRent branding (#00897B):
- Booking Confirmation - Tropical Teal header
- Payment Receipt - Amount highlight
- Booking Reminder - Orange warning header
- Cancellation Notice - Gray header with refund info

### 6.3 Configuration
**File**: `src/MotoRent.Domain/Core/MotoConfig.cs`
Added settings:
- `MOTO_SmtpHost`, `MOTO_SmtpPort`, `MOTO_SmtpUser`, `MOTO_SmtpPassword`
- `MOTO_SmtpFromEmail`, `MOTO_SmtpFromName`
- `MOTO_LineNotifyToken`

---

## Phase 7: Cancellation & Refunds ✓

### 7.1 CancellationPolicyService
**File**: `src/MotoRent.Services/CancellationPolicyService.cs`

**Methods:**
- `CalculateRefundAsync(booking)` - Apply shop policy, return RefundCalculation
- `GetPolicyInfoAsync()` - Get human-readable policy description
- `CanCancelWithFullRefundAsync(booking)` - Check if full refund available
- `CanCancelBooking(booking)` - Check if cancellable (static)

**Policy Types:**
- **Free**: Full refund always
- **TimeBased**: Free cancellation window, then penalties
- **NonRefundable**: No refunds

**Calculation Logic:**
- Hours until pickup calculated
- Free cancel if more than `FreeCancelHours` before pickup
- Late cancellation penalty if within window
- No-show penalty if past pickup time

---

## Implementation Status

| Phase | Status | Key Files |
|-------|--------|-----------|
| 1. Domain Entities | ✓ Complete | Booking.cs, BookingItem.cs, BookingStatus.cs |
| 2. Settings | ✓ Complete | SettingKeys.cs |
| 3. Services | ✓ Complete | BookingService.cs |
| 4. Staff UI | ✓ Complete | BookingList.razor, BookingDetails.razor, CreateBooking.razor |
| 5. Tourist Portal | ✓ Complete | MyBooking.razor, RentalHistory.razor |
| 6. Notifications | ✓ Complete | NotificationService.cs |
| 7. Cancellation | ✓ Complete | CancellationPolicyService.cs |

---

## Commits

1. Domain entities and database table
2. BookingService with CRUD operations
3. Staff booking management pages
4. Expected Arrivals dashboard widget
5. Tourist booking lookup page (MyBooking.razor)
6. RentalHistory booking display
7. NotificationService for Email + LINE
8. CancellationPolicyService for refund calculation

---

## Key Design Decisions

### Cross-Shop Vehicle Matching
- Uses `VehicleGroupKey` format: "Brand|Model|Year|VehicleType|EngineCC"
- Customer can check-in at ANY shop with matching vehicle
- `PreferredVehicleId` is optional, `AssignedVehicleId` set at check-in

### Payment Flexibility
- Shop configures payment model via settings
- Supports deposit, full prepay, or pay at pickup
- Tracks AmountPaid vs TotalAmount for balance calculation

### Notification Architecture
- SMTP for email with HTML templates
- LINE Notify API for LINE messages
- Graceful degradation if not configured
- Logging for debugging

### Cancellation Policies
- Shop-configurable via settings
- Time-based with configurable windows
- Automatic refund calculation
- Support for late cancellation and no-show penalties
