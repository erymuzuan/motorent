# Plan: Add Fuel Level, Accessories Checklist & Cleanliness to CheckOut

## Summary
Enhance the CheckOut wizard (Return step) with three new assessment sections:
1. **Fuel/Battery Level** — record fuel gauge at return, compare with check-in level
2. **Accessories Checklist** — verify return of rented accessories (helmets, locks, keys, etc.)
3. **Vehicle Cleanliness** — quick cleanliness assessment with optional cleaning fee

## Existing Codebase Context

| What | Where | Notes |
|------|-------|-------|
| `VehicleInspection.FuelLevel` | `Domain/Entities/VehicleInspection.cs` | Already exists as `int?` (percentage). Not wired to checkout. |
| `Accessory` entity | `Domain/Entities/Accessory.cs` | Has `Name`, `DailyRate`, `IsIncluded` (free with rental) |
| `RentalAccessory` entity | `Domain/Entities/RentalAccessory.cs` | Has `RentalId`, `AccessoryId`, `Quantity`, `ChargedAmount`. No "returned" flag. |
| `CheckOutRequest` | `Services/RentalService.dto.cs:87-113` | Needs new fields for fuel, accessories, cleanliness |
| `RentalService.CheckOutAsync` | `Services/RentalService.workflow.cs:239+` | Backend checkout flow — needs to store new data |
| `CheckOut.razor` | `Client/Pages/Rentals/CheckOut.razor` | 3-step wizard (Return, Damage, Settle). New sections go in Return step. |

## Files to Modify

### 1. `src/MotoRent.Domain/Entities/Rental.cs`
Add fields to store checkout assessment data:
```csharp
// Fuel assessment at return
public int? FuelLevelAtCheckIn { get; set; }   // stored at check-in for comparison
public int? FuelLevelAtCheckOut { get; set; }   // recorded at return
public decimal FuelSurcharge { get; set; }

// Cleanliness assessment
public string? CleanlinessLevel { get; set; }   // "Clean", "Dirty", "VeryDirty"
public decimal CleaningFee { get; set; }

// Accessories return status
public List<AccessoryReturnItem> ReturnedAccessories { get; set; } = [];
```

### 2. `src/MotoRent.Domain/Models/AccessoryReturnItem.cs` (new)
```csharp
public class AccessoryReturnItem
{
    public int AccessoryId { get; set; }
    public string Name { get; set; } = "";
    public int QuantityRented { get; set; }
    public int QuantityReturned { get; set; }
    public bool IsMissing => QuantityReturned < QuantityRented;
    public decimal MissingCharge { get; set; }
}
```

### 3. `src/MotoRent.Services/RentalService.dto.cs` — `CheckOutRequest`
Add fields:
```csharp
// Fuel level
public int? FuelLevelAtReturn { get; set; }
public decimal FuelSurcharge { get; set; }

// Cleanliness
public string? CleanlinessLevel { get; set; }
public decimal CleaningFee { get; set; }

// Accessories
public List<AccessoryReturnItem>? ReturnedAccessories { get; set; }
public decimal AccessoryMissingCharge { get; set; }
```

### 4. `src/MotoRent.Services/RentalService.workflow.cs` — `CheckOutAsync`
After existing PostRentalInspection storage, add:
- Store `FuelLevelAtCheckOut`, `FuelSurcharge` on rental
- Store `CleanlinessLevel`, `CleaningFee` on rental
- Store `ReturnedAccessories` on rental
- Include `FuelSurcharge + CleaningFee + AccessoryMissingCharge` in the additional charges calculation

### 5. `src/MotoRent.Client/Pages/Rentals/CheckOut.razor` — Return Step
Add three new sections to `RenderReturnStep()` (after the Post-Rental Inspection section, before Return Notes):

#### Fuel Level Section
- Slider or segmented control: Empty / 1/4 / Half / 3/4 / Full (mapped to 0, 25, 50, 75, 100)
- If check-in fuel level is available, show comparison: "Check-in: 3/4 → Return: 1/2"
- Auto-calculate fuel surcharge if returned lower than check-in level
- Surcharge = configurable setting (e.g., per-quarter-tank fee from shop settings)

#### Accessories Checklist Section
- Load `RentalAccessory` records for this rental
- For each accessory: show name, quantity rented, checkbox/counter for returned quantity
- Highlight missing items in red with charge amount
- Auto-calculate missing accessory charges

