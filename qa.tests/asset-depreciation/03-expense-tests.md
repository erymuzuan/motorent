# Expense Tests

## Test Category: Expense Tracking

### Prerequisites
- User logged in as OrgAdmin or ShopManager
- At least one active asset exists
- Access to Finance menu

---

## Test Case 3.1: Record Maintenance Expense

**Objective:** Verify recording a maintenance expense for an asset.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Click **"+ Add Expense"**
3. Fill in the expense details:
   - Select Asset: [Choose a vehicle]
   - Category: **Maintenance**
   - Amount: 2,500 THB
   - Date: Today
   - Description: "Oil change and filter replacement"
   - Vendor: "Phuket Honda Service"
   - Reference: "SVC-2025-001"
4. Click **Save**

**Expected Results:**
- [ ] Expense is created successfully
- [ ] Expense appears in expense list
- [ ] Asset's Total Expenses increases by 2,500 THB
- [ ] Expense shows under Maintenance category

---

## Test Case 3.2: Record Insurance Expense

**Objective:** Verify recording an insurance expense.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Click **"+ Add Expense"**
3. Fill in:
   - Asset: [Select vehicle]
   - Category: **Insurance**
   - Amount: 8,000 THB
   - Date: Policy start date
   - Description: "Annual comprehensive insurance"
   - Vendor: "Thai Insurance Co."
   - Reference: "POL-2025-12345"
   - Check **"Tax Deductible"**
4. Click **Save**

**Expected Results:**
- [ ] Insurance expense recorded
- [ ] Tax deductible flag is saved
- [ ] Appears under Insurance category in reports
- [ ] Asset total expenses updated

---

## Test Case 3.3: Record Accident Expense

**Objective:** Verify recording repair costs from an accident.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Click **"+ Add Expense"**
3. Fill in:
   - Asset: [Select vehicle]
   - Category: **Accident**
   - Amount: 15,000 THB
   - Date: Repair completion date
   - Description: "Front fairing and mirror replacement - minor collision"
   - Vendor: "Body Shop Plus"
   - Reference: "ACC-RPR-001"
4. Click **Save**

**Expected Results:**
- [ ] Accident expense recorded
- [ ] Shows in Accident category
- [ ] Significant impact visible on profitability

---

## Test Case 3.4: Record Registration Expense

**Objective:** Verify recording annual registration fees.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Click **"+ Add Expense"**
3. Fill in:
   - Asset: [Select vehicle]
   - Category: **Registration**
   - Amount: 500 THB
   - Date: Registration date
   - Description: "Annual vehicle registration 2025"
   - Vendor: "DLT Phuket"
   - Reference: "REG-2025"
4. Click **Save**

**Expected Results:**
- [ ] Registration expense recorded
- [ ] Categorized correctly

---

## Test Case 3.5: Record Consumables Expense

**Objective:** Verify recording consumable items expense.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Click **"+ Add Expense"**
3. Fill in:
   - Asset: [Select vehicle]
   - Category: **Consumables**
   - Amount: 350 THB
   - Description: "Fuel for relocation to branch"
4. Click **Save**

**Expected Results:**
- [ ] Consumables expense recorded
- [ ] Small amount tracking works correctly

---

## Test Case 3.6: View Expenses by Asset

**Objective:** Verify viewing all expenses for a specific asset.

**Steps:**
1. Navigate to **Finance > Assets > [Select Asset with expenses]**
2. Click to view Asset Details
3. Find the Expenses section/tab

**Expected Results:**
- [ ] All expenses for this asset listed
- [ ] Category breakdown visible
- [ ] Total expenses match summary
- [ ] Filter/sort options work

---

## Test Case 3.7: Expense Category Breakdown on Dashboard

**Objective:** Verify expense breakdown chart on Asset Dashboard.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find the "Expense Breakdown" donut chart

**Expected Results:**
- [ ] Donut chart displays
- [ ] All expense categories represented
- [ ] Percentages add up to 100%
- [ ] Legend shows:
  - Category name
  - Amount
  - Percentage
- [ ] Total shown at bottom

---

## Test Case 3.8: Edit Existing Expense

**Objective:** Verify editing an existing expense record.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Find an existing expense
3. Click **Edit** button
4. Modify:
   - Amount (change from 2,500 to 2,800)
   - Description (add more detail)
5. Click **Save**

**Expected Results:**
- [ ] Changes saved successfully
- [ ] Asset totals updated with new amount
- [ ] Audit trail shows modification

---

## Test Case 3.9: Delete Expense (if supported)

**Objective:** Verify expense deletion or reversal.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Find an expense to delete
3. Click **Delete** button
4. Confirm deletion

**Expected Results:**
- [ ] Confirmation dialog appears
- [ ] Expense is removed or marked as deleted
- [ ] Asset total expenses reduced by deleted amount
- [ ] Cannot delete expenses from closed periods (if applicable)

---

## Test Case 3.10: Filter Expenses by Category

**Objective:** Verify filtering expenses by category.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Use category filter dropdown
3. Select each category in turn:
   - Maintenance
   - Insurance
   - Financing
   - Accident
   - Registration
   - Consumables
   - Other

**Expected Results:**
- [ ] Filter correctly shows only selected category
- [ ] "All" option shows all expenses
- [ ] Counts update correctly for each filter

---

## Test Case 3.11: Filter Expenses by Date Range

**Objective:** Verify filtering expenses by date.

**Steps:**
1. Navigate to **Finance > Expenses**
2. Set date range filter:
   - From: First of current month
   - To: Today
3. Apply filter

**Expected Results:**
- [ ] Only expenses within date range shown
- [ ] Clearing filter shows all expenses
- [ ] Invalid date range shows error or no results

---

## Notes

- All amounts in Thai Baht (THB)
- Verify expense impacts on ROI calculations
- Check that Tax Deductible flag is properly stored and displayed
