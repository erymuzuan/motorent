# Safe & Go Asset Depreciation Guide - Financial Management

Welcome to the Asset Depreciation module! This guide helps Organization Admins and Shop Managers track vehicle value, depreciation, expenses, and profitability.

## Overview

The Asset Depreciation feature provides:
- **Asset Tracking** - Link financial records to vehicles
- **Depreciation Calculation** - Multiple methods to track value decline
- **Expense Management** - Categorize and track all vehicle costs
- **Loan Tracking** - Full financing and amortization management
- **Profitability Reports** - ROI analysis per vehicle

## Who Should Use This

| Role | Access Level |
|------|--------------|
| **OrgAdmin** | Full access to all asset features |
| **ShopManager** | View and manage assets for their shop |

## Navigation

Access asset features from **Finance** menu:

| Menu Item | Description |
|-----------|-------------|
| **Finance > Asset Dashboard** | Financial command center with KPIs, charts, and alerts |
| **Finance > Assets** | View all tracked assets with summary |
| **Finance > Depreciation** | Record and manage depreciation entries |
| **Finance > Expenses** | Track operational costs by category |
| **Finance > Loans** | Manage vehicle financing |
| **Finance > Reports > Profitability** | Vehicle ROI analysis |

---

## Asset Dashboard - Financial Command Center

The Asset Dashboard (`/finance/asset-dashboard`) provides a comprehensive view of your fleet's financial health at a glance.

### KPI Summary Cards

| Card | What It Shows |
|------|---------------|
| **Total Invested** | Total acquisition cost of all tracked vehicles |
| **Book Value** | Current value after depreciation (with % of original) |
| **Accumulated Depreciation** | Total depreciation recorded with progress bar |
| **Net Profit/Loss** | Revenue minus expenses minus depreciation |
| **Fleet ROI** | Return on investment percentage for entire fleet |

### Charts

- **Depreciation Trend** - 12-month chart showing book value decline and monthly depreciation
- **Expense Breakdown** - Donut chart showing costs by category (Maintenance, Financing, Insurance, etc.)

### Performance Tracking

- **Top Performers** - Vehicles with highest ROI, ranked with gold/silver/bronze indicators
- **Attention Needed** - Alerts for:
  - Assets needing monthly depreciation
  - Overdue loan payments
  - Upcoming payments (next 7 days)
  - Underperforming vehicles (negative ROI)

### Quick Actions

Direct access to common tasks:
- Run Monthly Depreciation
- Record Expense
- View All Assets
- Manage Loans
- Profitability Report

### Recent Activity

Timeline of recent financial events:
- Depreciation entries
- Expenses recorded
- Loan payments made
- Revenue from rentals

---

## Getting Started

### 1. Creating an Asset Record

To track depreciation for a vehicle:

1. Navigate to **Finance > Assets**
2. Click **"+ Add Asset"**
3. Fill in the asset details:
   - **Vehicle** - Select the vehicle to track
   - **Acquisition Date** - When the vehicle was purchased
   - **Acquisition Cost** - Purchase price (THB)
   - **Vendor Name** - Dealer or seller
   - **Reference No.** - Invoice or receipt number

4. Configure depreciation settings:
   - **Method** - Choose how to calculate depreciation
   - **Useful Life** - Expected lifetime in months (default: 60)
   - **Residual Value** - Expected value at end of life

5. Click **Save**

### 2. Depreciation Methods Explained

| Method | How It Works | Best For |
|--------|--------------|----------|
| **Day Out of Door** | Immediate % depreciation on first rental | Motorbikes that lose value when first used |
| **Straight Line** | Equal monthly amounts | Standard vehicles with predictable decline |
| **Declining Balance** | % of current book value | High initial depreciation, slower later |
| **Custom** | User-defined monthly schedule | Special situations requiring manual control |
| **Hybrid** | Day Out of Door + Straight Line | Combined first-use drop then steady decline |

#### Day Out of Door Example
- Acquisition Cost: 100,000 THB
- Day Out of Door: 20%
- **First rental triggers:** 20,000 THB immediate depreciation
- Remaining 80,000 THB depreciates over useful life

#### Straight Line Example
- Acquisition Cost: 100,000 THB
- Useful Life: 60 months
- Residual Value: 10,000 THB
- **Monthly depreciation:** (100,000 - 10,000) / 60 = 1,500 THB

### 3. Recording Depreciation

Depreciation can be recorded automatically or manually:

#### Automatic Recording
1. Go to **Finance > Depreciation**
2. Click **"Run Monthly Depreciation"**
3. Select the period (month/year)
4. Review calculated amounts
5. Click **Confirm**

#### Manual Override
When you need to adjust the calculated amount:

1. Navigate to **Finance > Assets > [Vehicle]**
2. Click **"Record Depreciation"**
3. Enter the period dates
4. Check **"Override calculated amount"**
5. Enter your custom amount
6. Provide a reason for the override
7. Click **Save**

---

## Expense Tracking

### Expense Categories

| Category | Examples |
|----------|----------|
| **Maintenance** | Oil changes, tire replacement, repairs |
| **Insurance** | Annual insurance premiums |
| **Financing** | Loan interest payments |
| **Accident** | Repair costs from incidents |
| **Registration** | Annual registration fees, taxes |
| **Consumables** | Fuel for relocations, cleaning supplies |

### Recording an Expense

