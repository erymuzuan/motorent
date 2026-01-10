# Motorbike Maintenance Tracking Feature

## Requirements Summary

| Aspect | Decision |
|--------|----------|
| **Trigger** | Both date AND mileage (whichever comes first) |
| **Detail Level** | Minimal - last service date + next due date per service type |
| **Enforcement** | Soft warning with staff override |
| **Service Types** | Configurable list with predefined defaults |
| **Mileage Input** | At rental check-in/check-out (already exists) |
| **Warnings** | Multiple locations (list, detail, dashboard, check-out) |

## Current State

- `Motorbike.Mileage` - Already tracked
- `Motorbike.LastServiceDate` - Basic field exists
- `Rental.MileageStart/End` - Captured during check-in/check-out
- `CheckOutDialog` - Already updates motorbike mileage

## New Entities

### 1. ServiceType
Configurable maintenance types with default intervals per shop.

```
ServiceTypeId, ShopId, Name, Description, DaysInterval, KmInterval, SortOrder, IsActive
```

**Default types**: Oil Change (30d/3000km), Brake Check (60d/5000km), Tire Inspection (90d/8000km), General Service (180d/15000km)

### 2. MaintenanceSchedule
Per-motorbike tracking for each service type.

```
MaintenanceScheduleId, MotorbikeId, ServiceTypeId
LastServiceDate, LastServiceMileage, LastServiceBy, LastServiceNotes
NextDueDate, NextDueMileage
ServiceTypeName (denormalized), MotorbikeName (denormalized)
```

### 3. MaintenanceStatus Enum
`Ok` | `DueSoon` (7 days or 200km) | `Overdue`

## Files to Create

### Domain Layer
| File | Purpose |
|------|---------|
| `src/MotoRent.Domain/Entities/ServiceType.cs` | Service type entity |
| `src/MotoRent.Domain/Entities/MaintenanceSchedule.cs` | Schedule tracking entity |
| `src/MotoRent.Domain/Entities/MaintenanceStatus.cs` | Status enum |

### Database
| File | Purpose |
|------|---------|
| `database/005-maintenance-schema.sql` | ServiceType + MaintenanceSchedule tables |

### Services
| File | Purpose |
|------|---------|
| `src/MotoRent.Services/MaintenanceService.cs` | Business logic + DTOs |

### UI Components
| File | Purpose |
|------|---------|
| `src/MotoRent.Client/Components/Shared/MaintenanceStatusBadge.razor` | Status badge (OK/DueSoon/Overdue) |
| `src/MotoRent.Client/Components/Manager/MaintenanceAlertsWidget.razor` | Dashboard widget |
| `src/MotoRent.Client/Pages/Maintenance/RecordMaintenanceDialog.razor` | Record service dialog |
| `src/MotoRent.Client/Pages/Manager/ServiceTypes.razor` | CRUD page for service types |
| `src/MotoRent.Client/Pages/Manager/ServiceTypeDialog.razor` | Add/edit service type |

## Files to Modify

| File | Change |
|------|--------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add JsonDerivedType for new entities (line 20) |
| `src/MotoRent.Domain/DataContext/RentalDataContext.cs` | Add Query<ServiceType>, Query<MaintenanceSchedule> |
| `src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs` | Register new repositories (line 27) |
| `src/MotoRent.Server/Program.cs` | Register MaintenanceService |
| `src/MotoRent.Client/Pages/Motorbikes.razor` | Add maintenance status column |
| `src/MotoRent.Client/Pages/Rentals/CheckOutDialog.razor` | Add maintenance warning before complete |
| `src/MotoRent.Client/Pages/Manager/Index.razor` | Add MaintenanceAlertsWidget |

## Implementation Steps

### Phase 1: Domain Layer
1. Create `ServiceType.cs` entity
2. Create `MaintenanceSchedule.cs` entity
3. Create `MaintenanceStatus.cs` enum
4. Update `Entity.cs` - add JsonDerivedType attributes
5. Update `RentalDataContext.cs` - add Query properties
6. Update `ServiceCollectionExtensions.cs` - register repositories

### Phase 2: Database
7. Create `005-maintenance-schema.sql` with tables + indexes
8. Run script on development database

### Phase 3: Service Layer
9. Create `MaintenanceService.cs` with:
   - ServiceType CRUD
   - CreateDefaultServiceTypesAsync()
   - RecordServiceAsync()
   - GetSchedulesWithStatusForMotorbikeAsync()
   - GetMaintenanceAlertsAsync()
   - GetMotorbikeMaintenanceSummaryAsync()
   - CalculateStatus() helper
10. Register in Program.cs

### Phase 4: UI Components
11. Create `MaintenanceStatusBadge.razor` - colored badge with icon
12. Create `MaintenanceAlertsWidget.razor` - dashboard widget
13. Create `RecordMaintenanceDialog.razor` - record service form
14. Create `ServiceTypeDialog.razor` - add/edit service type
15. Create `ServiceTypes.razor` - admin CRUD page

### Phase 5: Integration
16. Modify `Motorbikes.razor` - add maintenance column with badge
17. Modify `CheckOutDialog.razor` - add warning + acknowledge checkbox
18. Modify Manager `Index.razor` - add widget to dashboard

### Phase 6: Localization
19. Create `.resx` files for all new components (en, th, ms)

## Key Service Methods

```csharp
// Status calculation
MaintenanceStatus CalculateStatus(nextDueDate, nextDueMileage, currentMileage, today)
  - Overdue: today > nextDueDate OR currentMileage > nextDueMileage
  - DueSoon: within 7 days OR within 200km
  - Ok: otherwise

// Record service
RecordServiceAsync(motorbikeId, serviceTypeId, date, mileage, notes)
  - Updates LastService* fields
  - Calculates NextDue* fields based on ServiceType intervals

// Dashboard query
GetMaintenanceAlertsAsync(shopId, today, limit)
  - Returns bikes with DueSoon or Overdue status
  - Ordered: Overdue first, then by NextDueDate
```

## Warning Display Logic

**Motorbike List**: Show badge if any service is DueSoon/Overdue

**Check-Out Dialog**:
- Show alert if Overdue
- Require acknowledgment checkbox to proceed
- Staff can still complete (soft block)

**Dashboard Widget**:
- Show top 5 alerts
- Link to full maintenance page

## Verification

1. Create a new service type via ServiceTypes page
2. Initialize schedules for a motorbike
3. Record a service - verify NextDueDate/Mileage calculated
4. Fast-forward dates or mileage to trigger DueSoon/Overdue
5. Verify warning appears in motorbike list
6. Verify warning appears during check-out
7. Verify dashboard widget shows alerts
8. Complete check-out with warning acknowledged
