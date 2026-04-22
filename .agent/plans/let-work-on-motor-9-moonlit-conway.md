# MOTOR-9 — Assign Vehicles to a Third-Party Owner

## Context

The `VehicleOwner` domain (third-party owners who give us 70–80% of their vehicle's rental revenue) is fully modeled: `VehicleOwner` entity, `Vehicle.VehicleOwnerId` / `OwnerPaymentModel` / `OwnerDailyRate` / `OwnerRevenueSharePercent`, `VehicleOwnerService`, `OwnerPaymentService`, CRUD UI at `/settings/vehicle-owners`, and a finance page at `/finance/owner-payments`.

What is missing is the UI to **associate a vehicle with an owner**. The operator currently has no way to say "this bike in my fleet belongs to Adam, pay him 30% of rental" — the fields on `Vehicle` exist but no screen writes to them. MOTOR-9's screenshots show the request on the `/settings/vehicle-owners` flow: show plate numbers, add a "select vehicles" button, per-vehicle active toggle with a rule that you can't flip it while the vehicle is rented.

**Outcome:** org admins / shop managers can open an owner, see that owner's vehicles (with plate numbers), attach new vehicles from the operator's fleet (capturing payment model + rate inline), and detach them — with detach blocked while the vehicle is on an active rental.

## Chosen Approach

Promote the owner edit from a dialog into a **detail page** at `/settings/vehicle-owners/{VehicleOwnerId:int}` (follows `AgentDetails.razor` pattern). Keep the existing add-dialog for quick create; after create, redirect into the detail page. The detail page hosts three tabs: **Owner Info**, **Bank Details**, **Vehicles**.

The Vehicles tab is the focus of MOTOR-9. It lists attached vehicles (plate, brand/model, status badge, payment model + rate, active toggle, detach), and exposes an **Attach vehicles** action that opens a picker dialog listing unassigned vehicles in the operator's fleet; picking one prompts for payment model + rate inline before committing.

## Files to Modify / Create

### Service layer — `src/MotoRent.Services/VehicleOwnerService.cs`
Add three methods (reuse `GetOwnerVehiclesAsync` and the existing `RentalDataContext` session pattern):

- `Task<List<Vehicle>> GetUnassignedVehiclesAsync(string? searchTerm = null)` — query `Vehicle` where `VehicleOwnerId == null` and `Status != VehicleStatus.Retired`, ordered by `PlateNumber`, page size 500. In-memory search over plate/brand/model like `GetOwnersAsync` already does.
- `Task<SubmitOperation> AttachVehicleAsync(int vehicleId, int ownerId, OwnerPaymentModel model, decimal rateOrPercent, string username)` — load vehicle, guard `VehicleOwnerId is null`, load owner for `VehicleOwnerName`, set `VehicleOwnerId`, `OwnerPaymentModel`, `OwnerDailyRate` xor `OwnerRevenueSharePercent` (clear the unused one), `VehicleOwnerName`. Submit via `OpenSession(username)` with change description `"AttachVehicleToOwner"`.
- `Task<SubmitOperation> DetachVehicleAsync(int vehicleId, string username)` — load vehicle; **guard**: if `Status == VehicleStatus.Rented` return `SubmitOperation.CreateFailure("Cannot detach a vehicle that is currently rented.")`. Otherwise clear `VehicleOwnerId`, `OwnerPaymentModel`, `OwnerDailyRate`, `OwnerRevenueSharePercent`, `VehicleOwnerName` and submit `"DetachVehicleFromOwner"`.

Use existing constants from `src/MotoRent.Domain/Entities/VehicleStatus.cs` (`VehicleStatus.Rented`, `VehicleStatus.Retired`).

### New page — `src/MotoRent.Client/Pages/Owners/VehicleOwnerDetails.razor`
- `@page "/settings/vehicle-owners/{VehicleOwnerId:int}"`, `[Authorize(Policy = "RequireTenantManager")]`, `@inherits LocalizedComponentBase<VehicleOwnerDetails>`.
- Structure mirrors `src/MotoRent.Client/Pages/Agents/AgentDetails.razor`: `TablerHeader` with owner name + "Edit" (reuses `VehicleOwnerDialog`) + "Back to list".
- Three-tab layout (Tabler nav-tabs): Owner Info summary card, Bank Details summary card, **Vehicles** table.
- Vehicles tab renders a table: Plate, Vehicle (Brand/Model/Year), Status badge (reuse existing badge classes from `Vehicles.razor` / `VehicleDetails.razor`), Payment Model ("30%" or "฿200/day"), Actions (Detach button, disabled + tooltip when `Status == "Rented"`).
- Header action: `<button>Attach vehicles</button>` opens the picker dialog.
- Empty state matches the "No vehicle owners found" card styling in `VehicleOwnerList.razor`.

### New dialog — `src/MotoRent.Client/Pages/Owners/AttachVehicleDialog.razor`
Inherits `LocalizedDialogBase` style used by `VehicleOwnerDialog.razor`. Two-step inline flow in one modal:
1. **Pick vehicle** — search box (debounced like `VehicleOwnerList.razor:201-216`) + list of unassigned vehicles (plate, brand/model, current shop). Loaded via `VehicleOwnerService.GetUnassignedVehiclesAsync`.
2. **Payment setup** (revealed once a row is selected) — radio group: `DailyRate` / `RevenueShare` (bound to `OwnerPaymentModel` enum). One numeric input: THB/day for DailyRate, percentage 0–100 for RevenueShare (convert to 0.0–1.0 on save, matching `Vehicle.OwnerRevenueSharePercent` semantics documented at `Vehicle.cs:247-251`).

On save calls `AttachVehicleAsync`; parent reloads on success.

### Update — `src/MotoRent.Client/Pages/Owners/VehicleOwnerList.razor`
- On each owner card, change "Edit" button (line 101-104) into a link `href="/settings/vehicle-owners/{VehicleOwnerId}"` labelled "Open" (or keep "Edit" — it's the same entry point now).
- Keep the quick-edit dialog as an alternative trigger (clicking the name).
- `OpenAddDialog` (line 218): after successful create, navigate to `/settings/vehicle-owners/{newId}` so the user lands on the Vehicles tab. Requires `VehicleOwnerService.CreateOwnerAsync` to return the new id — it already does (entity ID is set after `SubmitChanges`).

### Nav — no change required
`NavMenu.razor:156-158` already links to `/settings/vehicle-owners`; the detail page is reached through the list.

### Resx files
Per project convention (`CLAUDE.md` localization rules), add new `.resx` keys to the existing `Resources/Pages/Owners/VehicleOwnerList.*.resx` and create:
- `Resources/Pages/Owners/VehicleOwnerDetails{,.en,.th,.ms}.resx`
- `Resources/Pages/Owners/AttachVehicleDialog{,.en,.th,.ms}.resx`

Short keys to add: `AttachVehicles`, `DetachVehicle`, `CannotDetachWhileRented`, `PickVehicle`, `PaymentModel`, `DailyRate`, `RevenueShare`, `RatePerDay`, `SharePercent`, `Plate`, `NoVehiclesAssigned`, tab labels `OwnerInfoTab`/`BankDetailsTab`/`VehiclesTab`.

## Reused Existing Code

- `VehicleOwnerService.GetOwnerVehiclesAsync` (`VehicleOwnerService.cs:60-66`) — already returns the vehicles for the detail page table.
- `VehicleOwnerService.GetOwnerByIdAsync` (`VehicleOwnerService.cs:55-58`) — header load.
- `RentalDataContext.OpenSession` / `SubmitChanges` pattern (same as `CreateOwnerAsync` line 78-83).
- `VehicleStatus.Rented` / `VehicleStatus.Retired` constants (`VehicleStatus.cs:17, 32`).
- `OwnerPaymentModel` enum (`OwnerPaymentModel.cs:6-20`).
- `VehicleOwnerDialog.razor` — reused unchanged for the Edit button on the detail page.
- `AgentDetails.razor` — pattern reference for detail-page layout & TablerHeader actions.
- Debounce pattern from `VehicleOwnerList.razor:201-216` — reused in the attach-dialog search.
- `LoadingSkeleton`, `MotoRentPageTitle`, `TablerHeader` — existing shared components.

## Verification

1. **Build**: `dotnet build` from repo root — no new compilation errors.
2. **Run**: `dotnet watch --project src/MotoRent.Server` and log in as an Org Admin.
3. **End-to-end check** (golden path):
   a. Go to `/settings/vehicle-owners`, open an existing owner → lands on `/settings/vehicle-owners/{id}` with three tabs.
   b. On **Vehicles** tab with no vehicles attached, click **Attach vehicles** → dialog opens, shows unassigned fleet vehicles with plate numbers (MOTOR-9 requirement #1).
   c. Pick a vehicle → payment-model section appears. Choose **RevenueShare 30%**, save → dialog closes, table shows the vehicle with plate + "30%" (MOTOR-9 requirement #2).
   d. Click **Detach** on that row while its `Status == "Available"` → vehicle leaves the list; verify in DB / `/vehicles/{id}` that `VehicleOwnerId` and payment fields are `null`.
   e. Via DB or the rental check-in flow, set a vehicle's `Status = "Rented"`, then try to **Detach** → button is disabled with a tooltip; clicking (if bypassed) surfaces the service-side error (MOTOR-9 requirement #3).
4. **Data integrity check**:
   - Query `Vehicle` rows after attach → `OwnerPaymentModel`, `OwnerDailyRate`/`OwnerRevenueSharePercent`, `VehicleOwnerName` are populated.
   - Confirm `OwnerPayment` generation for an ended rental still works (no change expected, but sanity check against an in-progress rental touching the attached vehicle).
5. **Huly**: Set MOTOR-9 status to In Progress on start; close on merge with a note referencing this plan file.

## Out of Scope

- Adding an owner picker to `VehicleForm.razor` (declined in favor of detail-page-driven flow).
- Renaming/removing `VehicleOwnerDialog.razor` — stays as quick-add and quick-edit.
- Adding a per-Vehicle `IsActive` property — the active/rented rule uses existing `Vehicle.Status`.
- Bulk attach of multiple vehicles in one dialog action (single-select for now; the dialog is reopened for each additional vehicle).

---

# Follow-up — Self-Healing RLS Bootstrap

## Context

MOTOR-9 browser testing surfaced a cross-tenant data leak: the attach dialog showed vehicles from *every* tenant (`PhuketBeach` + `KKCarRentals` together) because Postgres RLS was enabled on tenant tables but **not forced**, and the app connects as the `postgres` superuser, which bypasses non-forced RLS. The data-isolation mechanism that MotoRent *already has* (RLS policies on columns `tenant_id`, `DbConnectionInterceptor.SetTenantAsync` that runs `set_config('app.current_tenant', ...)` on every connection checkout) is not actually enforced end-to-end.

A static migration (`database/migrations/010-create-app-role-and-force-rls.sql`) exists to create the `motorent_app` role and `FORCE ROW LEVEL SECURITY`, but it's a one-off manual script — it hasn't been applied to any running DB, and it won't self-apply when new tenant tables are added later. The user wants the app itself to run a **self-healing RLS bootstrap** at every startup: idempotently create the role, grant privileges, and ensure `ENABLE + FORCE ROW LEVEL SECURITY` + a `tenant_isolation_<table>` policy on every table that has a `tenant_id` column. Core/shared tables (no `tenant_id` column) are naturally skipped by the discriminator and stay unguarded — exactly what `CorePgJsonRepository` expects.

This mirrors rx.pos's `PostgresBootstrap.EnsureRlsAsync()` pattern (`E:\project\work\rx.pos\source\dependencies\pg.pos.repository\PgRlsBootstrap.cs`), called once at Program.cs boot.

**Outcome:** after the app starts, any tenant-scoped table (by `tenant_id` column presence) has FORCE RLS with a canonical `tenant_isolation_<table>` policy; `motorent_app` role exists with the right grants; `postgres` can `SET ROLE motorent_app`; and all tenant queries going through `DbConnectionInterceptor` are RLS-enforced. Cross-tenant leak in the attach dialog closes as a side effect. New tenant tables added in the future are auto-protected on next boot — no human runbook needed.

## Chosen Approach

Add a new static class `PgRlsBootstrap` in `src/MotoRent.PostgreSqlRepository/` with one entry method `EnsureRlsAsync(string connectionString)`. Call it from `Program.cs` once, right after `builder.Build()`, awaited synchronously before `app.Run()`. Extend `DbConnectionInterceptor.SetTenantAsync` with a `SET ROLE motorent_app;` step before the existing `set_config` call.

Discrimination between tenant and core tables uses the rx.pos rule verbatim: `information_schema.columns WHERE column_name = 'tenant_id' AND table_schema = 'public'`. Core tables (`User`, `Organization`, `Setting`, `AccessToken`, `RegistrationInvite`, `LogEntry`, `SupportRequest`, `VehicleModel`, `SalesLead`, `Feedback`) have no `tenant_id` column and are automatically excluded. No hardcoded allow/deny list is needed.

## Files to Modify / Create

### New — `src/MotoRent.PostgreSqlRepository/PgRlsBootstrap.cs`
Static class with one public entry method; private helpers for role, policy, and per-table enforcement. Port directly from rx.pos's `PostgresBootstrap` but drop the `enforce_tenant_id()` trigger function (MotoRent doesn't use a DB-side tenant-stamping trigger — the app stamps `tenant_id` at insert via repository code, so we only need read/write isolation, not trigger-based assignment).

Method outline:
- `EnsureRlsAsync(string connectionString)` — opens a single connection as the configured user (postgres/superuser), calls the helpers in order:
  1. `EnsureAppRoleAsync(conn)` — `CREATE ROLE motorent_app NOLOGIN NOINHERIT` in a `DO $$ … $$` guard; idempotent grants on schema/tables/sequences + `ALTER DEFAULT PRIVILEGES` so future tables inherit grants; and `GRANT motorent_app TO CURRENT_USER` so the app's postgres connection can `SET ROLE motorent_app`.
  2. `GetTenantTablesAsync(conn)` — returns the list of table names with a `tenant_id` column.
  3. For each table: `EnsureTableRlsAsync(conn, table)` — checks `pg_class.relrowsecurity` / `relforcerowsecurity` / `pg_policies` and issues only the ALTERs/CREATE POLICY that are missing. Every statement is wrapped to be idempotent.
- Logs "`[RLS] Fixed RLS on N table(s)`" or "`[RLS] All N tenant table(s) have RLS enforced`" on success; throws on failure so startup fails loudly.

Per-table SQL (each guarded by a lookup first so re-boots are no-ops):
```
ALTER TABLE "Vehicle" ENABLE ROW LEVEL SECURITY;
CREATE POLICY "tenant_isolation_Vehicle" ON "Vehicle" USING ("tenant_id" = current_setting('app.current_tenant'));
ALTER TABLE "Vehicle" FORCE ROW LEVEL SECURITY;
GRANT SELECT, INSERT, UPDATE, DELETE ON "Vehicle" TO motorent_app;
```

### Modify — `src/MotoRent.PostgreSqlRepository/DbConnectionInterceptor.cs`
Add a `SET ROLE motorent_app;` statement at the top of `SetTenantAsync`, before the existing `set_config`. Without this, the app connection stays on `postgres` (superuser) and FORCE RLS is bypassed — the bootstrap work is wasted. Two-line change.

### Modify — `src/MotoRent.Server/Program.cs`
After `var app = builder.Build();` (currently line 357) and before any middleware wiring, add:
```csharp
await PgRlsBootstrap.EnsureRlsAsync(MotoConfig.ConnectionString);
```
Synchronous await — if RLS can't be enforced, we do NOT serve requests.

### Retire — `database/migrations/010-create-app-role-and-force-rls.sql`
Keep the file for historical reference but add a note at the top that it's superseded by the runtime bootstrap. Do not delete — may be useful for read-only diagnostic shells.

### No changes needed
- `CorePgJsonRepository.cs` / `CorePgPersistence.cs` — Core tables have no `tenant_id`, so the bootstrap skips them; no interceptor call needed (stays as-is).
- `PgJsonRepository.cs` / `PgPersistence.cs` — already call `SetTenantAsync` at all open-connection sites; the `SET ROLE` addition to the interceptor flows through for free.
- DB migration scripts — no new static migration; the app bootstraps on every start.

## Reused Existing Code

- `DbConnectionInterceptor` (`src/MotoRent.PostgreSqlRepository/DbConnectionInterceptor.cs`) — already implements the `app.current_tenant` session variable; we add `SET ROLE` to it.
- `MotoConfig.ConnectionString` (`src/MotoRent.Domain/Core/MotoConfig.cs`) — single source of the connection string at startup.
- rx.pos's `PostgresBootstrap.cs` as the reference port (SQL blocks come straight from there, minus the tenant-id trigger).

## Verification

1. **Start-time boot**: `dotnet run --project src/MotoRent.Server` — console should print `[RLS] All 55 tenant table(s) have RLS enforced` (or `Fixed RLS on N table(s)` on first run after the drop). Startup fails loudly if the connection user can't create the role.
2. **Idempotency**: stop, start, stop, start — second boot should print `All N tenant table(s) have RLS enforced` with `Fixed on 0`. No DDL churn.
3. **Role verification**:
   ```sql
   SELECT rolname, rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'motorent_app';
   -- expect: motorent_app | f | f
   ```
4. **FORCE check**:
   ```sql
   SELECT relname FROM pg_class
   WHERE relkind = 'r' AND NOT relforcerowsecurity
   AND relname IN (SELECT table_name FROM information_schema.columns WHERE column_name='tenant_id' AND table_schema='public');
   -- expect: 0 rows
   ```
5. **End-to-end: re-run MOTOR-9 attach flow** at `https://localhost:7103/settings/vehicle-owners/{id}` signed in as `admin@phuketbeach.test` / `PhuketBeach`. The attach dialog's unassigned-vehicle picker should now show **only 3 PhuketBeach plates** (`1กก 1234`, `2ขข 5678`, `กต 9012`) — zero `KKCarRentals` rows. Sign in again as a `KKCarRentals` admin and confirm the reverse (no PhuketBeach vehicles visible).
6. **New-table auto-protect**: create a new tenant table with a `tenant_id` column manually (`CREATE TABLE "TestEntity" (..., tenant_id varchar)`), restart the app, confirm the bootstrap detected and protected it.
7. **Core tables unchanged**: `SELECT * FROM "User"` from a `motorent_app` session still returns all users (Core tables lack `tenant_id`, so no policy applies).

## Out of Scope

- DB-side `tenant_id` enforcement trigger (rx.pos has `enforce_tenant_id()`). MotoRent stamps tenant_id in app code; adding a trigger is a belt-and-braces future nicety.
- Changing the app connection string to log in directly as `motorent_app` (alternative pattern). We keep `postgres` as the connection identity and `SET ROLE` down at query time, matching rx.pos and avoiding password rotation concerns.
- Retiring migration 010 entirely — keep it on disk as a manual fallback.
- Enforcing RLS on Core tables (by design: Core is cross-tenant).
