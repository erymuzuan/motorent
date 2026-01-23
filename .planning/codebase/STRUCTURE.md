# Codebase Structure

**Analysis Date:** 2026-01-23

## Directory Layout

```
motorent.document-template-editor/
├── .claude/                    # Claude AI skills and plans
│   ├── plans/                  # Implementation plans
│   └── skills/                 # Pattern documentation
├── .planning/                  # GSD planning documents
│   └── codebase/               # Codebase analysis (this file)
├── database/                   # SQL Server scripts
│   ├── migrations/             # Schema migration scripts
│   ├── seed/                   # Seed data scripts
│   └── tables/                 # Table DDL scripts
├── src/                        # Source code
│   ├── MotoRent.Client/        # Blazor WASM client (PWA)
│   ├── MotoRent.Core.Repository/ # LINQ expression tree to SQL
│   ├── MotoRent.Domain/        # Core entities and data context
│   ├── MotoRent.Messaging/     # RabbitMQ message broker
│   ├── MotoRent.Scheduler/     # Scheduled task runners
│   ├── MotoRent.Server/        # Blazor Server host
│   ├── MotoRent.Services/      # Business logic services
│   └── MotoRent.Worker/        # Background message subscribers
├── tests/                      # Unit tests
│   └── MotoRent.Domain.Tests/  # Domain layer tests
├── qa.tests/                   # QA test assets
├── MotoRent.sln                # Solution file
└── CLAUDE.md                   # AI assistant instructions
```

## Directory Purposes

