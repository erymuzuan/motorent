# Edge Case Tests

## Test Category: Edge Cases and Special Scenarios

### Prerequisites
- User logged in as OrgAdmin or ShopManager
- Various assets in different states
- Access to Finance menu

---

## Test Case 6.1: Pre-existing Vehicle with Initial Book Value

**Objective:** Verify handling of vehicles acquired before system implementation.

**Test Data:**
- Original Acquisition Date: 2 years ago
- Original Acquisition Cost: 80,000 THB
- Initial Book Value (current): 50,000 THB
- System Entry Date: Today

**Steps:**
1. Create new asset with "Pre-existing vehicle" checked
2. Enter original acquisition details
3. Enter current book value (50,000)
4. Save and view asset

**Expected Results:**
- [ ] Asset shows both original cost and initial book value
- [ ] Current book value starts at 50,000 (not 80,000)
- [ ] Depreciation calculates from system entry date
- [ ] Pre-existing flag visible in asset details
- [ ] Accumulated depreciation shows 0 at start (even though 30,000 already lost)

---

## Test Case 6.2: First Rental Triggers Day Out of Door

**Objective:** Verify Day Out of Door depreciation triggers correctly on first rental.

**Test Data:**
- Asset with Day Out of Door method
- Day Out of Door %: 20%
- Acquisition Cost: 50,000 THB

**Steps:**
1. Create asset with Day Out of Door depreciation
2. Verify asset has no first rental date
3. Create and complete a rental for the associated vehicle
4. Check asset's depreciation history

**Expected Results:**
- [ ] Before rental: Book value = 50,000, No depreciation entries
- [ ] After rental: Depreciation entry of 10,000 THB created
- [ ] Book value now 40,000 THB
- [ ] Entry type shows "Day Out of Door"
- [ ] First rental date recorded on asset
- [ ] Subsequent rentals don't trigger additional Day Out of Door

---

## Test Case 6.3: Depreciation Below Residual Value Prevention

**Objective:** Verify depreciation stops at residual value.

**Test Data:**
- Current Book Value: 5,500 THB
- Residual Value: 5,000 THB
- Monthly Depreciation (normal): 750 THB

**Steps:**
1. Attempt to record monthly depreciation
2. Review calculated amount

**Expected Results:**
- [ ] System calculates only 500 THB (difference to residual)
- [ ] Cannot depreciate below 5,000 THB
- [ ] Warning message displayed about reaching residual
- [ ] After recording, book value = exactly 5,000 THB

---

## Test Case 6.4: Fully Depreciated Asset

**Objective:** Verify behavior when asset reaches full depreciation.

**Steps:**
1. Find/create asset at residual value (fully depreciated)
2. Attempt to record more depreciation
3. Review asset status and options

**Expected Results:**
- [ ] Depreciation amount shows 0 or "Fully depreciated"
- [ ] Cannot record further depreciation
- [ ] Asset still functional (can track expenses, revenue)
- [ ] Dashboard shows asset correctly
- [ ] ROI calculation still works

---

## Test Case 6.5: Loan Payoff - Final Payment

**Objective:** Verify loan completion process.

**Steps:**
1. Have loan with only 1 payment remaining
2. Record final payment
3. Check loan status

**Expected Results:**
- [ ] Final payment recorded successfully
- [ ] Loan status changes to "Paid Off"
- [ ] Outstanding balance = 0
- [ ] No more payments due
- [ ] Asset loan reference updated/cleared

---

## Test Case 6.6: Asset Disposal with Gain

**Objective:** Verify disposal when selling above book value.

**Test Data:**
- Current Book Value: 30,000 THB
- Sale Amount: 35,000 THB
- Expected Gain: 5,000 THB

**Steps:**
1. Navigate to asset
2. Click "Dispose Asset"
3. Enter disposal date and sale amount (35,000)
4. Confirm disposal

**Expected Results:**
- [ ] Gain/Loss calculated as +5,000 THB
- [ ] Asset status changes to "Disposed"
- [ ] Disposal date recorded
- [ ] Gain appears in activity/reports
- [ ] Asset no longer in active list

---

## Test Case 6.7: Asset Disposal with Loss

**Objective:** Verify disposal when selling below book value.

**Test Data:**
- Current Book Value: 30,000 THB
- Sale Amount: 22,000 THB
- Expected Loss: 8,000 THB

**Steps:**
1. Navigate to asset
2. Click "Dispose Asset"
3. Enter disposal date and sale amount (22,000)
4. Confirm disposal

