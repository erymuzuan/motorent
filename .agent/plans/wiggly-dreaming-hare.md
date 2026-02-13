# MotoRent: MSSQL to PostgreSQL Migration Plan

## Context

MotoRent currently runs on SQL Server with schema-per-tenant multi-tenancy and a custom JSON repository pattern. Due to cost and licensing concerns, we're migrating to PostgreSQL. The `sma.sentinel-marketplace` project provides a proven reference implementation using PostgreSQL with Row Level Security (RLS) for multi-tenancy.

**Key decisions:**
- **Multi-tenancy**: Switch from schema-per-tenant to RLS (single schema + `tenant_id` column)
- **Strategy**: Big bang replacement of `MotoRent.SqlServerRepository` with new `MotoRent.PostgreSqlRepository`
- **SQL scripts**: Replace MSSQL scripts entirely with PostgreSQL DDL

**Scope**: ~55 tenant table scripts, ~4 Core table scripts, 1 new C# project (~10 files), updates to DI wiring and subscription service.

---

## Phase 1: Create `MotoRent.PostgreSqlRepository` Project

Create a new project mirroring the sentinel-marketplace's `sentinel.repository.pg` pattern.

### 1.1 Project setup
- **New project**: `src/MotoRent.PostgreSqlRepository/MotoRent.PostgreSqlRepository.csproj`
- **Dependencies**: `Npgsql` (9.0.3), `Polly` (8.5.2)
- **Project reference**: `MotoRent.Domain`
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/sentinel.repository.pg.csproj`

### 1.2 DbConnectionInterceptor (tenant context)
- **New file**: `src/MotoRent.PostgreSqlRepository/DbConnectionInterceptor.cs`
- Sets `app.current_tenant` via `set_config()` on each connection
- Injected into all tenant-scoped repositories
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/DbConnectionInterceptor.cs`

### 1.3 PgQueryProvider (LINQ to PostgreSQL)
- **New file**: `src/MotoRent.PostgreSqlRepository/PgQueryProvider.cs`
- Extends abstract `QueryProvider` from `MotoRent.Domain`
- Uses expression tree pipeline: `Evaluator` -> `QueryBinder` -> `OrderByRewriter` -> `UnusedColumnRemover` -> `RedundantSubqueryRemover` -> `PgQueryFormatter`
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgQueryProvider.cs`

### 1.4 PgQueryFormatter (SQL generation)
- **New file**: `src/MotoRent.PostgreSqlRepository/PgQueryFormatter.cs`
- Translates expression trees to PostgreSQL SQL
- Key differences from `TsqlQueryFormatter`:
  - Double-quoted identifiers `"TableName"` instead of brackets `[TableName]`
  - No schema prefix (RLS handles isolation)
  - PostgreSQL string concat `||` instead of `+`
  - Boolean `true`/`false` instead of `1`/`0`
  - Enum values stored/compared as strings
  - `LIKE '%' || @param || '%'` pattern for Contains
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgQueryFormatter.cs`
- **Existing**: `src/MotoRent.SqlServerRepository/TsqlQueryFormatter.cs` (to port from)

### 1.5 PgPagingTranslator
- **New file**: `src/MotoRent.PostgreSqlRepository/PgPagingTranslator.cs`
- Implements `IPagingTranslator` with `LIMIT/OFFSET` (replaces `OFFSET/FETCH`)
- Much simpler than `Sql2012PagingTranslator` - no subquery wrapping needed
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgPagingTranslator.cs`

### 1.6 PgMetadata (table introspection)
- **New file**: `src/MotoRent.PostgreSqlRepository/PgMetadata.cs`
- Interface `IPgMetadata` with `GetTable(string name)` method
- Queries `information_schema.columns` + constraint tables
- Detects generated columns, identity columns, primary keys
- Caches with `ConcurrentDictionary`
- Excludes `tenant_id` from writable columns (handled by INSERT separately)
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgMetadata.cs`

