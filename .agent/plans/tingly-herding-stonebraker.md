# Plan: Fleet Model — Shared Vehicle Attributes

## Problem
Every vehicle is edited individually, even though vehicles of the same make/model/year share specs, pricing, and capacity. This is tedious for fleets with many units of the same model (e.g., 10 Honda Clicks all needing the same daily rate).

## Solution: FleetModel Entity (Two-Layer Architecture)
Introduce a **tenant-level `FleetModel` entity** that owns shared attributes. Individual `Vehicle` records link to a FleetModel and only store per-unit fields (plate, color, status, maintenance, owner).

**Key design decisions (user-confirmed):**
- Template pricing only — discounts applied at check-in/out, not per-vehicle
- Auto-apply — FleetModel is source of truth; vehicles read from it on load
- Minimal vehicle edit — per-unit fields only, shared specs shown read-only

## Architecture

```
VehicleModel (Core/Global)     FleetModel (Tenant)           Vehicle (Tenant)
├─ Make                        ├─ FleetModelId (PK)          ├─ VehicleId (PK)
├─ Model                       ├─ VehicleModelId (FK, opt)   ├─ FleetModelId (FK) ← NEW
├─ VehicleType                 ├─ Brand                      ├─ LicensePlate
├─ EngineCC                    ├─ Model                      ├─ LicensePlateProvince
├─ SuggestedDailyRate          ├─ Year                       ├─ Color
└─ ...                         ├─ VehicleType                ├─ Status
                               ├─ EngineCC / EngineLiters    ├─ ImagePath
                               ├─ Segment / Transmission     ├─ Notes
                               ├─ SeatCount / PassengerCap   ├─ HomeShopId
                               ├─ DailyRate                  ├─ CurrentShopId
                               ├─ HourlyRate                 ├─ VehiclePoolId
                               ├─ Rate15Min/30Min/1Hour      ├─ Mileage
                               ├─ DepositAmount              ├─ EngineHours
                               ├─ DurationType               ├─ LastServiceDate
                               ├─ DriverDailyFee             ├─ VehicleOwnerId
                               ├─ GuideDailyFee              ├─ OwnerPaymentModel
                               ├─ MaxRiderWeight             ├─ OwnerDailyRate
                               └─ ImageStoreId               └─ OwnerRevenueSharePercent
```

**Read-through pattern:** When VehicleService loads vehicles, it populates shared properties from FleetModel. All 28+ consuming files continue reading `vehicle.DailyRate` etc. unchanged. No massive refactor needed.

---

## Tasks & Dependencies

### Task 1: FleetModel Entity & Database
**Depends on:** Nothing

- Create `FleetModel` entity class in `MotoRent.Domain/Entities/`
  - Properties: FleetModelId, VehicleModelId?, Brand, Model, Year, VehicleType, EngineCC, EngineLiters, Segment, Transmission, SeatCount, PassengerCapacity, MaxRiderWeight, DailyRate, HourlyRate, Rate15Min, Rate30Min, Rate1Hour, DepositAmount, DurationType, DriverDailyFee, GuideDailyFee, ImageStoreId, IsActive, Notes
  - Computed helpers: DisplayName, EngineDisplay, GetGroupKey()
- Create SQL table `[MotoRent].[FleetModel]` in `database/tables/`
- Register repository in DI

### Task 2: FleetModelService
**Depends on:** Task 1

- Create `FleetModelService` in `MotoRent.Services/`
  - `GetFleetModelsAsync()` — list with filters (type, search)
  - `GetFleetModelByIdAsync(id)` — single by ID
  - `CreateFleetModelAsync()` / `UpdateFleetModelAsync()` / `DeleteFleetModelAsync()`
  - `GetFleetModelForVehicleAsync(vehicle)` — lookup by group key
  - `GetFleetModelCountsAsync()` — unit counts per model

### Task 3: Vehicle Entity Changes
**Depends on:** Task 1

- Add `FleetModelId` (int?) to Vehicle entity
- Add `FleetModelId` computed column to SQL table
- Keep shared properties on Vehicle (for read-through compatibility) but mark with `[JsonIgnore]` so they're not persisted in Vehicle JSON
- Add `PopulateFromFleetModel(FleetModel fm)` method to Vehicle
- Update `Vehicle.GetGroupKey()` to remain consistent

### Task 4: VehicleService — FleetModel Integration
**Depends on:** Tasks 2, 3

- Modify vehicle load methods to populate shared properties from FleetModel:
  - `GetVehiclesAsync()` — batch-load FleetModels, populate vehicles
  - `GetVehicleByIdAsync()` — load FleetModel, populate
  - `GetVehiclesByTypeAsync()` — same
  - `GetAvailableVehiclesForShopAsync()` — same
- Modify `CreateVehicleAsync()` — require FleetModelId
- Modify `UpdateVehicleAsync()` — don't persist shared fields
- Update `GetVehicleGroupsAsync()` — group by FleetModelId instead of computed key

### Task 5: FleetModel Management UI
**Depends on:** Task 2

