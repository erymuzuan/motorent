# MotoRent Accidents Module - Implementation Plan

## Overview
Comprehensive accident management module for tracking vehicle incidents from initial report through resolution, including parties, documents, costs, and insurance claims.

## Requirements Summary
- **Scope**: Full spectrum (vehicle damage to fatalities, police involvement)
- **Parties**: Insurance, police, hospitals, third parties (drivers, pedestrians)
- **Documents**: Photos, police reports, medical records, legal docs, settlements
- **Workflow**: Reported -> In Progress -> Resolved
- **Financials**: Estimated vs actual costs, reserves, insurance payouts
- **Coverage**: Any vehicle incident (rental, staff use, or parked)

---

## Phase 1: Entities & Enums

### 1.1 Enumerations
Create in `src/MotoRent.Domain/Entities/`:

| File | Values |
|------|--------|
| `AccidentSeverity.cs` | Minor, Moderate, Major, Injury, Hospitalization, Fatality |
| `AccidentStatus.cs` | Reported, InProgress, Resolved |
| `AccidentPartyType.cs` | Renter, Staff, ThirdPartyDriver, ThirdPartyPedestrian, ThirdPartyPassenger, Witness, InsuranceCompany, PoliceOfficer, Hospital, LegalRepresentative |
| `AccidentDocumentType.cs` | Photo, Video, PoliceReport, MedicalRecord, InsuranceClaim, RepairQuote, Invoice, Receipt, LegalDocument, SettlementAgreement, WitnessStatement, DiagramSketch |
| `AccidentCostType.cs` | VehicleRepair, MedicalTreatment, ThirdPartyCompensation, LegalFees, LostRentalRevenue, InsuranceDeductible, InsurancePayout, SettlementPayment, TowingStorage, RentalCarReplacement, AdministrativeFees |

### 1.2 Entity Classes

**Accident.cs** - Main entity
```
Properties: AccidentId, VehicleId, RentalId?, ShopId, ReferenceNo, Title, Description,
AccidentDate, ReportedDate, Location, GpsCoordinates?, Severity, Status, PoliceInvolved,
PoliceCaseNumber?, InsuranceClaimFiled, InsuranceClaimNumber?, TotalEstimatedCost,
TotalActualCost, ReserveAmount, InsurancePayoutReceived, NetCost, ResolvedDate?,
ResolutionNotes?, ResolvedBy?, VehicleName?, VehicleLicensePlate?, RenterName?
```

**AccidentParty.cs** - People/orgs involved
```
Properties: AccidentPartyId, AccidentId, PartyType, Name, Phone?, Email?, Address?,
IdNumber?, Organization?, BadgeNumber?, VehicleLicensePlate?, InsurancePolicyNo?,
InvolvementDescription?, IsInjured, InjuryDescription?, IsAtFault?, FaultPercentage?, Notes?, RenterId?
```

**AccidentDocument.cs** - Uploaded files
```
Properties: AccidentDocumentId, AccidentId, DocumentType, FileName, FilePath, FileSize,
ContentType?, Title?, Notes?, DocumentDate?, UploadedDate, UploadedBy, AccidentPartyId?
```

**AccidentCost.cs** - Financial tracking
```
Properties: AccidentCostId, AccidentId, CostType, Description, EstimatedAmount, ActualAmount?,
IsCredit, PaidDate?, ReferenceNo?, VendorName?, AccidentPartyId?, IsApproved, ApprovedBy?, Notes?
```

**AccidentNote.cs** - Timeline/activity log
```
Properties: AccidentNoteId, AccidentId, Content, NoteType (General/PhoneCall/Email/StatusChange/etc),
IsPinned, IsInternal, RelatedEntityType?, RelatedEntityId?, PreviousStatus?, NewStatus?
```

### 1.3 Update Entity.cs
Add JsonDerivedType attributes:
```csharp
[JsonDerivedType(typeof(Accident), nameof(Accident))]
[JsonDerivedType(typeof(AccidentParty), nameof(AccidentParty))]
[JsonDerivedType(typeof(AccidentDocument), nameof(AccidentDocument))]
[JsonDerivedType(typeof(AccidentCost), nameof(AccidentCost))]
[JsonDerivedType(typeof(AccidentNote), nameof(AccidentNote))]
```

---

## Phase 2: Database Tables

Create in `database/tables/`:

| Table | Key Indexes |
|-------|-------------|
| `MotoRent.Accident.sql` | ShopId+Status, VehicleId, RentalId, AccidentDate, Severity+Status, ReferenceNo (unique) |
| `MotoRent.AccidentParty.sql` | AccidentId, AccidentId+PartyType |
| `MotoRent.AccidentDocument.sql` | AccidentId, AccidentId+DocumentType |
| `MotoRent.AccidentCost.sql` | AccidentId, AccidentId+CostType |
| `MotoRent.AccidentNote.sql` | AccidentId |

All tables use JSON column + computed columns pattern per existing conventions.

---

## Phase 3: DataContext & Repository Registration

### 3.1 RentalDataContext.cs
Add Query properties:
```csharp
public Query<Accident> Accidents { get; }
public Query<AccidentParty> AccidentParties { get; }
public Query<AccidentDocument> AccidentDocuments { get; }
public Query<AccidentCost> AccidentCosts { get; }
public Query<AccidentNote> AccidentNotes { get; }
```

### 3.2 ServiceCollectionExtensions.cs
Add repository registrations:
```csharp
services.AddSingleton<IRepository<Accident>, Repository<Accident>>();
services.AddSingleton<IRepository<AccidentParty>, Repository<AccidentParty>>();
services.AddSingleton<IRepository<AccidentDocument>, Repository<AccidentDocument>>();
services.AddSingleton<IRepository<AccidentCost>, Repository<AccidentCost>>();
services.AddSingleton<IRepository<AccidentNote>, Repository<AccidentNote>>();
```