### 1.7 PgJsonRepository<T> (tenant-scoped CRUD)
- **New file**: `src/MotoRent.PostgreSqlRepository/PgJsonRepository.cs`
- Implements `IRepository<T>` for all RLS-protected tenant entities
- Always calls `Interceptor.SetTenantAsync(conn)` before queries
- Polly retry with exponential backoff for transient PG errors (deadlock `40P01`, connection failure `08006`, timeout)
- Methods: `LoadOneAsync`, `LoadAsync` (paginated), `LoadAsync` (OData), `DeleteAsync`, `GetListAsync`, `GetDistinctAsync`, `GetScalarAsync`, aggregate methods
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgJsonRepository.cs`
- **Existing**: `src/MotoRent.SqlServerRepository/SqlJsonRepository.cs` (to port from, has additional aggregate/groupby/datareader methods sentinel lacks)

### 1.8 CorePgJsonRepository<T> (non-tenant CRUD)
- **New file**: `src/MotoRent.PostgreSqlRepository/CorePgJsonRepository.cs`
- Same as `PgJsonRepository` but does NOT call `SetTenantAsync`
- Used for: `Organization`, `User`, `Setting`, `AccessToken`, `RegistrationInvite`, `LogEntry`, `Feedback`, `SalesLead`, `SupportRequest`, `VehicleModel`
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/CorePgJsonRepository.cs`

### 1.9 PgPersistence (batch writes)
- **New file**: `src/MotoRent.PostgreSqlRepository/PgPersistence.cs`
- Implements `IPersistence`
- Explicit `BEGIN`/`COMMIT`/`ROLLBACK` transactions
- `INSERT ... RETURNING "EntityId"` instead of SQL Server `OUTPUT`
- `tenant_id` inserted via `current_setting('app.current_tenant')`
- JSONB parameter type for `Json` column
- Max 100 items per transaction
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgPersistence.cs`

### 1.10 CorePgPersistence (non-tenant writes)
- **New file**: `src/MotoRent.PostgreSqlRepository/CorePgPersistence.cs`
- Same as `PgPersistence` but no tenant interception, no `tenant_id` in INSERT
- Used for Core schema entities

### 1.11 PgOdataSqlTranslator (OData filter support)
- **New file**: `src/MotoRent.PostgreSqlRepository/PgOdataSqlTranslator.cs`
- Translates OData `$filter` to PostgreSQL WHERE clauses
- **Reference**: `sma.sentinel-marketplace/sources/sentinel.repository.pg/PgOdataSqlTranslator.cs`

### 1.12 ServiceCollectionExtensions (DI registration)
- **New file**: `src/MotoRent.PostgreSqlRepository/ServiceCollectionExtensions.cs`
- `AddMotoRentPostgreSqlRepository()` method
- Registers: `IPagingTranslator` -> `PgPagingTranslator`, `QueryProvider` -> `PgQueryProvider`, `IPersistence` -> `PgPersistence`, `IPgMetadata` -> `PgMetadata`, `DbConnectionInterceptor`
- Registers all ~55 tenant entities as `IRepository<T>` -> `PgJsonRepository<T>`
- Registers all ~10 Core entities as `IRepository<T>` -> `CorePgJsonRepository<T>`
- Port registrations from existing `src/MotoRent.SqlServerRepository/ServiceCollectionExtensions.cs`

### 1.13 QueryableExtensions update
- **Modify**: `src/MotoRent.Domain/DataContext/QueryableExtensions.cs`
- Replace SQL Server bracket notation `[column]` with PostgreSQL double-quote `"column"` notation
- Or make it database-agnostic by parameterizing the quote style

---

## Phase 2: PostgreSQL Database Schema

Replace all MSSQL table scripts in `database/tables/` with PostgreSQL equivalents.

### 2.1 Type mapping reference

| MSSQL | PostgreSQL |
|-------|-----------|
| `INT IDENTITY(1,1)` | `INT GENERATED ALWAYS AS IDENTITY` |
| `NVARCHAR(n)` | `VARCHAR(n)` |
| `NVARCHAR(MAX)` | `TEXT` or `JSONB` (for Json column) |
| `VARCHAR(n)` | `VARCHAR(n)` (unchanged) |
| `BIT` | `BOOLEAN` |
| `DECIMAL(p,s)` / `MONEY` | `NUMERIC(p,s)` |
| `DATETIMEOFFSET` | `TIMESTAMPTZ` |
| `DATE` | `DATE` (unchanged) |
| `SYSDATETIMEOFFSET()` | `NOW()` or `CURRENT_TIMESTAMP` |
| `JSON_VALUE([Json],'$.Prop')` | `("Json"->>'Prop')` |
| `AS CAST(JSON_VALUE(...) AS TYPE)` | `GENERATED ALWAYS AS ((...) ::TYPE) STORED` |

### 2.2 Convert tenant table scripts (~55 files)
For each `database/tables/MotoRent.*.sql`:
1. Convert `CREATE TABLE [<schema>].[Entity]` to `CREATE TABLE "Entity"`
2. Convert `IDENTITY(1,1)` to `GENERATED ALWAYS AS IDENTITY`
3. Convert computed columns to generated stored columns using JSONB operators
4. Change `[Json] NVARCHAR(MAX)` to `"Json" JSONB NOT NULL`
5. Add `"tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant')`
6. Convert audit columns: `DATETIMEOFFSET` -> `TIMESTAMPTZ`, `SYSDATETIMEOFFSET()` -> `NOW()`
7. Add RLS policy: `ALTER TABLE "Entity" ENABLE ROW LEVEL SECURITY;`
8. Add RLS policy: `CREATE POLICY tenant_isolation_entity ON "Entity" USING ("tenant_id" = current_setting('app.current_tenant'));`
9. Convert indexes: remove bracket notation, use double-quotes
10. Handle `BIT` -> `BOOLEAN` conversions (e.g., Shop.IsActive)

