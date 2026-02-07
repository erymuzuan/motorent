# Test Plan 02: Check-In Staff - Rental Check-In Workflow

## Role Description
**Role:** Staff handling vehicle check-in
**Access Policy:** RequireTenantStaff
**Primary Pages:** `/rentals/checkin`, `/rentals/checkin/{BookingRef}`, `/rentals`

## Prerequisites
- [ ] Logged in as a user with Staff, ShopManager, or OrgAdmin role
- [ ] Active till session open (required for check-in)
- [ ] At least 3 available vehicles in the shop (different types: motorbike, jet ski if possible)
- [ ] At least 1 existing renter record in the system
- [ ] Insurance packages configured for fleet models
- [ ] Accessories configured for fleet models
- [ ] At least 1 service location configured (for pickup/dropoff tests)
- [ ] At least 1 approved rental agreement template (DocumentType.RentalAgreement)
- [ ] At least 1 confirmed booking (for check-in from booking tests)

---

## Section 1: Renter Selection - Step 0 (`SelectRenterStep`)

### TC-1.1: Search Existing Renter
**Steps:**
1. Navigate to `/rentals/checkin`
2. In the Renter step, type an existing renter's name in the search field
3. Wait for results (300ms debounce)

**Expected:**
- Search results appear (max 10)
- Results show renter name, phone, nationality
- Recent renters section shows up to 5 recent entries

### TC-1.2: Select Existing Renter
**Steps:**
1. Search for a renter by phone number
2. Click on the renter from search results
3. Expand the selected renter's detail panel

**Expected:**
- Selected renter highlighted
- Collapsible detail panel shows: Nationality, Passport/ID, Email, Hotel, Driving License
- Next button enabled

### TC-1.3: Create New Renter - Manual Entry
**Steps:**
1. Click "Create New Renter" button
2. In RenterDialog, switch to "Manual Entry" tab
3. Fill in:
   - Full Name: "Maria Garcia" (required)
   - Nationality: "Spain"
   - Passport Number: "ESP12345678"
   - Driving License Number: "DL-987654"
   - License Country: "Spain"
   - License Expiry: 1 year from now
   - Phone: "+66898765432" (required)
   - Email: "maria@example.com"
   - Hotel Name: "Krabi Resort"
   - Emergency Contact: "+34612345678"
4. Click Save

**Expected:**
- Renter created successfully
- Dialog closes
- New renter auto-selected in the step
- All fields saved correctly

### TC-1.4: Create New Renter - Thai National
**Steps:**
1. Click "Create New Renter"
2. In Manual Entry tab:
   - Full Name: "Somchai Jaidee"
   - Nationality: "Thailand"
   - Enter National ID Number (shown for Thai nationals instead of Passport)
   - Phone: "+66812345678"
3. Save

**Expected:**
- For Thai nationals, National ID field shown instead of Passport
- Renter created successfully

### TC-1.5: Create New Renter - OCR Document Scan
**Steps:**
1. Click "Create New Renter"
2. Switch to "Scan Documents" tab
3. Upload a passport image (drag-and-drop or file picker)
4. Wait for OCR processing (Gemini Flash API)

**Expected:**
- OCR extracts: Full Name, Document Number, Nationality
- Extracted fields auto-populated in the form
- Fields can be manually corrected after extraction
- Uploaded document shown with badge indicator

### TC-1.6: Create New Renter - Scan Driving License
**Steps:**
1. In "Scan Documents" tab, upload a driving license image
2. Wait for OCR processing

**Expected:**
- License Number, Issuing Country, Expiry Date extracted
- Fields populated in the license section
- Can upload both passport and license documents

### TC-1.7: Create Renter - Validation
**Steps:**
1. Click "Create New Renter"
2. Try to save with empty Full Name
3. Try to save with empty Phone

**Expected:**
- Validation errors shown for required fields
- Cannot save without Full Name and Phone

### TC-1.8: Remove Uploaded Document
**Steps:**
1. Upload a document in Scan Documents tab
2. Click the remove button on the uploaded document

**Expected:**
- Document removed from the list
- Previously extracted data remains in fields (not cleared)

---

## Section 2: Vehicle Selection - Step 1 (`SelectVehicleStep`)

