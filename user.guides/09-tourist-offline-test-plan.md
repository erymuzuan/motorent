# Tourist Offline Feature - Manual Test Plan

This document outlines the manual testing procedures for the tourist portal offline download functionality.

## Prerequisites

### Test Environment
- Development server running (`dotnet watch`)
- Browser: Chrome, Firefox, or Safari (latest version)
- Browser DevTools available for IndexedDB inspection

### Test Data
- Active rental in the database with WebId
- Shop with contact information (phone, email, address, GPS coordinates)
- Test URL: `/tourist/{AccountNo}/my-rental/{RentalWebId}`

### Database Setup
```sql
-- Verify test data exists
SELECT WebId, Status, RenterName, VehicleName
FROM [PhuketRentals].[Rental]
WHERE Status = 'Active';
```

---

## Test Cases

### TC-001: Initial Page Load - Online State

**Objective:** Verify the page loads correctly and shows "Save for Offline" option

**Steps:**
1. Open browser and navigate to rental page URL
2. Wait for page to fully load

**Expected Results:**
- [ ] Page displays rental information correctly
- [ ] "Save for Offline" card is visible on right sidebar
- [ ] Card shows "Download this rental info to access it even without internet"
- [ ] "Download Now" button is enabled and clickable
- [ ] No console errors related to IndexedDB

**Verification:**
```javascript
// Browser console
typeof window.MotoRentOfflineDb !== 'undefined' // Should return true
```

---

### TC-002: Download for Offline - Success

**Objective:** Verify clicking "Download Now" saves rental data to IndexedDB

**Steps:**
1. Navigate to rental page
2. Locate the "Save for Offline" card
3. Click the "Download Now" button
4. Observe UI changes

**Expected Results:**
- [ ] Button shows loading state briefly
- [ ] Card header changes from "Save for Offline" to "Saved Offline"
- [ ] Green checkmark icon appears
- [ ] Text shows "Available offline"
- [ ] "Last synced: Just now" appears
- [ ] "Sync Latest" button replaces "Download Now"

**Verification:**
```javascript
// Browser console - verify data saved
(async () => {
  const rental = await MotoRentOfflineDb.getActiveRental();
  console.log('Saved rental:', rental);
  return rental ? 'SUCCESS' : 'FAILED';
})();
```

---

### TC-003: IndexedDB Data Integrity

**Objective:** Verify all required rental data is stored correctly

**Steps:**
1. Complete TC-002 (download)
2. Open browser DevTools > Application > IndexedDB
3. Navigate to "motorent-tourist" database
4. Check "activeRental" store

**Expected Results:**
- [ ] Database "motorent-tourist" exists
- [ ] "activeRental" store contains rental object
- [ ] Rental object includes:
  - rentalId
  - webId
  - renterName
  - vehicleName (or brand/model)
  - startDate
  - expectedEndDate
  - status
  - downloadedAt (timestamp)
  - lastSyncAt (timestamp)
  - shopContact information

**Verification:**
```javascript
// Browser console - check all fields
(async () => {
  const rental = await MotoRentOfflineDb.getActiveRental();
  const required = ['rentalId', 'status', 'downloadedAt', 'lastSyncAt'];
  const missing = required.filter(f => !rental[f]);
  return missing.length ? 'Missing: ' + missing.join(', ') : 'All fields present';
})();
```

---

### TC-004: Sync Latest - Update Data

**Objective:** Verify "Sync Latest" button refreshes data from server

**Steps:**
1. Complete TC-002 (download)
2. Wait 30 seconds
3. Click "Sync Latest" button
4. Observe UI update

**Expected Results:**
- [ ] Button shows loading/syncing state
- [ ] "Last synced" timestamp updates
- [ ] Data in IndexedDB is refreshed
- [ ] No error messages displayed

---

### TC-005: Page Reload - Persisted State

**Objective:** Verify offline status persists after page reload

**Steps:**
1. Complete TC-002 (download)
2. Hard refresh the page (Ctrl+Shift+R)
3. Wait for page to load

**Expected Results:**
- [ ] Page recognizes existing offline data
- [ ] Shows "Saved Offline" state immediately
- [ ] "Available offline" with green checkmark
- [ ] "Sync Latest" button visible
- [ ] No "Download Now" button

---

### TC-006: Offline Mode - Simulated

**Objective:** Verify page displays saved data when offline

**Steps:**
1. Complete TC-002 (download)
2. Open DevTools > Network tab
3. Enable "Offline" checkbox
4. Refresh the page

