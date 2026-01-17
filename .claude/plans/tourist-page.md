# Tourist Offline PWA Implementation Plan

## Overview

Build an offline-capable PWA for tourists who rent vehicles from MotoRent shops. The app provides comprehensive offline access to rental information, emergency services, photo documentation, navigation, and points of interest.

**Access Flow:** QR code shown at check-in opens PWA with rental data pre-loaded.

## Feature Categories

### 1. Trust & Documentation
- Timestamped GPS-tagged photo documentation
- Digital contract viewer (signed at check-in)
- Transparent pricing breakdown
- Multi-language support

### 2. Safety & Emergency
- Digital emergency card (Tourist Police 1155, Ambulance 1669, Shop contact)
- Full accident report wizard with photos, location, damage assessment, witness info
- Insurance info display

### 4. Easy Shop Contact (Priority)
- **Floating Contact FAB** - Always visible button with multiple contact options
- **One-tap Call** - Direct phone call to shop (`tel:` link)
- **WhatsApp Chat** - Popular with international tourists (`wa.me/` link)
- **LINE Chat** - Essential for Thailand market (`line.me/` link)
- **Navigate to Shop** - Open shop location in Google Maps
- **Shop Hours** - Display operating hours with open/closed status
- **Contact Card** - Prominent shop info on rental dashboard

### 3. Explorer Co-Pilot
- Google Maps SDK with downloadable offline areas
- Hybrid POI: tenant-curated + Google Places API
- Curated routes with export to Google Maps
- "Near Me" feature for essentials (petrol, hospital, ATM, repair)

## Architecture

```
Tourist Device
+------------------------------------------+
|  Blazor WASM    Service Worker   IndexedDB|
|  Components     (Background Sync) (Data)  |
+------------------------------------------+
          | HTTPS (when online)
          v
+------------------------------------------+
|  MotoRent Server                         |
|  Tourist API  |  Sync API  |  Maps Proxy |
+------------------------------------------+
```

**Offline-First Pattern:**
1. Reads from IndexedDB first
2. Background sync updates when online
3. Writes queue locally, sync when connected

## IndexedDB Schema

Database: `motorent-tourist-{accountNo}`

| Store | Key | Purpose |
|-------|-----|---------|
| `activeRental` | rentalId | Current rental package |
| `tenantContext` | accountNo | Branding/shop info |
| `emergencyContacts` | id | Emergency numbers |
| `poi` | poiId | Points of interest |
| `routes` | routeId | Curated routes |
| `pendingPhotos` | localId | Photos awaiting sync |
| `accidentReports` | localId | Draft/submitted reports |
| `contracts` | rentalId | Signed contract copies |
| `syncQueue` | auto | Outbound sync items |

## QR Code Flow

**URL Structure:**
```
https://rent.motorent.app/r/{rentalWebId}
  -> Redirects to /tourist/{accountNo}/my-rental/{webId}
```

**Flow:**
1. Staff shows QR at check-in completion
2. Tourist scans -> Opens PWA
3. Page checks IndexedDB for cached data
4. If not cached: prompts "Download for offline use?"
5. Downloads complete rental package
6. Shows rental dashboard with offline indicator

## New Entities

### PointOfInterest
```csharp
public class PointOfInterest : Entity
{
    public int PointOfInterestId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; }
    public string? NameLocal { get; set; }
    public string Category { get; set; }  // petrol, hospital, atm, repair, police, attraction
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Phone { get; set; }
    public string? Hours { get; set; }
    public bool IsFeatured { get; set; }
    public List<string> Tags { get; set; }
}
```

### TouristRoute
```csharp
public class TouristRoute : Entity
{
    public int TouristRouteId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; }
    public string Category { get; set; }  // scenic, cultural, adventure
    public string Difficulty { get; set; }
    public decimal DurationHours { get; set; }
    public decimal DistanceKm { get; set; }
    public List<RouteWaypoint> Waypoints { get; set; }
    public List<string> Highlights { get; set; }
}
```

### EmergencyContact
```csharp
public class EmergencyContact : Entity
{
    public int EmergencyContactId { get; set; }
    public int? ShopId { get; set; }  // Null = global Thailand
    public string Type { get; set; }  // police, ambulance, shop, insurance
    public string Name { get; set; }
    public string Phone { get; set; }
    public int Priority { get; set; }
}
```

### Shop Entity Updates (for Easy Contact)
Add these fields to existing `Shop` entity:
```csharp
// Easy Contact fields
public string? WhatsAppNumber { get; set; }      // International format: 66812345678
public string? LineId { get; set; }              // LINE ID or @username
public string? LineUrl { get; set; }             // line.me/ti/p/xxx or line.me/R/ti/p/@xxx
public string? FacebookMessenger { get; set; }   // m.me/pagename
public string OperatingHours { get; set; }       // JSON: {"mon":"09:00-20:00", ...}
public bool IsOpen24Hours { get; set; }
```

