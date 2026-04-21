# Production Deployment — Tenant Isolation Fix

**Target**: `app.safengoth.com` (PostgreSQL `appmotorent` on `172.30.30.147`)
**Related migration**: `database/migrations/010-create-app-role-and-force-rls.sql`

## Why

Production app currently connects to PostgreSQL as role `postgres`, which is a
superuser. PostgreSQL superusers **always bypass** Row Level Security policies,
regardless of `FORCE ROW LEVEL SECURITY`. This means every tenant can see every
other tenant's data (vehicles, rentals, renters, payments, etc.) — the policies
we wrote per-table are a no-op for this role.

Dev environment was fixed already (see `database/migrations/010-…sql` + code
changes in `MotoRentRequestContext.GetConnectionString()`). This runbook applies
the same fix to production.

## Pre-flight checks

**Diagnose current state** (run on the prod PostgreSQL via psql or pgAdmin):

```sql
-- 1. Confirm the app-role bug exists
SELECT rolname, rolsuper, rolbypassrls FROM pg_roles WHERE rolname = 'postgres';
-- Expected: rolsuper=t, rolbypassrls=t  → confirms RLS is bypassed

-- 2. How many tenant tables lack FORCE RLS?
SELECT
  CASE WHEN c.relforcerowsecurity THEN 'FORCE' ELSE 'NOT FORCED' END AS status,
  COUNT(*) AS tables
FROM pg_class c
JOIN information_schema.columns col ON col.table_name = c.relname
WHERE c.relkind='r' AND col.column_name='tenant_id' AND col.table_schema='public'
GROUP BY c.relforcerowsecurity;

-- 3. How many tenants exist, and how is data distributed?
SELECT "AccountNo", "Name" FROM "Organization" ORDER BY "OrganizationId";
SELECT tenant_id, COUNT(*) FROM "Vehicle" GROUP BY tenant_id;
SELECT tenant_id, COUNT(*) FROM "Rental" GROUP BY tenant_id;
```

If only one tenant exists, no data has leaked (nowhere to leak to). If multiple
tenants exist, investigate whether any tenant has "extra" rows that came from
another tenant via UI actions before this fix.

## Deployment steps

### Step 1 — Build & push the new app image

The image `myhiro.azurecr.io/motorent/app-safengoth` must contain these code
fixes from commit history after this work:

- `src/MotoRent.Server/Services/MotoRentRequestContext.cs` — `GetConnectionString()` now reads `MotoConfig.ConnectionString`
- `src/MotoRent.Services/Core/PgSubscriptionService.cs` — `DeleteSchemaAsync` sets `app.current_tenant` before DELETE
- `src/MotoRent.Server/appsettings.json` — fallback connection string updated
- `src/MotoRent.Domain/Core/MotoConfig.cs` — hardcoded fallback updated

Build pipeline:
```bash
# on dev/CI machine
docker build -t myhiro.azurecr.io/motorent/app-safengoth:<tag> .
docker push myhiro.azurecr.io/motorent/app-safengoth:<tag>
```

### Step 2 — Choose a strong password for the new DB role

```bash
# example — generate a 32-char random password
openssl rand -base64 32
```

Save it somewhere secure (password manager). You will put it in:
1. The SQL migration (edit placeholder `<CHANGE-ME-STRONG-PASSWORD>`)
2. The `.env` file for docker compose

### Step 3 — Apply the SQL migration on prod DB

SSH to `172.30.30.147`, copy the migration file, edit the password placeholder,
and run:

```bash
# copy migration to server
scp database/migrations/010-create-app-role-and-force-rls.sql erymuzuan@172.30.30.147:~/

# on the server, edit the password placeholder
ssh erymuzuan@172.30.30.147
cd ~
sed -i 's/<CHANGE-ME-STRONG-PASSWORD>/YourActualStrongPassword/' 010-create-app-role-and-force-rls.sql

# run it (adjust container name if different)
docker compose -f /home/erymuzuan/apps/motorent/app.safengoth.com/docker-compose.yaml exec -T db \
  psql -U postgres -d appmotorent < 010-create-app-role-and-force-rls.sql

# clean up the file with the password in it
shred -u 010-create-app-role-and-force-rls.sql
```