**Files:**
- `src/MotoRent.Client/Pages/Vehicles/FleetModels.razor` — list page
- `src/MotoRent.Client/Pages/Vehicles/EditFleetModel.razor` — edit page
- `src/MotoRent.Client/Pages/Vehicles/CreateFleetModel.razor` — create page
- `src/MotoRent.Client/Pages/Vehicles/FleetModelForm.razor` — shared form component

**FleetModels list page (`/fleet-models` or `/vehicles/models`):**
- Card grid showing each model with: image, name, type badge, specs, rate, unit count
- Filter by vehicle type
- Link to edit, link to view units

**FleetModel form (extracted from current VehicleForm shared sections):**
- Vehicle type selection (create only)
- Brand & Model (with datalist from VehicleModel lookup)
- Year, Engine specs, Segment, Transmission, Seats, Capacity
- Pricing: DailyRate, HourlyRate, interval rates
- Deposit, Duration type
- Driver/Guide fees (Boat/Van)
- Image

### Task 6: Simplify Vehicle Edit Page
**Depends on:** Tasks 4, 5

- Refactor `VehicleForm.razor`:
  - Show FleetModel info as read-only card with "Edit Model" link
  - Editable fields only: LicensePlate, Province, Color, Status, Notes, Mileage/EngineHours, LastServiceDate, Owner info
  - Remove pricing inputs, spec inputs
- Update `CreateVehicle.razor`:
  - Step 1: Select existing FleetModel (or create new one)
  - Step 2: Enter per-unit details (plate, color)
  - Quick Recognition: match against FleetModels instead of VehicleModel

### Task 7: Data Migration Script
**Depends on:** Tasks 1, 3

- SQL script to:
  1. Group existing vehicles by Brand|Model|Year|VehicleType|EngineCC|EngineLiters
  2. Create FleetModel records from each group (using first vehicle's shared attributes)
  3. Set FleetModelId on all vehicles in each group
- This should be idempotent and safe to run multiple times

### Task 8: VehicleGroup Model Update
**Depends on:** Task 4

- Update `VehicleGroup.FromVehicles()` to use FleetModel for rates
- Simplify since all vehicles in a group share the same FleetModel
- `HasPriceRange` becomes always false (same pricing)
- Tourist Browse/Landing pages continue working via VehicleGroup

### Task 9: Localization
**Depends on:** Tasks 5, 6

- Create .resx files for new pages (FleetModels, EditFleetModel, CreateFleetModel, FleetModelForm)
- Update existing VehicleForm/EditVehicle .resx with new keys
- Languages: default, en, th, ms

---

## Dependency Graph

```
Task 1 (Entity + DB)
  ├── Task 2 (Service) ──── Task 5 (FleetModel UI)
  ├── Task 3 (Vehicle changes)
  │     └── Task 4 (VehicleService integration) ── Task 6 (Simplify Vehicle Edit)
  │                                               Task 8 (VehicleGroup update)
  └── Task 7 (Data migration)

Task 9 (Localization) ── depends on Tasks 5, 6
```

## Files Modified (Key)

| File | Change |
|------|--------|
| `src/MotoRent.Domain/Entities/FleetModel.cs` | **NEW** — entity class |
| `database/tables/MotoRent.FleetModel.sql` | **NEW** — SQL table |
| `src/MotoRent.Domain/Entities/Vehicle.cs` | Add FleetModelId, JsonIgnore shared props |
| `database/tables/MotoRent.Vehicle.sql` | Add FleetModelId column |
| `src/MotoRent.Services/FleetModelService.cs` | **NEW** — CRUD service |
| `src/MotoRent.Services/VehicleService.cs` | FleetModel integration on load |
| `src/MotoRent.Client/Pages/Vehicles/FleetModels.razor` | **NEW** — list page |
| `src/MotoRent.Client/Pages/Vehicles/EditFleetModel.razor` | **NEW** — edit page |
| `src/MotoRent.Client/Pages/Vehicles/CreateFleetModel.razor` | **NEW** — create page |
| `src/MotoRent.Client/Pages/Vehicles/FleetModelForm.razor` | **NEW** — form component |
| `src/MotoRent.Client/Pages/Vehicles/VehicleForm.razor` | Remove shared fields, add read-only FleetModel card |
| `src/MotoRent.Client/Pages/Vehicles/CreateVehicle.razor` | FleetModel selection step |
| `src/MotoRent.Client/Pages/Vehicles/EditVehicle.razor` | Minor — passes FleetModel info |
| `src/MotoRent.Domain/Models/VehicleGroup.cs` | Use FleetModel for rates |
| `database/migrations/CreateFleetModels.sql` | **NEW** — migration script |

## Verification

1. **Build**: `dotnet build` passes
2. **Create FleetModel**: Navigate to /fleet-models, create "Honda Click 125 2024" with rates
3. **Create Vehicle**: Navigate to /vehicles/motorbike/new, select the FleetModel, enter plate/color only
4. **Edit Vehicle**: Open vehicle — shared specs read-only, only unit fields editable
5. **Edit FleetModel**: Change daily rate — all linked vehicles reflect new rate immediately
6. **Tourist Browse**: /browse page shows correct rates from FleetModel
7. **Check-in**: Start rental — pricing calculated correctly from FleetModel rates
8. **Existing data**: Run migration — all existing vehicles linked to auto-created FleetModels
