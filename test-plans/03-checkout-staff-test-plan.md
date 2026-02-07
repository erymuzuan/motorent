# Test Plan 03: Checkout Staff - Return, Damage & Settlement Workflow

## Role Description
**Role:** Staff handling vehicle return, damage assessment, and settlement
**Access Policy:** RequireTenantStaff
**Primary Pages:** `/rentals/checkout/{RentalRef}`, `/staff/return`, `/damage-reports`

## Prerequisites
- [ ] Logged in as a user with Staff, ShopManager, or OrgAdmin role
- [ ] Active till session open
- [ ] At least 3 active rentals available for checkout testing:
  - 1 daily rental (on time or overdue)
  - 1 hourly rental
  - 1 rental with accessories checked out
- [ ] At least 1 rental with a pooled vehicle (for cross-shop return tests)
- [ ] At least 1 rental overdue by 1+ days
- [ ] Service locations configured (for drop-off tests)
- [ ] Vehicle with 3D model (GLB file) available for 3D inspection tests

---

## Section 1: Return Information - Step 1 (CheckOut.razor)

### TC-1.1: Start Checkout - Daily Rental
**Steps:**
1. Navigate to `/rentals/checkout/{RentalRef}` for an active daily rental
2. Observe the rental summary displayed

**Expected:**
- Rental summary shows: Renter name, vehicle details, rental dates, amount paid
- Actual return date selector visible (pre-filled with today)
- Mileage input visible (for motorbike/car/van vehicles)

### TC-1.2: Set Actual Return Date
**Steps:**
1. Change the actual return date to today
2. If rental is overdue (expected return was yesterday), observe late return alert

**Expected:**
- Return date accepted
- If overdue: Late return alert shown with extra days/hours count
- Extra charges calculated (daily or hourly mode depending on configuration)

### TC-1.3: Enter Mileage at Return
**Steps:**
1. Enter mileage value greater than mileage at check-in
2. Try entering a value less than mileage at check-in

**Expected:**
- Mileage accepted when >= MileageStart
- Validation error when < MileageStart
- Mileage input only shown for vehicles that track mileage (Motorbike, Car, Van)

### TC-1.4: Hourly Rental Return
**Precondition:** Active hourly rental.

**Steps:**
1. Open checkout for an hourly rental
2. Observe the return form

**Expected:**
- Actual return date selector hidden (auto-calculated)
- Extra hours calculated if over booked hours
- Extra charge = extra hours x hourly rate

### TC-1.5: Interval Rental Return (Jet Ski)
**Precondition:** Active interval rental.

**Steps:**
1. Open checkout for a jet ski rental
2. Observe the return form

**Expected:**
- Return date selector hidden
- If over the interval duration: prorated hourly charge for extra minutes

### TC-1.6: Cross-Shop Return
**Precondition:** Rental of a pooled vehicle.

**Steps:**
1. Open checkout for a pooled vehicle rental
2. Select a different return shop from the cross-shop selector

**Expected:**
- Cross-shop return dropdown visible (only for pooled vehicles)
- Shows shops in the vehicle pool
- Selected return shop stored as ReturnShopId

### TC-1.7: Cross-Shop Return - Non-Pooled Vehicle
**Precondition:** Rental of a non-pooled vehicle.

**Steps:**
1. Open checkout for a non-pooled vehicle

**Expected:**
- Cross-shop return selector not visible
- Vehicle must return to the shop it was rented from (RentedFromShopId)

### TC-1.8: Drop-Off Location & Time
**Steps:**
1. Select a drop-off service location from the selector
2. Set a drop-off time during operating hours
3. Change time to outside operating hours

**Expected:**
- Location fee displayed when location selected
- In-hours: only location fee applies
- Out-of-hours: additional out-of-hours fee calculated and shown
- Both fees appear in settlement summary

### TC-1.9: Fuel Level Assessment
**Steps:**
1. Select each fuel level option in sequence:
   - Full (100%) - battery 4 bars icon
   - Three-Quarters (75%) - battery 3 bars icon
   - Half (50%) - battery 2 bars icon
   - Quarter (25%) - battery 1 bar icon
   - Empty (0%) - battery off icon

