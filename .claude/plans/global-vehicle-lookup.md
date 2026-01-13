# Global Vehicle Lookups with Gemini Recognition

## Overview
Add a centralized vehicle model lookup database in [Core] schema with Gemini-powered image recognition for automatic vehicle identification from photos.

## Entity

### VehicleModel (Core Schema)
**File:** `src/MotoRent.Domain/Core/VehicleModel.cs`

Single entity combining make and model information for simplicity.

| Property | Type | Description |
|----------|------|-------------|
| VehicleModelId | int | Primary key |
| Make | string | Brand/manufacturer (Honda, Toyota, Yamaha) |
| Model | string | Model name (Click 125, PCX 160, Camry) |
| VehicleType | VehicleType | Motorbike, Car, Van, JetSki, Boat |
| Segment | CarSegment? | For cars only |
| EngineCC | int? | For motorbikes |
| EngineLiters | decimal? | For cars |
| SeatCount | int? | For cars/vans |
| YearFrom | int? | Production start year |
| YearTo | int? | Production end (null=current) |
| IsActive | bool | Soft delete flag |
| DisplayOrder | int | Sort priority |
| Aliases | string[] | Alternative names for Gemini matching |
| SuggestedDailyRate | decimal? | Pricing hint |
| SuggestedDeposit | decimal? | Deposit hint |
| ImageStoreId | string? | Reference image for recognition |
| CountryOfOrigin | string? | Japan, Thailand, etc. |

### VehicleRecognitionResult (DTO)
**File:** `src/MotoRent.Domain/Lookups/VehicleRecognitionResult.cs`

Result from Gemini API with: Confidence, VehicleType, RecognizedMake, RecognizedModel, MatchedVehicleModel, LicensePlate, Color, Year, RawJson, Error

## Database Table

### Core.VehicleModel
```sql
CREATE TABLE [Core].[VehicleModel]
(
    [VehicleModelId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [Make] AS CAST(JSON_VALUE([Json], '$.Make') AS NVARCHAR(100)) PERSISTED,
    [Model] AS CAST(JSON_VALUE([Json], '$.Model') AS NVARCHAR(100)) PERSISTED,
    [VehicleType] AS CAST(JSON_VALUE([Json], '$.VehicleType') AS NVARCHAR(20)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [DisplayOrder] AS CAST(JSON_VALUE([Json], '$.DisplayOrder') AS INT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Indexes
CREATE INDEX IX_VehicleModel_Make ON [Core].[VehicleModel]([Make], [IsActive])
CREATE INDEX IX_VehicleModel_Type ON [Core].[VehicleModel]([VehicleType], [IsActive])
CREATE UNIQUE INDEX IX_VehicleModel_Make_Model ON [Core].[VehicleModel]([Make], [Model])
```

## Services

### VehicleLookupService
**File:** `src/MotoRent.Services/VehicleLookupService.cs`

Methods:
- `GetMakesAsync(VehicleType? type)` - Get distinct makes (optionally filtered by type)
- `GetModelsAsync(string make)` - List models for a make
- `GetModelsByTypeAsync(VehicleType type)` - Models by vehicle type
- `MatchAsync(string make, string model)` - Fuzzy match make+model combo
- `SearchAsync(string term, VehicleType? type)` - Autocomplete search
- CRUD operations for super admin (Create, Update, Deactivate)

### VehicleRecognitionService
**File:** `src/MotoRent.Services/VehicleRecognitionService.cs`

Methods:
- `RecognizeVehicleAsync(string imagePath)` - Analyze vehicle photo
- `RecognizeVehicleFromBase64Async(string base64, string mimeType)` - Base64 input

Gemini prompt extracts: vehicleType, make, model, year, color, licensePlate, engineCC, segment, confidence

## UI Components

### Super Admin Pages
1. **VehicleLookups.razor** (`/super-admin/vehicle-lookups`)
   - Single table with Make and Model columns
   - Filter by vehicle type and make
   - Add/Edit/Deactivate actions
   - Import/Export for bulk data

2. **VehicleModelDialog.razor** - Add/edit vehicle model form
   - Make (text input with autocomplete from existing)
   - Model, VehicleType, Segment, EngineCC, etc.

### Enhanced Vehicle Registration
1. **VehicleRecognitionPanel.razor** - Photo upload with Gemini analysis
   - Drag-drop upload zone
   - "Analyze" button
   - Results preview with "Apply to Form" button

