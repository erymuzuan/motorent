# Architecture

**Analysis Date:** 2026-01-19

## Pattern Overview

**Overall:** Layered Architecture with Multi-Tenant Support

**Key Characteristics:**
- Blazor Server + WebAssembly hybrid with server-side rendering
- Custom Repository Pattern with JSON storage in SQL Server
- Multi-tenant architecture using schema-per-tenant isolation
- Service-oriented business logic layer
- Claims-based authentication with OAuth providers

## Layers

**Presentation Layer (MotoRent.Client):**
- Purpose: UI components, pages, and user interaction
- Location: `src/MotoRent.Client/`
- Contains: Razor components, layouts, dialogs, controls
- Depends on: MotoRent.Domain, MotoRent.Services
- Used by: MotoRent.Server (hosts the Blazor components)

**Host Layer (MotoRent.Server):**
- Purpose: ASP.NET Core host, authentication, API controllers, SignalR hubs
- Location: `src/MotoRent.Server/`
- Contains: Program.cs (DI setup), controllers, middleware, request context
- Depends on: All other projects
- Used by: External clients (browsers)

**Services Layer (MotoRent.Services):**
- Purpose: Business logic, workflows, external integrations
- Location: `src/MotoRent.Services/`
- Contains: Service classes (RentalService, VehicleService, etc.), OCR, search
- Depends on: MotoRent.Domain
- Used by: MotoRent.Client, MotoRent.Server

**Domain Layer (MotoRent.Domain):**
- Purpose: Entities, data context, repository interfaces, core abstractions
- Location: `src/MotoRent.Domain/`
- Contains: Entity classes, RentalDataContext, Query/Repository, IRequestContext
- Depends on: None (core layer)
- Used by: All other projects

**Messaging Layer (MotoRent.Messaging):**
- Purpose: RabbitMQ message broker integration
- Location: `src/MotoRent.Messaging/`
- Contains: RabbitMqMessageBroker implementation
- Depends on: MotoRent.Domain (IMessageBroker interface)
- Used by: MotoRent.Server (optional, enabled via config)

**Background Processing:**
- **MotoRent.Scheduler:** Scheduled jobs (e.g., maintenance alerts)
  - Location: `src/MotoRent.Scheduler/`
- **MotoRent.Worker:** Background worker processes
  - Location: `src/MotoRent.Worker/`

## Data Flow

**Web Request Flow:**

1. HTTP request arrives at MotoRent.Server
2. TenantDomainMiddleware resolves tenant from domain/subdomain
3. Authentication middleware validates cookie/claims
4. IRequestContext (MotoRentRequestContext) provides tenant info via claims
5. Blazor component renders, injecting services
6. Services use RentalDataContext to query/persist data
7. Repository determines schema from IRequestContext.GetSchema()
8. SQL Server returns data from tenant-specific schema

**Entity Persistence Flow:**

1. Component loads entity via service (e.g., `RentalService.GetRentalByIdAsync()`)
2. Service calls `RentalDataContext.LoadOneAsync<T>(predicate)`
3. DataContext uses `ObjectBuilder.GetObject<IRepository<T>>()` to get repository
4. Repository builds SQL query with tenant schema: `[AccountNo].[Rental]`
5. Entity deserialized from JSON column via `JsonSerializerService`
6. Entity modified in component
7. Component calls service save method
8. Service opens `PersistenceSession`, attaches entity, calls `SubmitChanges()`
9. DataContext serializes entity to JSON, calls repository insert/update
10. Optional: Message published to RabbitMQ for async processing

**State Management:**
- No client-side state management library
- Data loaded in `OnInitializedAsync()` or `OnParametersSetAsync()`
- Component state in `@code` block private fields (m_ prefix)
- Cross-component communication via service injection or cascading parameters

## Key Abstractions

**Entity (Base Class):**
- Purpose: Base for all persistable domain objects
- Examples: `src/MotoRent.Domain/Entities/Rental.cs`, `src/MotoRent.Domain/Entities/Vehicle.cs`
- Pattern: JSON polymorphism with `[JsonDerivedType]` attributes, abstract `GetId()`/`SetId()`

**RentalDataContext:**
- Purpose: Unit of Work for queries and persistence sessions
- Examples: `src/MotoRent.Domain/DataContext/RentalDataContext.cs`
- Pattern: Provides `CreateQuery<T>()`, `LoadAsync<T>()`, `OpenSession()` for transactions

