# Feature Landscape: Document Template Editor

**Domain:** Document Template Designer for Rental SaaS
**Researched:** 2026-01-23
**Confidence:** MEDIUM (based on domain expertise; WebSearch unavailable for competitor verification)

## Table Stakes

Features users expect. Missing = product feels incomplete or unprofessional.

### Designer Core

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Drag-and-drop element placement | Standard in all template builders (Canva, Google Docs, Word) | Medium | Core interaction pattern |
| Text element with formatting | Basic content building block | Low | Bold, italic, alignment, font size |
| Image element (logo upload) | Branding is essential for business documents | Low | Support upload and URL reference |
| Container/layout elements | Users need to organize content in rows/columns | Medium | Two-column layout minimum |
| Property panel | Users expect right-click or sidebar configuration | Low | Shows options for selected element |
| Visual data binding display | Users need to see what's dynamic vs static | Low | `[Rental.StartDate]` vs plain text |
| Undo/redo | Standard document editing expectation | Medium | At least 10 levels |
| Save/load templates | Persistence is fundamental | Low | Existing repository pattern |
| Live preview | Users need to see final output before saving | Medium | Render with sample data |

### Data Binding

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Property picker UI | Users shouldn't type field names manually | Medium | Tree view of available fields |
| Date/currency formatting | Localized display critical for Thai market | Low | `{StartDate:dd MMM yyyy}`, `{Amount:C}` |
| Null handling | Empty fields shouldn't show `null` or break layout | Low | Show empty string or placeholder |
| Context-aware fields | Different document types have different available data | Medium | Agreement has Rental, Receipt has Payment |

### Document Types

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Rental Agreement template | Primary legal document for vehicle handover | Low | Standard rental form |
| Receipt template | Proof of payment required by customers | Low | Payment confirmation |
| Booking Confirmation template | Sent to customers after reservation | Low | Email-friendly format |

### Printing/Export

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| Browser print (Ctrl+P) | Standard web printing | Low | CSS print media |
| PDF download | Customers expect PDF receipts/agreements | Medium | PDF generation library needed |
| A4 paper size support | Standard document format | Low | CSS page setup |

## Differentiators

Features that set product apart. Not universally expected, but high value when present.

### Smart Features

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| AI Clause Suggester | Speeds up agreement creation with industry-standard clauses | Medium | Gemini API already integrated |
| Professional default templates | Users can start immediately without design work | Low | Pre-built templates per document type |
| Template cloning | Quick way to create variations | Low | Copy existing template |
| Template preview with real data | See how document looks with actual rental, not just sample | Low | Select rental to preview |

### Advanced Layout

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Repeater element | Display lists (accessories, clauses, charges) dynamically | High | Complex data binding |
| Multi-page support | Long agreements need page breaks | Medium | Manual page breaks, auto-pagination |
| Header/footer per page | Professional document appearance | Medium | Consistent branding on all pages |
| Page numbering | "Page 1 of 3" standard for legal docs | Low | System field binding |
| Signature placeholder | Digital signature capture integration | Medium | Existing signature capture can integrate |
| QR code element | Link to digital copy or verification | Low | Generate QR from URL |

### Template Management

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Multiple templates per type | Different templates for different purposes | Low | Basic CRUD |
| Default template per type | Staff don't need to choose every time | Low | Setting flag |
| Template approval workflow | OrgAdmin approves before staff can use | Low | Status flag: Draft/Approved |
| Template categories | Organize templates by purpose/language | Low | Tag or category field |

### Localization

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Multi-language templates | Thai customers get Thai, tourists get English | Medium | Same content, different language versions |
| Thai/English toggle in designer | Quick preview in both languages | Low | UI toggle |
| Currency formatting per locale | THB for Thai, USD for international display | Low | Already supported in .NET |

## Anti-Features

Features to explicitly NOT build. Common mistakes in this domain.

