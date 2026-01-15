# QA Test: Enable Dynamic Pricing Feature

## Test ID: DP-001
## Feature: Dynamic Pricing Toggle
## Priority: Critical

---

## Preconditions
- User is logged in as OrgAdmin or ShopManager
- User has access to Organization Settings

## Test Steps

### Step 1: Navigate to Settings
1. Click **Settings** in the navigation menu
2. Click **Organization Settings**

**Expected:** Organization Settings page loads successfully

### Step 2: Locate Pricing Panel
1. Scroll down to find the **Pricing** panel/section

**Expected:** Pricing panel is visible with:
- Enable Dynamic Pricing toggle
- Show Multiplier on Invoice toggle (disabled until dynamic pricing is enabled)

### Step 3: Enable Dynamic Pricing
1. Toggle **Enable Dynamic Pricing** to ON

**Expected:**
- Toggle switches to ON state
- "Show Multiplier on Invoice" toggle becomes enabled
- Success toast message appears

### Step 4: Save Settings
1. Click **Save** button (if required)

**Expected:** Settings are persisted

### Step 5: Verify Persistence
1. Navigate away from the page
2. Return to Organization Settings

**Expected:** Dynamic Pricing toggle remains ON

---

## Test Data
- Organization: Any test organization
- User Role: OrgAdmin or ShopManager

## Pass Criteria
- [ ] Pricing panel is visible in Organization Settings
- [ ] Dynamic Pricing toggle can be enabled
- [ ] Show Multiplier on Invoice toggle becomes available
- [ ] Settings persist after page reload

## Notes
- This is a prerequisite for all other dynamic pricing tests
