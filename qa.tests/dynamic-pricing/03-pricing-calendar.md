# QA Test: Pricing Calendar View

## Test ID: DP-003
## Feature: Pricing Calendar Visualization
## Priority: High

---

## Preconditions
- Dynamic Pricing is enabled (DP-001 passed)
- At least one shop has pricing rules applied (DP-002 passed)
- User is logged in as OrgAdmin or ShopManager

## Test Steps

### Step 1: Navigate to Pricing Calendar
1. Click **Settings** in the navigation menu
2. Click **Pricing Rules**
3. Select a shop with pricing rules
4. Click **View Calendar** button

**Expected:** Pricing Calendar page loads with:
- Shop selector dropdown
- Month/Year selector
- Vehicle Type filter
- Legend showing color codes
- Calendar grid for current month

### Step 2: Verify Legend
1. Observe the legend at the top of the calendar

**Expected:** Legend shows four categories:
- **Green** (Discount): < 1.0x multiplier
- **Yellow** (Normal): 1.0x multiplier
- **Orange** (Shoulder): 1.2x - 1.4x multiplier
- **Red** (Peak): > 1.5x multiplier

### Step 3: Verify Calendar Display
1. Observe the calendar grid

**Expected:**
- Days of week headers (Sun-Sat or Mon-Sun based on locale)
- Each day cell shows the date number
- Days with pricing rules show colored background
- Days with rules show multiplier badge (e.g., "1.6x")

### Step 4: Navigate to High Season Month
1. Use month navigation to go to December or January

**Expected:**
- Calendar updates to show selected month
- High season days (Christmas, New Year) show red/orange backgrounds
- Multipliers like 2.0x, 2.5x visible

### Step 5: Navigate to Low Season Month
1. Use month navigation to go to June or July

**Expected:**
- Calendar updates to show selected month
- Low season days show green backgrounds
- Multipliers like 0.6x, 0.7x visible

### Step 6: Filter by Vehicle Type
1. Select a specific vehicle type from filter (if available)

**Expected:**
- Calendar updates to show rules applicable to that vehicle type
- Some days may change color/multiplier based on vehicle-specific rules

### Step 7: View Active Rules for Month
1. Scroll down to "Active Rules This Month" section

**Expected:**
- List of rules active during the displayed month
- Shows rule name, type (Season/Event/DayOfWeek), and date range
- Color-coded by rule type

---

## Test Data

### Expected Colors by Date (Andaman Coast Preset)
| Date Range | Expected Color | Multiplier |
|------------|----------------|------------|
| Dec 20 - Jan 10 | Red | 2.0x - 2.8x |
| Nov 1 - Mar 31 | Orange | 1.6x |
| Jun 1 - Aug 31 | Green | 0.55x - 0.65x |
| Apr 13 - Apr 17 | Orange/Red | 1.8x (Songkran) |

## Pass Criteria
- [ ] Calendar page loads successfully
- [ ] Legend displays four color categories
- [ ] Days show appropriate colors based on multiplier
- [ ] Month navigation works correctly
- [ ] Multiplier badges display on colored days
- [ ] Vehicle type filter updates calendar
- [ ] Active rules section shows rules for current month

## Notes
- Calendar shows combined effect of all applicable rules for each day
- Higher priority rules override lower priority rules
- Multiple rules on same day may stack or override based on priority
