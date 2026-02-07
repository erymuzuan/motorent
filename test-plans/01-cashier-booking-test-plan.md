# Test Plan 01: Cashier - Booking & Payment Workflow

## Role Description
**Role:** Staff (Cashier)
**Access Policy:** RequireTenantStaff
**Primary Pages:** `/bookings/create`, `/bookings`, `/bookings/{BookingRef}`, `/staff/till`

## Prerequisites
- [ ] Logged in as a user with Staff, ShopManager, or OrgAdmin role
- [ ] Assigned to a shop with active fleet vehicles
- [ ] At least 2 fleet models with available vehicles in the shop
- [ ] At least 1 agent configured (for agent booking tests)
- [ ] Insurance packages configured for at least 1 fleet model
- [ ] Accessories configured for at least 1 fleet model

---

## Section 1: Till Management (`/staff/till`)

### TC-1.1: Open Till Session
**Steps:**
1. Navigate to `/staff/till`
2. Verify the "Open Till Session" button is displayed
3. Click "Open Till Session"
4. Enter an opening float amount (e.g., 5000)
5. Confirm opening

**Expected:**
- Till dashboard appears with "Session Active" badge
- Opening float shows the entered amount
- Cash in/out cards show 0
- Staff name and session start time displayed
- Quick Payment and Quick Payout buttons visible

### TC-1.2: Open Till - Stale Session Warning
**Precondition:** A till session was opened yesterday and never closed.

**Steps:**
1. Navigate to `/staff/till`
2. Observe the stale session warning

**Expected:**
- Yellow/warning header displayed
- Shows session opened date from yesterday
- "Close Shift" button prominently displayed
- Cannot perform new transactions until stale session is closed

### TC-1.3: Close Till Session
**Precondition:** Active till session exists.

**Steps:**
1. Navigate to `/staff/till`
2. Click "Close Shift" in Till Operations section
3. Enter cash denomination counts in the close session dialog
4. Confirm closure

**Expected:**
- Cash count form displays denomination fields
- Expected vs actual cash comparison shown
- Session closes successfully
- Returns to "Open Till Session" screen

### TC-1.4: Quick Payment - Rental Payment
**Precondition:** Active till session.

**Steps:**
1. Click "Rental Payment" quick button
2. Enter payment amount (e.g., 1500)
3. Select payment method: Cash
4. Confirm transaction

**Expected:**
- Transaction recorded in till
- Cash in total increases by 1500
- Transaction appears in Recent Receipts

### TC-1.5: Quick Payout - Agent Commission
**Precondition:** Active till session.

**Steps:**
1. Click "Agent Commission" quick payout button
2. Enter payout amount (e.g., 500)
3. Add a reference note
4. Confirm payout

**Expected:**
- Transaction recorded as cash-out
- Cash out total increases by 500
- Transaction appears in Recent Receipts

### TC-1.6: Cash Drop
**Precondition:** Active till session with cash balance.

**Steps:**
1. Click "Cash Drop" in Till Operations
2. Enter drop amount
3. Confirm cash drop

**Expected:**
- "Total dropped to safe" amount updates
- Cash balance in till decreases

### TC-1.7: Void Transaction
**Precondition:** Active till session with at least 1 transaction.

**Steps:**
1. Click a recent receipt in the Recent Receipts card
2. Click void/cancel option
3. Enter void reason in the VoidTransactionDialog
4. Requires manager PIN approval (ManagerPinDialog)
5. Confirm void

**Expected:**
- Transaction marked as VOID with strikethrough styling
- VOID badge appears on the receipt
- Cash totals adjusted accordingly

### TC-1.8: Reprint Receipt
**Precondition:** Active till session with receipts.

**Steps:**
1. Click the reprint button on any receipt in Recent Receipts
2. Verify print dialog opens

**Expected:**
- Receipt print dialog shows correct transaction details
- Receipt number, amount, and type displayed correctly

---

## Section 2: Booking Creation (`/bookings/create`)

### TC-2.1: Create Basic Booking (Happy Path)
**Steps:**
1. Navigate to `/bookings/create`
2. **Step 0 - Customer Info:**
   - Enter Customer Name: "John Smith"
   - Enter Phone: "+66812345678"
   - Enter Email: "john@example.com" (optional)
   - Enter Nationality: select "United Kingdom"
   - Enter Hotel Name: "Patong Beach Hotel"
   - Click Next
3. **Step 1 - Dates & Vehicle:**
   - Set Start Date: tomorrow
   - Set End Date: 3 days from now
   - Select a fleet model by clicking Add (+)
   - Verify vehicle count shows 1
   - Click Next
4. **Step 2 - Add-ons:**
   - Select an insurance package from dropdown
   - Enter preferred color (optional)
   - Check 1 accessory (if available)
   - Click Next
