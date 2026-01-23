# Cashier-Till Manual Testing Plan

## Overview
Manual testing guide for the MotoRent Cashier-Till functionality, covering till session management, payment processing, receipt generation, and reconciliation.

## Prerequisites

### Environment Setup
1. Start the development server:
   ```bash
   dotnet watch --project src/MotoRent.Server
   ```
2. Navigate to: `https://localhost:7103`

### Test User
- **Impersonate URL**: `/account/impersonate?user=staff@krabirentals.com&account=KrabiBeachRentals&hash={MD5}`
- **MD5 Hash**: `MD5("staff@krabirentals.com:KrabiBeachRentals")`
- **Role**: Staff user for Krabi Beach Rentals tenant

### Test Data
Ensure the KrabiBeachRentals schema has:
- Shop with vehicles (Honda Click, PCX, Yamaha NMAX)
- Insurance packages (Basic, Premium, Full Coverage)
- Accessories (Helmets, Phone Holder, etc.)
- Unpaid bookings for payment testing

---

## Test Cases

### 1. Open Till Session

**Steps:**
1. Navigate to `/staff/till`
2. Click "Open Till Session"
3. Select shop from dropdown
4. Enter opening float denominations:
   - ฿1,000 x 5 = ฿5,000
   - ฿500 x 6 = ฿3,000
   - ฿100 x 15 = ฿1,500
   - ฿50 x 6 = ฿300
   - ฿20 x 10 = ฿200
   - **Total: ฿10,000**
5. Click "Open Session"

**Expected Results:**
- Till dashboard shows "Session Active"
- Till Balance displays ฿10,000
- Opening Float shows ฿10,000
- Cash In/Out shows ฿0

---

### 2. Process Booking Deposit - Card Payment

**Steps:**
1. Click "Booking Deposit" in Quick Payments
2. Search for booking by reference (e.g., `NC23EY`)
3. Click on booking to select
4. Change Payment Method to "Card"
5. Verify amount auto-fills to balance due
6. Click "Record Payment"

**Expected Results:**
- Receipt generated (RCP-YYMMDD-XXXXX format)
- Receipt shows Card payment method
- Non-Cash Payments section updates with Card amount
- Booking status changes to "FullyPaid"

---

### 3. Process Booking Deposit - PromptPay Payment

**Steps:**
1. Click "Booking Deposit"
2. Search for different booking
3. Select booking
4. Change Payment Method to "PromptPay"
5. Enter amount
6. Click "Record Payment"

**Expected Results:**
- Receipt generated
- PromptPay appears in Non-Cash Payments breakdown
- Total Non-Cash amount increases

---

### 4. Process Booking Deposit - Bank Transfer

**Steps:**
1. Click "Booking Deposit"
2. Search for booking
3. Select booking
4. Change Payment Method to "Bank Transfer"
5. Enter amount
6. Click "Record Payment"

**Expected Results:**
- Receipt generated
- Bank Transfer appears in Non-Cash Payments
- Booking marked as paid

---

### 5. Process Booking Deposit - Cash Payment (THB)

**Steps:**
1. Click "Booking Deposit"
2. Search and select booking
3. Keep Payment Method as "Cash"
4. Select currency "THB"
5. Enter denominations received:
   - Example: ฿1,000 x 1 + ฿100 x 2 = ฿1,200
6. Verify Total updates correctly
7. Click "Record Payment"

**Expected Results:**
- Receipt generated
- Cash In section updates
- Till Balance increases by cash amount
- Booking marked as paid

---

### 6. Verify Receipt Content

**For each receipt, verify:**

**Header:**
- [ ] Receipt number (RCP-YYMMDD-XXXXX)
- [ ] Issue date and time
- [ ] Shop name

**Customer Section:**
- [ ] Customer name
- [ ] Phone number

**Vehicle Section:**
- [ ] Vehicle brand and model
- [ ] Engine CC

**Line Items:**
- [ ] Description (Booking Deposit, Rental Payment, etc.)
- [ ] Quantity
- [ ] Unit Price
- [ ] Amount
- [ ] Total

**Payment Section:**
- [ ] Payment method badge (Cash/Card/PromptPay/BankTransfer)
- [ ] Currency
- [ ] Amount
- [ ] Reference (if applicable)

**Footer:**
- [ ] "Thank you for your business!"
- [ ] Issued by staff email
- [ ] Timestamp

---

### 7. Close Till Session

**Steps:**
1. Scroll to "Till Operations" section
2. Click "Close Shift"
3. Enter closing denomination count:
   - Must match expected cash balance
   - For no cash transactions: same as opening float
4. Review variance (should be ฿0 for balanced till)
5. Click "Review Summary"
6. Verify Closing Summary shows:
   - Currency: THB
   - Expected vs Counted
   - Variance: +฿0
7. Click "Close Session"

**Expected Results:**
- Till session closes successfully
- Page shows "Start Your Shift" prompt
- Database shows TillSession with Status = "Closed"
- IsForceClose = 0 (normal close)

---

## Verification Queries

### Check Receipts
```sql
SELECT [ReceiptId], [ReceiptNo], [ReceiptType], [CustomerName],
       [GrandTotal], [Status]
FROM [KrabiBeachRentals].[Receipt]
ORDER BY [ReceiptId] DESC
```

### Check Paid Bookings
```sql
SELECT [BookingId], [BookingRef], [CustomerName],
       [TotalAmount], [AmountPaid], [PaymentStatus]
FROM [KrabiBeachRentals].[Booking]
WHERE [PaymentStatus] = 'FullyPaid'
```

### Check Till Sessions
```sql
SELECT [TillSessionId], [Status], [StaffUserName],
       [OpenedAt], [ClosedAt], [IsForceClose]
FROM [KrabiBeachRentals].[TillSession]
ORDER BY [TillSessionId] DESC
```

---

## Known Issues

### Cash Denomination Entry (Browser Automation)
- **Issue**: Blazor two-way binding doesn't sync when DOM values are set programmatically
- **Symptom**: Denomination counts show in DOM but Total remains ฿0
- **Workaround**: Use Card/PromptPay/BankTransfer for automated testing
- **Manual Testing**: Works correctly with direct user input

### Booking Selection
- Search returns bookings but clicking may not always trigger Blazor events
- Use booking reference search for reliable selection

---

## Test Data Generation

### Create Test Bookings via API/Database
If more bookings needed, seed via PowerShell script:
```powershell
.\database\scripts\provision-KrabiBeachRentals.ps1
```

Or insert directly with varied:
- Customer names (international tourists)
- Vehicles (Honda Click, PCX, Yamaha NMAX, Aerox)
- Date ranges (1-7 day rentals)
- Insurance options (Basic, Premium, Full Coverage)
- Accessories (Helmets, Phone Holder, Rain Poncho)

---

## Payment Method Summary

| Method | Non-Cash | Affects Till Balance | Notes |
|--------|----------|---------------------|-------|
| Cash | No | Yes | Requires denomination entry |
| Card | Yes | No | Simple amount field |
| PromptPay | Yes | No | Simple amount field |
| Bank Transfer | Yes | No | Simple amount field |

---

## Session Summary Template

After testing, document:

| Metric | Value |
|--------|-------|
| Opening Float | ฿_____ |
| Total Cash In | ฿_____ |
| Total Cash Out | ฿_____ |
| Total Non-Cash | ฿_____ |
| Receipts Issued | _____ |
| Closing Balance | ฿_____ |
| Variance | ฿_____ |
| Session Duration | _____ |