**Example conversion** (Shop):
```sql
-- FROM (MSSQL):
CREATE TABLE [<schema>].[Shop] (
    [ShopId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- TO (PostgreSQL):
CREATE TABLE "Shop" (
    "ShopId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "IsActive" BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json" JSONB NOT NULL,
    "tenant_id" VARCHAR(50) NOT NULL DEFAULT current_setting('app.current_tenant'),
    "CreatedBy" VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedTimestamp" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
ALTER TABLE "Shop" ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_shop ON "Shop"
    USING ("tenant_id" = current_setting('app.current_tenant'));
```

### 2.3 Convert Core table scripts (~4 files)
Core tables (Feedback, SalesLead, SupportRequest, VehicleModel) do NOT get `tenant_id` or RLS.
Same type conversions but no multi-tenancy columns.

### 2.4 Handle special cases
- **Date columns**: MSSQL has explicit `DATE` columns alongside computed columns for indexing date values from JSON. In PostgreSQL, use `GENERATED ALWAYS AS` with `immutable_text_to_date()` helper function if needed
- **Seed scripts**: Convert `MotoRent.DenominationGroup.seed.sql` and any other seed data
- **Indexes**: Convert all `CREATE INDEX` statements to PostgreSQL syntax
- **UNIQUE indexes**: Tenant-aware unique indexes need `tenant_id` added: `CREATE UNIQUE INDEX ... ON "Entity"("tenant_id", "field")`

### 2.5 PostgreSQL helper functions
Create `database/functions.sql` with:
- `immutable_text_to_date(text)` - for DATE generated columns from JSON text

### 2.6 Remove old numbered migration scripts
Delete `database/001-create-schema.sql` through `database/010-seed-vehicle-lookups.sql` and any other MSSQL-specific scripts.

---

## Phase 3: Multi-Tenancy Refactoring

### 3.1 Update SqlSubscriptionService
- **Modify**: `src/MotoRent.Services/Core/SqlSubscriptionService.cs`
- `CreateSchemaAsync()`: No longer creates PostgreSQL schemas. Instead:
  - Verify RLS policies exist
  - Seed initial data with tenant_id set
  - Replace `SqlCommand`/`SqlConnection` with `NpgsqlCommand`/`NpgsqlConnection`
