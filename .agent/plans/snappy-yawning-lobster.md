# Plan: Create Manual Test Plans for 3 Staff Roles

## Context
Create manual test plans for three distinct operational roles in MotoRent, saved to `.\test-plans\` folder. These plans will guide real users through testing the system from each role's perspective, covering happy paths, edge cases, and validation rules.

## Files to Create

### 1. `test-plans/01-cashier-booking-test-plan.md`
**Role**: Cashier handling bookings and payments

**Scope** (based on codebase exploration):
- **Till Management** (`/staff/till`) - Open/close till sessions, cash management, denomination counting
- **Booking Creation** (`/bookings/create`) - 5-step wizard: Customer info, Dates & Vehicle selection, Add-ons (insurance/accessories), Payment recording, Confirmation
- **Booking Management** (`/bookings/{BookingRef}`) - View details, confirm bookings, record additional payments, cancel
- **Booking List** (`/bookings`) - Search/filter bookings by status, date, shop
- **Payment Recording** - Cash, Card, PromptPay, BankTransfer methods
- **Agent Commission** - Referral tracking and commission calculation
- **Receipt Search** (`/staff/till-transactions`)

**Key pages**: `CreateBooking.razor`, `BookingList.razor`, `BookingDetails.razor`, `RecordBookingPaymentDialog.razor`, `Till.razor`

### 2. `test-plans/02-checkin-staff-test-plan.md`
**Role**: Staff handling vehicle check-in

**Scope** (based on codebase exploration):
- **Prerequisite**: Active till session required
- **Check-In Wizard** (`/rentals/checkin`) - 6-step process:
  - Step 0: Select/Register Renter (search, create new, OCR document scan via Gemini)
  - Step 1: Select Vehicle (available fleet, pooled vehicles)
  - Step 2: Configure Rental (Daily/Hourly/Interval, insurance, accessories, driver/guide, dynamic pricing, location fees)
  - Step 3: Collect Deposit (Cash/Card Pre-Auth/Passport Hold) + Payment method
  - Step 4: Pre-Rental Inspection (staff or external inspector)
  - Step 5: Agreement & Signature (summary review, digital signature, receipt)
- **Check-In from Booking** (`/rentals/checkin/{BookingRef}`) - Pre-filled from existing booking
- **Active Rentals** (`/staff/rentals`) - View active, due today, overdue, completed

**Key pages**: `CheckIn.razor`, `SelectRenterStep.razor`, `SelectVehicleStep.razor`, `ConfigureRentalStep.razor`, `CollectDepositStep.razor`, `PreInspectionStep.razor`, `AgreementSignatureStep.razor`, `RenterDialog.razor`

### 3. `test-plans/03-checkout-staff-test-plan.md`
**Role**: Staff handling checkout, damages, cleanliness, and return

**Scope** (based on codebase exploration):
- **Return/Checkout Wizard** (`/rentals/checkout/{RentalRef}`) - 3-step process:
  - Step 1: Return Info - Actual return date, mileage, cross-shop return, drop-off location, fuel level (5 levels), accessories checklist, cleanliness assessment (Clean/Dirty/Very Dirty), post-rental inspection, notes
  - Step 2: Damage Assessment - Standard inspection (None/Minor/Moderate/Major) OR 3D inspection with markers, photo upload (file + camera, max 5), estimated repair cost
  - Step 3: Settlement - Charges summary (extra days, damage, fuel surcharge, cleaning fee, missing accessories, location fees), deposit refund/collection calculation, refund method selection, confirmation
- **Mobile Return** (`/staff/return`) - Simplified search + checklist flow
- **Damage Reports** (`/damage-reports`) - View/manage damage reports with status tracking
- **Vehicle Status Updates** - Available after return

**Key pages**: `CheckOut.razor`, `Return.razor`, `DamageReports.razor`, `DamageReportDialog.razor`, `DamagePhotoUpload.razor`, `InspectionComparer.razor`, `VehicleInspectionPage.razor`

## Test Plan Format
Each test plan will include:
- **Role description** and prerequisites
- **Test scenarios** organized by feature area
- **Step-by-step instructions** for each test case
- **Expected results** for each step
- **Edge cases** and error scenarios
- **Checklist** for quick pass/fail tracking

## Verification
- Confirm all 3 files created in `.\test-plans\`
- Each file covers the complete workflow for that role
- Test cases reference actual page routes and UI elements from the codebase