**Expected:**
- Each level shows corresponding icon
- If fuel returned < fuel at check-in: fuel surcharge calculated
- Surcharge formula: (deficit quarters) x surcharge per quarter (default 50 per quarter)
- If fuel returned >= fuel at check-in: no surcharge

### TC-1.10: Fuel Surcharge Calculation
**Precondition:** Vehicle checked in at Full (100%).

**Steps:**
1. Set fuel level to Quarter (25%)
2. Observe surcharge

**Expected:**
- Deficit = 3 quarters (Full to Quarter)
- Surcharge = 3 x fuel surcharge rate per quarter
- Surcharge displayed in settlement summary

### TC-1.11: Accessories Return Checklist
**Precondition:** Rental with accessories checked out.

**Steps:**
1. For each accessory, enter the quantity returned
2. Set one accessory quantity to less than rented
3. Verify all accessories returned for another

**Expected:**
- Each accessory shows: name, quantity rented, quantity returned input (0 to QuantityRented)
- Missing calculated: QuantityRented - QuantityReturned
- Missing charge: 500 per missing accessory
- Fully returned accessories show no charge

### TC-1.12: Cleanliness Assessment
**Steps:**
1. Select "Clean" - observe no fee
2. Switch to "Dirty" - observe cleaning fee
3. Switch to "Very Dirty" - observe higher cleaning fee

**Expected:**
- **Clean** (sparkles icon): Fee = 0
- **Dirty** (water droplet icon): Fee from vehicle/fleet model CleaningFeeDirty (default 100)
- **Very Dirty** (filled droplet icon): Fee from vehicle/fleet model CleaningFeeVeryDirty (default 200)
- Default selection is "Clean"
- Fee reflected in settlement

### TC-1.13: Post-Rental Inspection
**Steps:**
1. Select an inspector using the InspectionSelector
2. Choose "Shop Staff" and select from dropdown
3. Add inspection notes

**Expected:**
- Inspector stored with name, user ID, timestamp
- Notes saved
- Required before completing checkout

### TC-1.14: Return Notes
**Steps:**
1. Enter notes in the Return Notes textarea (e.g., "Late return, customer apologized")

**Expected:**
- Notes accepted as free text
- Stored with the checkout record

---

## Section 2: Damage Assessment - Step 2

### TC-2.1: No Damage - Standard Mode
**Steps:**
1. Proceed to Damage Assessment step
2. Select Damage Level: "None"

**Expected:**
- Description and cost fields hidden or disabled
- Photo upload not required
- No damage charge in settlement
- Can proceed to Step 3

### TC-2.2: Minor Damage - Standard Mode
**Steps:**
1. Select Damage Level: "Minor"
2. Enter Description: "Small scratch on left side panel"
3. Enter Estimated Repair Cost: 500
4. Upload 1-2 photos

**Expected:**
- Description field becomes required
- Cost field accepts value (min 0, max 100000, step 100)
- Photos uploaded successfully (max 5)
- Damage charge = 500 in settlement

### TC-2.3: Major Damage - Standard Mode
**Steps:**
1. Select Damage Level: "Major"
2. Enter Description: "Front fender cracked, mirror broken"
3. Enter Estimated Repair Cost: 5000
4. Upload 3 photos

**Expected:**
- Same form as minor but with severity = "Major"
- Higher cost accepted
- All photos stored

### TC-2.4: Photo Upload - File and Camera
**Steps:**
1. Upload a photo via file picker (drag-and-drop or browse)
2. Upload a photo via camera capture
3. Try uploading more than 5 photos

**Expected:**
- File upload: Accepts image files up to 10MB
- Camera capture: Opens device camera
- Max 5 photos per damage report
- 6th photo rejected with message
- Photos show "Before"/"After" tags

### TC-2.5: 3D Inspection Mode
**Precondition:** Vehicle has a 3D model (GLB file).

