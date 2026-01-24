# MotoRent - Claude Code Instructions

## Project Overview
Motorbike rental system for Thailand tourist areas (Phuket, Krabi, etc.). Blazor Server + WASM PWA with Tabler css.

## Tech Stack
- **Frontend**: Blazor Server + WASM (PWA)
- **UI**: Tabler.css
- **Database**: SQL Server with JSON columns and computed columns
- **ORM**: Custom Repository Pattern (from rx-erp)
- **JSON**: System.Text.Json with polymorphism support
- **Messaging**: RabbitMQ (planned)
- **OCR**: Google Gemini Flash API (implemented - DocumentOcrService.cs)

## Project Structure
```
motorent/
├── .claude/                           # Skills documentation
│   ├── skills/                        # Pattern references
│   └── plans/                         # Implementation plans
├── database/                          # SQL scripts
├── src/
│   ├── MotoRent.Server/               # Blazor Server host
│   ├── MotoRent.Client/               # WASM client (PWA)
│   ├── MotoRent.Domain/               # Entities, interfaces & DataContext (implementation-agnostic)
│   ├── MotoRent.SqlServerRepository/  # SQL Server IRepository implementation
│   ├── MotoRent.Core.Repository/      # Core schema SQL Server repository
│   └── MotoRent.Services/             # Business services
└── tests/
```

## Build Commands
```bash
dotnet build
dotnet watch --project src/MotoRent.Server
```

## Code Conventions

### Naming
- Private fields: `m_` prefix (e.g., `m_context`, `m_loading`)
- Async methods: `Async` suffix
-

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
- Inherits `LocalizedComponentBase`
- Clone entities before editing: `var cloned = entity.Clone();`
- Use `@inject` for service injection
- Private fields use `m_` prefix in `@code` blocks

## Database Design
Tables use JSON columns with computed columns for indexing:
```sql
CREATE TABLE [MotoRent].[Entity]
(
    [EntityId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [ComputedColumn] AS CAST(JSON_VALUE([Json], '$.Property') AS TYPE), --  except for DATE and DATETIMEOFFSET
    [Date] DATE NULL,
    [TimeStamp] DATETIMEOFFSET NULL,
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
```

## Key Entities

### Core Entities (Multi-Tenant)
| Entity | Schema | Purpose |
|--------|--------|---------|
| Organization | [Core] | Tenant with AccountNo identifier |
| User | [Core] | User with AccountCollection for multi-tenant access |
| UserAccount | (embedded) | Links user to organization with roles |
| Setting | [Core] | Key-value settings per organization/user |
| AccessToken | [Core] | API access tokens with JWT payload |
| RegistrationInvite | [Core] | Invitation codes for tenant onboarding |
| LogEntry | [Core] | Audit and error logging |

### Operational Entities (Tenant-Specific)
| Entity | Purpose |
|--------|---------|
| Shop | Shop location within an organization |
| Renter | Tourist/customer information |
| Vehicle | Inventory with status tracking |
| Rental | Rental transactions |
// and others

## User Roles

### Platform Role (Core Schema)
| Role | Constant | Description |
|------|----------|-------------|
| Super Admin | `SUPER_ADMIN` (`administrator`) | Platform administrator - manages organizations, users, invites, logs |

### Tenant Roles (Tenant Schema)
| Role | Constant | Description |
|------|----------|-------------|
| Org Admin | `OrgAdmin` | Organization/shop owner - full tenant access |
| Shop Manager | `ShopManager` | Manages a shop location |
| Staff | `Staff` | Rental desk staff |
| Mechanic | `Mechanic` | Maintenance staff |

### Role Groups (defined in `UserAccount.cs`)
- **AllRoles**: OrgAdmin, ShopManager, Staff, Mechanic
- **ManagementRoles**: OrgAdmin, ShopManager
- **RentalRoles**: OrgAdmin, ShopManager, Staff
- **MaintenanceRoles**: OrgAdmin, ShopManager, Mechanic


### SuperAdmin Access
- **Not impersonating**: Access only `/super-admin/*` pages via user dropdown menu
- **Impersonating**: Gets impersonated user's tenant roles, sees menus accordingly

### Super Admin Pages (`/super-admin/*`)
- Organizations - Manage tenants
- Users - Manage system users
- Impersonate - Support user impersonation
- Registration Invites - Manage invite codes
- System Logs - View error logs
- System Settings - Global configuration

## Authentication
- **Providers**: Google, Microsoft OAuth 2.0
- **Cookie**: 14-day sliding expiration
- **JWT**: For API access tokens
- **Impersonation**: Super admin can impersonate users via `/account/impersonate`

### Impersonation URL Format
```
/account/impersonate?user={userName}&account={accountNo}&hash={MD5(userName:accountNo)}
```

## Multi-Tenant Architecture
- **[Core] schema**: Shared entities (Organization, User, Setting, etc.)
- **[AccountNo] schema**: Tenant-specific operational data
- **AccountNo**: Unique tenant identifier stored in Organization
- **IRequestContext**: Provides current tenant/user context from claims



## Skills Reference
See `.claude/skills/` for detailed patterns:
- `database-repository/` - Repository and Unit of Work
- `json-serialization/` - System.Text.Json setup
- `dialog-pattern/` - Dialog usage
- `code-standards/` - C# conventions
- `domain-entities/` - Entity definitions
- `rental-workflow/` - Check-in/check-out logic

## Connection String
Default: `Server=(local)\DEV2022;Database=MotoRent;Trusted_Connection=True;TrustServerCertificate=True;`

Configure in env `MOTO_SqlConnectionString`

## GitHub Repository
https://github.com/erymuzuan/motorent

## Important rules
**database-repository** skill MUST be observed in all data access patterns.
**code-standard** skill MUST be observed when editing .cs and .razor.cs file