### TC-2.1: Browse Fleet Models
**Steps:**
1. Complete renter selection, proceed to Vehicle step
2. Observe the fleet model grid

**Expected:**
- Vehicles grouped by FleetModel (3 per row)
- Each card shows: Model name, engine, available units, color availability, price range
- Fleet model images loaded (or vehicle type icon fallback)

### TC-2.2: Select Fleet Model then Vehicle
**Steps:**
1. Click on a fleet model card
2. Observe individual vehicle list within that model
3. Click on a specific vehicle

**Expected:**
- Phase 2 shows individual vehicles for selected model
- Each vehicle shows: License plate, color, year, daily/hourly rate, deposit
- Selected vehicle has highlighted border
- Next button enabled

### TC-2.3: Search/Filter Vehicles
**Steps:**
1. Type a search term in the filter field (e.g., part of a model name)

**Expected:**
- Fleet model list filters in real-time
- Only matching models displayed

### TC-2.4: Include Pooled Vehicles
**Steps:**
1. Toggle "Include Pooled" switch
2. Observe vehicles from other shops

**Expected:**
- Pooled vehicles from other shops appear
- Warning indicator shown for vehicles at different shop locations
- Pooled vehicle shows source shop name

### TC-2.5: Vehicle with No Image
**Steps:**
1. Find a vehicle/fleet model without uploaded images

**Expected:**
- Falls back to vehicle type icon instead of broken image

### TC-2.6: No Available Vehicles
**Precondition:** All vehicles of a fleet model are currently rented.

**Steps:**
1. Observe fleet model with 0 available units

**Expected:**
- Model shows 0 available
- Cannot select vehicles from that model (or model card is disabled/grayed)

---

## Section 3: Configure Daily Rental - Step 2 (`ConfigureRentalStep`)

### TC-3.1: Set Rental Period - Quick Buttons
**Steps:**
1. Select a standard motorbike vehicle
2. On Configure step, click "3 Days" quick button

**Expected:**
- Start Date set to today
- End Date set to 3 days from today
- Duration shows "3 Days"
- Total calculated: daily rate x 3

### TC-3.2: Set Rental Period - Manual Dates
**Steps:**
1. Manually set Start Date to tomorrow
2. Set End Date to 7 days from tomorrow
3. Verify calculated duration

**Expected:**
- Duration = 7 days
- Quick buttons: 1 Day, 3 Days, 1 Week, 2 Weeks, 1 Month all functional
- Price summary updates with each change

### TC-3.3: Select Insurance Package
**Steps:**
1. Open the Insurance dropdown
2. Review available insurance options (name, description, max coverage, deductible, daily rate)
3. Select an insurance package

**Expected:**
- Insurance daily rate added to price summary
- Insurance total = daily rate x number of days
- "No Insurance" is the default option

### TC-3.4: Add Accessories
**Steps:**
1. Check an accessory from the list
2. For paid accessories, adjust quantity (min 1, max QuantityAvailable)
3. Check a free (included) accessory

**Expected:**
- Paid accessories show daily rate and quantity input
- Free accessories highlighted with "Free" label
- Accessories total calculated in price summary
- Quantity cannot exceed available stock

### TC-3.5: Driver/Guide Options (Boats & Vans)
**Precondition:** Selected vehicle is a Boat or Van with HasDriverOption/HasGuideOption.

**Steps:**
1. Select a boat or van
2. Check "Include Driver" checkbox
3. Check "Include Tour Guide" checkbox

**Expected:**
- Driver daily fee added to price summary
- Guide daily fee added to price summary
- Options only appear for eligible vehicle types

### TC-3.6: Pickup/Dropoff Location
**Steps:**
1. Select a pickup location from the location selector
2. Set pickup time (e.g., 10:00)
3. Change time to an out-of-hours time (e.g., 22:00)

**Expected:**
- Location fee displayed when location selected
- Out-of-hours band detected with fee warning shown
- Location fees and out-of-hours fees appear in price summary

### TC-3.7: Dynamic Pricing
**Precondition:** Dynamic pricing rules configured for the fleet model.