**Steps:**
1. Toggle to "3D Inspection" mode
2. Click on the 3D vehicle model to place a damage marker
3. Fill in marker details:
   - Damage Type: select from list (Scratch, Dent, Crack, etc.)
   - Severity: Minor/Moderate/Major
   - Location Description: "Left side panel near exhaust"
   - Description: "3cm scratch from parking incident"
   - Estimated Cost: 300
4. Upload photo for this marker

**Expected:**
- 3D model rendered and interactive (rotate, zoom)
- Marker placed at clicked location with 3D coordinates
- Marker appears in the markers list panel
- Marker editor panel shows all fields
- Photo associated with specific marker

### TC-2.6: 3D Inspection - Multiple Markers
**Steps:**
1. Place 3 damage markers on different parts of the vehicle
2. Set different types and severities for each

**Expected:**
- All markers visible on 3D model
- Marker count badge shows 3
- Each marker independently editable
- Total damage cost = sum of all marker costs

### TC-2.7: 3D Inspection - Pre-Existing Damage
**Precondition:** Vehicle has pre-rental inspection data with damage markers.

**Steps:**
1. Open 3D inspection
2. Observe pre-existing damage markers

**Expected:**
- Pre-existing markers shown in different color/style
- Pre-existing markers marked as `IsPreExisting = true`
- Pre-existing damage costs NOT included in new charges
- Clear distinction between old and new damage

### TC-2.8: 3D Inspection Controls
**Steps:**
1. Click "Reset Camera" button
2. Click "Take Snapshot" button
3. Click "Clear All Markers" button

**Expected:**
- Reset Camera: Returns to default view angle
- Take Snapshot: Captures current 3D view as image
- Clear All Markers: Removes all new markers (not pre-existing)

### TC-2.9: Damage Types - Full List
**Steps:**
1. Place a marker, open the Damage Type selector
2. Verify all types available

**Expected:**
- Types: Scratch, Dent, Crack, Scuff, MissingPart, Paint, Rust, Mechanical, Broken, Wear, Other
- Each type selectable with appropriate label

### TC-2.10: Standard Mode - Maximum Cost
**Steps:**
1. Enter Estimated Repair Cost: 100000 (max)
2. Try entering 100001

**Expected:**
- 100000 accepted
- Values above 100000 capped or rejected

---

## Section 3: Settlement - Step 3

### TC-3.1: Settlement - No Extra Charges
**Precondition:** On-time return, no damage, full fuel, clean, all accessories returned.

**Steps:**
1. Complete Step 1 with no issues
2. Complete Step 2 with No Damage
3. Proceed to Settlement step

**Expected:**
- Original rental amount shown (marked as paid)
- No extra charges
- Deposit held amount displayed
- Full deposit refund amount = deposit held
- Refund method selector available

### TC-3.2: Settlement - Extra Days Charge
**Precondition:** Rental returned 2 days late.

**Steps:**
1. Set actual return date to 2 days after expected
2. Proceed to Settlement

**Expected:**
- Extra days: 2
- Extra charge = 2 x daily rental rate
- Extra charge shown in settlement breakdown
- Deposit adjusted: refund = deposit - extra charge
- If extra charge > deposit: amount to collect shown

### TC-3.3: Settlement - Extra Hours (Hourly Late Fee Mode)
**Precondition:** Daily rental with late fee mode = "Hourly", returned 5 hours late.

**Steps:**
1. Return daily rental a few hours late
2. Proceed to Settlement

**Expected:**
- Extra hours calculated
- Charge = extra hours x vehicle's hourly rate
- Displayed in settlement breakdown

### TC-3.4: Settlement - All Charges Combined
**Steps:**
1. Return late (2 extra days)
2. Report moderate damage (cost: 2000)
3. Set fuel to Quarter (surcharge applies)
4. Set cleanliness to Dirty (fee: 100)
5. Report 1 missing accessory (charge: 500)
6. Select drop-off location with out-of-hours fee
7. Proceed to Settlement