---

## Phase 4: Services

### AccidentService.cs
```csharp
// Core methods
GetAccidentsAsync(shopId, status?, severity?, vehicleId?, fromDate?, toDate?, searchTerm?, page, pageSize)
GetAccidentByIdAsync(accidentId)
GenerateReferenceNoAsync(shopId, accidentDate) // Format: ACC-YYYYMMDD-NNN
CreateAccidentAsync(accident, username)
UpdateAccidentAsync(accident, username)
UpdateStatusAsync(accidentId, newStatus, notes?, username)

// Financial methods
RecalculateFinancialsAsync(accidentId, username)
GetFinancialSummaryAsync(accidentId)

// Statistics
GetStatisticsAsync(shopId, fromDate?, toDate?)
```

### Supporting Services
- **AccidentPartyService**: CRUD for parties
- **AccidentDocumentService**: CRUD + file upload handling
- **AccidentCostService**: CRUD + approval workflow
- **AccidentNoteService**: CRUD + auto-notes for status changes

---

## Phase 5: UI Pages

### 5.1 Page Structure
```
Pages/Accidents/
├── AccidentList.razor           # List with filters (col-4 + col-8)
├── AccidentDetails.razor        # Detail page with tabs
├── ReportAccidentDialog.razor   # New accident dialog
├── Components/
│   ├── AccidentDetailsTab.razor
│   ├── AccidentPartiesTab.razor
│   ├── AccidentDocumentsTab.razor
│   ├── AccidentCostsTab.razor
│   ├── AccidentTimelineTab.razor
│   └── AccidentFinancialSummary.razor
└── Dialogs/
    ├── AddPartyDialog.razor
    ├── AddCostDialog.razor
    ├── AddNoteDialog.razor
    └── UploadDocumentDialog.razor
```

### 5.2 AccidentList.razor
- Route: `/accidents`
- Left column (col-4): Status filter, Severity filter, Vehicle select, Date range, Search
- Left column: Statistics summary card
- Right column (col-8): Table with Reference, Date, Vehicle, Severity, Status, Estimated Cost
- Header action: "Report Accident" button

### 5.3 AccidentDetails.razor
- Route: `/accidents/{AccidentId:int}`
- Left column (col-4): Summary card, Financial summary card
- Right column (col-8): Tabbed interface
  - Details: Basic info, description, location
  - Parties: List of involved parties, add/edit
  - Documents: Gallery view, upload, categorized by type
  - Costs: Cost table with estimated/actual, add/edit, approval
  - Timeline: Activity log, add notes

### 5.4 Status Badges
| Status | Badge Class |
|--------|-------------|
| Reported | `bg-warning-lt text-warning` |
| InProgress | `bg-primary-lt text-primary` |
| Resolved | `bg-success-lt text-success` |

### 5.5 Severity Badges
| Severity | Badge Class |
|----------|-------------|
| Minor | `bg-blue-lt text-blue` |
| Moderate | `bg-orange-lt text-orange` |
| Major | `bg-red-lt text-red` |
| Injury | `bg-pink-lt text-pink` |
| Hospitalization | `bg-purple-lt text-purple` |
| Fatality | `bg-dark text-white` |

---

## Phase 6: Navigation & Localization

### 6.1 NavMenu.razor
Add to Operations section:
```razor
<li class="nav-item">
    <a class="nav-link" href="/accidents">
        <span class="nav-link-icon"><i class="ti ti-alert-triangle"></i></span>
        <span class="nav-link-title">@Localizer["Accidents"]</span>
    </a>
</li>
```

### 6.2 Localization Files
Create for each page component:
- `AccidentList.resx` / `.th.resx`
- `AccidentDetails.resx` / `.th.resx`
- Dialog `.resx` files

---

## Implementation Order

1. **Enums & Entities** (Phase 1)
   - Create all enum files
   - Create all entity classes
   - Update Entity.cs with JsonDerivedType attributes

2. **Database** (Phase 2)
   - Create SQL table scripts
   - Run scripts to create tables

3. **DataContext** (Phase 3)
   - Update RentalDataContext with Query properties
   - Update ServiceCollectionExtensions with repositories

4. **Services** (Phase 4)
   - AccidentService (core CRUD + financials)
   - Supporting services (Party, Document, Cost, Note)

5. **Basic UI** (Phase 5.1-5.2)
   - AccidentList.razor
   - ReportAccidentDialog.razor

6. **Detail Page** (Phase 5.3)
   - AccidentDetails.razor with tabs
   - Tab components

7. **Polish** (Phase 6)
   - Localization files
   - Navigation menu integration

---

## Verification Plan

1. **Build verification**: `dotnet build` passes
2. **Database**: Run SQL scripts, verify tables created
3. **Service test**: Create accident via service, verify saved
4. **UI test** (Claude in Chrome):
   - Navigate to `/accidents`
   - Click "Report Accident", fill form, save
   - View accident details
   - Add party, document, cost
   - Change status to Resolved
   - Verify financial calculations

---

## Critical Files to Modify

| File | Changes |
|------|---------|
| `src/MotoRent.Domain/Entities/Entity.cs` | Add 5 JsonDerivedType attributes |
| `src/MotoRent.Domain/DataContext/RentalDataContext.cs` | Add 5 Query properties + constructor init |
| `src/MotoRent.Domain/DataContext/ServiceCollectionExtensions.cs` | Add 5 repository registrations |
| `src/MotoRent.Client/Shared/NavMenu.razor` | Add Accidents menu item |
