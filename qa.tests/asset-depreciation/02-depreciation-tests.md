# Depreciation Tests

## Test Category: Depreciation Recording and Methods

### Prerequisites
- User logged in as OrgAdmin or ShopManager
- At least one active asset exists with depreciation settings configured
- Access to Finance menu

---

## Test Case 2.1: Record Monthly Depreciation - Straight Line

**Objective:** Verify monthly depreciation recording for Straight Line method.

**Test Data:**
- Asset: Honda Click 125 (or any available asset)
- Acquisition Cost: 50,000 THB
- Useful Life: 60 months
- Residual Value: 5,000 THB
- Expected Monthly Depreciation: (50,000 - 5,000) / 60 = 750 THB

**Steps:**
1. Navigate to **Finance > Depreciation** (or Asset Dashboard > Run Monthly Depreciation)
2. Select the period (current month/year)
3. Find the asset in the list
4. Verify the calculated depreciation amount
5. Click **Record Depreciation** or **Confirm**

**Expected Results:**
- [ ] Calculated amount equals 750 THB
- [ ] Depreciation is recorded successfully
- [ ] Asset's Current Book Value decreases by 750 THB
- [ ] Asset's Accumulated Depreciation increases by 750 THB
- [ ] Depreciation entry appears in asset's history

---

## Test Case 2.2: Record Depreciation - Declining Balance

**Objective:** Verify depreciation calculation for Declining Balance method.

**Test Data:**
- Asset configured with Declining Balance method
- Acquisition Cost: 60,000 THB
- Annual Rate: 20%
- Current Book Value: 48,000 THB
- Expected Monthly Depreciation: 48,000 × (20% / 12) = 800 THB

**Steps:**
1. Navigate to **Finance > Depreciation**
2. Select the period
3. Find the declining balance asset
4. Verify the calculated amount
5. Record the depreciation

**Expected Results:**
- [ ] Calculated amount approximately 800 THB (percentage of book value)
- [ ] Book value updates correctly
- [ ] Entry shows "Declining Balance" as method
- [ ] Next month calculation uses new (lower) book value

---

## Test Case 2.3: Day Out of Door Depreciation on First Rental

**Objective:** Verify automatic Day Out of Door depreciation triggers on first rental.

**Test Data:**
- Asset with Day Out of Door method
- Acquisition Cost: 50,000 THB
- Day Out of Door %: 20%
- Expected immediate depreciation: 10,000 THB

**Steps:**
1. Create a new rental for the vehicle associated with this asset
2. Complete the rental check-out process
3. Navigate to **Finance > Asset Dashboard**
4. View the asset's details

**Expected Results:**
- [ ] Depreciation entry created automatically
- [ ] Amount equals 10,000 THB (20% of 50,000)
- [ ] Entry type shows "Day Out of Door" or similar
- [ ] Current Book Value reduced to 40,000 THB
- [ ] Asset marked as "First rental completed"

---

## Test Case 2.4: Manual Depreciation Override

**Objective:** Verify ability to override calculated depreciation with manual amount.

**Steps:**
1. Navigate to **Finance > Assets > [Select Asset]**
2. Click **"Record Depreciation"**
3. Enter the period dates
4. Check **"Override calculated amount"** checkbox
5. Enter custom amount: 1,200 THB
6. Enter reason: "Adjustment for accelerated wear"
7. Click **Save**

**Expected Results:**
- [ ] Override checkbox enables manual amount entry
- [ ] System accepts the override amount
- [ ] Reason field is required when overriding
- [ ] Entry is recorded with "Manual" type indicator
- [ ] Asset values update correctly
- [ ] Audit trail shows override reason

---

## Test Case 2.5: Batch Depreciation Processing

**Objective:** Verify recording depreciation for multiple assets at once.

**Steps:**
1. Navigate to **Finance > Depreciation**
2. Click **"Run Monthly Depreciation"**
3. Select the period (month/year)
4. Review the list of assets requiring depreciation
5. Select multiple assets using checkboxes
6. Click **"Process Selected"** or **"Confirm All"**