**Steps:**
1. Select dates that trigger a dynamic pricing rule (e.g., peak season)
2. Observe price adjustment

**Expected:**
- Adjusted rate per day shown (vs. base rate)
- Base total vs. adjusted total displayed
- Applied rule summary visible
- Per-day breakdown available for review

### TC-3.8: Price Summary - Sticky Panel
**Steps:**
1. Configure a rental with insurance, accessories, location, driver
2. Scroll through the page

**Expected:**
- Sticky price summary panel stays visible
- Shows: Vehicle total, Driver/Guide totals, Insurance total, Accessories breakdown, Location fees, Required deposit, Final total

---

## Section 4: Configure Hourly Rental - Step 2 (`ConfigureHourlyRentalStep`)

### TC-4.1: Select Hours - Preset Buttons
**Precondition:** Vehicle supports hourly rental.

**Steps:**
1. Select a vehicle that supports hourly rental
2. Verify duration type selector shows (if vehicle supports both daily and hourly)
3. Switch to Hourly if needed
4. Click "2 Hours" preset button

**Expected:**
- Hours set to 2
- Start time defaults to now
- End time auto-calculated
- Total = hourly rate x 2

### TC-4.2: Package Pricing
**Precondition:** Fleet model has package pricing configured.

**Steps:**
1. Select hours that match a package (e.g., 3 hours)
2. Observe package price vs. linear price

**Expected:**
- Package discount shown when available
- Package price used instead of linear (hours x rate)
- Non-package hours use linear pricing

### TC-4.3: Custom Hours
**Steps:**
1. Enter custom hours: 5
2. Verify calculation

**Expected:**
- Hours accepted (range 1-24)
- Total calculated correctly
- Start/end time updated

### TC-4.4: Hourly with Insurance & Accessories
**Steps:**
1. Configure 3-hour rental
2. Select insurance
3. Add accessories

**Expected:**
- Insurance and accessories applied per rental (not per hour)
- Total includes hourly rate + insurance + accessories

---

## Section 5: Configure Interval Rental - Step 2 (`ConfigureIntervalRentalStep`)

### TC-5.1: Select Interval - Jet Ski
**Precondition:** Vehicle uses interval pricing (jet ski).

**Steps:**
1. Select a jet ski vehicle
2. On Configure step, observe fixed interval options

**Expected:**
- Shows available intervals: 15 min, 30 min, 1 hour
- Only intervals with configured rates shown (Rate15Min/Rate30Min/Rate1Hour)
- Each shows its specific rate

### TC-5.2: 15-Minute Interval
**Steps:**
1. Select 15-minute interval
2. Set start time

**Expected:**
- End time = start time + 15 minutes
- Rate = Rate15Min for the vehicle
- TotalAmount calculated

### TC-5.3: 1-Hour Interval
**Steps:**
1. Select 1-hour interval
2. Set start date and time

**Expected:**
- End time = start time + 60 minutes
- Rate = Rate1Hour for the vehicle

---

## Section 6: Collect Deposit - Step 3 (`CollectDepositStep`)

### TC-6.1: Cash Deposit
**Steps:**
1. Reach the Deposit step
2. Verify rental amount and required deposit displayed
3. Select Deposit Type: Cash
4. Enter deposit amount matching the required amount
5. Select Rental Payment Method: Cash
6. Check the confirmation checkbox

**Expected:**
- Amount field accepts the deposit
- Confirmation checkbox enables proceeding
- "Deposit Collected" status shown

### TC-6.2: Cash Deposit - Below Required
**Steps:**
1. Select Cash deposit
2. Enter amount less than required deposit

**Expected:**
- Warning message shown that amount is below required
- Still allows proceeding (soft validation)

### TC-6.3: Cash Deposit - Above Required
**Steps:**
1. Select Cash deposit
2. Enter amount exceeding required deposit

**Expected:**
- Info message shown that amount exceeds required
- Accepted without error

### TC-6.4: Card Pre-Authorization
**Steps:**
1. Select Deposit Type: Card Pre-Auth
2. Enter Card Last 4 Digits: "4567"
3. Enter Auth Reference: "AUTH-123456"
4. Enter Pre-Auth Amount