| Anti-Feature | Why Avoid | What to Do Instead |
|--------------|-----------|-------------------|
| Full WYSIWYG rich text editor | Massive complexity (TinyMCE, CKEditor). Overkill for template text blocks | Simple formatting: bold, italic, size, alignment only |
| Custom CSS input | Security risk, maintenance nightmare, breaks print | Predefined style options (colors, fonts from brand) |
| Drag anywhere on canvas (pixel positioning) | Frustrating precision, bad print results | Grid/column-based layout system |
| Real-time collaboration | Massive infrastructure complexity | Single-editor model, last-save-wins |
| Complex conditional logic (if/else) | UI complexity explosion, debugging nightmare | Simple show/hide based on data presence (v2) |
| Email template sending | Different delivery mechanism, HTML email is complex | Separate email feature, export PDF to attach |
| Template versioning/history | Database and UI complexity | v2 feature, start with simple overwrite |
| Programmatic template API | Overkill for this use case | UI-only template management |
| Arbitrary page sizes | Increases complexity, A4 covers 99% of needs | A4 only for v1, Letter optional |
| Invoice templates | Receipts cover payment confirmation. Invoices have tax/accounting complexity | Receipts for v1, invoices for v2 |

---

## Rental Document Field Requirements

### Rental Agreement Required Fields

Based on standard rental industry practice and legal requirements.

#### Header Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Company Name | `Organization.Name` | Yes | Legal entity name |
| Company Logo | `Organization.LogoStoreId` | Yes | Branding |
| Company Address | `Organization.Address.*` | Yes | Full address |
| Tax ID | `Organization.TaxNo` | Yes | For Thai tax compliance |
| Company Phone | `Organization.Phone` | Yes | Contact info |
| Shop Name | `Shop.Name` | Yes | Location identifier |
| Shop Address | `Shop.Address` | Yes | Pickup location |
| Agreement Number | `Rental.RentalId` or generated | Yes | Unique reference |
| Agreement Date | Generated (DateTime.Now) | Yes | When signed |

#### Renter Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Renter Full Name | `Renter.FullName` | Yes | Legal name |
| Nationality | `Renter.Nationality` | Yes | For foreigners |
| Passport Number | `Renter.PassportNo` | Conditional | Required for foreigners |
| National ID | `Renter.NationalIdNo` | Conditional | For Thai nationals |
| Driving License Number | `Renter.DrivingLicenseNo` | Yes | Must have valid license |
| License Country | `Renter.DrivingLicenseCountry` | Yes | License jurisdiction |
| License Expiry | `Renter.DrivingLicenseExpiry` | Yes | Must be valid |
| Phone Number | `Renter.Phone` | Yes | Emergency contact |
| Email | `Renter.Email` | Recommended | For receipts |
| Hotel/Address | `Renter.HotelName`, `HotelAddress` | Recommended | Where staying |
| Emergency Contact | `Renter.EmergencyContact` | Recommended | In case of accident |

#### Vehicle Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Vehicle Type | `Vehicle.VehicleType` | Yes | Motorbike/Car/etc |
| Brand | `Vehicle.Brand` | Yes | Honda, Yamaha, etc |
| Model | `Vehicle.Model` | Yes | Click, PCX, etc |
| License Plate | `Vehicle.LicensePlate` | Yes | Registration number |
| Color | `Vehicle.Color` | Recommended | Identification |
| Year | `Vehicle.Year` | Recommended | Model year |
| Engine Size | `Vehicle.EngineCC` or `EngineLiters` | Recommended | 125cc, 1.5L |
| Starting Mileage | `Rental.MileageStart` | Recommended | Odometer reading |

#### Rental Terms Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Rental Start Date | `Rental.StartDate` | Yes | When rental begins |
| Rental End Date | `Rental.ExpectedEndDate` | Yes | When due back |
| Rental Duration | Calculated (days) | Yes | Number of days |
| Daily Rate | `Rental.RentalRate` | Yes | Price per day |
| Total Rental Amount | `Rental.TotalAmount` | Yes | Subtotal |
| Pickup Location | `Rental.PickupLocationName` or Shop | Conditional | If not at shop |
| Dropoff Location | `Rental.DropoffLocationName` or Shop | Conditional | If not at shop |