5. **Step 3 - Payment:**
   - Verify booking summary shows correct totals
   - Enter payment amount matching the deposit required
   - Select Payment Method: Cash
   - Select Payment Type: Deposit
   - Click Next
6. **Step 4 - Confirmation:**
   - Review all details displayed
   - Click "Create Booking"

**Expected:**
- Booking created with Status = "Pending"
- Redirects to `/bookings/{BookingRef}`
- BookingRef is 6-character alphanumeric
- Payment recorded with type "Deposit"
- Payment status reflects amount paid

### TC-2.2: Create Booking - Validation Errors (Step 0)
**Steps:**
1. Navigate to `/bookings/create`
2. Leave Customer Name empty, click Next
3. Verify validation message for required name
4. Enter name, leave Phone empty, click Next
5. Verify validation message for required phone

**Expected:**
- Cannot advance past Step 0 without Customer Name and Phone
- Validation messages shown inline

### TC-2.3: Create Booking - No Vehicle Selected (Step 1)
**Steps:**
1. Complete Step 0 with valid data
2. On Step 1, set dates but do not select any vehicle
3. Click Next

**Expected:**
- Cannot advance past Step 1
- Validation message indicating at least 1 vehicle must be selected

### TC-2.4: Create Booking - Date Validation (Step 1)
**Steps:**
1. Complete Step 0 with valid data
2. On Step 1, set Start Date to a past date
3. Attempt to set End Date before Start Date

**Expected:**
- Start Date minimum is today
- End Date minimum is Start Date
- Cannot select invalid date ranges

### TC-2.5: Create Booking - Multiple Vehicles
**Steps:**
1. Complete Step 0 with valid data
2. On Step 1, add 2 different fleet models (or 2 of the same)
3. Verify quantity can be adjusted with Add/Remove buttons
4. Continue through remaining steps

**Expected:**
- Multiple items shown in Step 2 (one row per vehicle)
- Each item has separate insurance and accessory selection
- Total amount sums all items correctly
- Booking created with correct vehicle count

### TC-2.6: Create Booking - No Payment
**Steps:**
1. Complete Steps 0-2 with valid data
2. On Step 3, leave payment amount as 0 or empty
3. Complete booking

**Expected:**
- Booking created with PaymentStatus = "Unpaid"
- No payment record created
- Balance due equals full booking total

### TC-2.7: Create Booking - Full Payment
**Steps:**
1. Complete Steps 0-2 with valid data
2. On Step 3, click "Full Amount" quick button
3. Select Payment Method: Card
4. Enter Transaction Ref: "TXN-12345"
5. Complete booking

**Expected:**
- PaymentStatus = "FullyPaid"
- Balance due = 0
- Payment record shows Card method with transaction ref

### TC-2.8: Create Booking with Agent Referral
**Steps:**
1. Navigate to `/bookings/create`
2. On Step 0, select an Agent from the dropdown
3. Verify commission info displayed (type, rate)
4. If agent allows surcharge, enter surcharge amount (e.g., 200)
5. Optionally check "Hide surcharge from customer"
6. Complete all steps through confirmation

**Expected:**
- BookingSource = "Agent"
- Agent info card visible in booking details
- Commission calculated based on agent's type:
  - Percentage: rate % of total
  - FixedPerBooking: fixed amount
  - FixedPerVehicle: fixed amount x vehicle count
  - FixedPerDay: fixed amount x rental days
- Shop receivable = TotalAmount - AgentCommission
- Surcharge visible/hidden per checkbox setting

### TC-2.9: Create Booking - Document OCR Scan
**Steps:**
1. On Step 0, click the document scan button
2. Upload a passport image in the ScanDocumentDialog
3. Wait for OCR processing (Gemini Flash API)

**Expected:**
- Customer Name auto-populated from passport
- Passport number extracted
- Nationality detected
- Fields can be manually corrected after scan

### TC-2.10: Create Booking - Link Existing Renter
**Steps:**
1. On Step 0, use the renter search/link feature
2. Search by phone number of an existing renter
3. Select the renter from results

**Expected:**
- Customer fields populated from renter record
- Linked renter shown with name and phone
- RenterId stored on booking

---

## Section 3: Booking List (`/bookings`)

### TC-3.1: View Booking List
**Steps:**
1. Navigate to `/bookings`
2. Observe summary cards at the top

**Expected:**
- Summary cards show: Today Arrivals, Pending, Confirmed, Checked In counts
- Booking cards displayed with: date, status badge, customer name, phone, vehicle count, dates
- BookingRef displayed and clickable

### TC-3.2: Filter by Status Tab
**Steps:**
1. On booking list, click "Pending" tab
2. Then click "Confirmed" tab
3. Then click "All" tab

**Expected:**
- List filters to show only bookings with selected status
- Counts in summary cards match filtered results

