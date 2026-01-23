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
| Deposit | Cash/card deposits |
| Insurance | Insurance packages |
| Accessory | Helmets, phone holders, etc. |
| Payment | Payment records |
| DamageReport | Damage documentation |
| Document | Passport/license OCR data |

## Vehicle Status Values
- `Available` - Ready for rental
- `Rented` - Currently rented out
- `Maintenance` - Under repair/service

## Rental Status Values
- `Reserved` - Booking confirmed
- `Active` - Currently rented
- `Completed` - Returned successfully
- `Cancelled` - Booking cancelled

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

## Navigation Menu Visibility

The NavMenu shows **tenant menus only** - SuperAdmin without impersonation sees no menus (uses dropdown to access `/super-admin/start-page`).

| Menu | Visible To | Role Group |
|------|------------|------------|
| Dashboard | All tenant users | AllRoles |
| Rentals | OrgAdmin, ShopManager, Staff | RentalRoles |
| Finance | OrgAdmin, ShopManager | ManagementRoles |
| Inventory | OrgAdmin, ShopManager, Mechanic | MaintenanceRoles |
| Customers | OrgAdmin, ShopManager, Staff | RentalRoles |
| Settings | OrgAdmin, ShopManager | ManagementRoles |

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

## Current Implementation Status

### Core Infrastructure
- [x] Solution structure
- [x] Domain entities with JSON polymorphism
- [x] Database schema (SQL scripts)
- [x] Repository pattern
- [x] Core multi-tenant module
- [x] Google/Microsoft OAuth authentication
- [x] Super admin impersonation

### Shop Management
- [x] Vehicle CRUD pages
- [x] Insurance packages management
- [x] Accessories management
- [x] Daily rate configuration (embedded in entity dialogs)

### Customer Management
- [x] Renter management pages
- [x] Document OCR (Gemini Flash API - DocumentOcrService.cs)
- [x] Document verification UI

### Rental Operations
- [x] Rental check-in (5-step wizard)
- [x] Rental check-out with damage assessment
- [x] Active rentals dashboard

### Finance
- [x] Payment processing (Cash, Card, PromptPay, BankTransfer)
- [x] Invoice generation
- [x] Deposit tracking and refunds
- [x] Daily/weekly/monthly reports
- [x] Owner payments

### Super Admin
- [x] Organization management pages
- [x] User management pages
- [x] Registration invites
- [x] System logs

### Tourist Portal
- [x] Landing page with tenant branding
- [x] Browse available vehicles
- [x] Online reservation wizard
- [x] Rental history

### PWA Features
- [x] Service worker for offline support
- [x] Install manifest with shortcuts
- [x] Camera access for document capture
- [ ] Push notifications (infrastructure ready)

### Additional Features
- [x] Accident/incident reporting
- [x] Vehicle pool (cross-shop sharing)
- [x] Service locations with drop-off fees
- [x] Maintenance tracking
- [x] Vehicle images gallery

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