#### Insurance Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Insurance Package | `Insurance.Name` | Conditional | If insurance selected |
| Insurance Description | `Insurance.Description` | Recommended | Coverage details |
| Insurance Daily Rate | `Insurance.DailyRate` | Conditional | Price |
| Deductible Amount | `Insurance.Deductible` | Conditional | Excess amount |
| Maximum Coverage | `Insurance.MaxCoverage` | Conditional | Cap |

#### Deposit Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Deposit Amount | `Deposit.Amount` | Yes | Security deposit |
| Deposit Type | `Deposit.DepositType` | Yes | Cash/Card/Passport |
| Card Last 4 Digits | `Deposit.CardLast4` | Conditional | For card deposits |

#### Financial Summary
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Rental Subtotal | Calculated | Yes | Days x Rate |
| Insurance Subtotal | Calculated | Conditional | Days x Insurance Rate |
| Accessories Total | Calculated | Conditional | Sum of accessories |
| Location Fees | Calculated | Conditional | Pickup/dropoff fees |
| Total Amount | `Rental.TotalAmount` | Yes | Grand total |
| Deposit Held | `Deposit.Amount` | Yes | Security deposit |
| Amount Paid | Payment sum | Yes | What customer paid |
| Balance Due | Calculated | Conditional | If not fully paid |

#### Signature Section
| Field | Source | Required | Notes |
|-------|--------|----------|-------|
| Renter Signature | Signature capture | Yes | Digital signature image |
| Date Signed | DateTime.Now | Yes | When signed |
| Staff Name | Current user | Recommended | Who processed |

---

### Receipt Required Fields

#### Header
| Field | Source | Required |
|-------|--------|----------|
| Company Name | `Organization.Name` | Yes |
| Company Logo | `Organization.LogoStoreId` | Yes |
| Company Address | `Organization.Address.*` | Yes |
| Tax ID | `Organization.TaxNo` | Yes |
| Receipt Number | `Payment.PaymentId` or generated | Yes |
| Receipt Date | `Payment.PaidOn` or DateTime.Now | Yes |

#### Customer
| Field | Source | Required |
|-------|--------|----------|
| Customer Name | `Renter.FullName` | Yes |
| Phone | `Renter.Phone` | Recommended |

#### Rental Reference
| Field | Source | Required |
|-------|--------|----------|
| Rental ID | `Rental.RentalId` | Yes |
| Vehicle | `Vehicle.Brand` + `Model` + `LicensePlate` | Yes |
| Rental Period | `StartDate` - `EndDate` | Yes |

#### Payment Details
| Field | Source | Required |
|-------|--------|----------|
| Payment Method | `Payment.PaymentMethod` | Yes |
| Transaction Reference | `Payment.TransactionRef` | Conditional |
| Amount Paid | `Payment.Amount` | Yes |

#### Line Items (Repeater)
| Field | Source | Required |
|-------|--------|----------|
| Description | Various | Yes |
| Quantity/Days | Calculated | Yes |
| Unit Price | Rate | Yes |
| Line Total | Calculated | Yes |

#### Totals
| Field | Source | Required |
|-------|--------|----------|
| Subtotal | Calculated | Yes |
| Deposit Held | `Deposit.Amount` | Conditional |
| Total Paid | Sum of payments | Yes |

#### Footer
| Field | Source | Required |
|-------|--------|----------|
| Thank you message | Static | Recommended |
| Contact info | `Shop.Phone` | Recommended |

---

### Booking Confirmation Required Fields

#### Header
| Field | Source | Required |
|-------|--------|----------|
| Company Name | `Organization.Name` | Yes |
| Company Logo | `Organization.LogoStoreId` | Yes |
| Confirmation Title | Static ("Booking Confirmation") | Yes |

#### Booking Reference
| Field | Source | Required |
|-------|--------|----------|
| Booking Reference | `Booking.BookingRef` | Yes |
| Booking Status | `Booking.Status` | Yes |
| Booking Date | `Booking.CreatedTimestamp` | Yes |

