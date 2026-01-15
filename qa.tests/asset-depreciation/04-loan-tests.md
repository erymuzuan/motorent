# Loan Tests

## Test Category: Loan Management

### Prerequisites
- User logged in as OrgAdmin or ShopManager
- At least one asset exists (preferably newly created)
- Access to Finance menu

---

## Test Case 4.1: Create New Loan for Asset

**Objective:** Verify creating a loan record for a financed vehicle.

**Test Data:**
- Principal Amount: 100,000 THB
- Annual Interest Rate: 8% (0.08)
- Term: 24 months
- Down Payment: 20,000 THB
- Expected Monthly Payment: ~4,523 THB

**Steps:**
1. Navigate to **Finance > Loans**
2. Click **"+ Add Loan"**
3. Fill in loan details:
   - Select Asset: [Choose a vehicle]
   - Lender Name: "Bangkok Bank"
   - Principal Amount: 100,000 THB
   - Annual Interest Rate: 8%
   - Term: 24 months
   - Down Payment: 20,000 THB
   - Start Date: First of current month
4. Click **Save**

**Expected Results:**
- [ ] Loan is created successfully
- [ ] Monthly payment calculated automatically
- [ ] Amortization schedule generated
- [ ] Loan linked to asset
- [ ] Asset shows loan reference

---

## Test Case 4.2: View Loan Details and Amortization

**Objective:** Verify loan detail page and amortization schedule.

**Steps:**
1. Navigate to **Finance > Loans**
2. Click on a loan to view details
3. Review the Payment Schedule tab

**Expected Results:**
- [ ] Loan summary shows:
  - Principal amount
  - Interest rate
  - Monthly payment
  - Total interest over life
  - Outstanding balance
- [ ] Amortization schedule shows:
  - Payment number
  - Due date
  - Total payment
  - Principal portion
  - Interest portion
  - Remaining balance
- [ ] All 24 payments listed (for 24-month term)

---

## Test Case 4.3: Record Loan Payment

**Objective:** Verify recording a monthly loan payment.

**Steps:**
1. Navigate to **Finance > Loans > [Select Loan]**
2. View Payment Schedule
3. Find the current due payment
4. Click **"Record Payment"**
5. Enter:
   - Payment Date: Today
   - Reference: "TXN-2025-001"
6. Click **Confirm**

**Expected Results:**
- [ ] Payment marked as "Paid"
- [ ] Remaining principal updated
- [ ] Interest portion automatically creates expense entry
- [ ] Next payment highlighted as due
- [ ] Payment history shows the recorded payment

---

## Test Case 4.4: View Outstanding Loan Balance

**Objective:** Verify outstanding balance calculation after payments.

**Steps:**
1. Record 3 consecutive loan payments
2. Navigate to **Finance > Loans > [Select Loan]**
3. Review outstanding balance

**Expected Results:**
- [ ] Outstanding balance = Principal - Sum of principal portions paid
- [ ] Payments remaining count decreases
- [ ] Progress indicator shows loan payoff progress

---

## Test Case 4.5: Overdue Payment Alert

**Objective:** Verify system identifies overdue loan payments.

**Test Setup:**
- Have a loan with an unpaid payment past due date

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Check "Attention Needed" section
3. Look for overdue payment alerts

**Expected Results:**
- [ ] Alert shows "X loan payments overdue"
- [ ] Clicking alert navigates to loan list with overdue filter
- [ ] Overdue payments highlighted in red or warning color
- [ ] Late payment status shown on payment schedule

---

## Test Case 4.6: Upcoming Payment Alert

**Objective:** Verify upcoming payment notifications.

**Test Setup:**
- Have a loan with payment due within next 7 days

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Check "Attention Needed" section

**Expected Results:**
- [ ] Alert shows "X payments due this week"
- [ ] Clicking navigates to upcoming payments view
- [ ] Due dates clearly displayed

---

## Test Case 4.7: Interest Expense Auto-Creation

**Objective:** Verify interest portion creates expense record automatically.

**Steps:**
1. Record a loan payment
2. Navigate to **Finance > Expenses**
3. Filter by category: **Financing**

**Expected Results:**
- [ ] Expense entry created for interest portion
- [ ] Category is "Financing"
- [ ] Amount matches interest portion from payment
- [ ] Linked to correct asset
- [ ] Description indicates loan interest payment

---

## Test Case 4.8: Loan Payoff

**Objective:** Verify completing all loan payments.

**Test Setup:**
- Have a loan with only 1-2 payments remaining

**Steps:**
1. Record all remaining loan payments
2. View loan status after final payment

**Expected Results:**
- [ ] Loan status changes to "Paid Off"
- [ ] Outstanding balance shows 0
- [ ] All payments marked as paid
- [ ] Final payment completes successfully
- [ ] Asset no longer shows active loan

---

## Test Case 4.9: Early Loan Payoff

**Objective:** Verify recording early/lump-sum payoff.

**Steps:**
1. Navigate to **Finance > Loans > [Select Loan]**
2. Click **"Early Payoff"** or **"Pay Off Loan"**
3. Enter payoff amount (remaining principal)
4. Confirm payoff

**Expected Results:**
- [ ] Payoff amount calculated correctly
- [ ] Remaining payments cancelled
- [ ] Loan marked as "Paid Off"
- [ ] Interest savings noted (if applicable)

---

## Test Case 4.10: View Loans by Status

**Objective:** Verify filtering loans by status.

**Steps:**
1. Navigate to **Finance > Loans**
2. Use status filter:
   - **All**
   - **Active**
   - **Paid Off**
   - **Overdue** (if available)

**Expected Results:**
- [ ] Active shows loans in progress
- [ ] Paid Off shows completed loans
- [ ] Counts match filter results

---

## Test Case 4.11: Edit Loan Details

**Objective:** Verify editing loan information (limited fields).

**Steps:**
1. Navigate to **Finance > Loans > [Select Loan]**
2. Click **Edit**
3. Modify allowed fields:
   - Lender Name
   - Account/Reference number
4. Click **Save**

**Expected Results:**
- [ ] Changes saved
- [ ] Principal, rate, term should NOT be editable (after creation)
- [ ] Warning if trying to edit locked fields

---

## Test Case 4.12: Loan Impact on Profitability

**Objective:** Verify loan costs reflect in profitability reports.

**Steps:**
1. Record several loan payments for an asset
2. Navigate to **Finance > Reports > Profitability**
3. View the asset's financial summary

**Expected Results:**
- [ ] Interest expenses included in total expenses
- [ ] ROI calculation includes financing costs
- [ ] Expense breakdown shows Financing category

---

## Notes

- Interest calculations should use standard amortization formula
- Monthly payment = P × [r(1+r)^n] / [(1+r)^n - 1]
- Verify calculations match bank/financial standards
- Test with different interest rates and terms
