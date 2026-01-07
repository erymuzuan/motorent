# Domain Entities

Entity definitions for MotoRent rental system.

## Entity Overview

| Entity | Description | Key Fields |
|--------|-------------|------------|
| Shop | Multi-tenant shop | Name, Location, Phone |
| Renter | Tourist/customer | FullName, Passport, Phone |
| Document | ID/license images | DocumentType, ImagePath, OcrData |
| Motorbike | Inventory | LicensePlate, Brand, Status |
| Rental | Rental transaction | Renter, Motorbike, Dates, Amount |
| Deposit | Cash/card deposits | Type, Amount, Status |
| Insurance | Insurance packages | Name, DailyRate, Coverage |
| Accessory | Helmets, etc. | Name, DailyRate, Quantity |
| RentalAccessory | Junction table | RentalId, AccessoryId |
| Payment | Payment records | Type, Method, Amount |
| DamageReport | Damage documentation | Description, Severity, Cost |
| DamagePhoto | Before/after photos | PhotoType, ImagePath |
| RentalAgreement | Digital signature | SignatureImagePath |

## Base Entity Class

```csharp
// MotoRent.Domain/Entities/Entity.cs
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Shop), nameof(Shop))]
[JsonDerivedType(typeof(Renter), nameof(Renter))]
[JsonDerivedType(typeof(Document), nameof(Document))]
[JsonDerivedType(typeof(Motorbike), nameof(Motorbike))]
[JsonDerivedType(typeof(Rental), nameof(Rental))]
[JsonDerivedType(typeof(Deposit), nameof(Deposit))]
[JsonDerivedType(typeof(Insurance), nameof(Insurance))]
[JsonDerivedType(typeof(Accessory), nameof(Accessory))]
[JsonDerivedType(typeof(RentalAccessory), nameof(RentalAccessory))]
[JsonDerivedType(typeof(Payment), nameof(Payment))]
[JsonDerivedType(typeof(DamageReport), nameof(DamageReport))]
[JsonDerivedType(typeof(DamagePhoto), nameof(DamagePhoto))]
[JsonDerivedType(typeof(RentalAgreement), nameof(RentalAgreement))]
public abstract class Entity
{
    public string? WebId { get; set; }

    [JsonIgnore] public string? CreatedBy { get; set; }
    [JsonIgnore] public DateTimeOffset CreatedTimestamp { get; set; }
    [JsonIgnore] public string? ChangedBy { get; set; }
    [JsonIgnore] public DateTimeOffset ChangedTimestamp { get; set; }

    public abstract int GetId();
    public abstract void SetId(int value);
}
```

## Shop

```csharp
public class Shop : Entity
{
    public int ShopId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;  // Phuket, Krabi
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? LogoPath { get; set; }
    public string? TermsAndConditions { get; set; }
    public bool IsActive { get; set; } = true;

    public override int GetId() => this.ShopId;
    public override void SetId(int value) => this.ShopId = value;
}
```

## Renter

```csharp
public class Renter : Entity
{
    public int RenterId { get; set; }
    public int ShopId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Nationality { get; set; }
    public string? PassportNo { get; set; }
    public string? NationalIdNo { get; set; }
    public string? DrivingLicenseNo { get; set; }
    public string? DrivingLicenseCountry { get; set; }
    public DateTimeOffset? DrivingLicenseExpiry { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? HotelName { get; set; }
    public string? HotelAddress { get; set; }
    public string? EmergencyContact { get; set; }
    public string? ProfilePhotoPath { get; set; }

    public override int GetId() => this.RenterId;
    public override void SetId(int value) => this.RenterId = value;
}
```

## Document

```csharp
public class Document : Entity
{
    public int DocumentId { get; set; }
    public int RenterId { get; set; }
    public string DocumentType { get; set; } = string.Empty;  // Passport, NationalId, DrivingLicense
    public string ImagePath { get; set; } = string.Empty;
    public string? OcrRawJson { get; set; }   // Gemini response
    public string? ExtractedData { get; set; } // Parsed fields
    public DateTimeOffset UploadedOn { get; set; }
    public bool IsVerified { get; set; }

    public override int GetId() => this.DocumentId;
    public override void SetId(int value) => this.DocumentId = value;
}
```

## Motorbike

```csharp
public class Motorbike : Entity
{
    public int MotorbikeId { get; set; }
    public int ShopId { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;     // Honda, Yamaha
    public string Model { get; set; } = string.Empty;     // Click, PCX, Aerox
    public int EngineCC { get; set; }                     // 110, 125, 150
    public string? Color { get; set; }
    public int Year { get; set; }
    public string Status { get; set; } = "Available";     // Available, Rented, Maintenance
    public decimal DailyRate { get; set; }
    public decimal DepositAmount { get; set; }
    public string? ImagePath { get; set; }
    public string? Notes { get; set; }
    public int Mileage { get; set; }
    public DateTimeOffset? LastServiceDate { get; set; }

    public override int GetId() => this.MotorbikeId;
    public override void SetId(int value) => this.MotorbikeId = value;
}
```

## Rental

```csharp
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
    public string Status { get; set; } = "Reserved";  // Reserved, Active, Completed, Cancelled
    public string? Notes { get; set; }
    public int? InsuranceId { get; set; }
    public int? DepositId { get; set; }

    public override int GetId() => this.RentalId;
    public override void SetId(int value) => this.RentalId = value;
}
```

## Status Values

### Motorbike.Status
| Status | Description |
|--------|-------------|
| Available | Ready for rental |
| Rented | Currently out |
| Maintenance | Under repair |

### Rental.Status
| Status | Description |
|--------|-------------|
| Reserved | Booked, not started |
| Active | Currently in progress |
| Completed | Returned successfully |
| Cancelled | Cancelled before start |

### Deposit.Status
| Status | Description |
|--------|-------------|
| Held | Deposit collected |
| Refunded | Returned to customer |
| Forfeited | Kept due to damage |

### Payment.Status
| Status | Description |
|--------|-------------|
| Pending | Awaiting payment |
| Completed | Payment received |
| Refunded | Money returned |

### DamageReport.Status
| Status | Description |
|--------|-------------|
| Pending | Awaiting resolution |
| Charged | Customer paid |
| Waived | No charge |
| InsuranceClaim | Sent to insurance |

## File Location

```
MotoRent.Domain/
└── Entities/
    ├── Entity.cs
    ├── Shop.cs
    ├── Renter.cs
    ├── Document.cs
    ├── Motorbike.cs
    ├── Rental.cs
    ├── Deposit.cs
    ├── Insurance.cs
    ├── Accessory.cs
    ├── RentalAccessory.cs
    ├── Payment.cs
    ├── DamageReport.cs
    ├── DamagePhoto.cs
    └── RentalAgreement.cs
```
