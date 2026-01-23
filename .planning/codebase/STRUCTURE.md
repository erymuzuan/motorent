# Codebase Structure

**Analysis Date:** 2026-01-19

## Directory Layout

```
motorent.cashier-till/
├── .claude/                    # Claude Code skills and plans
│   ├── plans/                  # Implementation plans
│   └── skills/                 # Pattern documentation
├── .planning/                  # GSD planning documents
│   └── codebase/               # Codebase analysis (this file)
├── database/                   # SQL Server schema
│   ├── migrations/             # Schema migrations
│   ├── seed/                   # Seed data scripts
│   └── tables/                 # Table definitions
├── src/                        # Source code
│   ├── MotoRent.Client/        # Blazor WebAssembly client
│   ├── MotoRent.Domain/        # Domain entities and data context
│   ├── MotoRent.Messaging/     # RabbitMQ integration
│   ├── MotoRent.Scheduler/     # Background scheduler
│   ├── MotoRent.Server/        # ASP.NET Core host
│   ├── MotoRent.Services/      # Business logic services
│   └── MotoRent.Worker/        # Background worker
├── tests/                      # Test projects
│   └── MotoRent.Domain.Tests/  # Domain unit tests
├── qa.tests/                   # QA test data and scripts
├── MotoRent.sln                # Solution file
└── CLAUDE.md                   # AI assistant instructions
```

## Directory Purposes

**src/MotoRent.Client/:**
- Purpose: Blazor WebAssembly application with all UI components
- Contains: Razor pages, components, layouts, dialogs, controls, resources
- Key files: `_Imports.razor`, `Routes.razor`

**src/MotoRent.Client/Pages/:**
- Purpose: Routable Blazor pages (@page directive)
- Contains: Feature-organized pages (Rentals/, Settings/, SuperAdmin/, etc.)
- Key files: `Home.razor`, `RentalList.razor`

**src/MotoRent.Client/Components/:**
- Purpose: Reusable UI components without routes
- Contains: Feature widgets, shared components
- Key files: `Components/Shared/TablerHeader.razor`, `Components/Vehicles/VehicleImageGallery.razor`

**src/MotoRent.Client/Controls/:**
- Purpose: Low-level UI controls (file upload, dialogs, maps)
- Contains: Generic controls used across features
- Key files: `Controls/Dialogs/MessageBoxDialog.razor`, `Controls/FileUpload.razor`

**src/MotoRent.Client/Layout/:**
- Purpose: Application layouts for different user roles
- Contains: Layouts with navigation, headers, footers
- Key files: `ManagerLayout.razor`, `StaffLayout.razor`, `TouristLayout.razor`

**src/MotoRent.Client/Resources/:**
- Purpose: Localization resource files
- Contains: .resx files for en, th, ms cultures
- Key files: Mirror structure of Pages/ and Components/

**src/MotoRent.Client/Services/:**
- Purpose: Client-side UI services (modal, toast, dialog)
- Contains: Service classes for UI interactions
- Key files: `ModalService.cs`, `ToastService.cs`, `DialogService.cs`

**src/MotoRent.Domain/:**
- Purpose: Core domain model and data access infrastructure
- Contains: Entity classes, DataContext, Repository, interfaces
- Key files: `Entities/Entity.cs`, `DataContext/RentalDataContext.cs`

**src/MotoRent.Domain/Entities/:**
- Purpose: All domain entity classes
- Contains: Entity definitions with JSON serialization attributes
- Key files: `Rental.cs`, `Vehicle.cs`, `Renter.cs`, `Payment.cs`

**src/MotoRent.Domain/Core/:**
- Purpose: Core multi-tenant entities and interfaces
- Contains: Organization, User, Setting, IRequestContext
- Key files: `IRequestContext.cs`, `Organization.cs`, `User.cs`

**src/MotoRent.Domain/DataContext/:**
- Purpose: Data access layer implementation
- Contains: RentalDataContext, Repository, Query, PersistenceSession
- Key files: `RentalDataContext.cs`, `Repository.cs`, `ObjectBuilder.cs`

**src/MotoRent.Services/:**
- Purpose: Business logic and external integrations
- Contains: Service classes for each domain area
- Key files: `RentalService.cs`, `VehicleService.cs`, `PaymentService.cs`

**src/MotoRent.Services/Core/:**
- Purpose: Core infrastructure services
- Contains: Logging, settings, directory services
- Key files: `SqlLogger.cs`, `SettingConfigService.cs`

**src/MotoRent.Server/:**
- Purpose: ASP.NET Core web host
- Contains: Program.cs, controllers, middleware, SignalR hubs
- Key files: `Program.cs`, `Services/MotoRentRequestContext.cs`

**src/MotoRent.Server/Controllers/:**
- Purpose: API endpoints (authentication, file uploads)
- Contains: MVC controllers
- Key files: `AccountController.cs`, `DocumentsController.cs`

