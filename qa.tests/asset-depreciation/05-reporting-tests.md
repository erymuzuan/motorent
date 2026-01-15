# Reporting Tests

## Test Category: Reports and Dashboard

### Prerequisites
- User logged in as OrgAdmin or ShopManager
- Multiple assets with depreciation history, expenses, and rental revenue
- Access to Finance menu

---

## Test Case 5.1: Asset Dashboard - KPI Cards

**Objective:** Verify all KPI summary cards display correct values.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Review each KPI card:
   - Total Invested
   - Book Value
   - Accumulated Depreciation
   - Net Profit/Loss
   - Fleet ROI

**Expected Results:**
- [ ] Total Invested = Sum of all acquisition costs
- [ ] Book Value = Sum of all current book values
- [ ] Accumulated Depreciation = Sum of all depreciation
- [ ] Net Profit/Loss = Total Revenue - Total Expenses - Total Depreciation
- [ ] Fleet ROI % = (Net Profit / Total Invested) × 100
- [ ] Percentages and sub-text are accurate

---

## Test Case 5.2: Asset Dashboard - Depreciation Trend Chart

**Objective:** Verify depreciation trend chart displays correctly.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find the "Depreciation Trend" chart (left chart)
3. Review 12-month data

**Expected Results:**
- [ ] Chart shows 12 months of data
- [ ] Two data series visible (Book Value and Monthly Depreciation)
- [ ] Legend correctly identifies each series
- [ ] Values decrease over time (book value)
- [ ] Link to full report works

---

## Test Case 5.3: Asset Dashboard - Expense Breakdown Chart

**Objective:** Verify expense breakdown donut chart.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find the "Expense Breakdown" donut chart (right chart)
3. Review category breakdown

**Expected Results:**
- [ ] Donut chart displays all expense categories
- [ ] Colors are distinct and readable
- [ ] Legend shows:
  - Category name
  - Amount
  - Percentage
- [ ] Percentages add up to 100%
- [ ] Total matches sum of all expenses

---

## Test Case 5.4: Asset Dashboard - Top Performers

**Objective:** Verify top performers list shows highest ROI assets.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find "Top Performers" section
3. Review the rankings

**Expected Results:**
- [ ] Up to 5 assets displayed
- [ ] Ranked by ROI (highest first)
- [ ] Gold/Silver/Bronze indicators for top 3
- [ ] Each item shows:
  - Vehicle name and plate
  - ROI percentage with trend arrow
  - Revenue amount
- [ ] Link to profitability report works

---

## Test Case 5.5: Asset Dashboard - Attention Needed Alerts

**Objective:** Verify attention alerts are accurate and actionable.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find "Attention Needed" section
3. Review each alert type

**Expected Results:**
- [ ] Depreciation Due: Count of assets needing monthly depreciation
- [ ] Overdue Payments: Count of loan payments past due
- [ ] Upcoming Payments: Count of payments due within 7 days
- [ ] Underperforming: Count of assets with negative ROI
- [ ] Each alert links to relevant page
- [ ] "All Clear" message if no alerts

---

## Test Case 5.6: Asset Dashboard - Quick Actions

**Objective:** Verify quick action buttons work correctly.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find "Quick Actions" section
3. Click each action button

**Expected Results:**
- [ ] Run Monthly Depreciation → Opens depreciation page
- [ ] Record Expense → Opens expense creation
- [ ] View All Assets → Shows asset list
- [ ] Manage Loans → Opens loans page

---

## Test Case 5.7: Asset Dashboard - Recent Activity

**Objective:** Verify recent activity timeline displays correctly.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find "Recent Activity" section
3. Review timeline entries

**Expected Results:**
- [ ] Shows most recent 5-10 activities
- [ ] Activity types include:
  - Depreciation entries
  - Expenses recorded
  - Loan payments
  - Revenue from rentals
- [ ] Each entry shows:
  - Icon indicating type
  - Title and description
  - Timestamp (humanized, e.g., "2 hours ago")
  - Amount (positive for revenue, negative for expenses)

---

## Test Case 5.8: Asset Dashboard - Asset List with Status

**Objective:** Verify asset list displays with status filtering.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Scroll to asset list section
3. Review list content

**Expected Results:**
- [ ] All assets displayed (or paginated)
- [ ] Each row shows:
  - Vehicle name and license plate
  - Status badge (color-coded)
  - Acquisition cost
  - Current book value
  - Accumulated depreciation
  - ROI percentage
- [ ] View Details button works
- [ ] Edit button works

---

## Test Case 5.9: Profitability Report

**Objective:** Verify profitability report shows accurate ROI data.

**Steps:**
1. Navigate to **Finance > Reports > Profitability**
2. Review the report

**Expected Results:**
- [ ] All assets listed with:
  - Vehicle identification
  - Total Revenue
  - Total Expenses (by category breakdown)
  - Total Depreciation
  - Net Profit/Loss
  - ROI %
- [ ] Sort by ROI works
- [ ] Filter options functional
- [ ] Summary totals at bottom

---

## Test Case 5.10: ROI Calculation Verification

**Objective:** Verify ROI calculation is mathematically correct.

**Test Data:**
- Acquisition Cost: 50,000 THB
- Total Revenue: 30,000 THB
- Total Expenses: 8,000 THB
- Total Depreciation: 12,000 THB
- Expected Net Profit: 30,000 - 8,000 - 12,000 = 10,000 THB
- Expected ROI: (10,000 / 50,000) × 100 = 20%

**Steps:**
1. Find an asset with known values
2. Calculate expected ROI manually
3. Compare with system-displayed ROI

**Expected Results:**
- [ ] ROI % matches manual calculation
- [ ] Net Profit/Loss calculation correct
- [ ] Formula applied consistently across all assets

---

## Test Case 5.11: Depreciation Report - Full Year View

**Objective:** Verify full year depreciation summary.

**Steps:**
1. Navigate to **Finance > Depreciation Report**
2. Select the current year
3. Review summary and breakdown

**Expected Results:**
- [ ] Total Depreciation for year shown
- [ ] Monthly breakdown accurate
- [ ] By-Asset view shows per-vehicle totals
- [ ] Year totals match sum of monthly values

---

## Test Case 5.12: Export Reports

**Objective:** Verify report export functionality.

**Steps:**
1. Navigate to each report page
2. Click export button (CSV)
3. Download and review file

**Expected Results:**
- [ ] Depreciation Report exports correctly
- [ ] Profitability Report exports correctly
- [ ] CSV format is valid
- [ ] Data matches on-screen display
- [ ] Column headers present

---

## Test Case 5.13: Report Date Filtering

**Objective:** Verify date range filtering on reports.

**Steps:**
1. Navigate to a report page
2. Set custom date range
3. Apply filter

**Expected Results:**
- [ ] Only data within range displayed
- [ ] Totals reflect filtered data
- [ ] Clear filter restores all data

---

## Test Case 5.14: Empty State Handling

**Objective:** Verify reports handle no-data scenarios gracefully.

**Test Setup:**
- Use a date range with no data, or view reports before any data entry

**Steps:**
1. Navigate to reports with no applicable data
2. Review display

**Expected Results:**
- [ ] "No data available" or similar message shown
- [ ] Charts display empty state gracefully
- [ ] No errors or broken layouts
- [ ] Instructions for adding data may be shown

---

## Notes

- Verify all percentages round correctly
- Check for negative ROI display (should show clearly)
- Test with large numbers (millions) for formatting
- Test with very small numbers for decimal handling
