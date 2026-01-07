# Motorbike Rental System - Thailand (MotoRent)

## Project Overview
Web-based PWA application for motorbike rental services in Thailand tourist areas (Phuket, Krabi, etc.). Supports both rental shop operations and tourist self-service booking.

## Confirmed Requirements

| Requirement | Decision |
|-------------|----------|
| Users | Both shop owners & tourists (renters) |
| Tech Stack | Blazor Server + WASM (PWA) |
| Database | SQL Server |
| OCR/AI | Google Gemini Flash Preview API |
| Payments | Full accounting (deposits, invoicing, reports) |
| Rental Duration | Daily only (MVP) |
| Deposits | Cash + Card pre-authorization |
| Mobile | PWA (Progressive Web App) |
| UI Framework | **MudBlazor** (Material Design) |
| JSON | **System.Text.Json** (high performance, from forex patterns) |

## Architecture

### Leveraging Existing Patterns
Based on exploration of `rx-erp`, `rx-pos`, and `forex`:

```
┌─────────────────────────────────────────────────────────┐
│                    Blazor Server + WASM PWA             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │
│  │ Shop Portal │  │Tourist Portal│  │  Mobile PWA    │  │
│  └─────────────┘  └─────────────┘  └─────────────────┘  │
├─────────────────────────────────────────────────────────┤
│              MudBlazor UI Components (Material Design)  │
├─────────────────────────────────────────────────────────┤
│                    Services Layer                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌───────────┐  │
│  │ Rental   │ │ Document │ │ Payment  │ │ Inventory │  │
│  │ Service  │ │ OCR Svc  │ │ Service  │ │ Service   │  │
│  └──────────┘ └──────────┘ └──────────┘ └───────────┘  │
├─────────────────────────────────────────────────────────┤
│                    Domain Layer                         │
│  Renters, Motorbikes, Rentals, Payments, Documents      │
├─────────────────────────────────────────────────────────┤
│            Custom Repository Pattern (from rx-pos)      │
├─────────────────────────────────────────────────────────┤
│                    SQL Server                           │
└─────────────────────────────────────────────────────────┘
           │
           ▼
    ┌──────────────┐
    │ Google Gemini│
    │ Flash API    │
    │ (OCR)        │
    └──────────────┘
```

## Project Structure

```
MotoRent/
├── .claude/                        # Claude Code context
│   ├── skills/
│   │   ├── database-repository/    # Repository & data context patterns
│   │   ├── messaging-events/       # RabbitMQ subscriber patterns
│   │   ├── blazor-mudblazor/       # MudBlazor components & forms
│   │   ├── code-standards/         # C# conventions
│   │   └── rental-workflow/        # Domain-specific rental patterns
│   ├── plans/
│   │   └── motorent-plan.md        # This file
│   └── project-overview.md
├── src/
│   ├── MotoRent.Server/            # Blazor Server host
│   ├── MotoRent.Client/            # WASM client (PWA)
│   ├── MotoRent.Domain/            # Domain entities
│   │   ├── Entities/
│   │   ├── Core/
│   │   ├── JsonSupports/
│   │   └── Extensions/
│   └── MotoRent.Services/          # Business services
└── tests/
    └── MotoRent.Tests/
```

## Domain Entities

### Core Entities