- `DeleteSchemaAsync()`: Delete by `tenant_id` instead of dropping schema
  - `DELETE FROM "Entity" WHERE "tenant_id" = @accountNo` for each table
- Remove all `[schema]` placeholder logic

### 3.2 Connection string configuration
- **Modify**: `src/MotoRent.Domain/Settings/MotoConfig.cs`
- Change env var from `MOTO_SqlConnectionString` to support PostgreSQL format
- PostgreSQL format: `Host=localhost;Database=MotoRent;Username=postgres;Password=...`

### 3.3 Update IRequestContext implementations
- **Modify**: `src/MotoRent.Server/Services/MotoRentRequestContext.cs`
- Remove schema-based logic, return accountNo for RLS tenant setting
- Ensure `GetAccountNoAsync()` / `GetAccountNo()` works for DbConnectionInterceptor

### 3.4 Core Repository updates
- **Modify**: `src/MotoRent.Core.Repository/`
- This project handles User, Organization, Setting, AccessToken, etc.
- Either: port to use `CorePgJsonRepository` pattern OR merge into `MotoRent.PostgreSqlRepository`
- Replace `Microsoft.Data.SqlClient` with `Npgsql`
- Replace T-SQL formatters with PG formatters

---

## Phase 4: Integration & Wiring

### 4.1 Update Program.cs
- **Modify**: `src/MotoRent.Server/Program.cs`
- Replace `builder.Services.AddMotoRentSqlServerRepository()` with `builder.Services.AddMotoRentPostgreSqlRepository()`
- Remove `using MotoRent.SqlServerRepository`
- Add `using MotoRent.PostgreSqlRepository`

### 4.2 Update project references
- **Modify**: `src/MotoRent.Server/MotoRent.Server.csproj`
- Remove: `<ProjectReference Include="..\MotoRent.SqlServerRepository\...">`
- Add: `<ProjectReference Include="..\MotoRent.PostgreSqlRepository\...">`
- Same for Core.Repository if merged

### 4.3 Update solution file
- Add `MotoRent.PostgreSqlRepository` project to solution
- Optionally remove `MotoRent.SqlServerRepository` from solution (or keep for reference)

### 4.4 Update CLAUDE.md / project documentation
- Update connection string references
- Update database conventions section
- Update build commands if needed

---

## Phase 5: Performance Optimization

Since you're not familiar with PostgreSQL, these are important performance considerations:

### 5.1 JSONB GIN Indexes
- Add GIN indexes on `"Json"` column for tables with frequent JSON property queries
- `CREATE INDEX idx_entity_json ON "Entity" USING GIN ("Json")`
- Especially useful for full-text search within JSON

### 5.2 Generated column indexes
- PostgreSQL STORED generated columns are indexable (just like MSSQL computed columns)
- Ensure all existing composite indexes are recreated on generated columns
- Consider partial indexes: `CREATE INDEX ... WHERE "Status" = 'Active'` for hot queries

### 5.3 Connection pooling
- Configure `Npgsql` connection pooling: `Pooling=true;Minimum Pool Size=5;Maximum Pool Size=100`
- Consider `NpgsqlDataSource` for connection management (newer Npgsql pattern)

### 5.4 RLS performance
- RLS adds a filter to every query; ensure `tenant_id` is indexed
- Add `tenant_id` to composite indexes where it appears in WHERE clauses
- Consider: `CREATE INDEX idx_entity_tenant ON "Entity"("tenant_id")` on all tables

### 5.5 Query plan analysis
- Use `EXPLAIN ANALYZE` to verify query plans after migration
- Watch for sequential scans on large tables
- Ensure generated columns used in WHERE/ORDER BY have appropriate indexes

---

## Phase 6: Testing & Verification

### 6.1 Build verification
```bash
dotnet build
```

### 6.2 Database setup
- Start PostgreSQL (Docker or local)
- Run all DDL scripts from `database/tables/`
- Run seed scripts
- Create test tenant via the app's provisioning flow