1. Go to **Finance > Expenses**
2. Click **"+ Add Expense"**
3. Select the **Asset** (vehicle)
4. Choose **Category**
5. Enter **Amount** and **Date**
6. Add **Vendor** and **Reference** if applicable
7. Mark if **Tax Deductible**
8. Click **Save**

### Viewing Expenses by Asset

1. Navigate to **Finance > Assets > [Vehicle]**
2. Click the **Expenses** tab
3. View all expenses with category breakdown
4. Filter by date range or category

---

## Loan Management

### Setting Up a Loan

When you finance a vehicle purchase:

1. Go to **Finance > Loans**
2. Click **"+ Add Loan"**
3. Select the **Asset**
4. Enter loan details:
   - **Lender Name** - Bank or finance company
   - **Principal Amount** - Loan amount (THB)
   - **Annual Interest Rate** - e.g., 8% = 0.08
   - **Term** - Length in months
   - **Down Payment** - Initial payment made
   - **Start Date** - When loan began

5. The system calculates:
   - Monthly payment amount
   - Full amortization schedule

6. Click **Save**

### Recording Loan Payments

1. Go to **Finance > Loans > [Loan]**
2. View the **Payment Schedule** tab
3. Find the payment due
4. Click **"Record Payment"**
5. Enter payment date and reference
6. Click **Confirm**

The system automatically:
- Updates remaining principal
- Records interest as an expense
- Marks payment as complete

### Amortization Schedule

View the full payment breakdown:

| Payment # | Due Date | Total | Principal | Interest | Balance |
|-----------|----------|-------|-----------|----------|---------|
| 1 | Feb 2026 | 5,000 | 3,500 | 1,500 | 96,500 |
| 2 | Mar 2026 | 5,000 | 3,550 | 1,450 | 92,950 |
| ... | ... | ... | ... | ... | ... |

---

## Understanding the Dashboard

### Asset Summary Cards

| Card | What It Shows |
|------|---------------|
| **Total Assets** | Count of tracked vehicles |
| **Total Book Value** | Sum of current values |
| **Accumulated Depreciation** | Total depreciation recorded |
| **Net Profit/Loss** | Revenue - Expenses - Depreciation |

### Asset Status

| Status | Meaning |
|--------|---------|
| **Active** | Vehicle in operation, depreciation continues |
| **Disposed** | Vehicle sold, shows gain/loss on sale |
| **Write-Off** | Vehicle removed with zero value (theft, total loss) |

---

## Profitability Analysis

### Vehicle ROI Report

Navigate to **Finance > Reports > Profitability** to see:

- **ROI %** - Return on Investment per vehicle
- **Revenue** - Total rental income
- **Expenses** - All costs by category
- **Net Profit/Loss** - Bottom line per vehicle

### Key Metrics

| Metric | Formula |
|--------|---------|
| **Depreciable Base** | Acquisition Cost - Residual Value |
| **Current Book Value** | Acquisition Cost - Accumulated Depreciation |
| **Net Profit/Loss** | Revenue - Expenses - Depreciation |
| **ROI %** | Net Profit / Acquisition Cost × 100 |

### Projections

View future depreciation projections:

1. Go to **Finance > Assets > [Vehicle]**
2. Click the **Projections** tab
3. See month-by-month forecast for next 12 months
4. View projected book value at year-end

---

## Asset Disposal

### Selling a Vehicle

When you sell a tracked vehicle:

1. Navigate to **Finance > Assets > [Vehicle]**
2. Click **"Dispose Asset"**
3. Enter:
   - **Disposal Date**
   - **Sale Amount**
4. System calculates **Gain/Loss**:
   - Sale Amount > Book Value = Gain
   - Sale Amount < Book Value = Loss
5. Click **Confirm**

### Writing Off a Vehicle

For vehicles with no resale value (theft, total loss):

1. Navigate to **Finance > Assets > [Vehicle]**
2. Click **"Write Off"**
3. Enter **Reason** for write-off
4. System records full book value as loss
5. Click **Confirm**

---

## Pre-Existing Vehicles

For vehicles acquired before implementing Safe & Go:

1. Create asset with **"Pre-existing vehicle"** checked
2. Enter **Initial Book Value** (current estimated value)
3. Set **System Entry Date** (today)
4. Depreciation calculates from entry date, not original purchase

---

## Best Practices

### Daily
- No daily asset tasks required

### Monthly
- [ ] Run depreciation for the period
- [ ] Record loan payments
- [ ] Review expense entries

### Quarterly
- [ ] Review profitability reports
- [ ] Identify underperforming vehicles
- [ ] Update depreciation settings if needed

### Annually
- [ ] Reconcile book values with actual condition
- [ ] Review and adjust residual values
- [ ] Plan vehicle replacements based on ROI

---

## Common Questions

**Q: When does Day Out of Door depreciation trigger?**
A: Automatically when the first rental is completed for that vehicle.

**Q: Can I change the depreciation method after recording entries?**
A: Yes, but past entries remain unchanged. New entries use the new method.

**Q: What happens to expenses when I dispose of an asset?**
A: All expense records are preserved for reporting. The asset shows as Disposed.

**Q: How do I track a leased vehicle?**
A: Create the asset without a loan. Track lease payments as regular expenses under the Financing category.

---

## Getting Help

- Click **Documentation** in the footer for technical details
- Contact your system administrator for configuration changes
- Review the Implementation Plan in `.claude/plans/asset-depreciation.md`

---

*Safe & Go - Vehicle Rental Management System*