**Expected:**
- Settlement breakdown shows ALL charges:
  - Extra days charge: 2 x daily rate
  - Damage estimate: 2000
  - Fuel surcharge: calculated from fuel deficit
  - Cleaning fee: 100
  - Missing accessory: 500
  - Drop-off location fee: from service location
  - Out-of-hours fee: from operating hours
- Total additional = sum of all above
- Deposit settlement: Deposit held - Total additional
- If negative: amount to collect from renter

### TC-3.5: Settlement - Deposit Covers All Charges
**Precondition:** Deposit > total extra charges.

**Steps:**
1. Configure minor charges totaling less than deposit
2. Review Settlement step

**Expected:**
- Refund amount = Deposit held - Total charges
- Refund amount shown as positive
- Deposit status will be "Refunded"
- Refund method selector available

### TC-3.6: Settlement - Charges Exceed Deposit
**Precondition:** Major damage and extra days exceed deposit amount.

**Steps:**
1. Configure charges totaling more than deposit
2. Review Settlement step

**Expected:**
- Amount to collect = Total charges - Deposit held
- No refund (deposit forfeited)
- Deposit status will be "Forfeited"
- Amount to collect shown prominently

### TC-3.7: Select Refund Method
**Steps:**
1. Reach settlement with refund due
2. Select refund method: Cash
3. Change to Card
4. Change to Bank Transfer

**Expected:**
- Three refund methods available: Cash, Card, BankTransfer
- Selection stored as RefundMethod

### TC-3.8: Refund Confirmation Checkbox
**Steps:**
1. Reach settlement with refund due
2. Try to click "Complete" without checking refund confirmation
3. Check the "Refund Processed" checkbox
4. Click "Complete"

**Expected:**
- Cannot complete without checking refund confirmation (when refund > 0)
- Checkbox required before "Complete" button enables

### TC-3.9: Maintenance Warning Acknowledgment
**Precondition:** Vehicle has overdue or due-soon maintenance items.

**Steps:**
1. Reach settlement step
2. Observe maintenance warning

**Expected:**
- Warning displayed with overdue and due-soon counts
- Acknowledgment checkbox required
- Cannot complete without acknowledging maintenance warning

### TC-3.10: Complete Checkout
**Steps:**
1. Fill all required fields across all 3 steps
2. Check refund processed (if applicable)
3. Check maintenance acknowledgment (if applicable)
4. Click "Complete" button

**Expected:**
- Processing spinner shown
- Rental status changes to "Completed"
- Vehicle status changes to "Available"
- Deposit status updated (Refunded or Forfeited)
- Damage reports created (if any)
- Payment records created for:
  - Additional charges (if any)
  - Deposit refund (if applicable)
- Success confirmation shown

### TC-3.11: Dynamic Pricing Breakdown
**Precondition:** Rental had dynamic pricing applied at check-in.

**Steps:**
1. Observe settlement summary for a rental with dynamic pricing

**Expected:**
- Dynamic pricing breakdown visible in original rental amount section
- Shows base vs. adjusted rates

---

## Section 4: Mobile Return (`/staff/return`)

### TC-4.1: Search Rental
**Steps:**
1. Navigate to `/staff/return`
2. Search by license plate number
3. Clear, search by renter name

**Expected:**
- Search results appear for matching rentals
- Results show vehicle and renter info

### TC-4.2: Quick Access - Recent Active Rentals
**Steps:**
1. On mobile return page, observe "Due Today" and "Overdue" sections

**Expected:**
- Due today rentals listed for quick access
- Overdue rentals listed with days overdue indicator

### TC-4.3: Return Checklist
**Steps:**
1. Select an active rental
2. Go through the return checklist:
   - Check "Bike condition OK"
   - Check "Fuel checked"
   - Check "Keys returned"
   - Check "Helmets returned"
   - Check "Accessories returned"

**Expected:**
- Each checkbox toggleable
- All items must be reviewed

### TC-4.4: Mobile Return - Report Damage
**Steps:**
1. Uncheck "Bike condition OK"
2. Damage section appears
3. Enter damage notes: "Scratched left handlebar"
4. Enter damage charge: 1000