### 6.3 Functional testing
- Create an organization (tenant provisioning)
- CRUD operations on key entities: Shop, Vehicle, Renter, Rental
- Verify RLS isolation: two tenants cannot see each other's data
- Test pagination, filtering, sorting
- Test aggregate queries (Count, Sum, etc.)
- Test OData filter queries

### 6.4 Browser verification
- Run `dotnet watch --project src/MotoRent.Server`
- Navigate through all major pages
- Verify data loads correctly
- Test create/edit/delete flows

---

## Files to Create (New)
| File | Purpose |
|------|---------|
| `src/MotoRent.PostgreSqlRepository/MotoRent.PostgreSqlRepository.csproj` | Project file |
| `src/MotoRent.PostgreSqlRepository/DbConnectionInterceptor.cs` | RLS tenant context |
| `src/MotoRent.PostgreSqlRepository/PgQueryProvider.cs` | LINQ query provider |
| `src/MotoRent.PostgreSqlRepository/PgQueryFormatter.cs` | Expression to SQL |
| `src/MotoRent.PostgreSqlRepository/PgPagingTranslator.cs` | LIMIT/OFFSET pagination |
| `src/MotoRent.PostgreSqlRepository/PgMetadata.cs` | Table introspection |
| `src/MotoRent.PostgreSqlRepository/PgJsonRepository.cs` | Tenant-scoped repository |
| `src/MotoRent.PostgreSqlRepository/CorePgJsonRepository.cs` | Core (no-RLS) repository |
| `src/MotoRent.PostgreSqlRepository/PgPersistence.cs` | Tenant batch persistence |
| `src/MotoRent.PostgreSqlRepository/CorePgPersistence.cs` | Core batch persistence |
| `src/MotoRent.PostgreSqlRepository/PgOdataSqlTranslator.cs` | OData filter translation |
| `src/MotoRent.PostgreSqlRepository/ServiceCollectionExtensions.cs` | DI registration |
| `database/functions.sql` | Helper functions |
| `database/tables/*.sql` | ~59 converted PostgreSQL table scripts |

## Files to Modify
| File | Change |
|------|--------|
| `src/MotoRent.Server/Program.cs` | Switch to PG repository registration |
| `src/MotoRent.Server/MotoRent.Server.csproj` | Update project references |
| `src/MotoRent.Domain/DataContext/QueryableExtensions.cs` | PG-compatible SQL helpers |
| `src/MotoRent.Services/Core/SqlSubscriptionService.cs` | RLS-based provisioning |
| `src/MotoRent.Domain/Settings/MotoConfig.cs` | PG connection string |
| `MotoRent.sln` or `.slnx` | Add new project |

## Key Reference Files (sentinel-marketplace)
| File | Maps to |
|------|---------|
| `sentinel.repository.pg/PgJsonRepository.cs` | `PgJsonRepository.cs` |
| `sentinel.repository.pg/CorePgJsonRepository.cs` | `CorePgJsonRepository.cs` |
| `sentinel.repository.pg/PgPersistence.cs` | `PgPersistence.cs` |
| `sentinel.repository.pg/PgQueryFormatter.cs` | `PgQueryFormatter.cs` |
| `sentinel.repository.pg/PgQueryProvider.cs` | `PgQueryProvider.cs` |
| `sentinel.repository.pg/PgMetadata.cs` | `PgMetadata.cs` |
| `sentinel.repository.pg/PgPagingTranslator.cs` | `PgPagingTranslator.cs` |
| `sentinel.repository.pg/DbConnectionInterceptor.cs` | `DbConnectionInterceptor.cs` |

## Team Execution Plan

This work will be done by a team of 3 specialists:

1. **C# Developer** - Phases 1, 3, 4 (PostgreSqlRepository project, service refactoring, DI wiring)
2. **PostgreSQL Developer** - Phase 2 (all DDL script conversions, helper functions)
3. **PostgreSQL DBA** - Phase 5, 6 (performance optimization, indexing strategy, RLS verification, query plan analysis)