#### Cleanliness Section
- Three-option selector (matching damage-options pattern): Clean / Dirty / Very Dirty
- If Dirty/VeryDirty: show cleaning fee (configurable per shop)
- Info banner: "Cleaning fee will be deducted from deposit"

### 6. `src/MotoRent.Client/Pages/Rentals/CheckOut.razor` — Settle Step
In `RenderSettleStep()`, add settlement rows for:
- Fuel surcharge (if > 0)
- Cleaning fee (if > 0)
- Missing accessories charge (if > 0)

Update `m_totalAdditional` calculation to include these three new charges.

### 7. `src/MotoRent.Client/Pages/Rentals/CheckOut.razor.css`
Add styles for:
- `.fuel-level-selector` — segmented fuel gauge control
- `.accessory-checklist` — list with checkboxes
- `.cleanliness-options` — three-option selector (reuse damage-options pattern)

### 8. Resource Files
**`CheckOut.resx`** — add keys:
- `FuelLevel`, `FuelLevelAtCheckIn`, `FuelLevelAtReturn`, `FuelSurcharge`
- `Empty`, `Quarter`, `Half`, `ThreeQuarters`, `Full`
- `FuelLowerThanCheckIn`, `FuelSurchargeApplied`
- `AccessoriesChecklist`, `AccessoriesChecklistDesc`, `QuantityRented`, `QuantityReturned`, `Missing`, `MissingAccessoryCharge`
- `Cleanliness`, `CleanlinessDesc`, `CleanLevel`, `DirtyLevel`, `VeryDirtyLevel`, `CleaningFee`, `CleaningFeeDeducted`

**`CheckOut.th.resx`** — Thai translations for all above keys

### 9. `src/MotoRent.Client/Pages/Rentals/CheckIn.razor` (optional enhancement)
Store fuel level at check-in time on the Rental entity so it can be compared at checkout. If this is too large a scope, skip and just show fuel level at return without comparison.

## State Fields to Add in CheckOut.razor @code

```csharp
// Fuel level
private int m_fuelLevelAtReturn = 100;   // default Full
private int? m_fuelLevelAtCheckIn;
private decimal m_fuelSurcharge;

// Cleanliness
private string m_cleanlinessLevel = "Clean";
private decimal m_cleaningFee;

// Accessories
private List<AccessoryReturnItem> m_rentalAccessories = [];
private decimal m_accessoryMissingCharge;
```

## Updated Total Calculation

```csharp
private decimal m_totalAdditional =>
    m_extraDaysCharge +
    (m_use3DInspection ? m_3dInspectionDamageEstimate : (m_damageLevel != "None" ? m_damageEstimate : 0)) +
    m_dropoffTotalFees +
    m_fuelSurcharge +
    m_cleaningFee +
    m_accessoryMissingCharge;
```

## UI Layout (Return Step — new sections)

```
[Existing: Summary Card]
[Existing: Return Information (date, mileage)]
[Existing: Cross-shop return]
[Existing: Drop-off location]
[Existing: Late/Early return alerts]

--- NEW SECTIONS ---

[Fuel Level]
  Segmented: Empty | 1/4 | Half | 3/4 | Full
  (if check-in level known) "Check-in: 3/4" comparison
  (if lower) Warning: "Fuel surcharge: +฿200"

[Accessories Checklist]  (only shown if rental has accessories)
  [ ] 2x Helmet — Returned: [2] / 2
  [ ] 1x Phone Holder — Returned: [0] / 1  ⚠ Missing: +฿300
  Total missing charge: ฿300

[Vehicle Cleanliness]
  Three cards: [Clean ✓] [Dirty] [Very Dirty]
  (if not Clean) "Cleaning fee: +฿150"

--- END NEW ---

[Existing: Post-Rental Inspection]
[Existing: Return Notes]
```

## Verification
1. `dotnet build` — no compile errors
2. Navigate to checkout with an active rental
3. Return step shows fuel, accessories (if any rented), and cleanliness sections
4. Change fuel level below check-in → surcharge appears
5. Mark accessory as missing → charge appears
6. Select Dirty/VeryDirty → cleaning fee appears
7. Settlement step shows all new charges in the summary
8. Complete checkout → all data persisted to rental entity
9. Check rental record in DB — fuel, cleanliness, accessories data stored