**Expected Results:**
- [ ] All active assets are listed
- [ ] Calculated amounts shown for each asset
- [ ] Able to select multiple assets
- [ ] All selected assets processed successfully
- [ ] Summary shows total depreciation recorded
- [ ] Individual asset records updated correctly

---

## Test Case 2.6: Depreciation Stops at Residual Value

**Objective:** Verify depreciation stops when book value reaches residual value.

**Test Data:**
- Asset with book value close to residual value
- Current Book Value: 5,500 THB
- Residual Value: 5,000 THB
- Monthly Depreciation (calculated): 750 THB

**Steps:**
1. Navigate to depreciation recording
2. Attempt to record depreciation for this asset
3. Review the calculated/allowed amount

**Expected Results:**
- [ ] System calculates only 500 THB (not 750)
- [ ] Depreciation limited to not go below residual
- [ ] Warning or info message displayed
- [ ] After recording, book value equals exactly 5,000 THB
- [ ] Future depreciation attempts show 0 or "Fully depreciated"

---

## Test Case 2.7: View Depreciation History

**Objective:** Verify complete depreciation history is displayed.

**Steps:**
1. Navigate to **Finance > Assets > [Select Asset with history] > Details**
2. Scroll to Depreciation History section
3. Review the history table

**Expected Results:**
- [ ] All depreciation entries displayed in chronological order
- [ ] Each entry shows:
  - Period (month/year with dates)
  - Method used
  - Book Value Start
  - Depreciation Amount
  - Book Value End
  - Entry Type (System/Manual)
- [ ] Totals match accumulated depreciation on summary

---

## Test Case 2.8: View Depreciation Projections

**Objective:** Verify future depreciation projections are calculated correctly.

**Steps:**
1. Navigate to **Finance > Assets > [Select Asset] > Details**
2. Find the Asset Value Chart
3. Click **"Projection"** toggle button
4. Review the projection chart

**Expected Results:**
- [ ] Chart shows projected book value over time
- [ ] Projection extends to end of useful life
- [ ] Book value line slopes downward correctly
- [ ] Ends at residual value (floor)
- [ ] Y-axis shows values from acquisition cost to residual value

---

## Test Case 2.9: Depreciation Report - Monthly Summary

**Objective:** Verify the Depreciation Report page monthly summary view.

**Steps:**
1. Navigate to **Finance > Depreciation Report**
2. Select current year
3. Select **"Monthly Summary"** report type
4. Review the report

**Expected Results:**
- [ ] Summary cards show:
  - Total Depreciation for year
  - Average Monthly depreciation
  - Assets Depreciated count
  - Total Entries count
- [ ] Bar chart displays monthly breakdown
- [ ] Monthly table shows each month with:
  - Depreciation Amount
  - Number of Entries
  - Percentage of Year

---

## Test Case 2.10: Depreciation Report - By Asset View

**Objective:** Verify the Depreciation Report page by-asset view.

**Steps:**
1. Navigate to **Finance > Depreciation Report**
2. Select current year
3. Select **"By Asset"** report type
4. Review the report

**Expected Results:**
- [ ] Table shows each asset with:
  - Vehicle name and license plate
  - Starting Book Value (for year)
  - Total Depreciation (for year)
  - Ending Book Value
  - Number of entries
- [ ] Link to Asset Details works
- [ ] Totals at bottom match summary cards

---

## Test Case 2.11: Export Depreciation Report

**Objective:** Verify export functionality of depreciation report.

**Steps:**
1. Navigate to **Finance > Depreciation Report**
2. Select year and report type
3. Click **"Export CSV"** button
4. Open the downloaded file

**Expected Results:**
- [ ] File downloads successfully
- [ ] CSV file opens correctly in Excel/Sheets
- [ ] Data matches what's displayed on screen
- [ ] Headers are readable
- [ ] Numbers formatted correctly

---

## Notes

- Depreciation calculations should be verified against manual calculations
- Record screenshots of calculated vs. expected values
- Note any rounding differences