### ShopContactInfo (DTO for offline storage)
```csharp
public class ShopContactInfo
{
    public int ShopId { get; set; }
    public string Name { get; set; }
    public string? LogoUrl { get; set; }
    public string Phone { get; set; }
    public string? WhatsAppUrl { get; set; }     // wa.me/66xxx?text=Hi...
    public string? LineUrl { get; set; }
    public string? Email { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Address { get; set; }
    public string GoogleMapsUrl { get; set; }    // Pre-generated navigation URL
    public Dictionary<string, string> Hours { get; set; }
    public bool IsCurrentlyOpen { get; set; }    // Calculated at download
    public string? NextOpenTime { get; set; }    // "Opens at 9:00 AM"
}
```

## Client-Side Services

Path: `src/MotoRent.Client/Services/Offline/`

| Service | Purpose |
|---------|---------|
| `IndexedDbService` | JS interop for IndexedDB operations |
| `NetworkStateService` | Online/offline detection |
| `OfflineRentalService` | Download/cache rental packages |
| `OfflinePhotoService` | GPS-tagged photo capture and sync |
| `OfflinePOIService` | POI caching and "Near Me" queries |
| `OfflineRouteService` | Curated routes management |
| `AccidentReportService` | Offline accident report wizard |
| `BackgroundSyncService` | Process sync queue |

## Server-Side API

Path: `src/MotoRent.Server/Controllers/TouristApiController.cs`

```csharp
[Route("api/tourist/{accountNo}")]
[AllowAnonymous]
public class TouristApiController : ControllerBase
{
    [HttpGet("rental-package/{rentalId}/{accessToken}")]
    public Task<ActiveRentalPackageDto> GetRentalPackage(...);

    [HttpGet("poi")]
    public Task<List<PointOfInterestDto>> GetPOIs(lat, lng, radiusKm);

    [HttpGet("routes")]
    public Task<List<TouristRouteDto>> GetRoutes();

    [HttpGet("emergency-contacts")]
    public Task<List<EmergencyContactDto>> GetEmergencyContacts();

    [HttpPost("photos/sync")]
    public Task<PhotoSyncResultDto> SyncPhoto([FromForm] request);

    [HttpPost("accident-report")]
    public Task<AccidentReportResultDto> SubmitAccidentReport([FromBody] report);
}
```

## Component Structure

### New Pages
```
src/MotoRent.Client/Pages/Tourist/
├── MyRental.razor              # Offline-first rental dashboard (with ShopContactCard)
├── MyRentalContract.razor      # Contract viewer
├── EmergencyCard.razor         # Quick emergency contacts
├── PhotoCapture.razor          # GPS-tagged photo capture
├── AccidentWizard/
│   ├── AccidentWizard.razor    # Multi-step container
│   ├── Step1_Location.razor    # When/where
│   ├── Step2_Description.razor # What happened
│   ├── Step3_Damage.razor      # Damage assessment
│   ├── Step4_Parties.razor     # Other parties/witnesses
│   ├── Step5_Photos.razor      # Photo documentation
│   └── Step6_Review.razor      # Review and submit
└── Explorer/
    ├── ExplorerHome.razor      # Explorer hub
    ├── POIMap.razor            # Map with POIs
    ├── POIList.razor           # List view
    ├── RouteList.razor         # Curated routes
    └── RouteDetails.razor      # Route detail/export
```

### Shared Components
```
src/MotoRent.Client/Components/Tourist/
├── OfflineIndicator.razor      # Online/offline status
├── SyncStatusBadge.razor       # Pending sync count
├── EmergencyButton.razor       # Floating emergency FAB
├── ShopContactFAB.razor        # Floating shop contact button (PRIMARY)
├── ShopContactSheet.razor      # Bottom sheet with all contact options
├── ShopContactCard.razor       # Shop info card for dashboard
├── PhotoCaptureButton.razor    # Camera with GPS
├── POICard.razor               # POI display
├── RouteCard.razor             # Route preview
├── NearMeButton.razor          # Quick "Near Me" action
└── DamageSelector.razor        # Vehicle damage picker
```

### ShopContactFAB Design
```
+---------------------------+
|  [Shop Logo]              |
|  Adam's Moto Rental       |
|  ● OPEN until 8pm         |
+---------------------------+
|  📞  Call Now             |  <- tel:+66-xxx
|  💬  WhatsApp             |  <- wa.me/66xxx
|  🟢  LINE                 |  <- line.me/ti/p/xxx
|  📧  Email                |  <- mailto:xxx
|  📍  Navigate to Shop     |  <- Google Maps
+---------------------------+
```

## Contact URL Generation

For easy one-tap contact, pre-generate these URLs in the rental package:

| Channel | URL Format | Example |
|---------|------------|---------|
| Phone | `tel:+66-XX-XXX-XXXX` | `tel:+66-81-234-5678` |
| WhatsApp | `https://wa.me/66XXXXXXXXX?text={encoded}` | `wa.me/66812345678?text=Hi%20I%20rented%20...` |
| LINE | `https://line.me/ti/p/{lineId}` or `line.me/R/ti/p/@{lineId}` | `line.me/ti/p/@adammoto` |
| Email | `mailto:{email}?subject={encoded}` | `mailto:help@shop.com?subject=Rental%20123` |
| Maps | `https://www.google.com/maps/dir/?api=1&destination={lat},{lng}` | Navigate from current location |