**Expected:**
- Damage fields shown when condition not OK
- Damage notes and charge captured
- Charge reflected in financial summary

### TC-4.5: Mobile Return - Financial Summary
**Steps:**
1. Complete the checklist
2. Review financial summary

**Expected:**
- Rental cost breakdown displayed
- Extra days charges (if overdue)
- Damage charges (if reported)
- Total due shown
- Deposit held shown
- Refund or collection amount calculated

### TC-4.6: Mobile Return - Complete Button Labels
**Steps:**
1. Complete return with refund due, observe button label
2. Complete return with amount to collect, observe button label
3. Complete return with no refund, observe button label

**Expected:**
- Button context-aware:
  - Refund scenario: Shows refund amount
  - Collection scenario: Shows amount to collect
  - Even scenario: Shows "no refund" or neutral label

---

## Section 5: Damage Reports (`/damage-reports`)

### TC-5.1: View Damage Reports List
**Steps:**
1. Navigate to `/damage-reports`
2. Observe the list of damage reports

**Expected:**
- Reports listed with: RentalId, vehicle info, severity, estimated cost, status
- Status badges: Pending, Charged, Waived, InsuranceClaim

### TC-5.2: Damage Report Status Transitions
**Steps:**
1. Find a "Pending" damage report
2. Change status to "Charged"
3. Find another "Pending" report, change to "Waived"
4. Find another, change to "InsuranceClaim"

**Expected:**
- Pending -> Charged: Damage cost deducted from deposit
- Pending -> Waived: No charge applied
- Pending -> InsuranceClaim: Marked for insurance processing

### TC-5.3: View Damage Report Details
**Steps:**
1. Click on a damage report
2. View the DamageReportDialog

**Expected:**
- Shows: Description, Severity, Estimated Cost, Actual Cost, Status
- Photos displayed (if uploaded)
- Rental reference linked
- Reported date and time shown

### TC-5.4: Update Actual Repair Cost
**Steps:**
1. Open a damage report
2. Update Actual Cost (different from Estimated Cost)

**Expected:**
- Actual cost saved separately from estimate
- Both values visible for comparison

---

## Section 6: Inspection Comparison

### TC-6.1: Compare Pre and Post Inspection
**Precondition:** Rental with both pre-rental and post-rental inspection.

**Steps:**
1. During checkout, view the pre-existing damage reference
2. Compare with new damage markers

**Expected:**
- Pre-rental inspection data loaded for reference
- Side-by-side or overlay comparison available (InspectionComparer)
- Clear distinction between pre-existing and new damage

---

## Section 7: Edge Cases & Error Scenarios

### TC-7.1: Checkout Without Post-Rental Inspection
**Steps:**
1. Skip the inspection on Step 1
2. Try to proceed to damage assessment

**Expected:**
- Cannot proceed without recording post-rental inspection
- Validation message shown

### TC-7.2: Zero Deposit Rental
**Precondition:** Rental with no deposit collected (Passport hold).

**Steps:**
1. Return vehicle with some damage
2. Observe settlement

**Expected:**
- Deposit held = 0 (Passport hold)
- Full damage amount shown as "Amount to Collect"
- No refund calculation

### TC-7.3: Checkout Cancelled Rental
**Precondition:** Rental with Status = "Cancelled".

**Steps:**
1. Navigate to `/rentals/checkout/{RentalRef}` for a cancelled rental

**Expected:**
- Error or redirect
- Cannot checkout a cancelled rental
- Only "Active" rentals can be checked out

### TC-7.4: Double Checkout Attempt
**Steps:**
1. Complete checkout for a rental
2. Navigate back to the same checkout URL

**Expected:**
- Error indicating rental already completed
- Cannot checkout twice

### TC-7.5: Missing All Accessories
**Precondition:** Rental with 3 accessories, all missing.

**Steps:**
1. Set all accessory quantities returned to 0
2. Proceed to settlement

**Expected:**
- Missing charge = 3 x 500 = 1500
- All accessories marked as missing in settlement

