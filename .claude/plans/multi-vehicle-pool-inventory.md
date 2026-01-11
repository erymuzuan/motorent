# Multi-Vehicle Type & Pool-Based Inventory Implementation Plan

## Summary
Extend MotoRent to support multiple vehicle types (Cars, Jet Skis, Boats, Vans) with pool-based multi-shop inventory sharing.

## User Decisions
- **Entity Design**: Rename Motorbike → Vehicle with VehicleType property
- **Multi-Shop**: Pool-based inventory - vehicles can be rented/returned at any pool shop
- **Jet Ski Rates**: Fixed interval rates (15min, 30min, 1hr)
- **Driver/Guide**: Simple add-on fee (checkbox + flat daily fee on vehicle)

---

## Phase 1: Domain Entities & Enums

### New Enums
Create in `src/MotoRent.Domain/Entities/`:

| File | Values |
|------|--------|
| `VehicleType.cs` | Motorbike, Car, JetSki, Boat, Van |
| `CarSegment.cs` | SmallSedan, BigSedan, SUV, Pickup, Hatchback, Minivan, Luxury |
| `RentalDurationType.cs` | Daily, FixedInterval |
| `VehicleStatus.cs` | Available, Rented, Maintenance, Reserved, Retired |

### New Entity: VehiclePool
```
src/MotoRent.Domain/Entities/VehiclePool.cs
- VehiclePoolId (PK)
- Name (string)
- Description (string?)
- ShopIds (List<int>) - shops in this pool
- IsActive (bool)
```

### Rename: Motorbike → Vehicle
```
src/MotoRent.Domain/Entities/Vehicle.cs (rename from Motorbike.cs)

Changes:
- MotorbikeId → VehicleId
- ShopId → HomeShopId (original shop)
- Add: VehiclePoolId (int?) - pool membership
- Add: CurrentShopId (int) - physical location
- Add: VehicleType (enum)
- Add: Segment (CarSegment?) - cars only
- Add: DurationType (RentalDurationType)
- Add: Rate15Min, Rate30Min, Rate1Hour (decimal?) - jet ski interval rates
- Add: DriverDailyFee, GuideDailyFee (decimal?) - boats/vans
- Add: SeatCount, PassengerCapacity (int?) - cars/vans/boats
- Keep: LicensePlate, Brand, Model, Color, Year, Status, DailyRate, DepositAmount, ImagePath, Notes, Mileage, LastServiceDate, EngineCC
```

### Update: Rental Entity
```
src/MotoRent.Domain/Entities/Rental.cs

Changes:
- ShopId → RentedFromShopId
- MotorbikeId → VehicleId (keep MotorbikeId as alias for backward compat)
- Add: ReturnedToShopId (int?) - for cross-shop returns
- Add: VehiclePoolId (int?) - track pool rental
- Add: DurationType (RentalDurationType)
- Add: IntervalMinutes (int?) - for jet ski (15, 30, 60)
- Add: IncludeDriver, IncludeGuide (bool)
- Add: DriverFee, GuideFee (decimal)
- DailyRate → RentalRate (keep DailyRate as alias)
```

---

## Phase 2: Database Schema

### New Table: VehiclePool
```sql
database/tables/MotoRent.VehiclePool.sql
- [VehiclePoolId] INT PK IDENTITY
- [Name] computed
- [IsActive] computed
- [Json] NVARCHAR(MAX)
- Audit columns
```

### Rename: Motorbike → Vehicle
```sql
database/tables/MotoRent.Vehicle.sql (rename from MotoRent.Motorbike.sql)

New computed columns:
- [VehiclePoolId], [CurrentShopId], [HomeShopId]
- [VehicleType], [Segment], [DurationType]
- [Rate15Min], [Rate30Min], [Rate1Hour]
- [DriverDailyFee], [GuideDailyFee]

New indexes:
- IX_Vehicle_VehiclePoolId_Status
- IX_Vehicle_VehicleType_Status
- IX_Vehicle_CurrentShopId_Status
```

### Update: Rental Table
```sql
database/tables/MotoRent.Rental.sql

New computed columns:
- [RentedFromShopId], [ReturnedToShopId]
- [VehiclePoolId], [VehicleId]
- [DurationType], [IntervalMinutes]
- [IncludeDriver], [IncludeGuide]
```

### Migration Script
```sql
database/migrations/006-vehicle-migration.sql
1. Create VehiclePool table
2. Create Vehicle table
3. Create default pool per shop
4. Migrate Motorbike → Vehicle data
5. Update Rental references
6. Drop Motorbike table (after validation)
```

---

## Phase 3: DataContext & Repository

### Update RentalDataContext
```csharp
src/MotoRent.Domain/DataContext/RentalDataContext.cs

- Remove: Query<Motorbike> Motorbikes
- Add: Query<Vehicle> Vehicles
- Add: Query<VehiclePool> VehiclePools
```

