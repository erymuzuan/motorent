# HashIds URL Encoding Implementation Plan

## Overview
Implement HashIds.NET pattern for encoding/decoding entity IDs in URLs and query string parameters, following the pattern established in rx-erp.

## Current State

### Already Configured
1. **HashIds DI Registration** (`Program.cs:26`)
   ```csharp
   builder.Services.AddSingleton<IHashids>(_ => new Hashids(builder.Configuration["HashId:Salt"] ?? "motorent", 8));
   ```

2. **MotoRentComponentBase** (`Controls/MotoRentComponentBase.cs`)
   - `[Inject] protected IHashids HashId`
   - `DecodeId(string? id, int index = 0)` - Decodes hash string to int
   - `DecodeIdList(string? id)` - Decodes to array of ints
   - `EncodeId(params int[] ids)` - Encodes ints to hash string

### Pages Already Using HashIds Pattern
| Page | Parameter | Status |
|------|-----------|--------|
| VehicleDetails.razor | `string VehicleId` + `DecodeId()` | ✅ Complete |
| EditVehicle.razor | `string VehicleId` + `DecodeId()` | ✅ Complete |
| RentalDetails.razor | `string RentalId` + `DecodeId()` | ✅ Complete |
| AccidentDetails.razor | `string AccidentId` + `DecodeId()` | ✅ Complete |

## Implementation Tasks

### Phase 1: Pages with Integer Route Parameters (Need to change to string + DecodeId)

| File | Current | Change To |
|------|---------|-----------|
| `SuperAdmin/SupportRequestDetails.razor` | `int Id` with `:int` constraint | `string Id` + `DecodeId()` |
| `Tourist/VehicleDetails.razor` | `int VehicleId` with `:int` constraint | `string VehicleId` + `DecodeId()` |
| `Tourist/Browse.razor` | `int? SelectedShopId` with `:int` constraint | `string? SelectedShopId` + `DecodeId()` |

### Phase 2: Dialog Components (Parameters remain int, callers encode)
Dialogs receive integer IDs from parent components - they don't need changes. The calling code should pass decoded ints.

| Dialog | Parameters | Action |
|--------|------------|--------|
| InvoiceDialog.razor | `int RentalId` | No change needed |
| QRCodeDialog.razor | `int RentalId` | No change needed |
| RecordMaintenanceDialog.razor | `int MotorbikeId`, `int ServiceTypeId` | No change needed |

### Phase 3: Links Using Raw Integer IDs (Need EncodeId)

#### Vehicles.razor
```razor
// Line 364, 521
href="/vehicles/@vehicle.VehicleId/edit"
// Change to:
href="/vehicles/@EncodeId(vehicle.VehicleId)/edit"
```

#### VehicleDetails.razor
```razor
// Line 19
href="/vehicles/@VehicleId/edit"
// Change to:
href="/vehicles/@VehicleId/edit"  // Already encoded from URL param

// Line 236
href="/rentals/view/@rental.RentalId"
// Change to:
href="/rentals/view/@EncodeId(rental.RentalId)"
```

#### RentalDetails.razor
```razor
// Line 330
href="/renters/@m_renter.RenterId"
// Change to:
href="/renters/@EncodeId(m_renter.RenterId)"

// Line 432
href="/inventory/vehicles/@m_vehicle.VehicleId"
// Change to:
href="/vehicles/@EncodeId(m_vehicle.VehicleId)"
```

#### Deposits.razor
```razor
// Line 155
href="/rentals?id=@item.Deposit.RentalId"
// Change to:
href="/rentals/view/@EncodeId(item.Deposit.RentalId)"
```

#### Payments.razor
```razor
// Line 119
href="/rentals?id=@item.Payment.RentalId"
// Change to:
href="/rentals/view/@EncodeId(item.Payment.RentalId)"
```

#### Manager/Revenue.razor
```razor
// Line 198
href="/manager/rentals/@tx.RentalId"
// Change to:
href="/rentals/view/@EncodeId(tx.RentalId)"
```

