# MotoRent - Claude Code Instructions

## Project Overview
Motorbike rental system for Thailand tourist areas (Phuket, Krabi, etc.). Blazor Server + WASM PWA with MudBlazor UI.

## Tech Stack
- **Frontend**: Blazor Server + WASM (PWA)
- **UI**: MudBlazor (Material Design) - Tropical Teal theme (#00897B)
- **Database**: SQL Server with JSON columns and computed columns
- **ORM**: Custom Repository Pattern (from rx-erp)
- **JSON**: System.Text.Json with polymorphism support
- **Messaging**: RabbitMQ (planned)
- **OCR**: Google Gemini Flash API (planned)

## Project Structure
```
motorent/
├── .claude/                    # Skills documentation
│   ├── skills/                 # Pattern references
│   └── plans/                  # Implementation plans
├── database/                   # SQL scripts
├── src/
│   ├── MotoRent.Server/        # Blazor Server host
│   ├── MotoRent.Client/        # WASM client (PWA)
│   ├── MotoRent.Domain/        # Entities & DataContext
│   └── MotoRent.Services/      # Business services
└── tests/
```

## Build Commands
```bash
cd D:\project\work\motorent
dotnet build
dotnet run --project src/MotoRent.Server
```

## Code Conventions

### Naming
- Private fields: `m_` prefix (e.g., `m_context`, `m_loading`)
- Static fields: `s_` prefix (e.g., `s_defaultOptions`)
- Constants: `c_` prefix or PascalCase
- Async methods: `Async` suffix

### Entity Pattern
All entities inherit from `Entity` base class with:
- `WebId` - GUID for client-side tracking
- `GetId()` / `SetId()` - Abstract ID accessors
- Audit fields: `CreatedBy`, `ChangedBy`, `CreatedTimestamp`, `ChangedTimestamp`

### Repository Pattern
```csharp
// Load data
var context = new RentalDataContext();
var motorbike = await context.LoadOneAsync<Motorbike>(m => m.MotorbikeId == id);

// Save data
using var session = context.OpenSession("username");
session.Attach(motorbike);
await session.SubmitChanges("Operation");
```

### Service Pattern
Services are scoped and injected via DI:
```csharp
public class MotorbikeService
{
    private readonly RentalDataContext m_context;
    public MotorbikeService(RentalDataContext context) => m_context = context;
}
```

### Blazor Components
- Use MudBlazor components (MudDataGrid, MudDialog, MudForm, etc.)
- Clone entities before editing: `var cloned = entity.Clone();`
- Use `@inject` for service injection
- Private fields use `m_` prefix in `@code` blocks

## Database Design
Tables use JSON columns with computed columns for indexing:
```sql
CREATE TABLE [MotoRent].[Entity]
(
    [EntityId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [ComputedColumn] AS CAST(JSON_VALUE([Json], '$.Property') AS TYPE),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
```

## Key Entities
| Entity | Purpose |
|--------|---------|
| Shop | Multi-tenant shop configuration |
| Renter | Tourist/customer information |
| Motorbike | Inventory with status tracking |
| Rental | Rental transactions |
| Deposit | Cash/card deposits |
| Insurance | Insurance packages |
| Accessory | Helmets, phone holders, etc. |
| Payment | Payment records |
| DamageReport | Damage documentation |
| Document | Passport/license OCR data |

## Motorbike Status Values
- `Available` - Ready for rental
- `Rented` - Currently rented out
- `Maintenance` - Under repair/service

## Rental Status Values
- `Reserved` - Booking confirmed
- `Active` - Currently rented
- `Completed` - Returned successfully
- `Cancelled` - Booking cancelled

## Current Implementation Status
- [x] Solution structure
- [x] Domain entities with JSON polymorphism
- [x] MudBlazor theme configuration
- [x] Database schema (SQL scripts)
- [x] Repository pattern
- [x] Motorbike CRUD pages
- [x] Renter management pages
- [ ] Document OCR (Gemini)
- [ ] Rental check-in/check-out
- [ ] Payment processing
- [ ] Reports

## Skills Reference
See `.claude/skills/` for detailed patterns:
- `database-repository/` - Repository and Unit of Work
- `json-serialization/` - System.Text.Json setup
- `blazor-mudblazor/` - MudBlazor components
- `dialog-pattern/` - Dialog usage
- `code-standards/` - C# conventions
- `domain-entities/` - Entity definitions
- `rental-workflow/` - Check-in/check-out logic

## Connection String
Default: `Server=localhost;Database=MotoRent;Trusted_Connection=True;TrustServerCertificate=True;`

Configure in `appsettings.json` under `ConnectionStrings:MotoRent`

## GitHub Repository
https://github.com/erymuzuan/motorent
