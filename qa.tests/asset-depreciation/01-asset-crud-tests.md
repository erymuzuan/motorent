# Asset CRUD Tests

## Test Category: Asset Creation and Management

### Prerequisites
- User logged in as OrgAdmin or ShopManager
- At least one vehicle exists in the system without an asset record
- Access to Finance menu

---

## Test Case 1.1: Create New Asset for Existing Vehicle

**Objective:** Verify that a new asset record can be created for an existing vehicle.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Click **"+ Add Asset"** button
3. In the Asset Dialog:
   - Select a vehicle from the dropdown (vehicle without existing asset)
   - Enter Acquisition Date (e.g., today's date)
   - Enter Acquisition Cost (e.g., 50,000 THB)
   - Enter Vendor Name (e.g., "Honda Dealer Phuket")
   - Enter Reference No. (e.g., "INV-2025-001")
4. Configure Depreciation Settings:
   - Select Method: **Straight Line**
   - Enter Useful Life: **60** months
   - Enter Residual Value: **5,000** THB
5. Click **Save**

**Expected Results:**
- [ ] Asset is created successfully
- [ ] Success notification displayed
- [ ] Redirected to Asset Dashboard
- [ ] New asset appears in asset list
- [ ] Current Book Value equals Acquisition Cost (no depreciation yet)
- [ ] Accumulated Depreciation shows 0

---

## Test Case 1.2: Create Asset with Day Out of Door Depreciation

**Objective:** Verify asset creation with Day Out of Door depreciation method.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Click **"+ Add Asset"**
3. Enter basic asset details (vehicle, date, cost, vendor)
4. Configure Depreciation Settings:
   - Select Method: **Day Out of Door**
   - Enter Day Out of Door %: **20%**
   - Enter Useful Life: **60** months
   - Enter Residual Value: **5,000** THB
5. Click **Save**

**Expected Results:**
- [ ] Asset is created successfully
- [ ] Day Out of Door percentage is saved
- [ ] Current Book Value still equals Acquisition Cost (depreciation triggers on first rental)
- [ ] Asset shows "Pending first rental" indicator

---

## Test Case 1.3: Create Asset with Hybrid Depreciation

**Objective:** Verify asset creation with Hybrid (Day Out of Door + Straight Line) method.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Click **"+ Add Asset"**
3. Enter basic asset details
4. Configure Depreciation Settings:
   - Select Method: **Hybrid (Day Out + Straight Line)**
   - Enter Day Out of Door %: **15%**
   - Enter Useful Life: **48** months
   - Enter Residual Value: **10,000** THB
5. Click **Save**

**Expected Results:**
- [ ] Asset is created with Hybrid method
- [ ] Both Day Out of Door % and Straight Line settings are saved
- [ ] Settings panel shows correct configuration

---

## Test Case 1.4: Edit Existing Asset

**Objective:** Verify that asset details can be edited.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find an existing asset in the list
3. Click the **Edit** button (pencil icon)
4. Modify the following fields:
   - Vendor Name (add or change)
   - Reference No. (update)
   - Residual Value (adjust from 5,000 to 8,000)
5. Click **Save**

**Expected Results:**
- [ ] Changes are saved successfully
- [ ] Success notification displayed
- [ ] Updated values shown in asset details
- [ ] Depreciation recalculated if residual value changed

---

## Test Case 1.5: View Asset Details Page

**Objective:** Verify the Asset Details page displays correct information.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Find an asset with some depreciation history
3. Click the **chart icon** button to view details
4. Review all sections on the details page

**Expected Results:**
- [ ] Summary cards display:
  - Acquisition Cost with date
  - Current Book Value with percentage
  - Accumulated Depreciation with remaining life
  - ROI with net profit/loss
- [ ] Asset Value Chart loads correctly
- [ ] Toggle between History and Projection views works
- [ ] Depreciation Settings panel shows correct configuration
- [ ] Depreciation History table shows all entries

---

## Test Case 1.6: Filter Assets by Status

**Objective:** Verify status filtering works correctly on the dashboard.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Scroll to the Asset List section
3. Click on each status filter tab:
   - **All**
   - **Active**
   - **Disposed**
   - **Written Off**

**Expected Results:**
- [ ] "All" shows all assets regardless of status
- [ ] "Active" shows only assets with Active status
- [ ] "Disposed" shows only sold/disposed assets
- [ ] "Written Off" shows only written-off assets
- [ ] Count badges update correctly for each filter
- [ ] Empty state message shown if no assets match filter

---

## Test Case 1.7: Prevent Duplicate Asset for Same Vehicle

**Objective:** Verify system prevents creating multiple assets for the same vehicle.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Click **"+ Add Asset"**
3. Try to select a vehicle that already has an asset record

**Expected Results:**
- [ ] Vehicle dropdown does not show vehicles with existing assets
- OR
- [ ] Error message displayed if selecting a vehicle with existing asset
- [ ] Asset creation is prevented

---

## Test Case 1.8: Create Pre-Existing Asset

**Objective:** Verify creation of asset for vehicle acquired before system implementation.

**Steps:**
1. Navigate to **Finance > Asset Dashboard**
2. Click **"+ Add Asset"**
3. Check the **"Pre-existing vehicle"** checkbox
4. Enter:
   - Vehicle selection
   - Original Acquisition Date (in the past, e.g., 2 years ago)
   - Original Acquisition Cost: 80,000 THB
   - Initial Book Value: 50,000 THB (current estimated value)
   - System Entry Date: Today
5. Configure depreciation settings
6. Click **Save**

**Expected Results:**
- [ ] Asset created with pre-existing flag
- [ ] Current Book Value starts at Initial Book Value (50,000), not Acquisition Cost
- [ ] Depreciation calculates from System Entry Date, not Acquisition Date
- [ ] Asset details show both original and current values

---

## Notes

- All currency values are in Thai Baht (THB)
- Screenshots should be captured for any failed tests
- Report any unexpected behavior or error messages