**Expected Results:**
- [ ] Gain/Loss calculated as -8,000 THB (loss)
- [ ] Loss displayed in red or negative format
- [ ] Asset status changes to "Disposed"
- [ ] Loss impacts profitability reporting

---

## Test Case 6.8: Asset Write-Off (Total Loss)

**Objective:** Verify write-off for theft or total loss.

**Test Data:**
- Current Book Value: 40,000 THB

**Steps:**
1. Navigate to asset
2. Click "Write Off Asset"
3. Enter reason: "Theft - police report #12345"
4. Confirm write-off

**Expected Results:**
- [ ] Asset status changes to "Write Off"
- [ ] Full book value recorded as loss (40,000)
- [ ] Reason saved in records
- [ ] Asset removed from active operations
- [ ] Loss reflected in reports

---

## Test Case 6.9: Negative ROI Display

**Objective:** Verify correct display of negative ROI.

**Test Setup:**
- Asset with high expenses and low revenue resulting in negative ROI

**Steps:**
1. View asset with negative ROI
2. Check dashboard and reports

**Expected Results:**
- [ ] Negative ROI displayed with minus sign
- [ ] Color-coded red or warning color
- [ ] Appears in "Underperforming" alerts
- [ ] Profitability report shows correctly
- [ ] Sorting places negative ROI assets appropriately

---

## Test Case 6.10: Zero Revenue Asset

**Objective:** Verify handling of asset with no rental revenue.

**Steps:**
1. View asset that has never been rented
2. Check ROI and profitability calculations

**Expected Results:**
- [ ] Revenue shows 0 or "-"
- [ ] ROI calculation handles zero revenue
- [ ] No division by zero errors
- [ ] Asset still appears in reports
- [ ] Expenses and depreciation tracked normally

---

## Test Case 6.11: Multiple Depreciation Methods Change

**Objective:** Verify changing depreciation method mid-life.

**Steps:**
1. Create asset with Straight Line method
2. Record several months of depreciation
3. Edit asset and change to Declining Balance
4. Record new depreciation

**Expected Results:**
- [ ] Previous entries remain unchanged (Straight Line amounts)
- [ ] New entry uses Declining Balance calculation
- [ ] History shows both methods
- [ ] Total accumulated depreciation is sum of all entries

---

## Test Case 6.12: Very Long Useful Life

**Objective:** Verify handling of extended useful life periods.

**Test Data:**
- Useful Life: 120 months (10 years)
- Acquisition Cost: 100,000 THB
- Residual Value: 10,000 THB

**Steps:**
1. Create asset with 120-month useful life
2. View depreciation projections
3. Record depreciation

**Expected Results:**
- [ ] Monthly depreciation = 750 THB
- [ ] Projection chart extends 10 years
- [ ] System handles long period without issues
- [ ] UI displays months and years clearly

---

## Test Case 6.13: Very Short Useful Life

**Objective:** Verify handling of short useful life (rapid depreciation).

**Test Data:**
- Useful Life: 12 months
- Acquisition Cost: 30,000 THB
- Residual Value: 5,000 THB

**Steps:**
1. Create asset with 12-month useful life
2. View monthly depreciation amount
3. Record depreciation

**Expected Results:**
- [ ] Monthly depreciation = 2,083 THB
- [ ] Fully depreciated within 12 months
- [ ] System handles accelerated depreciation
- [ ] Projection shows correct end date

---

## Test Case 6.14: Currency and Large Numbers

**Objective:** Verify display of large currency values.

**Test Data:**
- Asset with Acquisition Cost: 1,500,000 THB (1.5 million)

**Steps:**
1. Create high-value asset
2. View in dashboard and reports
3. Check number formatting

**Expected Results:**
- [ ] Numbers display with thousand separators (1,500,000)
- [ ] No overflow or truncation
- [ ] Charts scale appropriately
- [ ] Totals calculate correctly

---

## Test Case 6.15: Concurrent User Operations

**Objective:** Verify system handles concurrent edits gracefully.

**Steps:**
1. Open same asset in two browser windows
2. Edit different fields in each window
3. Save both

**Expected Results:**
- [ ] First save succeeds
- [ ] Second save either:
  - Warns about concurrent modification
  - Merges changes correctly
  - Shows conflict resolution
- [ ] No data corruption

---

## Notes

- Document any unexpected behavior
- Test boundary conditions carefully
- Verify audit trail captures special operations
- Check for appropriate error messages
