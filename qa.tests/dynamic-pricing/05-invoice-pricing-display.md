# QA Test: Invoice Dynamic Pricing Display

## Test ID: DP-005
## Feature: Invoice Pricing Breakdown
## Priority: High

---

## Preconditions
- Dynamic Pricing is enabled (DP-001 passed)
- A rental exists that was created with dynamic pricing (DP-004 passed)
- "Show Multiplier on Invoice" setting is enabled
- User is logged in with rental access

## Test Steps

### Step 1: Open Rental with Dynamic Pricing
1. Navigate to **Rentals** list
2. Find a rental that spans dates with pricing rules
3. Click to open rental details

**Expected:** Rental details page loads

### Step 2: Open Invoice
1. Click **Invoice** or **View Invoice** button

**Expected:** Invoice dialog opens showing rental details

### Step 3: Verify Basic Invoice Information
1. Check header section

**Expected:**
- Rental ID/Number displayed
- Renter name and contact info
- Vehicle details (brand, model, plate)
- Rental dates (start and end)

### Step 4: Verify Dynamic Pricing Section
1. Scroll to pricing breakdown section

**Expected:** Section titled "Pricing Breakdown" or "Per-Day Pricing" shows:
- Table with Date, Rate, Multiplier, Amount columns
- Each rental day listed with applicable multiplier
- Days grouped by multiplier when consecutive
- Applied rule names shown (if setting enabled)

### Step 5: Verify Multiplier Display
1. Check multiplier column values

**Expected:**
- Multipliers displayed as "1.6x", "2.0x", etc.
- Different multipliers for different date ranges
- Base rate (1.0x) shown for days without rules

### Step 6: Verify Totals
1. Check totals section

**Expected:**
- Subtotal (rental days total)
- Any additional charges (accessories, insurance)
- Deposits applied
- Grand total

### Step 7: Test with Multiplier Hidden
1. Go to Organization Settings
2. Disable "Show Multiplier on Invoice"
3. Return to invoice

**Expected:**
- Daily amounts still shown
- Multiplier column/values hidden
- Total remains same

---

## Test Data

### Sample Invoice Breakdown
| Date | Base Rate | Multiplier | Amount |
|------|-----------|------------|--------|
| Dec 25 | ฿500 | 2.2x | ฿1,100 |
| Dec 26 | ฿500 | 2.2x | ฿1,100 |
| Dec 27 | ฿500 | 2.2x | ฿1,100 |
| Dec 28 | ฿500 | 2.2x | ฿1,100 |
| Dec 29 | ฿500 | 2.2x | ฿1,100 |
| Dec 30 | ฿500 | 2.2x | ฿1,100 |
| Dec 31 | ฿500 | 2.5x | ฿1,250 |
| Jan 1 | ฿500 | 2.8x | ฿1,400 |
| Jan 2 | ฿500 | 2.8x | ฿1,400 |
| **Subtotal** | | | **฿10,650** |

### Applied Rules Display
- "Ultra Peak Season" (Dec 20 - Jan 10): 2.2x
- "Russian New Year" (Jan 1 - Jan 14): 2.8x (higher priority)

## Pass Criteria
- [ ] Invoice opens successfully for rental with dynamic pricing
- [ ] Per-day pricing breakdown section is visible
- [ ] Each day shows correct multiplier
- [ ] Daily amounts calculate correctly
- [ ] Applied rule names are shown (when enabled)
- [ ] Totals are accurate
- [ ] "Show Multiplier on Invoice" setting works correctly
- [ ] Invoice can be printed/exported with pricing breakdown

## Notes
- Invoice should be printable with full pricing breakdown
- PDF export should include all pricing details
- Email invoice should include same breakdown
- Multiplier display is optional based on organization setting