#### Customer
| Field | Source | Required |
|-------|--------|----------|
| Customer Name | `Booking.CustomerName` | Yes |
| Email | `Booking.CustomerEmail` | Yes |
| Phone | `Booking.CustomerPhone` | Yes |

#### Reservation Details
| Field | Source | Required |
|-------|--------|----------|
| Pickup Date | `Booking.StartDate` | Yes |
| Return Date | `Booking.EndDate` | Yes |
| Pickup Time | `Booking.PickupTime` | Conditional |
| Pickup Location | `Booking.PickupLocationName` | Conditional |
| Dropoff Location | `Booking.DropoffLocationName` | Conditional |
| Number of Days | Calculated | Yes |

#### Vehicle(s) - Repeater for multi-vehicle bookings
| Field | Source | Required |
|-------|--------|----------|
| Vehicle Type | `BookingItem.VehicleGroupKey` or specific vehicle | Yes |
| Daily Rate | `BookingItem.DailyRate` | Yes |
| Subtotal | Calculated | Yes |

#### Pricing Summary
| Field | Source | Required |
|-------|--------|----------|
| Vehicle Total | Calculated | Yes |
| Insurance Total | Calculated | Conditional |
| Location Fees | Calculated | Conditional |
| Total Amount | `Booking.TotalAmount` | Yes |
| Deposit Required | `Booking.DepositRequired` | Yes |
| Amount Paid | `Booking.AmountPaid` | Conditional |
| Balance Due | Calculated | Conditional |

#### Instructions
| Field | Source | Required |
|-------|--------|----------|
| What to bring | Static (passport, license, etc) | Yes |
| Shop address | `Shop.Address` | Yes |
| Shop phone | `Shop.Phone` | Yes |
| Map link/QR | Generated | Recommended |

---

## Standard Rental Agreement Clauses

These clauses should be available in the AI Clause Suggester and/or included in default templates.

### Liability and Responsibility

**Renter Responsibility Clause**
> The Renter agrees to be fully responsible for the rented vehicle during the rental period. The Renter shall return the vehicle in the same condition as received, normal wear and tear excepted.

**Age and License Requirement**
> The Renter confirms they are at least 18 years of age (21 for cars/vans) and hold a valid driving license appropriate for the vehicle type being rented. International Driving Permit required for non-Thai licenses.

**Insurance Responsibility**
> If the Renter declines optional insurance coverage, the Renter assumes full financial responsibility for any damage to, loss of, or theft of the vehicle up to the full replacement value.

### Vehicle Use Restrictions

**Permitted Use**
> The vehicle shall be used solely for personal transportation within Thailand. The Renter shall not use the vehicle for racing, towing, off-road driving, or any illegal purpose.

**Prohibited Actions**
> The Renter shall NOT: (1) Allow anyone other than the registered renter to operate the vehicle, (2) Operate the vehicle under the influence of alcohol or drugs, (3) Transport the vehicle outside Thailand, (4) Use the vehicle for commercial purposes including ride-sharing services.

**Helmet Requirement (Motorbikes)**
> Both rider and passenger must wear helmets at all times when operating or riding on the motorbike. Failure to wear helmets voids any insurance coverage.

### Damage and Loss

**Damage Liability**
> In case of damage to the vehicle, the Renter is liable for repair costs up to the deductible amount stated in this agreement. With Full Coverage Insurance, liability is limited to the deductible. Without insurance, the Renter is liable for full repair or replacement costs.

**Theft and Total Loss**
> In case of theft or total loss, the Renter must immediately notify the rental shop and file a police report. The Renter is liable for the full replacement value of the vehicle minus any insurance coverage.

**Damage Assessment**
> All damage will be assessed upon return. The rental shop reserves the right to deduct repair costs from the deposit. Any costs exceeding the deposit must be paid before the Renter leaves.

### Financial Terms

**Deposit Terms**
> A deposit is required at the start of the rental. The deposit will be returned upon satisfactory return of the vehicle. The deposit may be held for up to 7 days for credit card deposits pending final charges.

