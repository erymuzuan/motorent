# QA Test: Rental Check-in with Dynamic Pricing

## Test ID: DP-004
## Feature: Dynamic Pricing in Rental Workflow
## Priority: Critical

---

## Preconditions
- Dynamic Pricing is enabled (DP-001 passed)
- Shop has pricing rules applied (DP-002 passed)
- User is logged in as Staff, ShopManager, or OrgAdmin
- At least one available vehicle exists
- At least one renter exists

## Test Steps

### Step 1: Start New Rental
1. Navigate to **Rentals** > **New Rental** or click **Check In** button
2. Select a vehicle
3. Select a renter
4. Proceed to date/pricing configuration step

**Expected:** Configure Rental step loads

### Step 2: Select Dates Spanning Multiple Rate Periods
1. Set **Start Date** to a date in high season (e.g., December 25)
2. Set **End Date** to span into different rate period (e.g., January 15)

**Expected:**
- Total price calculates automatically
- If dynamic pricing applies, per-day breakdown section appears

### Step 3: Verify Per-Day Pricing Breakdown
1. Observe the pricing breakdown section

**Expected:** Section shows:
- Header: "Per-Day Pricing Breakdown" or similar
- Table with columns: Date, Base Rate, Multiplier, Daily Total
- Each day shows applicable multiplier (e.g., 2.5x for Jan 1-7)
- Days with different multipliers grouped or highlighted
- Grand total matches sum of daily totals

### Step 4: Verify Applied Rules Display
1. Check for applied rules information

**Expected:**
- Shows which pricing rules are being applied
- Rule names visible (e.g., "Russian New Year", "High Season")
- Multiplier values shown

### Step 5: Change Dates to Low Season
1. Change dates to low season period (e.g., June 15 - June 25)

**Expected:**
- Prices recalculate with lower multipliers (e.g., 0.6x)
- Per-day breakdown updates
- Total price is lower than base rate × days

### Step 6: Complete Check-in
1. Continue through remaining steps
2. Complete the check-in process

**Expected:**
- Rental is created successfully
- Applied pricing rules are stored with rental

---

## Test Data

### Suggested Test Scenarios

#### Scenario A: High Season Only
- Start: December 25, 2025
- End: January 5, 2026
- Expected: High multipliers (1.6x - 2.8x)

#### Scenario B: Low Season Only
- Start: June 15, 2025
- End: June 25, 2025
- Expected: Low multipliers (0.55x - 0.65x)

#### Scenario C: Mixed Seasons
- Start: October 25, 2025
- End: November 10, 2025
- Expected: Mix of shoulder (1.1x) and high season (1.6x)

#### Scenario D: Event Period
- Start: April 12, 2025
- End: April 18, 2025
- Expected: Songkran multiplier (1.8x)

### Sample Calculation
| Date | Base Rate | Multiplier | Daily Total |
|------|-----------|------------|-------------|
| Dec 25 | ฿500 | 2.2x | ฿1,100 |
| Dec 26 | ฿500 | 2.2x | ฿1,100 |
| Dec 27 | ฿500 | 2.2x | ฿1,100 |
| Jan 1 | ฿500 | 2.8x | ฿1,400 |
| Jan 2 | ฿500 | 2.8x | ฿1,400 |
| **Total** | | | **฿6,100** |

## Pass Criteria
- [ ] Per-day pricing breakdown displays when dynamic pricing applies
- [ ] Each day shows correct multiplier based on active rules
- [ ] Daily totals calculate correctly (base × multiplier)
- [ ] Grand total equals sum of daily totals
- [ ] Applied rules are displayed with names
- [ ] Price updates when dates change
- [ ] Rental completes successfully with pricing data stored

## Notes
- If no pricing rules apply for selected dates, standard flat rate is used
- Multiple rules on same day: highest priority rule wins
- Base rate comes from vehicle's daily rate setting
