# Architecture

**Analysis Date:** 2026-01-23

## Pattern Overview

**Overall:** Clean Architecture with Multi-Tenant SaaS Pattern

**Key Characteristics:**
- Layered architecture separating Domain, Services, and Presentation
- Multi-tenant data isolation using SQL Server schemas (AccountNo = schema name)
- Repository pattern with Unit of Work for data access
- JSON column storage with computed columns for indexing
- Server-side Blazor with WASM PWA client capability
- Event-driven background processing via RabbitMQ message broker

## Layers

**Presentation Layer (MotoRent.Server + MotoRent.Client):**
- Purpose: HTTP request handling, Blazor components, API controllers
- Location: `src/MotoRent.Server/`, `src/MotoRent.Client/`
- Contains: Razor components, layouts, API controllers, middleware, SignalR hubs
- Depends on: Services, Domain
- Used by: End users (browser/PWA), Tourist portal

**Application/Services Layer (MotoRent.Services):**
- Purpose: Business logic, orchestration, external integrations
- Location: `src/MotoRent.Services/`
- Contains: Service classes (RentalService, VehicleService, etc.), OCR service, search service
- Depends on: Domain
- Used by: Presentation layer, Worker, Scheduler

**Domain Layer (MotoRent.Domain):**
- Purpose: Core entities, data context, repository interfaces, value objects
- Location: `src/MotoRent.Domain/`
- Contains: Entity classes, DataContext (RentalDataContext, CoreDataContext), IRepository interface
- Depends on: None (pure domain)
- Used by: All other layers

**Infrastructure Layer (MotoRent.Core.Repository + MotoRent.Messaging):**
- Purpose: Data access implementation, message broker integration
- Location: `src/MotoRent.Core.Repository/`, `src/MotoRent.Messaging/`
- Contains: SQL JSON repository, LINQ expression tree translation, RabbitMQ broker
- Depends on: Domain
- Used by: Server (via DI registration)

**Background Processing Layer (MotoRent.Worker + MotoRent.Scheduler):**
- Purpose: Async message processing, scheduled tasks
- Location: `src/MotoRent.Worker/`, `src/MotoRent.Scheduler/`
- Contains: Subscribers (event handlers), task runners (scheduled jobs)
- Depends on: Services, Domain, Messaging
- Used by: Runs as separate processes

## Data Flow

**Web Request Flow:**

1. HTTP request arrives at `MotoRent.Server`
2. `TenantDomainMiddleware` resolves tenant from URL/domain for tourist pages
3. Authentication middleware validates cookie/claims
4. `IRequestContext` (MotoRentRequestContext or TouristRequestContext) extracts AccountNo from claims
5. Blazor component loads, injects services
6. Service uses `RentalDataContext` to query data
7. Repository builds SQL with tenant schema `[{AccountNo}].[TableName]`
8. JSON deserialized to entity, returned to component

**Data Persistence Flow:**

1. Component calls service method with entity
2. Service opens `PersistenceSession` from DataContext
3. Entities attached to session for insert/update/delete
4. `SubmitChanges()` executes SQL per entity
5. JSON serialized to `[Json]` column
6. If RabbitMQ enabled, `BrokeredMessage` published with entity change
7. Worker subscribers receive message for async processing

**State Management:**
- Server-side: Scoped services per SignalR circuit
- Client state: Component fields with `@code` blocks
- Shared state: `IRequestContext` provides user/tenant context via DI
- Caching: HybridCache for Core schema entities

## Key Abstractions

**Entity (Base Class):**
- Purpose: Base class for all domain entities with polymorphic JSON serialization
- Examples: `src/MotoRent.Domain/Entities/Entity.cs`
- Pattern: JSON discriminator (`$type`) for polymorphism, abstract `GetId()`/`SetId()` methods
- Key properties: `WebId` (GUID), audit fields (`CreatedBy`, `ChangedBy`, timestamps)

**RentalDataContext:**
- Purpose: Data access facade for tenant-scoped entities
- Examples: `src/MotoRent.Domain/DataContext/RentalDataContext.cs`
- Pattern: Query builder with LINQ-like API, session-based persistence
- Key methods: `CreateQuery<T>()`, `LoadAsync()`, `LoadOneAsync()`, `OpenSession()`