**Expected:**
- Card last 4, auth ref, and amount captured
- Deposit type recorded as "CardPreAuth"

### TC-6.5: Passport Hold
**Steps:**
1. Select Deposit Type: Passport
2. Enter Passport Number
3. Verify warning about passport holding displayed

**Expected:**
- No amount field (amount = 0 for passport holds)
- Warning message about holding passport shown
- "Passport Held" status displayed
- Passport number stored

### TC-6.6: Rental Payment Method - Non-Cash
**Steps:**
1. Select Rental Payment Method: BankTransfer
2. Enter payment transaction ref

**Expected:**
- Transaction ref field appears for non-cash methods
- Payment ref stored separately from deposit info

### TC-6.7: Skip Confirmation Checkbox
**Steps:**
1. Fill in deposit details but do not check the confirmation checkbox
2. Try to proceed to next step

**Expected:**
- Cannot proceed without checking confirmation checkbox

---

## Section 7: Pre-Rental Inspection - Step 4 (`PreInspectionStep`)

### TC-7.1: Select Shop Staff Inspector
**Steps:**
1. Reach the Inspection step
2. Select "Shop Staff" mode
3. Choose a staff member from the dropdown (filtered to Rental/Maintenance roles)
4. Add inspection notes (optional)

**Expected:**
- Dropdown shows staff with OrgAdmin, ShopManager, Staff, or Mechanic roles
- Selected inspector name shown in confirmation
- IsSystemUser = true

### TC-7.2: Select External Inspector
**Steps:**
1. Switch to "External Inspector" mode
2. Enter inspector name: "Thai Vehicle Inspections"
3. Enter company affiliation: "TVI Co., Ltd."
4. Add notes

**Expected:**
- Inspector name and company stored
- IsSystemUser = false
- Confirmation shows name with affiliation

### TC-7.3: Skip Inspection Selection
**Steps:**
1. Reach Inspection step without selecting an inspector
2. Try to proceed to Agreement step

**Expected:**
- Cannot advance without selecting an inspector
- Validation message shown

---

## Section 8: Agreement & Signature - Step 5 (`AgreementSignatureStep`)

### TC-8.1: Review Rental Summary
**Steps:**
1. Reach the Agreement step
2. Review the rental summary panel

**Expected:**
- Renter details: Name, phone, passport/ID
- Vehicle details: Type, brand, model, license plate, mileage
- Duration details vary by type:
  - Daily: Start-End dates, day count, daily rate
  - Hourly: Hours, hourly rate, start time
  - Interval: Duration (15/30/60 min), rate
- Driver/Guide inclusions (if selected)
- Insurance name (if selected)
- Accessories list
- Deposit type and amount
- Total amount

### TC-8.2: Accept Terms & Conditions
**Steps:**
1. Read/scroll through the terms & conditions text area
2. Check the acceptance checkbox

**Expected:**
- Terms text loaded from localized resource
- Checkbox enables/disables Complete button (along with print requirement)

### TC-8.3: Print Agreement - Single Template
**Precondition:** Only 1 approved RentalAgreement template exists.

**Steps:**
1. Click "Print Agreement" button
2. Verify print page opens in new window

**Expected:**
- Print page URL includes: RenterId, VehicleId, TemplateId, and rental parameters
- Agreement rendered with all rental details

### TC-8.4: Print Agreement - Multiple Templates
**Precondition:** Multiple approved RentalAgreement templates exist.

**Steps:**
1. Click the print dropdown
2. Select a specific template
3. Verify print page opens

**Expected:**
- Dropdown lists all approved templates
- Each template generates correct print output

### TC-8.5: Complete Check-In
**Steps:**
1. Accept terms (checkbox checked)
2. Print agreement (tracked flag set)
3. Click "Complete" button
4. Wait for processing

**Expected:**
- Spinner shown during processing
- Rental created with Status = "Active"
- Redirects to rental details or confirmation page
- Vehicle status changes to "Rented"
- Till transaction created for payment and deposit

### TC-8.6: Complete Check-In - Not Printed
**Steps:**
1. Accept terms but do not print the agreement
2. Try to click "Complete"

**Expected:**
- Complete button disabled until agreement is printed
- Must print before completing

---