Verify the migration output shows:
- `Applied FORCE ROW LEVEL SECURITY to N table(s)`
- `Role motorent_app created`
- Final query shows `rolsuper=f, rolbypassrls=f, rolcanlogin=t`
- Final query shows only `FORCE` status (no `NOT FORCED`)

### Step 4 — Update `.env` file on prod

File: `/home/erymuzuan/apps/motorent/app.safengoth.com/app.safengoth.com.env`

Change this line:
```diff
-MOTO_ConnectionString=Host=172.30.30.147;Port=5432;Database=appmotorent;Username=postgres;Password=postgres;Include Error Detail=true;
+MOTO_ConnectionString=Host=172.30.30.147;Port=5432;Database=appmotorent;Username=motorent_app;Password=YourActualStrongPassword;Include Error Detail=true;
```

### Step 5 — Recycle the app

```bash
cd /home/erymuzuan/apps/motorent/app.safengoth.com
docker compose pull         # pull the new image if tag changed
docker compose up -d        # recreates web container with new env
docker compose logs -f web  # watch for startup errors
```

### Step 6 — Verify

1. **Connection users** (from server):
   ```sql
   SELECT usename, COUNT(*) FROM pg_stat_activity
   WHERE datname='appmotorent' GROUP BY usename;
   ```
   Expected: `motorent_app` with several idle connections, no `postgres`
   connections from the app (only the psql session itself).

2. **Functional isolation**:
   - Log in to https://app.safengoth.com as a user of Tenant A
   - Browse Vehicles / Rentals / Renters — should show ONLY Tenant A's data
   - Log out, log in as a user of Tenant B — should show ONLY Tenant B's data
   - If only one tenant exists in prod, create a second test tenant via the
     onboarding page and verify cross-isolation

3. **RLS self-check** (as postgres superuser via psql):
   ```sql
   -- Simulate the app's behavior (motorent_app with tenant context)
   SET SESSION AUTHORIZATION motorent_app;
   SET app.current_tenant = 'SomeExistingAccountNo';
   SELECT COUNT(*), tenant_id FROM "Vehicle" GROUP BY tenant_id;
   -- Should return ONE row with tenant_id = 'SomeExistingAccountNo'
   RESET SESSION AUTHORIZATION;
   ```

## Rollback

If something goes wrong:

```bash
# On the server — change .env back to postgres
sed -i 's/Username=motorent_app;Password=[^;]*/Username=postgres;Password=postgres/' \
  /home/erymuzuan/apps/motorent/app.safengoth.com/app.safengoth.com.env

docker compose up -d
```

The role and FORCE RLS can stay — they do no harm when the app reverts to
connecting as `postgres` (which still bypasses RLS). You can drop the role
later if desired:

```sql
REASSIGN OWNED BY motorent_app TO postgres;
DROP OWNED BY motorent_app;
DROP ROLE motorent_app;
```

## Follow-up housekeeping (separate PRs)

1. `ASPNETCORE_ENVIRONMENT=Development` is set in legacy `web.config` — should
   be `Production` for real prod deploys (the docker compose setup here does
   not set it, so that's fine).
2. `Include Error Detail=true` in the connection string leaks internal schema
   info in error messages — turn off in prod.
3. Secrets (Google/AWS/RabbitMQ) are in plaintext in the `.env` / `web.config`
   — move to a secret manager (Azure Key Vault, AWS Secrets Manager, or Docker
   secrets) in a follow-up.
4. The `CREATE TABLE` scripts under `database/tables/*.sql` should be patched
   to include `ALTER TABLE … FORCE ROW LEVEL SECURITY` so future tables are
   safe by default. Migration 010 covers what exists today, but this is a
   maintenance gap.
