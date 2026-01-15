# QA Test: Apply Regional Preset

## Test ID: DP-002
## Feature: Regional Pricing Presets
## Priority: High

---

## Preconditions
- Dynamic Pricing is enabled (DP-001 passed)
- User is logged in as OrgAdmin or ShopManager
- At least one shop exists in the organization

## Test Steps

### Step 1: Navigate to Pricing Rules
1. Click **Settings** in the navigation menu
2. Click **Pricing Rules**

**Expected:** Pricing Rules page loads with:
- Shop selector dropdown
- "Apply Regional Preset" button
- "View Calendar" button
- Rules list (may be empty)

### Step 2: Select a Shop
1. Select a shop from the dropdown

**Expected:** Page updates to show rules for selected shop

### Step 3: Open Apply Preset Dialog
1. Click **Apply Regional Preset** button

**Expected:** Dialog opens showing:
- Description text about regional presets
- Three category groups:
  - Beach Destinations (Andaman Coast, Gulf Coast, Eastern, Western)
  - Border & Regional (Southern Border, Isaan)
  - Metropolitan (Northern, Central)
- Each preset shows name, description, demographics, and rule count
- "Start Blank" option with 0 rules

### Step 4: Select Andaman Coast Preset
1. Click on **Andaman Coast (Phuket, Krabi)** option

**Expected:**
- Radio button becomes selected
- Info box appears showing "What happens next?" explanation
- Rule count shows 58 rules

### Step 5: Apply Preset
1. Click **Apply Preset** button

**Expected:**
- Loading spinner appears
- Dialog closes
- Success message: "Created 58 pricing rules from Andaman Coast preset"
- Rules list now shows 58 rules

### Step 6: Verify Rules Created
1. Scroll through the rules list

**Expected:** Rules include:
- Season rules (High Season, Low Season, Shoulder Season, etc.)
- Event rules (Russian New Year, Chinese New Year, Songkran, etc.)
- Various tourist demographic events (UK, German, French, etc.)

---

## Test Data

### Regional Presets and Expected Rule Counts
| Preset | Expected Rules |
|--------|----------------|
| Andaman Coast (Phuket, Krabi) | 58 |
| Gulf Coast (Koh Samui) | 14 |
| Southern Border (Hat Yai) | 32 |
| Northern (Chiang Mai) | 14 |
| Eastern (Pattaya) | 14 |
| Central (Bangkok) | 11 |
| Western (Hua Hin) | 9 |
| Isaan (Udon Thani) | 10 |

## Pass Criteria
- [ ] Apply Regional Preset dialog opens correctly
- [ ] All 8 regional presets are displayed with correct categories
- [ ] Selecting a preset shows rule count
- [ ] Applying preset creates correct number of rules
- [ ] Rules appear in the rules list after applying
- [ ] Success message displays with correct count

## Notes
- Rules are created for the currently selected shop only
- Applying a preset does NOT delete existing rules
- User can apply multiple presets to the same shop