## Section 9: Check-In from Booking (`/rentals/checkin/{BookingRef}`)

### TC-9.1: Pre-filled Booking Check-In
**Precondition:** Confirmed booking with linked renter and preferred vehicle.

**Steps:**
1. Navigate to `/rentals/checkin/{BookingRef}`
2. Observe pre-filled data

**Expected:**
- Renter auto-selected from booking's linked renter
- Vehicle matched by PreferredVehicleId or FleetModelId + color
- Start/End dates from booking
- Insurance from booking preferences
- Toast message about booking source displayed

### TC-9.2: Booking Check-In - No Linked Renter
**Precondition:** Booking without linked RenterId.

**Steps:**
1. Check in from a booking where renter was not linked
2. Observe Step 0 (Renter)

**Expected:**
- Customer info from booking used to search/create renter
- Staff can create new renter from booking customer data

### TC-9.3: Booking Check-In - Invalid Status
**Precondition:** Booking with Status = "Cancelled" or "Completed".

**Steps:**
1. Navigate to `/rentals/checkin/{BookingRef}` for a cancelled booking

**Expected:**
- Error message displayed
- Cannot proceed with check-in
- Only Pending or Confirmed bookings allowed

### TC-9.4: Booking Check-In - Updates Booking
**Steps:**
1. Complete check-in from a booking
2. Navigate back to booking details

**Expected:**
- Booking item status = "CheckedIn"
- Assigned vehicle plate and shop shown on item
- RentalId linked to the booking item
- Booking status = "CheckedIn" (if first item checked in)

---

## Section 10: Active Rentals (`/rentals`)

### TC-10.1: View Rental List
**Steps:**
1. Navigate to `/rentals`
2. Observe summary cards and rental list

**Expected:**
- Summary cards: Active, Due Today, Overdue, Completed counts
- Each card clickable to filter the list
- Rental entries show vehicle, renter, dates, status

### TC-10.2: Filter Rental Tabs
**Steps:**
1. Click "Active" tab
2. Click "Due Today" tab
3. Click "Overdue" tab
4. Click "Completed" tab

**Expected:**
- List filters to matching status
- Due Today shows rentals with expected return date = today
- Overdue shows rentals past expected return date

### TC-10.3: Search Rentals
**Steps:**
1. Type renter name or license plate in search bar

**Expected:**
- Real-time filtering of rental list
- Matches by renter name, vehicle plate, or rental reference

---

## Section 11: Duration Type Auto-Selection

### TC-11.1: Vehicle with Interval Pricing (Jet Ski)
**Steps:**
1. Select a vehicle where `UsesIntervalPricing = true`

**Expected:**
- Automatically routes to Interval configuration step
- No option to switch to daily/hourly

### TC-11.2: Vehicle with Both Daily and Hourly
**Steps:**
1. Select a vehicle where `SupportsHourlyRental = true` AND `DailyRate > 0`

**Expected:**
- Toggle shown to switch between Daily and Hourly configuration
- Both options functional

### TC-11.3: Vehicle with Hourly Only
**Steps:**
1. Select a vehicle where `SupportsHourlyRental = true` but no `DailyRate`

**Expected:**
- Defaults to Hourly configuration
- No toggle to switch to Daily

### TC-11.4: Vehicle with Daily Only
**Steps:**
1. Select a standard motorbike (DailyRate set, no hourly support)

**Expected:**
- Defaults to Daily configuration
- No toggle shown

---

## Section 12: Edge Cases & Error Scenarios

### TC-12.1: Check-In Without Active Till
**Steps:**
1. Close all till sessions
2. Navigate to `/rentals/checkin`

**Expected:**
- Error or redirect indicating active till session required
- Cannot proceed with check-in

### TC-12.2: Select Pooled Vehicle from Different Shop
**Steps:**
1. Enable "Include Pooled" toggle
2. Select a vehicle from a different shop
3. Complete check-in

**Expected:**
- Warning about vehicle being at different location
- Check-in still possible (vehicle is pooled)

### TC-12.3: Accessory Out of Stock
**Precondition:** Accessory with QuantityAvailable = 0.

**Steps:**
1. Observe accessory list in Configure step