### Update Entity.cs
```csharp
src/MotoRent.Domain/Entities/Entity.cs

- Remove: [JsonDerivedType(typeof(Motorbike), nameof(Motorbike))]
- Add: [JsonDerivedType(typeof(Vehicle), nameof(Vehicle))]
- Add: [JsonDerivedType(typeof(VehiclePool), nameof(VehiclePool))]
```

### Register Repositories
```csharp
src/MotoRent.Domain/ServiceCollectionExtensions.cs

- Remove: IRepository<Motorbike>
- Add: IRepository<Vehicle>
- Add: IRepository<VehiclePool>
```

---

## Phase 4: Services

### New: VehiclePoolService
```csharp
src/MotoRent.Services/VehiclePoolService.cs

Methods:
- GetActivePoolsAsync()
- GetPoolsForShopAsync(shopId)
- GetShopsInPoolAsync(poolId)
- GetPooledShopIdsAsync(shopId) - all shops sharing pools with this shop
- CreatePoolAsync(), UpdatePoolAsync()
- AddShopToPoolAsync(), RemoveShopFromPoolAsync()
```

### Rename: MotorbikeService → VehicleService
```csharp
src/MotoRent.Services/VehicleService.cs (rename)

New methods:
- GetAvailableVehiclesForShopAsync(shopId, vehicleType?, includePooled)
- AssignToPoolAsync(vehicleId, poolId)
- RemoveFromPoolAsync(vehicleId)
- UpdateLocationAsync(vehicleId, newShopId)
- GetVehiclesByTypeAsync(shopId, vehicleType)

Updated methods:
- All methods renamed from Motorbike to Vehicle
```

### New: RentalPricingService
```csharp
src/MotoRent.Services/RentalPricingService.cs

Methods:
- CalculatePricing(vehicle, durationType, dates, interval?, driver?, guide?, insurance?, accessories?)
- Returns: RentalPricing DTO with breakdown
```

### Update: RentalService
```csharp
src/MotoRent.Services/RentalService.cs

CheckInAsync changes:
- Validate pool membership for pooled vehicles
- Set VehiclePoolId on rental
- Handle interval rentals differently

CheckOutAsync changes:
- Validate return shop is in pool (for pooled vehicles)
- Update Vehicle.CurrentShopId on cross-shop return
- Set ReturnedToShopId on rental
```

### Update: InvoiceService
```csharp
src/MotoRent.Services/InvoiceService.cs

- Support interval-based pricing for jet skis
- Add driver/guide fee line items
- Handle different duration displays
```

---

## Phase 5: UI Components

### Vehicle Management Pages
```
src/MotoRent.Client/Pages/Vehicles/
├── Vehicles.razor              (rename from Motorbikes.razor)
├── VehicleDialog.razor         (rename from MotorbikeDialog.razor)
├── VehicleTypeFilter.razor     (new - filter by type)
└── VehiclePoolBadge.razor      (new - show pool/location)
```

### Vehicle Pool Management
```
src/MotoRent.Client/Pages/Settings/
├── VehiclePools.razor          (new - list pools)
├── VehiclePoolDialog.razor     (new - create/edit pool)
└── VehiclePoolShops.razor      (new - manage shops in pool)
```

### Check-In Workflow Updates
```
src/MotoRent.Client/Pages/Rentals/CheckInSteps/

Updates:
- SelectMotorbikeStep.razor → SelectVehicleStep.razor
  - Add vehicle type filter
  - Show pool/location badges for pooled vehicles
  - Different card layouts per vehicle type

- ConfigureRentalStep.razor
  - Show driver/guide options for boats/vans
  - Add conditional rendering based on vehicle type

New:
- ConfigureIntervalRentalStep.razor
  - For jet skis only
  - Interval selection (15/30/60 min)
  - Time picker for start time
  - Different pricing display
```

### Check-Out Workflow Updates
```
src/MotoRent.Client/Pages/Rentals/CheckOutSteps/

- Add shop selection dropdown for pooled vehicle returns
- Validate return shop is in pool
- Show cross-shop return indicator
```

### Navigation Updates
```
src/MotoRent.Client/Layout/NavMenu.razor

- Rename "Motorbikes" → "Vehicles" (or "Fleet")
- Add "Vehicle Pools" under Settings (for admins)
```

---

## Phase 6: Localization

### Resource Files to Create/Update
```
Resources/Pages/Vehicles/
├── Vehicles.resx, .en.resx, .th.resx, .ms.resx
├── VehicleDialog.resx, .en.resx, .th.resx, .ms.resx
└── VehiclePools.resx, .en.resx, .th.resx, .ms.resx

Resources/Pages/Rentals/CheckInSteps/
├── SelectVehicleStep.resx, .en.resx, .th.resx, .ms.resx
└── ConfigureIntervalRentalStep.resx, .en.resx, .th.resx, .ms.resx
```