#### Manager/Fleet.razor
```razor
// Lines 192, 195
href="/motorbikes/@bike.MotorbikeId"
href="/motorbikes/@bike.MotorbikeId/edit"
// Change to:
href="/vehicles/@EncodeId(bike.MotorbikeId)"
href="/vehicles/@EncodeId(bike.MotorbikeId)/edit"
```

#### Tourist/Landing.razor
```razor
// Line 194
href="@TenantUrl($"browse/{shop.ShopId}")"
// Change to:
href="@TenantUrl($"browse/{EncodeId(shop.ShopId)}")"
```

#### Tourist/VehicleDetails.razor
```razor
// Line 313
href="@VehicleUrl(bike.MotorbikeId)"
// Verify VehicleUrl helper encodes, or change to:
href="@TenantUrl($"vehicle/{EncodeId(bike.MotorbikeId)}")"
```

#### SuperAdmin/SupportRequests.razor
```razor
// Lines 88, 119
href="/super-admin/support-requests/@sr.SupportRequestId"
// Change to:
href="/super-admin/support-requests/@EncodeId(sr.SupportRequestId)"
```

### Phase 4: NavigateTo Calls with Raw IDs (Need EncodeId)

| File | Line | Current | Change To |
|------|------|---------|-----------|
| `AccidentList.razor` | 311 | `$"/accidents/{accident.AccidentId}"` | `$"/accidents/{EncodeId(accident.AccidentId)}"` |
| `ActiveRentals.razor` | 282 | `$"/rentals/{rental.RentalId}"` | `$"/rentals/view/{EncodeId(rental.RentalId)}"` |
| `ActiveRentals.razor` | 301 | `$"/staff/return?rentalId={rental.RentalId}"` | `$"/staff/return?rentalId={EncodeId(rental.RentalId)}"` |
| `RentalList.razor` | 830 | `$"/rentals/view/{rental.Rental.RentalId}"` | `$"/rentals/view/{EncodeId(rental.Rental.RentalId)}"` |
| `Staff/Index.razor` | 180 | `$"/staff/return?rentalId={rentalId}"` | `$"/staff/return?rentalId={EncodeId(rentalId)}"` |
| `Vehicles.razor` | 652 | `$"/vehicles/{vehicle.VehicleId}/edit"` | `$"/vehicles/{EncodeId(vehicle.VehicleId)}/edit"` |
| `Tourist/Browse.razor` | 355 | `TenantUrl($"vehicle/{bike.MotorbikeId}")` | `TenantUrl($"vehicle/{EncodeId(bike.MotorbikeId)}")` |
| `CreateVehicle.razor` | 125 | `$"/vehicles/{m_vehicle.VehicleId}/edit"` | `$"/vehicles/{EncodeId(m_vehicle.VehicleId)}/edit"` |

### Phase 5: Query String Parameters

The Staff/Return.razor page uses `?rentalId=` query param. Need to check:

```razor
// Staff/Return.razor - parse query string with DecodeId
[SupplyParameterFromQuery] public string? RentalId { get; set; }
// Decode in OnParametersSetAsync:
m_rentalId = DecodeId(RentalId);
```

## Implementation Order

1. **Phase 1**: Update page route parameters (3 pages)
   - SuperAdmin/SupportRequestDetails.razor
   - Tourist/VehicleDetails.razor
   - Tourist/Browse.razor

2. **Phase 5**: Update query string handling
   - Staff/Return.razor

3. **Phase 3 & 4**: Update all links and NavigateTo calls (batch update)
   - Search and replace pattern in each file

## Testing Checklist

- [ ] Navigate to vehicle details via list page
- [ ] Edit vehicle from details page
- [ ] View rental from vehicle history
- [ ] Navigate to rental details from various pages
- [ ] Check tourist portal navigation
- [ ] Test return page with query string
- [ ] Verify URLs don't expose integer IDs
- [ ] Test browser back/forward navigation

## Notes

- The rx-erp pattern uses `{IdText}` naming convention for route params
- MotoRent uses just `{VehicleId}`, `{RentalId}` etc. which is fine
- Dialog components receive decoded ints from parent components
- Tourist pages need special attention for tenant-prefixed URLs