```csharp
// Renter (Tourist)
public class Renter : Entity
{
    public int RenterId { get; set; }
    public string FullName { get; set; }
    public string? Nationality { get; set; }
    public string? PassportNo { get; set; }
    public string? NationalIdNo { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? DrivingLicenseCountry { get; set; }
    public DateTimeOffset? DrivingLicenseExpiry { get; set; }
    public string Phone { get; set; }
    public string? Email { get; set; }
    public string? HotelName { get; set; }
    public string? HotelAddress { get; set; }
    public string? EmergencyContact { get; set; }
    public string? ProfilePhotoPath { get; set; }
    public int ShopId { get; set; }
}

// Document (Passport/License images + OCR data)
public class Document : Entity
{
    public int DocumentId { get; set; }
    public int RenterId { get; set; }
    public string DocumentType { get; set; }  // Passport, NationalId, DrivingLicense
    public string ImagePath { get; set; }
    public string? OcrRawJson { get; set; }   // Gemini response
    public string? ExtractedData { get; set; } // Parsed fields
    public DateTimeOffset UploadedOn { get; set; }
    public bool IsVerified { get; set; }
}

// Motorbike
public class Motorbike : Entity
{
    public int MotorbikeId { get; set; }
    public int ShopId { get; set; }
    public string LicensePlate { get; set; }
    public string Brand { get; set; }         // Honda, Yamaha, etc.
    public string Model { get; set; }         // Click, PCX, Aerox
    public int EngineCC { get; set; }         // 110, 125, 150, etc.
    public string? Color { get; set; }
    public int Year { get; set; }
    public string Status { get; set; }        // Available, Rented, Maintenance
    public decimal DailyRate { get; set; }
    public decimal DepositAmount { get; set; }
    public string? ImagePath { get; set; }
    public string? Notes { get; set; }
    public int Mileage { get; set; }
    public DateTimeOffset? LastServiceDate { get; set; }
}

// Rental
public class Rental : Entity
{
    public int RentalId { get; set; }
    public int ShopId { get; set; }
    public int RenterId { get; set; }
    public int MotorbikeId { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset ExpectedEndDate { get; set; }
    public DateTimeOffset? ActualEndDate { get; set; }
    public int MileageStart { get; set; }
    public int? MileageEnd { get; set; }
    public decimal DailyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }        // Reserved, Active, Completed, Cancelled
    public string? Notes { get; set; }
    public int? InsuranceId { get; set; }
    public int? DepositId { get; set; }
}

// Deposit
public class Deposit : Entity
{
    public int DepositId { get; set; }
    public int RentalId { get; set; }
    public string DepositType { get; set; }   // Cash, CardPreAuth, Passport
    public decimal Amount { get; set; }
    public string Status { get; set; }        // Held, Refunded, Forfeited
    public string? CardLast4 { get; set; }
    public string? TransactionRef { get; set; }
    public DateTimeOffset CollectedOn { get; set; }
    public DateTimeOffset? RefundedOn { get; set; }
}

// Insurance Package
public class Insurance : Entity
{
    public int InsuranceId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; }          // Basic, Premium, Full Coverage
    public string Description { get; set; }
    public decimal DailyRate { get; set; }
    public decimal MaxCoverage { get; set; }
    public decimal Deductible { get; set; }
    public bool IsActive { get; set; }
}

// Accessory
public class Accessory : Entity
{
    public int AccessoryId { get; set; }
    public int ShopId { get; set; }
    public string Name { get; set; }          // Helmet, Phone Holder, Rain Gear
    public decimal DailyRate { get; set; }
    public int QuantityAvailable { get; set; }
    public bool IsIncluded { get; set; }      // Free with rental
}

// RentalAccessory (junction)
public class RentalAccessory : Entity
{
    public int RentalAccessoryId { get; set; }
    public int RentalId { get; set; }
    public int AccessoryId { get; set; }
    public int Quantity { get; set; }
    public decimal ChargedAmount { get; set; }
}

// Payment
public class Payment : Entity
{
    public int PaymentId { get; set; }
    public int RentalId { get; set; }
    public string PaymentType { get; set; }   // Rental, Insurance, Accessory, Deposit, Damage
    public string PaymentMethod { get; set; } // Cash, Card, PromptPay, BankTransfer
    public decimal Amount { get; set; }
    public string Status { get; set; }        // Pending, Completed, Refunded
    public string? TransactionRef { get; set; }
    public DateTimeOffset PaidOn { get; set; }
    public string? Notes { get; set; }
}

// Shop (Multi-tenant)
public class Shop : Entity
{
    public int ShopId { get; set; }
    public string Name { get; set; }
    public string Location { get; set; }      // Phuket, Krabi, etc.
    public string Address { get; set; }
    public string Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public string? TermsAndConditions { get; set; }
    public bool IsActive { get; set; }
}

// Damage Report
public class DamageReport : Entity
{
    public int DamageReportId { get; set; }
    public int RentalId { get; set; }
    public int MotorbikeId { get; set; }
    public string Description { get; set; }
    public string Severity { get; set; }      // Minor, Moderate, Major
    public decimal EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string Status { get; set; }        // Pending, Charged, Waived, InsuranceClaim
    public DateTimeOffset ReportedOn { get; set; }
}

// Damage Photo
public class DamagePhoto : Entity
{
    public int DamagePhotoId { get; set; }
    public int DamageReportId { get; set; }
    public string PhotoType { get; set; }     // Before, After
    public string ImagePath { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CapturedOn { get; set; }
}

// Rental Agreement (with signature)
public class RentalAgreement : Entity
{
    public int RentalAgreementId { get; set; }
    public int RentalId { get; set; }
    public string AgreementText { get; set; }
    public string SignatureImagePath { get; set; }  // Touch signature image
    public DateTimeOffset SignedOn { get; set; }
    public string SignedByIp { get; set; }
}
```