**Expected:**
- Out-of-stock accessories not selectable or shown as unavailable
- Quantity max respects QuantityAvailable

### TC-12.4: Multiple Quick Date Buttons
**Steps:**
1. Click "1 Day", verify dates
2. Click "1 Week", verify dates change
3. Click "1 Month", verify dates change

**Expected:**
- Each button recalculates correctly
- Price summary updates with each change
- End date adjusts from current start date

### TC-12.5: Extremely Long Rental
**Steps:**
1. Set End Date to 90 days from Start Date
2. Add insurance and accessories

**Expected:**
- Total calculated correctly for 90 days
- Insurance: daily rate x 90
- Accessories: daily rate x 90
- Large total amount displayed without formatting issues

### TC-12.6: Navigate Backward After Completing Steps
**Steps:**
1. Complete all steps to Step 5
2. Click Previous to go back to Step 2
3. Change the insurance selection
4. Navigate forward to Step 5 again

**Expected:**
- Changed insurance reflected in agreement summary
- Total recalculated
- Previously entered data in other steps preserved

---

## Quick Pass/Fail Checklist

| # | Test Case | Pass | Fail | Notes |
|---|-----------|------|------|-------|
| 1.1 | Search existing renter | | | |
| 1.2 | Select existing renter | | | |
| 1.3 | Create new renter - manual | | | |
| 1.4 | Create Thai national renter | | | |
| 1.5 | OCR passport scan | | | |
| 1.6 | OCR driving license | | | |
| 1.7 | Renter validation | | | |
| 1.8 | Remove uploaded document | | | |
| 2.1 | Browse fleet models | | | |
| 2.2 | Select fleet then vehicle | | | |
| 2.3 | Search/filter vehicles | | | |
| 2.4 | Include pooled vehicles | | | |
| 2.5 | Vehicle with no image | | | |
| 2.6 | No available vehicles | | | |
| 3.1 | Quick date buttons | | | |
| 3.2 | Manual date entry | | | |
| 3.3 | Select insurance | | | |
| 3.4 | Add accessories | | | |
| 3.5 | Driver/guide options | | | |
| 3.6 | Pickup/dropoff location | | | |
| 3.7 | Dynamic pricing | | | |
| 3.8 | Sticky price summary | | | |
| 4.1 | Hourly preset buttons | | | |
| 4.2 | Package pricing | | | |
| 4.3 | Custom hours | | | |
| 4.4 | Hourly with add-ons | | | |
| 5.1 | Jet ski intervals | | | |
| 5.2 | 15-minute interval | | | |
| 5.3 | 1-hour interval | | | |
| 6.1 | Cash deposit | | | |
| 6.2 | Deposit below required | | | |
| 6.3 | Deposit above required | | | |
| 6.4 | Card pre-authorization | | | |
| 6.5 | Passport hold | | | |
| 6.6 | Non-cash payment method | | | |
| 6.7 | Skip confirmation checkbox | | | |
| 7.1 | Shop staff inspector | | | |
| 7.2 | External inspector | | | |
| 7.3 | Skip inspection | | | |
| 8.1 | Review rental summary | | | |
| 8.2 | Accept terms | | | |
| 8.3 | Print - single template | | | |
| 8.4 | Print - multiple templates | | | |
| 8.5 | Complete check-in | | | |
| 8.6 | Complete without printing | | | |
| 9.1 | Pre-filled booking check-in | | | |
| 9.2 | Booking - no linked renter | | | |
| 9.3 | Booking - invalid status | | | |
| 9.4 | Booking updates after check-in | | | |
| 10.1 | View rental list | | | |
| 10.2 | Filter rental tabs | | | |
| 10.3 | Search rentals | | | |
| 11.1 | Interval pricing auto-select | | | |
| 11.2 | Daily + hourly toggle | | | |
| 11.3 | Hourly only vehicle | | | |
| 11.4 | Daily only vehicle | | | |
| 12.1 | Check-in without till | | | |
| 12.2 | Pooled vehicle check-in | | | |
| 12.3 | Accessory out of stock | | | |
| 12.4 | Multiple quick date buttons | | | |
| 12.5 | Extremely long rental | | | |
| 12.6 | Navigate backward | | | |