**IRepository<T>:**
- Purpose: Data access abstraction per entity type
- Examples: `src/MotoRent.Domain/DataContext/Repository.cs`
- Pattern: Generic repository with LINQ-to-SQL translation, tenant schema resolution

**IRequestContext:**
- Purpose: Current user/tenant context provider
- Examples: `src/MotoRent.Domain/Core/IRequestContext.cs`, `src/MotoRent.Server/Services/MotoRentRequestContext.cs`
- Pattern: Provides `GetAccountNo()` for schema, `GetUserName()` for audit, timezone for dates

**ObjectBuilder:**
- Purpose: Service locator for repositories (used by DataContext)
- Examples: `src/MotoRent.Domain/DataContext/ObjectBuilder.cs`
- Pattern: Static service locator configured at startup, resolves from HttpContext scope

## Entry Points

**Web Application:**
- Location: `src/MotoRent.Server/Program.cs`
- Triggers: HTTP requests, SignalR connections
- Responsibilities: DI configuration, middleware pipeline, authentication setup, service registration

**Blazor Pages:**
- Location: `src/MotoRent.Client/Pages/`
- Triggers: Route navigation
- Responsibilities: UI rendering, user interaction, service orchestration

**API Controllers:**
- Location: `src/MotoRent.Server/Controllers/`
- Triggers: HTTP API requests
- Responsibilities: Authentication callbacks, file uploads, culture switching

**SignalR Hubs:**
- Location: `src/MotoRent.Server/Hubs/CommentHub.cs`
- Triggers: WebSocket connections
- Responsibilities: Real-time comments/notifications

**Background Workers:**
- Location: `src/MotoRent.Scheduler/Runners/`
- Triggers: Scheduled timers
- Responsibilities: Maintenance alerts, periodic tasks

## Error Handling

**Strategy:** Try-catch with logging and user-friendly messages

**Patterns:**
- Services return result objects (e.g., `CheckInResult`, `SubmitOperation`) with Success/Message
- Repository catches `SqlException`, auto-creates missing tables/schemas
- Components catch exceptions in lifecycle methods, call `Logger.LogError()` and `ShowError()`
- Global middleware (`ExceptionLoggingMiddleware`) logs unhandled exceptions to `LogEntry` table

**Example Service Pattern:**
```csharp
public async Task<CheckInResult> CheckInAsync(CheckInRequest request, string username)
{
    try
    {
        // Business logic...
        return CheckInResult.CreateSuccess(rentalId);
    }
    catch (Exception ex)
    {
        return CheckInResult.CreateFailure($"Check-in error: {ex.Message}");
    }
}
```

**Example Component Pattern:**
```csharp
try
{
    m_loading = true;
    await LoadDataAsync();
}
catch (Exception ex)
{
    Logger.LogError(ex, "Error loading data");
    ShowError("Failed to load data");
}
finally
{
    m_loading = false;
}
```

## Cross-Cutting Concerns

**Logging:**
- Framework: `ILogger<T>` injection + custom `ILogger` (SqlLogger) for audit
- Stored in: `[Core].[LogEntry]` table
- Access: `LogEntryService`, `/super-admin/system-logs` page

**Validation:**
- Component-level validation in form handlers
- Service-level business rule validation (return failure results)
- No centralized validation framework (manual checks)

**Authentication:**
- OAuth providers: Google, Microsoft, LINE
- Cookie authentication with 14-day sliding expiration
- Claims: `AccountNo`, `ShopId`, roles via `IRequestContext`
- Super admin impersonation via `/account/impersonate`

**Authorization:**
- Policies: `RequireTenantStaff`, `RequireTenantManager`, `RequireTenantOrgAdmin`
- Component attributes: `@attribute [Authorize(Policy = "...")]`
- Role groups defined in `UserAccount` class

**Localization:**
- Framework: `IStringLocalizer<T>` via `LocalizedComponentBase<T>`
- Resource files: `src/MotoRent.Client/Resources/` (en, th, ms)
- Pattern: `@Localizer["Key"]` in Razor

**Multi-Tenancy:**
- Tenant identifier: `AccountNo` claim
- Data isolation: Schema-per-tenant in SQL Server
- Schema resolution: `IRequestContext.GetSchema()` -> `Repository.GetTableName()`
- Core shared data: `[Core]` schema (Organizations, Users, Settings)

---

*Architecture analysis: 2026-01-19*