## Project Location
`D:\project\work\motorent`

## Additional Features Confirmed
- **Damage Documentation**: Before/after photos with cost estimates during check-out
- **Digital Signatures**: Touch signature on screen for rental agreements

## MVP Implementation Phases

### Phase 1: Project Setup & Core Infrastructure (COMPLETED)
1. ✅ Create solution structure
2. ✅ Set up Blazor Server + WASM hybrid
3. ✅ Implement domain entities with System.Text.Json polymorphism
4. ✅ Configure MudBlazor with Tropical Teal theme
5. ✅ Create .claude skills documentation
6. ✅ Configure SQL Server database schema (database/001-create-schema.sql)
7. ✅ Create seed data (database/002-seed-data.sql)
8. ✅ Implement custom repository pattern (MotoRent.Domain/DataContext/)

### Phase 2: Shop Management (Backend) (PARTIALLY COMPLETED)
1. ⏳ Shop registration & settings
2. ✅ Motorbike inventory CRUD (Motorbikes.razor, MotorbikeDialog.razor, MotorbikeService.cs)
3. ⏳ Insurance packages setup
4. ⏳ Accessories management
5. ⏳ Daily rate configuration

### Phase 3: Renter Registration & Document OCR (PARTIALLY COMPLETED)
1. ✅ Renter registration form (Renters.razor, RenterDialog.razor, RenterService.cs)
2. ⏳ Camera/photo upload for documents
3. ⏳ **Google Gemini Flash API integration**
4. ⏳ Manual verification/correction UI
5. ⏳ Store documents securely

### Phase 4: Rental Workflow
1. Check-in process with stepper wizard
2. Active rentals dashboard
3. Check-out process with damage documentation

### Phase 5: Payments & Accounting
1. Payment recording (Cash, Card, PromptPay)
2. Invoice generation
3. Daily/weekly/monthly reports
4. Deposit tracking

### Phase 6: Tourist Self-Service Portal
1. Browse available motorbikes
2. Online reservation
3. View rental history

### Phase 7: PWA Features
1. Service worker for offline support
2. Install prompt
3. Push notifications (rental expiry)
4. Camera access for document capture

## Key UI Screens (MudBlazor)

### MudBlazor Components to Use
- **MudDataGrid** - Motorbike inventory, rental lists
- **MudStepper** - Check-in/check-out wizard flows
- **MudDatePicker** - Rental date selection
- **MudFileUpload** - Document/photo uploads
- **MudDialog** - Confirmations, forms
- **MudSnackbar** - Notifications
- **MudChip** - Status badges
- **MudCard** - Dashboard cards
- **MudForm** - All forms with validation

## Database Schema (SQL Server)

```sql
-- Example: Rental table with JSON storage
CREATE TABLE [MotoRent].[Rental]
(
    [RentalId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing/querying
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(50)),
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [MotorbikeId] AS CAST(JSON_VALUE([Json], '$.MotorbikeId') AS INT),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL,
    [ChangedBy] VARCHAR(50) NOT NULL,
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL,
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL
)

CREATE INDEX IX_Rental_ShopId_Status ON [MotoRent].[Rental]([ShopId], [Status])
```

## Security Considerations
- Document images stored securely (Azure Blob or encrypted local)
- PII data encryption at rest
- Shop data isolation (multi-tenant)
- Role-based access (Owner, Staff, Tourist)
- HTTPS only
- Input validation for all forms

## Theme Configuration

```csharp
// Tropical Teal theme
Primary = "#00897B"        // Teal 600
PrimaryDarken = "#00695C"  // Teal 800
PrimaryLighten = "#4DB6AC" // Teal 300
Secondary = "#FF7043"      // Deep Orange accent
```

## Pattern Sources

| Pattern | Source Project |
|---------|----------------|
| Repository, Persistence, Messaging | `D:\project\work\rx-erp` |
| Authentication | `D:\project\work\rx-pos` |
| System.Text.Json | `D:\project\work\forex` |
