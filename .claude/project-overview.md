# MotoRent - Project Overview

Motorbike rental system for Thailand tourist areas (Phuket, Krabi, etc.).

## Quick Links

- **Plan**: [plans/motorent-plan.md](plans/motorent-plan.md)
- **Skills**: [skills/](skills/)

## Tech Stack

| Component | Technology |
|-----------|------------|
| Frontend | Blazor Server + WASM (PWA) |
| UI | MudBlazor (Material Design) |
| Database | SQL Server (JSON columns) |
| ORM | Custom Repository (rx-erp pattern) |
| JSON | System.Text.Json |
| Messaging | RabbitMQ |
| OCR | Google Gemini Flash API |

## Project Location
`D:\project\work\motorent`

## Project Structure

```
motorent/
├── .claude/                        # Claude Code context
│   ├── skills/                     # Skill documentation
│   │   ├── database-repository/    # Repository pattern, data context
│   │   ├── messaging-events/       # RabbitMQ pub/sub patterns
│   │   ├── json-serialization/     # System.Text.Json patterns
│   │   ├── blazor-mudblazor/       # MudBlazor components
│   │   ├── dialog-pattern/         # Dialog patterns
│   │   ├── code-standards/         # C# conventions
│   │   ├── domain-entities/        # Entity definitions
│   │   ├── gemini-ocr/             # Document OCR
│   │   ├── rental-workflow/        # Check-in/check-out logic
│   │   ├── extensions/             # Extension methods
│   │   └── localization/           # Multi-language support
│   └── project-overview.md
├── src/
│   ├── MotoRent.Server/            # Blazor Server host
│   ├── MotoRent.Client/            # WASM client (PWA)
│   ├── MotoRent.Domain/            # Domain entities
│   │   ├── Entities/               # Entity classes
│   │   ├── Core/                   # JsonSerializerService
│   │   ├── JsonSupports/           # Custom converters
│   │   └── Extensions/             # Extension methods
│   └── MotoRent.Services/          # Business services
└── tests/
    └── MotoRent.Tests/
```

## Skills Reference

| Skill | Description |
|-------|-------------|
| [database-repository](skills/database-repository/) | Repository pattern, data context, Unit of Work |
| [messaging-events](skills/messaging-events/) | RabbitMQ pub/sub, subscribers |
| [json-serialization](skills/json-serialization/) | System.Text.Json with polymorphism |
| [blazor-mudblazor](skills/blazor-mudblazor/) | MudBlazor components, theming |
| [dialog-pattern](skills/dialog-pattern/) | MudBlazor dialogs, confirmations |
| [code-standards](skills/code-standards/) | C# conventions, patterns |
| [domain-entities](skills/domain-entities/) | Entity definitions |
| [gemini-ocr](skills/gemini-ocr/) | Document OCR with Gemini |
| [rental-workflow](skills/rental-workflow/) | Check-in/check-out business logic |
| [extensions](skills/extensions/) | Generic extension methods |
| [localization](skills/localization/) | Multi-language support (English/Thai) |

## Pattern Sources

| Pattern | Source Project |
|---------|----------------|
| Repository, Persistence, Messaging | `D:\project\work\rx-erp` |
| Authentication | `D:\project\work\rx-pos` |
| System.Text.Json | `D:\project\work\forex` |

## Key Features (MVP)

1. **Renter Registration** - Passport/license OCR via Gemini
2. **Motorbike Inventory** - CRUD with status tracking
3. **Check-In** - Before photos, signature capture
4. **Check-Out** - Damage documentation, payment
5. **Deposits** - Cash and card pre-authorization
6. **Accounting** - Payments, invoices, reports

## Theme

- Primary: Tropical Teal (#00897B)
- Secondary: Deep Orange (#FF7043)
- Dark mode supported

## Domain Entities

| Entity | Description |
|--------|-------------|
| Shop | Multi-tenant shop configuration |
| Renter | Tourist/customer information |
| Document | Passport/license images + OCR data |
| Motorbike | Inventory with status tracking |
| Rental | Rental transactions |
| Deposit | Cash/card deposits |
| Insurance | Insurance packages |
| Accessory | Helmets, phone holders, etc. |
| RentalAccessory | Junction table |
| Payment | Payment records |
| DamageReport | Damage documentation |
| DamagePhoto | Before/after photos |
| RentalAgreement | Digital signatures |

## Database Design

Uses JSON columns with computed columns for indexing:

```sql
CREATE TABLE [MotoRent].[Rental]
(
    [RentalId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(50)),
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [MotorbikeId] AS CAST(JSON_VALUE([Json], '$.MotorbikeId') AS INT),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL,
    [ChangedBy] VARCHAR(50) NOT NULL,
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL,
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL
)
```

## Running the Application

```bash
cd D:\project\work\motorent\src\MotoRent.Server
dotnet run
```

Opens at: https://localhost:5001