**database/tables/:**
- Purpose: SQL Server table definitions
- Contains: CREATE TABLE scripts with `<schema>` placeholder
- Key files: `MotoRent.Rental.sql`, `MotoRent.Vehicle.sql`

## Key File Locations

**Entry Points:**
- `src/MotoRent.Server/Program.cs`: Application startup, DI configuration
- `src/MotoRent.Client/Routes.razor`: Blazor routing configuration
- `src/MotoRent.Server/Components/App.razor`: Root Blazor component

**Configuration:**
- `src/MotoRent.Server/appsettings.json`: App configuration
- `src/MotoRent.Domain/Settings/MotoConfig.cs`: Environment variable config
- `.env` or environment variables: Secrets (connection strings, OAuth keys)

**Core Logic:**
- `src/MotoRent.Services/RentalService.cs`: Rental check-in/check-out workflows
- `src/MotoRent.Domain/DataContext/RentalDataContext.cs`: Data access facade
- `src/MotoRent.Domain/DataContext/Repository.cs`: Generic repository implementation

**Testing:**
- `tests/MotoRent.Domain.Tests/`: Domain layer unit tests
- `qa.tests/`: QA test data and validation scripts

**Authentication:**
- `src/MotoRent.Server/Controllers/AccountController.cs`: OAuth callbacks, login/logout
- `src/MotoRent.Server/Services/MotoRentRequestContext.cs`: Claims-based user context

## Naming Conventions

**Files:**
- Entity classes: `{EntityName}.cs` (e.g., `Rental.cs`)
- Service classes: `{EntityName}Service.cs` (e.g., `RentalService.cs`)
- Razor pages: `{PageName}.razor` (PascalCase)
- Razor code-behind: `{PageName}.razor.cs` (auto-generated or explicit)
- Resource files: `{ComponentPath}/{ComponentName}.{culture}.resx`
- SQL tables: `{Schema}.{EntityName}.sql` (e.g., `MotoRent.Rental.sql`)

**Directories:**
- Feature folders: PascalCase (e.g., `Pages/Rentals/`, `Pages/Settings/`)
- Nested components: Match page structure (e.g., `Components/Vehicles/`)

**Code:**
- Private fields: `m_` prefix (e.g., `m_loading`, `m_context`)
- Static fields: `s_` prefix (e.g., `s_serviceProvider`)
- Constants: `c_` prefix or PascalCase (e.g., `c_thailandTimezone`)

## Where to Add New Code

**New Feature (e.g., "Invoicing"):**
- Entity: `src/MotoRent.Domain/Entities/Invoice.cs`
- Service: `src/MotoRent.Services/InvoiceService.cs`
- Pages: `src/MotoRent.Client/Pages/Finance/InvoiceList.razor`
- Dialog: `src/MotoRent.Client/Pages/Finance/InvoiceDialog.razor`
- Resources: `src/MotoRent.Client/Resources/Pages/Finance/InvoiceList.resx`
- SQL Table: `database/tables/MotoRent.Invoice.sql`
- Register: Add `services.AddScoped<InvoiceService>()` to `Program.cs`

**New Component (reusable):**
- Shared widget: `src/MotoRent.Client/Components/Shared/{Name}.razor`
- Feature-specific: `src/MotoRent.Client/Components/{Feature}/{Name}.razor`
- Control: `src/MotoRent.Client/Controls/{Name}.razor`

**New Page:**
- Staff-facing: `src/MotoRent.Client/Pages/{Feature}/{PageName}.razor`
- Manager analytics: `src/MotoRent.Client/Pages/Manager/{PageName}.razor`
- Super admin: `src/MotoRent.Client/Pages/SuperAdmin/{PageName}.razor`
- Tourist portal: `src/MotoRent.Client/Pages/Tourist/{PageName}.razor`

**Utilities:**
- Domain extensions: `src/MotoRent.Domain/Extensions/`
- JSON converters: `src/MotoRent.Domain/JsonSupports/`
- Service helpers: Within the service class or as private methods

## Special Directories

**.claude/skills/:**
- Purpose: Pattern documentation for AI assistants
- Generated: No
- Committed: Yes

**.planning/codebase/:**
- Purpose: Codebase analysis documents
- Generated: By GSD map-codebase command
- Committed: Yes

**database/tables/:**
- Purpose: SQL table CREATE scripts
- Generated: No (manual)
- Committed: Yes
- Note: Use `<schema>` placeholder for tenant schema

**src/MotoRent.Client/wwwroot/:**
- Purpose: Static web assets (CSS, JS, images)
- Generated: No
- Committed: Yes

**qa.tests/:**
- Purpose: QA test data and scripts
- Generated: Partially (test data)
- Committed: Yes

---

*Structure analysis: 2026-01-19*