**Pre-filled message template:**
```
Hi, I'm {RenterName} with rental #{RentalReference}.
I rented a {VehicleBrand} {VehicleModel} ({LicensePlate}).
```

## Service Worker Enhancement

Path: `src/MotoRent.Server/wwwroot/service-worker.js`

Enhancements needed:
1. **Background Sync** for photos and accident reports
2. **Periodic Sync** for data freshness
3. **Cache-first** for POI/routes, **network-first** for rental data
4. **Message passing** for sync status updates

```javascript
// New sync tags
const SYNC_PHOTOS = 'sync-photos';
const SYNC_ACCIDENT_REPORTS = 'sync-accident-reports';

// Background sync handler
self.addEventListener('sync', (event) => {
    if (event.tag === SYNC_PHOTOS) {
        event.waitUntil(syncPendingPhotos());
    }
    if (event.tag === SYNC_ACCIDENT_REPORTS) {
        event.waitUntil(syncAccidentReports());
    }
});
```

## Database Scripts

```
database/Tables/
├── Core.EmergencyContact.sql
├── PointOfInterest.sql
├── TouristRoute.sql
└── OfflinePackageDownload.sql
```

## Implementation Phases

### Phase 1: Core Offline Infrastructure
- IndexedDB setup with JS interop
- Network state detection
- Enhanced service worker
- Database schema scripts

**Files:**
- `src/MotoRent.Client/wwwroot/js/indexed-db.js`
- `src/MotoRent.Client/Services/Offline/IndexedDbService.cs`
- `src/MotoRent.Client/Services/Offline/NetworkStateService.cs`

### Phase 2: Rental Package & QR Code + Shop Contact (Priority)
- QR code generation at check-in
- Rental package download API
- Offline rental dashboard
- **Shop contact FAB with call/WhatsApp/LINE**
- **Shop contact card on dashboard**
- **Navigate to shop via Google Maps**

**Files:**
- `TouristApiController.cs`
- `OfflineRentalService.cs`
- `MyRental.razor` (with ShopContactCard)
- `MyRentalContract.razor`
- `ShopContactFAB.razor`
- `ShopContactSheet.razor`
- `ShopContactCard.razor`

**Shop Entity Migration:**
- Add WhatsAppNumber, LineId, LineUrl, OperatingHours fields

### Phase 3: Photo Documentation
- GPS-tagged photo capture
- Photo storage in IndexedDB
- Background sync to server

**Files:**
- `src/MotoRent.Client/wwwroot/js/photo-capture.js`
- `OfflinePhotoService.cs`
- `PhotoCapture.razor`

### Phase 4: Emergency & Accident Reporting
- Digital emergency card
- Full accident report wizard
- Offline report submission

**Files:**
- `EmergencyCard.razor`
- `EmergencyButton.razor`
- `AccidentWizard/` (6 step components)
- `AccidentReportService.cs`
- `DamageSelector.razor`

### Phase 5: Explorer Co-Pilot
- POI database with tenant curation
- Curated routes
- Google Maps integration
- "Near Me" feature

**Files:**
- Admin pages for POI/Route management
- `OfflinePOIService.cs`
- `OfflineRouteService.cs`
- `Explorer/` pages
- Google Places API proxy

### Phase 6: Multi-Language & Polish
- Offline language resources
- Storage management
- Offline maps download UI
- Testing

## Storage Budget

| Data Type | Size | Notes |
|-----------|------|-------|
| Rental Package | ~50KB | JSON + contract HTML |
| Photos (pending) | ~2MB each | Compressed JPEG |
| Thumbnails | ~50KB each | Previews |
| POI Data | ~500KB | Local area |
| Routes | ~200KB | Curated |
| **Total per rental** | **~10-20MB** | Excluding offline maps |

## Critical Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Server/wwwroot/service-worker.js` | Add background sync, photo caching |
| `src/MotoRent.Client/Controls/TouristComponentBase.cs` | Add offline state awareness |
| `src/MotoRent.Client/Pages/Tourist/RentalHistory.razor` | Add offline download prompt |
| Check-in completion pages | Add QR code display |

## Verification Plan

1. **Shop Contact Testing (Priority)**
   - Verify floating contact button appears on all tourist pages
   - Test one-tap call opens phone dialer
   - Test WhatsApp link opens with pre-filled message
   - Test LINE link opens LINE app or browser
   - Test "Navigate to Shop" opens Google Maps with directions
   - Verify shop hours show correct open/closed status
   - Test contact features work offline (stored contact info)

2. **Offline Mode Testing**
   - Enable airplane mode after downloading rental package
   - Verify all features work offline
   - Test photo capture stores locally
   - Verify shop contact info available offline

3. **Sync Testing**
   - Take photos offline
   - Submit accident report offline
   - Reconnect and verify sync completes

4. **QR Code Flow**
   - Complete a test rental check-in
   - Scan QR code with phone
   - Verify rental data loads correctly
   - Verify shop contact card displays prominently

5. **Maps & POI**
   - Download offline map region
   - Test "Near Me" without internet
   - Verify curated routes export to Google Maps