### Key Translations Needed
| Key | English | Thai |
|-----|---------|------|
| VehicleType_Motorbike | Motorbike | รถจักรยานยนต์ |
| VehicleType_Car | Car | รถยนต์ |
| VehicleType_JetSki | Jet Ski | เจ็ทสกี |
| VehicleType_Boat | Boat | เรือ |
| VehicleType_Van | Van | รถตู้ |
| IncludeDriver | Include Driver | รวมคนขับ |
| IncludeGuide | Include Guide | รวมไกด์ |
| Interval15Min | 15 Minutes | 15 นาที |

---

## Implementation Order

### Week 1: Core Infrastructure
1. Create enums (VehicleType, CarSegment, RentalDurationType, VehicleStatus)
2. Create VehiclePool entity
3. Rename Motorbike → Vehicle entity with all new fields
4. Update Rental entity
5. Create/update SQL schema files
6. Update Entity.cs JsonDerivedType
7. Update RentalDataContext

### Week 2: Services
1. Create VehiclePoolService
2. Rename MotorbikeService → VehicleService with pool logic
3. Create RentalPricingService
4. Update RentalService check-in/check-out
5. Update InvoiceService for duration types

### Week 3: UI - Vehicle Management
1. Rename Motorbikes.razor → Vehicles.razor
2. Update VehicleDialog with vehicle type fields
3. Create vehicle type filter component
4. Create pool management pages

### Week 4: UI - Rental Workflow
1. Update SelectVehicleStep for multi-type
2. Update ConfigureRentalStep for driver/guide
3. Create ConfigureIntervalRentalStep for jet skis
4. Update check-out for cross-shop returns
5. Add localization resources

---

## Verification Plan

### Unit Tests
- [ ] VehiclePoolService - pool membership validation
- [ ] RentalPricingService - all duration type calculations
- [ ] RentalService - cross-shop return validation

### Integration Tests
- [ ] Create vehicle in pool, rent from shop A, return to shop B
- [ ] Jet ski interval rental flow (15, 30, 60 min)
- [ ] Boat rental with driver + guide fees
- [ ] Invoice generation for all vehicle types

### Manual Testing
1. **Vehicle Pool Setup**
   - Create pool with 3 shops
   - Assign vehicles to pool
   - Verify availability across shops

2. **Jet Ski Rental**
   - Select jet ski → should show interval options
   - Select 30 min → verify pricing
   - Complete rental → verify invoice

3. **Boat with Driver**
   - Select boat → should show driver/guide options
   - Enable driver → verify daily fee × days
   - Complete rental → verify line items on invoice

4. **Cross-Shop Return**
   - Rent pooled vehicle from Shop A
   - Return at Shop B
   - Verify vehicle CurrentShopId updated
   - Verify vehicle appears at Shop B

---

## Critical Files Summary

### Entities (Create/Modify)
- `src/MotoRent.Domain/Entities/VehicleType.cs` (new)
- `src/MotoRent.Domain/Entities/CarSegment.cs` (new)
- `src/MotoRent.Domain/Entities/RentalDurationType.cs` (new)
- `src/MotoRent.Domain/Entities/VehicleStatus.cs` (new)
- `src/MotoRent.Domain/Entities/VehiclePool.cs` (new)
- `src/MotoRent.Domain/Entities/Vehicle.cs` (rename from Motorbike.cs)
- `src/MotoRent.Domain/Entities/Rental.cs` (modify)
- `src/MotoRent.Domain/Entities/Entity.cs` (modify)

### Database (Create/Modify)
- `database/tables/MotoRent.VehiclePool.sql` (new)
- `database/tables/MotoRent.Vehicle.sql` (rename from Motorbike.sql)
- `database/tables/MotoRent.Rental.sql` (modify)
- `database/migrations/006-vehicle-migration.sql` (new)

### Services (Create/Modify)
- `src/MotoRent.Services/VehiclePoolService.cs` (new)
- `src/MotoRent.Services/VehicleService.cs` (rename from MotorbikeService.cs)
- `src/MotoRent.Services/RentalPricingService.cs` (new)
- `src/MotoRent.Services/RentalService.cs` (modify)
- `src/MotoRent.Services/InvoiceService.cs` (modify)

### UI (Create/Modify)
- `src/MotoRent.Client/Pages/Vehicles/Vehicles.razor` (rename)
- `src/MotoRent.Client/Pages/Vehicles/VehicleDialog.razor` (rename + modify)
- `src/MotoRent.Client/Pages/Settings/VehiclePools.razor` (new)
- `src/MotoRent.Client/Pages/Rentals/CheckInSteps/SelectVehicleStep.razor` (rename)
- `src/MotoRent.Client/Pages/Rentals/CheckInSteps/ConfigureIntervalRentalStep.razor` (new)
- `src/MotoRent.Client/Pages/Rentals/CheckInSteps/ConfigureRentalStep.razor` (modify)



