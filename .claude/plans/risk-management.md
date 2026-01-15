# GPS Integration Plan for MotoRent

## Overview
Implement GPS tracking integration with provider-agnostic design, supporting live fleet maps, geofencing alerts, and trip history for Thailand motorbike rental operations.

## Requirements Summary
- **Provider**: Agnostic abstraction (GPS2GO, Fifotrack support)
- **Updates**: Periodic (5-15 min intervals)
- **Features**: Live fleet map, geofencing alerts, trip history
- **Geofencing**: Thai province templates + custom polygons
- **Data Retention**: 90 days
- **Tracking Scope**: All vehicles 24/7
- **Alerts**: In-app + LINE notifications
- **Kill Switch**: UI placeholder only (no hardware integration)

---

## New Entities

| Entity | Purpose |
|--------|---------|
| `GpsTrackingDevice` | GPS device registered to vehicle |
| `GpsPosition` | Position records (90-day retention) |
| `Geofence` | Zone definitions (polygon/circle) |
| `GeofenceAlert` | Breach notifications |
| `TripHistory` | Aggregated trip analytics |

---

## Service Layer

### IGpsProvider Interface
Provider-agnostic abstraction for GPS providers:
- `GetCurrentPositionAsync(deviceId)`
- `GetHistoricalPositionsAsync(deviceId, from, to)`
- `ActivateKillSwitchAsync(deviceId)` - placeholder

### Core Services
| Service | Responsibility |
|---------|---------------|
| `GpsTrackingService` | Device management, position polling |
| `GeofenceService` | Zone management, breach detection |
| `AlertService` | Alert creation, LINE/in-app notifications |
| `TripHistoryService` | Trip aggregation, route analysis |
| `ILineMessagingService` | LINE Messaging API integration |

---

## Database Tables

```
MotoRent.GpsTrackingDevice  - Device registration
MotoRent.GpsPosition        - Position history (indexed by VehicleId, DeviceTimestamp)
MotoRent.Geofence           - Zone definitions with Coordinates JSON
MotoRent.GeofenceAlert      - Alert records
MotoRent.TripHistory        - Aggregated trip data with RoutePolyline
```

All tables follow existing JSON column pattern with computed columns for indexing.

---

## UI Components

| Page | Location | Purpose |
|------|----------|---------|
| Fleet Map | `/gps/fleet-map` | Live vehicle positions on Google Maps |
| Geofences | `/gps/geofences` | Manage zones, import province templates |
| Alert Dashboard | `/gps/alerts` | Real-time alert monitoring |
| Trip History | `/gps/trips` | Route replay, behavior analysis |
| Device Management | `/gps/devices` | Register/monitor GPS devices |

---

## Background Jobs

| Job | Interval | Purpose |
|-----|----------|---------|
| `GpsPollingSubscriber` | 10 min (configurable) | Poll providers, store positions, check geofences |
| `GpsDataRetentionSubscriber` | Daily 2 AM | Delete positions older than 90 days |

---

## Configuration (MotoConfig)

```
MOTO_Gps2GoApiKey          - GPS2GO API credentials
MOTO_FifotrackApiKey       - Fifotrack API credentials
MOTO_GpsPollingIntervalMinutes  - Polling frequency (default: 10)
MOTO_GpsDataRetentionDays  - Retention period (default: 90)
MOTO_LineMessagingAccessToken - LINE API token
```

---

## Thai Province Templates

Pre-seeded geofences for common rental areas:
- Phuket (PHK) - High priority
- Krabi (KBI) - High priority
- Koh Samui/Surat Thani (SNI) - High priority
- Phang Nga (PNA) - Medium priority

---

## Implementation Phases

### Phase 1: Foundation (Week 1-2)
- [ ] Create database tables (5 tables)
- [ ] Add entities to Domain with JsonDerivedType attributes
- [ ] Register repositories in DI
- [ ] Implement IGpsProvider interface
- [ ] Basic GpsTrackingService with device CRUD

### Phase 2: Provider Integration (Week 3-4)
- [ ] Implement Gps2GoProvider
- [ ] Implement FifotrackProvider
- [ ] Create GpsPollingSubscriber background job
- [ ] Add position storage logic
- [ ] Create GpsDataRetentionSubscriber

### Phase 3: Geofencing (Week 5-6)
- [ ] Implement point-in-polygon algorithm in GeofenceService
- [ ] Seed Thai province boundary templates
- [ ] Integrate breach detection with polling cycle
- [ ] Implement AlertService with alert creation

### Phase 4: Notifications (Week 7)
- [ ] Implement ILineMessagingService
- [ ] Create LINE Flex Message templates for alerts
- [ ] Add SignalR hub for real-time in-app notifications
- [ ] Integrate with AlertService

### Phase 5: UI Components (Week 8-10)
- [ ] Fleet Map page with Google Maps
- [ ] Geofence management page with polygon drawing
- [ ] Alert Dashboard with real-time updates
- [ ] Trip History page with route replay
- [ ] Device Management page
- [ ] Kill Switch placeholder dialog

### Phase 6: Integration & Polish (Week 11-12)
- [ ] Add GPS widget to Fleet Manager dashboard
- [ ] Link trip history to rental check-out
- [ ] Add vehicle location to rental details
- [ ] Thai/English localization
- [ ] Unit and integration tests

---

## Critical Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add JsonDerivedType for 5 new entities |
| `src/MotoRent.Domain/Core/MotoConfig.cs` | Add GPS and LINE config properties |
| `src/MotoRent.Domain/Spatial/LatLng.cs` | Add polygon containment method |
| `src/MotoRent.Server/Extensions/ServiceCollectionExtensions.cs` | Register GPS services |

---

## Verification

1. **Unit Tests**: GeofenceService point-in-polygon, distance calculations
2. **Integration Tests**: GPS provider mock, alert creation flow
3. **Manual Testing**:
   - Register test device in UI
   - Verify positions appear on fleet map
   - Create custom geofence and verify breach detection
   - Confirm LINE notifications delivered
   - Check 90-day retention cleanup works

---

## Reference: Full Specification
See `risk-management.md` in project root for complete Fleet Management Module specification including:
- Theft Prevention System (Section 2)
- Staff Abuse Prevention (Section 3)
- Maintenance Scheduling (Section 4)
- Digital Vehicle Inspection (Section 5)
- QR Code Check-In/Out (Section 6)
- Fuel Management (Section 7)

## Things to remember
 - Apply code standard and database repository skill after code generation
 - Apply css styling for .razor files after editing/generating