**src/MotoRent.Server/**
- Purpose: ASP.NET Core Blazor Server host, API controllers, middleware
- Contains: `Program.cs`, Controllers/, Middleware/, Services/, Hubs/, Components/
- Key files:
  - `Program.cs`: DI registration, middleware pipeline, authentication config
  - `Controllers/AccountController.cs`: OAuth callback handlers
  - `Services/MotoRentRequestContext.cs`: Claims-based tenant/user context
  - `Services/TouristRequestContext.cs`: URL-based tenant context for tourist pages
  - `Middleware/TenantDomainMiddleware.cs`: Custom domain resolution
  - `Hubs/CommentHub.cs`: SignalR hub for real-time comments

**src/MotoRent.Client/**
- Purpose: Blazor components, layouts, pages, PWA assets
- Contains: Pages/, Layout/, Components/, Controls/, Services/, Interops/
- Key files:
  - `Layout/MainLayout.razor`: Main authenticated layout with nav
  - `Layout/TouristLayout.razor`: Tourist-facing pages layout
  - `Layout/NavMenu.razor`: Role-based navigation menu
  - `Pages/Rentals/CheckIn.razor`: Multi-step rental wizard
  - `Pages/SuperAdmin/*.razor`: Platform admin pages
  - `Services/ModalService.cs`, `ToastService.cs`: UI notification services

**src/MotoRent.Domain/**
- Purpose: Domain entities, data context, repository interfaces
- Contains: Entities/, Core/, DataContext/, Extensions/, Messaging/, Storage/
- Key files:
  - `Entities/Entity.cs`: Base class with JSON polymorphism attributes
  - `Entities/Rental.cs`, `Vehicle.cs`, `Renter.cs`: Core business entities
  - `Core/Organization.cs`, `User.cs`: Multi-tenant identity entities
  - `Core/IRequestContext.cs`: Tenant/user context interface
  - `DataContext/RentalDataContext.cs`: Tenant-scoped data access
  - `DataContext/CoreDataContext.cs`: Shared schema data access
  - `DataContext/Repository.cs`: Generic repository implementation
  - `DataContext/PersistenceSession.cs`: Unit of Work pattern

**src/MotoRent.Services/**
- Purpose: Business logic, external API integrations
- Contains: Service classes, Search/, Storage/, Core/, Tourist/
- Key files:
  - `RentalService.cs`: Rental CRUD and workflow
  - `VehicleService.cs`: Vehicle management
  - `BookingService.cs`: Online reservations
  - `DocumentOcrService.cs`: Gemini API for passport/license OCR
  - `MaintenanceAlertService.cs`: Automated maintenance scheduling
  - `DynamicPricingService.cs`: Seasonal pricing rules
  - `Core/SqlLogger.cs`: Error logging to database
  - `Search/OpenSearchService.cs`: Full-text search integration
  - `Storage/S3BinaryStore.cs`: AWS S3 file storage

**src/MotoRent.Core.Repository/**
- Purpose: Advanced LINQ expression tree translation to SQL
- Contains: QueryProviders/, repository implementations
- Key files:
  - `CoreSqlJsonRepository.cs`: Core schema repository with caching
  - `TsqlQueryFormatter.cs`: Expression tree to T-SQL conversion
  - `ServiceCollectionExtensions.cs`: DI registration

**src/MotoRent.Messaging/**
- Purpose: RabbitMQ message broker abstraction
- Contains: Broker implementation, configuration
- Key files:
  - `RabbitMqMessageBroker.cs`: IMessageBroker implementation
  - `RabbitMqConfigurationManager.cs`: Environment variable config

**src/MotoRent.Worker/**
- Purpose: Background message processing
- Contains: Infrastructure/, Subscribers/
- Key files:
  - `Program.cs`: Console app entry point
  - `Subscribers/RentalCheckOutSubscriber.cs`: Post-checkout processing
  - `Subscribers/OpenSearchIndexerSubscriber.cs`: Search index updates
  - `Infrastructure/Subscriber.cs`: Base subscriber class

**src/MotoRent.Scheduler/**
- Purpose: Scheduled background tasks
- Contains: Runners/
- Key files:
  - `Program.cs`: Console app with timer scheduling
  - `Runners/MaintenanceAlertRunner.cs`: Generate maintenance alerts
  - `Runners/DepreciationRunner.cs`: Calculate asset depreciation
  - `Runners/RentalExpiryRunner.cs`: Process overdue rentals

**database/tables/**
- Purpose: SQL table DDL scripts with `<schema>` placeholder for multi-tenancy
- Contains: `MotoRent.*.sql` (tenant tables), `Core.*.sql` (shared tables)
- Key files:
  - `MotoRent.Rental.sql`: Rental table with computed columns
  - `MotoRent.Vehicle.sql`: Vehicle inventory
  - `Core.VehicleModel.sql`: Shared vehicle model lookup

## Key File Locations

**Entry Points:**
- `src/MotoRent.Server/Program.cs`: Web app startup, DI, middleware
- `src/MotoRent.Worker/Program.cs`: Background worker console app
- `src/MotoRent.Scheduler/Program.cs`: Scheduled tasks console app

**Configuration:**
- `src/MotoRent.Server/appsettings.json`: Web app settings
- `src/MotoRent.Domain/Core/MotoConfig.cs`: Environment variable config
- Environment variables: `MOTO_SqlConnectionString`, `MOTO_GoogleClientId`, etc.

**Core Logic:**
- `src/MotoRent.Domain/Entities/*.cs`: Domain entity classes
- `src/MotoRent.Services/*Service.cs`: Business logic services
- `src/MotoRent.Domain/DataContext/*.cs`: Data access layer

**Testing:**
- `tests/MotoRent.Domain.Tests/`: Domain unit tests
- `qa.tests/`: Integration test assets

**Layouts:**
- `src/MotoRent.Client/Layout/MainLayout.razor`: Authenticated pages
- `src/MotoRent.Client/Layout/TouristLayout.razor`: Tourist portal
- `src/MotoRent.Client/Layout/StaffLayout.razor`: Staff-specific UI

**Resources/Localization:**
- `src/MotoRent.Client/Resources/`: Localization .resx files
- Pattern: `Resources/Pages/[PageName].en.resx`, `*.th.resx`

## Naming Conventions

**Files:**
- Entities: `{EntityName}.cs` (PascalCase, singular)
- Services: `{Entity}Service.cs`
- Pages: `{PageName}.razor` with optional `{PageName}.razor.cs` code-behind
- Dialogs: `{Action}Dialog.razor` (e.g., `RenterDialog.razor`)
- SQL Tables: `{Schema}.{Entity}.sql`

**Directories:**
- Feature folders in Pages: `Pages/Rentals/`, `Pages/SuperAdmin/`
- Component grouping: `Components/Shared/`, `Components/Vehicles/`
- Service subfolders: `Services/Core/`, `Services/Search/`, `Services/Tourist/`

**Entity Properties:**
- Primary key: `{EntityName}Id` (int)
- Foreign keys: `{RelatedEntity}Id` (int)
- Status fields: `Status` (string enum name)
- Dates: `{Action}Date` (DateOnly), `{Action}Timestamp` (DateTimeOffset)

**CSS Classes:**
- Use Tabler/Bootstrap conventions
- Prefix custom: `mr-` (e.g., `mr-navbar-gradient`)

## Where to Add New Code

**New Entity:**
1. Create entity class: `src/MotoRent.Domain/Entities/{Entity}.cs`
2. Add `[JsonDerivedType]` attribute to `Entity.cs` base class
3. Create SQL table: `database/tables/MotoRent.{Entity}.sql`
4. Optionally create service: `src/MotoRent.Services/{Entity}Service.cs`
5. Register service in `src/MotoRent.Server/Program.cs`

**New Feature Page:**
1. Create page: `src/MotoRent.Client/Pages/{Feature}/{PageName}.razor`
2. Add `@page "/route"` directive
3. Add localization: `src/MotoRent.Client/Resources/Pages/{Feature}/{PageName}.resx`
4. Add navigation entry in `src/MotoRent.Client/Layout/NavMenu.razor` if needed

**New Service:**
1. Create class: `src/MotoRent.Services/{Name}Service.cs`
2. Inject `RentalDataContext` for data access
3. Register in `src/MotoRent.Server/Program.cs`: `builder.Services.AddScoped<{Name}Service>()`

**New Dialog:**
1. Create: `src/MotoRent.Client/Pages/{Feature}/{Name}Dialog.razor`
2. Use pattern from existing dialogs (e.g., `RenterDialog.razor`)
3. Inject via `IModalService.ShowAsync<T>()`

**New API Controller:**
1. Create: `src/MotoRent.Server/Controllers/{Name}Controller.cs`
2. Inherit from `ControllerBase`
3. Add `[Authorize]` attribute as needed
4. Use constructor injection for services

**New Background Subscriber:**
1. Create: `src/MotoRent.Worker/Subscribers/{Name}Subscriber.cs`
2. Inherit from `Subscriber<TMessage>`
3. Implement `OnMessageAsync()` handler
4. Subscriber auto-discovered at runtime

**New Scheduled Task:**
1. Create: `src/MotoRent.Scheduler/Runners/{Name}Runner.cs`
2. Implement `ITaskRunner` interface
3. Register in scheduler's DI and schedule configuration

**Utilities/Extensions:**
- Domain extensions: `src/MotoRent.Domain/Extensions/`
- Client helpers: `src/MotoRent.Client/Services/`

## Special Directories

**.claude/skills/**
- Purpose: AI assistant pattern documentation
- Generated: Manually maintained
- Committed: Yes

**.planning/**
- Purpose: GSD planning and analysis documents
- Generated: By GSD commands
- Committed: Yes

**database/migrations/**
- Purpose: Schema migration scripts
- Generated: Manually created for production
- Committed: Yes

**src/*/obj/, src/*/bin/**
- Purpose: Build output
- Generated: Yes
- Committed: No (in .gitignore)

**qa.tests/**
- Purpose: QA automation test data and fixtures
- Generated: Manually maintained
- Committed: Yes

---

*Structure analysis: 2026-01-23*