### TC-3.3: Search Bookings
**Steps:**
1. On booking list, type a customer name in the search bar
2. Clear search, type a phone number
3. Clear search, type a BookingRef

**Expected:**
- Results filter in real-time by search term
- Matches by customer name, phone, or booking reference

### TC-3.4: Filter by Date
**Steps:**
1. On booking list, select a specific date from the date picker

**Expected:**
- Only bookings for selected date displayed
- "Today Arrivals" card reflects today's date filter

### TC-3.5: Payment Status Badges
**Steps:**
1. Create bookings with different payment states (paid, partial, unpaid)
2. View booking list

**Expected:**
- "Paid" badge (green with checkmark) for FullyPaid
- "PartiallyPaid" badge (orange with coin) for PartiallyPaid
- No payment badge for Unpaid

---

## Section 4: Booking Details (`/bookings/{BookingRef}`)

### TC-4.1: View Booking Details
**Steps:**
1. Click a booking from the list, or navigate to `/bookings/{BookingRef}`

**Expected:**
- Status card with current status badge
- Customer card with name, phone, email, nationality, hotel
- Vehicles card listing all booking items with insurance and costs
- Payment summary in right sidebar showing total, paid, balance
- Schedule card with pickup/return dates and duration
- Change history card showing recent changes

### TC-4.2: Confirm Pending Booking
**Precondition:** Booking with Status = "Pending"

**Steps:**
1. Open booking details
2. Click "Change Status" button
3. Select "Confirmed"

**Expected:**
- Status changes to "Confirmed" with success badge
- Change recorded in Change History

### TC-4.3: Record Additional Payment
**Precondition:** Booking with balance due > 0, active till session (if till enabled).

**Steps:**
1. Open booking details
2. Click "Record Payment" button
3. In RecordBookingPaymentDialog:
   - Click "Balance Due" quick button (fills remaining amount)
   - Select Payment Method: PromptPay
   - Enter Transaction Ref
   - Click Save

**Expected:**
- Payment recorded in payment history
- Amount paid updates, balance due decreases
- If full amount paid: PaymentStatus = "FullyPaid"
- Payment type auto-set to "Full" if amount >= balance
- Till transaction created (if till enabled)

### TC-4.4: Record Payment - Partial
**Steps:**
1. Open booking with balance due > 0
2. Record payment with amount less than balance due
3. Select Payment Method: BankTransfer
4. Enter Transaction Ref

**Expected:**
- PaymentStatus = "PartiallyPaid"
- Payment type = "Partial"
- Balance due reduced but not zero

### TC-4.5: Record Payment - No Active Till
**Precondition:** Till feature enabled but no active session.

**Steps:**
1. Open booking details
2. Click "Record Payment"

**Expected:**
- Prompt to open till session
- Redirects to `/staff/till` page
- Cannot record payment without active till session

### TC-4.6: Cancel Booking
**Precondition:** Booking with Status = "Pending" or "Confirmed" (not CheckedIn or Completed).

**Steps:**
1. Open booking details
2. Click "Cancel" button
3. Enter cancellation reason
4. Confirm cancellation

**Expected:**
- Status changes to "Cancelled"
- CancelledOn timestamp recorded
- CancellationReason stored
- Cannot further modify or check in
- CanBeCancelled = false for CheckedIn/Completed/Cancelled bookings

### TC-4.7: Cancel Booking - Already Checked In
**Precondition:** Booking with at least 1 item checked in.

**Steps:**
1. Open booking details
2. Verify Cancel button is not visible

**Expected:**
- Cancel button hidden or disabled when any item is checked in

### TC-4.8: Navigate to Check-In from Booking
**Precondition:** Booking with Status = "Pending" or "Confirmed".

**Steps:**
1. Open booking details
2. Click "Check In" button

**Expected:**
- Navigates to `/rentals/checkin/{BookingRef}`
- Check-in wizard pre-fills from booking data

### TC-4.9: Print Booking Confirmation
**Steps:**
1. Open booking details
2. Click "Print" button (or dropdown for multiple templates)
3. Select template if multiple available

**Expected:**
- Print dialog opens with booking confirmation
- All booking details rendered correctly

### TC-4.10: View Payment Details
**Steps:**
1. Open booking with payment history
2. Click a payment row in the payment history

**Expected:**
- Offcanvas slides in from right
- Shows: Payment amount, PaymentId, Date, Time, Method, Type
- Transaction ref (if non-cash)
- Card last 4 digits (if card)
- Recorded by username

---

## Section 5: Agent Commission Workflow

### TC-5.1: View Agent Commission on Booking
**Steps:**
1. Create a booking with an agent referral
2. Open booking details

**Expected:**
- Agent Info card displayed with:
  - Agent name and code
  - Commission amount
  - Surcharge amount (if applicable)
  - Shop receivable amount