**Expected Results:**
- [ ] Page loads (may take a moment)
- [ ] Rental details are displayed from cache
- [ ] Shop contact information shows
- [ ] Phone/email links are still clickable
- [ ] "Sync Latest" may show disabled or error state

**Note:** Full offline support depends on Service Worker implementation.

---

### TC-007: Multiple Sessions - Data Isolation

**Objective:** Verify IndexedDB data is isolated per browser/device

**Steps:**
1. Complete TC-002 in Chrome
2. Open same URL in Firefox
3. Check IndexedDB in Firefox

**Expected Results:**
- [ ] Firefox shows "Save for Offline" (not downloaded)
- [ ] Each browser maintains separate IndexedDB
- [ ] Downloading in one browser doesn't affect another

---

### TC-008: Shop Contact Information

**Objective:** Verify shop contact data is accessible offline

**Steps:**
1. Complete TC-002 (download)
2. Check shop contact card on page
3. Verify data matches stored data

**Expected Results:**
- [ ] Shop name displayed
- [ ] Phone number clickable (tel: link)
- [ ] Email address clickable (mailto: link)
- [ ] Address displayed
- [ ] GPS navigation works (if online)
- [ ] LINE link works (if app installed)

**Verification:**
```javascript
// Check shop contact in IndexedDB
(async () => {
  const rental = await MotoRentOfflineDb.getActiveRental();
  console.log('Shop contact:', {
    phone: rental.shopPhone,
    email: rental.shopEmail,
    address: rental.shopAddress
  });
})();
```

---

### TC-009: Error Handling - Network Failure

**Objective:** Verify graceful handling of download failure

**Steps:**
1. Navigate to rental page
2. Open DevTools > Network > Throttling > Offline
3. Click "Download Now" button

**Expected Results:**
- [ ] Button shows loading briefly
- [ ] Error message displays (snackbar or alert)
- [ ] UI returns to "Download Now" state
- [ ] No crash or unhandled exception

---

### TC-010: Browser Compatibility

**Objective:** Verify feature works across supported browsers

**Test Browsers:**
- [ ] Chrome (Windows)
- [ ] Chrome (Android)
- [ ] Safari (iOS)
- [ ] Firefox (Windows)
- [ ] Edge (Windows)

**For each browser, verify:**
- [ ] Page loads correctly
- [ ] Download button works
- [ ] Data is saved to IndexedDB
- [ ] Offline mode works (if supported)

---

## IndexedDB Structure Reference

### Database Name
`motorent-tourist`

### Object Stores

| Store Name | Key Path | Description |
|------------|----------|-------------|
| activeRental | rentalId | Current rental data |
| tenantContext | accountNo | Shop/tenant info |
| emergencyContacts | id | Emergency numbers |
| poi | poiId | Points of interest |
| routes | routeId | Curated routes |
| pendingPhotos | localId | Photos pending upload |
| accidentReports | localId | Offline accident reports |
| contracts | rentalId | Rental contracts |
| syncQueue | id (auto) | Pending sync operations |
| settings | key | App preferences |

---

## Console Commands for Testing

```javascript
// Initialize database
await MotoRentOfflineDb.init();

// Get active rental
const rental = await MotoRentOfflineDb.getActiveRental();

// Get all rentals
const all = await MotoRentOfflineDb.getAll('activeRental');

// Check if rental is stale (>60 min old)
const stale = await MotoRentOfflineDb.isRentalStale(1);

// Clear all data (CAUTION)
await MotoRentOfflineDb.deleteDatabase();

// Get storage estimate
const storage = await MotoRentOfflineDb.getStorageEstimate();
```

---

## Test Results Template

| Test Case | Date | Tester | Browser | Result | Notes |
|-----------|------|--------|---------|--------|-------|
| TC-001 | | | | Pass/Fail | |
| TC-002 | | | | Pass/Fail | |
| TC-003 | | | | Pass/Fail | |
| TC-004 | | | | Pass/Fail | |
| TC-005 | | | | Pass/Fail | |
| TC-006 | | | | Pass/Fail | |
| TC-007 | | | | Pass/Fail | |
| TC-008 | | | | Pass/Fail | |
| TC-009 | | | | Pass/Fail | |
| TC-010 | | | | Pass/Fail | |

---

## Known Limitations

1. **Service Worker Dependency**: Full offline page loading requires service worker (separate implementation)
2. **IndexedDB Support**: Some private/incognito modes may not support IndexedDB
3. **Storage Quota**: Large amounts of data may hit browser storage limits
4. **Cross-Origin**: Data is isolated to the origin domain

---

*Last Updated: January 2026*
*Safe & Go - Vehicle Rental Management System*