**CoreDataContext:**
- Purpose: Data access facade for shared Core schema entities (Organization, User, Setting)
- Examples: `src/MotoRent.Domain/DataContext/CoreDataContext.cs`
- Pattern: Same as RentalDataContext but uses `[Core]` schema
- Key entities: Organization, User, Setting, AccessToken, LogEntry

**IRepository<T>:**
- Purpose: Generic repository interface for entity persistence
- Examples: `src/MotoRent.Domain/DataContext/IRepository.cs`
- Pattern: CRUD + aggregate queries (count, sum, distinct, group by)
- Implementations: `Repository<T>` (tenant-scoped), `CoreSqlJsonRepository<T>` (Core schema)

**IRequestContext:**
- Purpose: Provides current user, tenant, and timezone context per-request
- Examples: `src/MotoRent.Domain/Core/IRequestContext.cs`
- Pattern: Claims-based identity extraction
- Key methods: `GetUserName()`, `GetAccountNo()`, `GetSchema()`, `GetShopId()`

**PersistenceSession:**
- Purpose: Unit of Work for batch entity operations
- Examples: `src/MotoRent.Domain/DataContext/PersistenceSession.cs`
- Pattern: Attach entities, submit changes atomically
- Key methods: `Attach()`, `Delete()`, `SubmitChanges()`

## Entry Points

**Web Application (MotoRent.Server):**
- Location: `src/MotoRent.Server/Program.cs`
- Triggers: HTTP requests, SignalR connections
- Responsibilities: DI registration, middleware pipeline, authentication, Blazor hosting

**Background Worker (MotoRent.Worker):**
- Location: `src/MotoRent.Worker/Program.cs`
- Triggers: RabbitMQ messages
- Responsibilities: Subscribe to entity changes, process async tasks (notifications, search indexing)

**Scheduler (MotoRent.Scheduler):**
- Location: `src/MotoRent.Scheduler/Program.cs`
- Triggers: Timer/cron schedule
- Responsibilities: Run periodic tasks (maintenance alerts, rental expiry, depreciation)

**API Controllers:**
- Location: `src/MotoRent.Server/Controllers/`
- Triggers: HTTP API requests
- Key controllers: `AccountController` (auth), `DocumentsController` (OCR), `DamagePhotosController`

## Error Handling

**Strategy:** Middleware-based exception logging + graceful component error boundaries

**Patterns:**
- `ExceptionLoggingMiddleware` captures all unhandled exceptions to LogEntry table
- `ErrorBoundary` in layouts catches component render errors with fallback UI
- Repository auto-creates missing tables on first SqlException (schema migration)
- `SubmitOperation` returns success/failure with message for persistence operations

**Error Logging:**
- `ILogger` interface (custom) with `SqlLogger` implementation
- Logs to `[Core].[LogEntry]` table with severity, message, stack trace
- `LogEntryService` for querying/displaying errors in Super Admin pages

## Cross-Cutting Concerns

**Logging:**
- Microsoft.Extensions.Logging for infrastructure
- Custom `ILogger` interface in Domain with `SqlLogger` for application errors

**Validation:**
- Model validation in service layer before persistence
- MudBlazor form validation in components

**Authentication:**
- Cookie-based auth with 14-day sliding expiration
- OAuth providers: Google, Microsoft, LINE
- Claims store AccountNo, ShopId, roles

**Authorization:**
- Role-based: SuperAdmin, OrgAdmin, ShopManager, Staff, Mechanic
- Policy-based: `RequireTenantStaff`, `RequireTenantManager` (requires AccountNo claim)
- Super admin impersonation via special claims

**Multi-Tenancy:**
- `IRequestContext.GetAccountNo()` extracts tenant from claims
- Repository uses `[{AccountNo}]` as SQL schema for data isolation
- `[Core]` schema for shared entities (Organization, User)

**Localization:**
- Resource files per component in `Resources/` folder
- `IStringLocalizer<T>` injection
- Supported cultures: en, th

**Caching:**
- `HybridCache` for Core entities
- Per-request scoped services eliminate need for most caching

---

*Architecture analysis: 2026-01-23*