**Late Return Fee**
> Vehicles must be returned by the agreed time. Late returns will be charged at 1.5x the daily rate per day, plus any incurred fees (re-booking costs, missed rentals, etc.).

**Fuel Policy**
> The vehicle is provided with [FULL/SPECIFIC] tank of fuel and must be returned with the same level. A refueling charge plus service fee will be applied if returned with less fuel.

### Insurance Coverage

**Basic Coverage Included**
> Basic third-party liability insurance is included, covering injury to others and damage to third-party property. This does NOT cover damage to the rental vehicle.

**Optional Full Coverage**
> Full Coverage Insurance is available for an additional daily fee. This covers damage to the rental vehicle subject to a deductible of [AMOUNT]. Coverage excludes negligent or prohibited use.

**Insurance Exclusions**
> Insurance does NOT cover: (1) Damage from prohibited use, (2) Driving under the influence, (3) Unlicensed operation, (4) Single-vehicle accidents caused by negligence, (5) Damage to wheels, tires, or undercarriage from improper use.

### Accident Procedures

**In Case of Accident**
> In the event of an accident: (1) Ensure safety of all persons, (2) Call emergency services if needed, (3) Do NOT admit fault, (4) Take photos of all vehicles and damage, (5) Obtain contact details of other parties and witnesses, (6) Contact the rental shop immediately.

**Police Report Required**
> A police report is required for all accidents, thefts, or losses. Failure to obtain a police report may void insurance coverage and result in full liability to the Renter.

### General Terms

**Reservation Cancellation**
> Reservations may be cancelled up to 24 hours before the pickup time for a full refund. Cancellations within 24 hours are subject to a cancellation fee of one day's rental.

**Modification of Terms**
> The rental shop reserves the right to modify these terms with notice. The terms in effect at the time of rental apply to that rental.

**Governing Law**
> This agreement is governed by the laws of Thailand. Any disputes shall be resolved in the courts of [Province], Thailand.

**Agreement Acceptance**
> By signing below, the Renter acknowledges having read, understood, and agreed to all terms and conditions of this rental agreement.

---

## Feature Dependencies

```
Core Elements
    |
    v
Data Binding System --> Property Picker UI
    |
    v
Document Context Models (Agreement/Receipt/Booking)
    |
    v
Template Storage (Database)
    |
    v
Rendering Engine
    |
    +---> HTML View (Browser Print)
    |
    +---> PDF Generation
```

**Critical Path:**
1. Element palette and canvas (core designer)
2. Data binding system with property picker
3. Context models for each document type
4. Template save/load
5. Rendering with sample data
6. Browser print support
7. PDF generation

---

## MVP Recommendation

For MVP, prioritize:

### Must Have (Table Stakes)
1. **Drag-and-drop designer** with text, image, two-column, divider elements
2. **Data binding** with property picker showing available fields
3. **Three document types**: Agreement, Receipt, Booking Confirmation
4. **Browser print** (Ctrl+P)
5. **Template CRUD** in Organization Settings

### Should Have (High Value, Low Complexity)
6. **Professional default templates** per document type
7. **Live preview** with sample data
8. **Date/currency formatting** options

### Could Have (Differentiators)
9. **AI Clause Suggester** for agreements
10. **PDF download**

### Defer to Post-MVP
- Repeater element for line items (complex)
- Multi-page support with headers/footers
- Template approval workflow
- Multi-language template variants
- Conditional show/hide based on data
- Template versioning/history

---

## Sources

- MotoRent existing entity definitions (Rental.cs, Renter.cs, Payment.cs, Booking.cs, Vehicle.cs, Shop.cs, Organization.cs)
- MotoRent PROJECT.md requirements document
- Domain expertise in rental industry practices
- Standard WYSIWYG document editor patterns

**Confidence Note:** Rental document field requirements and standard clauses are based on industry domain knowledge. WebSearch was unavailable to verify against Avis/Hertz/Sixt specific documents. Recommend validation with actual rental company agreements if available.