### TC-5.2: Commission Calculation - Percentage
**Setup:** Agent with CommissionType = "Percentage", Rate = 10%

**Steps:**
1. Create booking with this agent, total amount = 3000

**Expected:**
- Commission = 300 (10% of 3000)
- Respects MinCommission and MaxCommission caps if set

### TC-5.3: Commission Calculation - Fixed Per Vehicle
**Setup:** Agent with CommissionType = "FixedPerVehicle", Rate = 200

**Steps:**
1. Create booking with 3 vehicles

**Expected:**
- Commission = 600 (200 x 3 vehicles)

### TC-5.4: Commission Calculation - Fixed Per Day
**Setup:** Agent with CommissionType = "FixedPerDay", Rate = 100

**Steps:**
1. Create booking for 5 days

**Expected:**
- Commission = 500 (100 x 5 days)

---

## Section 6: Edge Cases & Error Scenarios

### TC-6.1: Create Booking - Very Long Rental Period
**Steps:**
1. Create booking with Start Date = today, End Date = 30 days from now
2. Select 1 vehicle

**Expected:**
- Duration calculated as 30 days
- Total amount = daily rate x 30
- Deposit amount correct

### TC-6.2: Create Booking - Same Day Rental
**Steps:**
1. Set Start Date = today, End Date = today
2. Complete booking

**Expected:**
- Duration = 1 day (minimum)
- Amount calculated for 1 day

### TC-6.3: Booking with All Payment Methods
**Steps:**
1. Create 4 separate bookings, each with a different payment method:
   - Cash
   - Card (enter transaction ref)
   - PromptPay (enter transaction ref)
   - BankTransfer (enter transaction ref)

**Expected:**
- All methods accepted and recorded
- Non-cash methods require transaction reference field
- Cash method does not show transaction ref field

### TC-6.4: Navigate Back Through Wizard Steps
**Steps:**
1. Start booking creation, reach Step 3
2. Click Previous repeatedly to go back to Step 0
3. Modify customer name
4. Click Next through all steps again

**Expected:**
- All previously entered data preserved
- Modified data reflected in confirmation step
- Can navigate back and forth without data loss

### TC-6.5: Concurrent Till Sessions
**Steps:**
1. Open till session in Shop A
2. Check if user sees warning about existing open sessions

**Expected:**
- Existing open sessions listed on the "Open Till" screen
- Shows shop names and opened times for each session

### TC-6.6: Agent Surcharge - Hidden from Customer
**Steps:**
1. Create booking with agent that allows surcharges
2. Enter surcharge = 300
3. Check "Hide surcharge from customer"
4. Review confirmation step

**Expected:**
- CustomerVisibleTotal does not include surcharge
- ShopReceivableAmount calculated correctly
- Surcharge visible in booking details for staff

---

## Quick Pass/Fail Checklist

| # | Test Case | Pass | Fail | Notes |
|---|-----------|------|------|-------|
| 1.1 | Open till session | | | |
| 1.2 | Stale session warning | | | |
| 1.3 | Close till session | | | |
| 1.4 | Quick payment - rental | | | |
| 1.5 | Quick payout - agent commission | | | |
| 1.6 | Cash drop | | | |
| 1.7 | Void transaction | | | |
| 1.8 | Reprint receipt | | | |
| 2.1 | Create basic booking | | | |
| 2.2 | Validation errors (Step 0) | | | |
| 2.3 | No vehicle selected | | | |
| 2.4 | Date validation | | | |
| 2.5 | Multiple vehicles | | | |
| 2.6 | No payment | | | |
| 2.7 | Full payment | | | |
| 2.8 | Agent referral booking | | | |
| 2.9 | Document OCR scan | | | |
| 2.10 | Link existing renter | | | |
| 3.1 | View booking list | | | |
| 3.2 | Filter by status tab | | | |
| 3.3 | Search bookings | | | |
| 3.4 | Filter by date | | | |
| 3.5 | Payment status badges | | | |
| 4.1 | View booking details | | | |
| 4.2 | Confirm pending booking | | | |
| 4.3 | Record additional payment | | | |
| 4.4 | Record partial payment | | | |
| 4.5 | No active till | | | |
| 4.6 | Cancel booking | | | |
| 4.7 | Cancel - already checked in | | | |
| 4.8 | Navigate to check-in | | | |
| 4.9 | Print confirmation | | | |
| 4.10 | View payment details | | | |
| 5.1 | Agent commission on booking | | | |
| 5.2 | Commission - percentage | | | |
| 5.3 | Commission - per vehicle | | | |
| 5.4 | Commission - per day | | | |
| 6.1 | Long rental period | | | |
| 6.2 | Same day rental | | | |
| 6.3 | All payment methods | | | |
| 6.4 | Navigate back in wizard | | | |
| 6.5 | Concurrent till sessions | | | |
| 6.6 | Agent surcharge hidden | | | |