2. **VehicleForm.razor** enhancements:
   - Brand autocomplete from lookups
   - Cascading model dropdown
   - "Not in list" suggestion banner

## Integration Points

### Entity.cs - Add polymorphism
```csharp
[JsonDerivedType(typeof(VehicleModel), nameof(VehicleModel))]
```

### CoreDataContext.cs - Add query
```csharp
public Query<VehicleModel> VehicleModels { get; }
```

### Program.cs - Register services
```csharp
builder.Services.AddScoped<VehicleLookupService>();
builder.Services.AddScoped<VehicleRecognitionService>();
```

## Seed Data (Popular Thai Market)

### Motorbikes
| Make | Models |
|------|--------|
| Honda | Click 125, PCX 160, Wave 110, Scoopy, ADV 150, Forza 350 |
| Yamaha | NMAX 155, Aerox, Fino, Grand Filano, XMAX 300 |
| Vespa | Primavera, Sprint |

### Cars
| Make | Models |
|------|--------|
| Toyota | Yaris, Vios, Camry, Fortuner, Innova |
| Honda | City, Civic, HR-V, CR-V |
| Mazda | Mazda 2, Mazda 3, CX-5 |

### Vans
| Make | Models |
|------|--------|
| Toyota | HiAce Commuter, HiAce VIP |
| Hyundai | H1, Staria |

## Implementation Steps

### Phase 1: Database & Entity
1. Create `database/tables/Core.VehicleModel.sql`
2. Create `src/MotoRent.Domain/Core/VehicleModel.cs`
3. Create `src/MotoRent.Domain/Lookups/VehicleRecognitionResult.cs`
4. Update `Entity.cs` with JsonDerivedType attribute
5. Update `CoreDataContext.cs` with Query property

### Phase 2: Services
1. Create `src/MotoRent.Services/VehicleLookupService.cs`
2. Create `src/MotoRent.Services/VehicleRecognitionService.cs`
3. Register services in `Program.cs`

### Phase 3: Super Admin UI
1. Create `Pages/SuperAdmin/VehicleLookups.razor`
2. Create `Pages/SuperAdmin/VehicleModelDialog.razor`
3. Add localization resources (.resx files)
4. Add nav menu entry

### Phase 4: Vehicle Form Enhancement
1. Create `Components/Vehicles/VehicleRecognitionPanel.razor`
2. Modify `Pages/Vehicles/VehicleForm.razor` - add recognition panel
3. Add make autocomplete using MudAutocomplete
4. Add cascading model dropdown

### Phase 5: Seed Data & Testing
1. Create `database/010-seed-vehicle-lookups.sql`
2. Test with real vehicle photos
3. Verify Thai license plate extraction

## Verification

1. Run database migrations to create Core.VehicleModel table
2. Navigate to `/super-admin/vehicle-lookups` and verify CRUD operations
3. Add a new vehicle at `/vehicles/motorbike/new`:
   - Upload a vehicle photo
   - Verify Gemini recognition returns make/model/plate
   - Verify lookup matching highlights known vehicles
   - Verify "not in list" suggestion for unknown vehicles
4. Test make autocomplete and model cascading dropdown

## Files to Modify
- `src/MotoRent.Domain/Entities/Entity.cs` - Add JsonDerivedType
- `src/MotoRent.Domain/DataContext/CoreDataContext.cs` - Add Query property
- `src/MotoRent.Client/Pages/Vehicles/VehicleForm.razor` - Add recognition panel
- `src/MotoRent.Server/Program.cs` - Register services

## Files to Create
- `database/tables/Core.VehicleModel.sql`
- `database/010-seed-vehicle-lookups.sql`
- `src/MotoRent.Domain/Core/VehicleModel.cs`
- `src/MotoRent.Domain/Lookups/VehicleRecognitionResult.cs`
- `src/MotoRent.Services/VehicleLookupService.cs`
- `src/MotoRent.Services/VehicleRecognitionService.cs`
- `src/MotoRent.Client/Pages/SuperAdmin/VehicleLookups.razor`
- `src/MotoRent.Client/Pages/SuperAdmin/VehicleModelDialog.razor`
- `src/MotoRent.Client/Components/Vehicles/VehicleRecognitionPanel.razor`
- Localization resources (4 .resx files per component)

## Status: COMPLETED
All implementation tasks have been completed and the build succeeds.