### TC-7.6: Fuel Level Same as Check-In
**Steps:**
1. Set fuel level at return to same as check-in level

**Expected:**
- No fuel surcharge applied
- Fuel surcharge = 0

### TC-7.7: Photo Upload - Large File
**Steps:**
1. Try uploading an image file larger than 10MB

**Expected:**
- Upload rejected with file size error
- Message indicating 10MB limit

### TC-7.8: 3D Inspection - No 3D Model Available
**Precondition:** Vehicle without a GLB 3D model file.

**Steps:**
1. Try to switch to 3D Inspection mode

**Expected:**
- 3D mode unavailable or greyed out
- Falls back to standard inspection only
- Clear indication that 3D model not available

### TC-7.9: Extremely Late Return (30+ days)
**Steps:**
1. Return a rental 30+ days late
2. Review settlement calculation

**Expected:**
- Extra days calculated correctly (30+)
- Large extra charge amount displayed properly
- Settlement math correct even with large numbers

### TC-7.10: All Fuel Levels - Edge Cases
**Steps:**
1. Test return at Empty (0%) when checked in at Empty (0%)
2. Test return at Full (100%) when checked in at Quarter (25%)

**Expected:**
- Same level: No surcharge
- Higher level at return than check-in: No surcharge (no credit given)

---

## Quick Pass/Fail Checklist

| # | Test Case | Pass | Fail | Notes |
|---|-----------|------|------|-------|
| 1.1 | Start checkout - daily | | | |
| 1.2 | Set actual return date | | | |
| 1.3 | Enter mileage at return | | | |
| 1.4 | Hourly rental return | | | |
| 1.5 | Interval rental return | | | |
| 1.6 | Cross-shop return | | | |
| 1.7 | Non-pooled vehicle | | | |
| 1.8 | Drop-off location & time | | | |
| 1.9 | Fuel level assessment | | | |
| 1.10 | Fuel surcharge calculation | | | |
| 1.11 | Accessories return checklist | | | |
| 1.12 | Cleanliness assessment | | | |
| 1.13 | Post-rental inspection | | | |
| 1.14 | Return notes | | | |
| 2.1 | No damage - standard | | | |
| 2.2 | Minor damage | | | |
| 2.3 | Major damage | | | |
| 2.4 | Photo upload | | | |
| 2.5 | 3D inspection mode | | | |
| 2.6 | Multiple 3D markers | | | |
| 2.7 | Pre-existing damage | | | |
| 2.8 | 3D controls | | | |
| 2.9 | Damage types | | | |
| 2.10 | Maximum cost | | | |
| 3.1 | No extra charges | | | |
| 3.2 | Extra days charge | | | |
| 3.3 | Extra hours charge | | | |
| 3.4 | All charges combined | | | |
| 3.5 | Deposit covers charges | | | |
| 3.6 | Charges exceed deposit | | | |
| 3.7 | Refund method selection | | | |
| 3.8 | Refund confirmation | | | |
| 3.9 | Maintenance warning | | | |
| 3.10 | Complete checkout | | | |
| 3.11 | Dynamic pricing breakdown | | | |
| 4.1 | Mobile search rental | | | |
| 4.2 | Quick access rentals | | | |
| 4.3 | Return checklist | | | |
| 4.4 | Mobile report damage | | | |
| 4.5 | Mobile financial summary | | | |
| 4.6 | Complete button labels | | | |
| 5.1 | Damage reports list | | | |
| 5.2 | Status transitions | | | |
| 5.3 | Damage report details | | | |
| 5.4 | Actual repair cost | | | |
| 6.1 | Pre/post inspection compare | | | |
| 7.1 | Checkout without inspection | | | |
| 7.2 | Zero deposit rental | | | |
| 7.3 | Cancelled rental checkout | | | |
| 7.4 | Double checkout | | | |
| 7.5 | All accessories missing | | | |
| 7.6 | Fuel same as check-in | | | |
| 7.7 | Large file upload | | | |
| 7.8 | No 3D model | | | |
| 7.9 | Extremely late return | | | |
| 7.10 | Fuel edge cases | | | |
