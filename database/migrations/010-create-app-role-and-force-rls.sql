-- =============================================================================
-- Migration 010: Create non-superuser app role + enforce FORCE RLS
-- =============================================================================
--
-- SUPERSEDED by the runtime self-healing bootstrap in
-- src/MotoRent.PostgreSqlRepository/PgRlsBootstrap.cs (invoked from
-- src/MotoRent.Server/Program.cs on every start). The app now creates the
-- motorent_app role, grants privileges, and applies ENABLE + FORCE ROW LEVEL
-- SECURITY + tenant_isolation_<table> policies on every table with a
-- `tenant_id` column — idempotently, so new tenant tables are auto-protected.
--
-- This file is kept for diagnostic / read-only shells where the app cannot be
-- run (e.g., taking a forensic DB snapshot and re-applying RLS manually).
-- You typically should NOT need to run it by hand.
-- =============================================================================
--
-- WHY: The application was connecting as `postgres` (a superuser), which
--   silently bypasses ALL Row Level Security policies — even when FORCE ROW
--   LEVEL SECURITY is enabled. This caused cross-tenant data leaks where
--   Tenant A could see Tenant B's vehicles, rentals, renters, etc.
--
-- WHAT THIS DOES:
--   1. Force RLS on every tenant-scoped table (idempotent — re-applies to
--      tables that migration 009 may have missed, e.g. tables that did not
--      exist when 009 ran).
--   2. Create role `motorent_app` with NOSUPERUSER + NOBYPASSRLS so RLS
--      policies actually apply to it.
--   3. Grant the role enough privileges to run the application
--      (SELECT/INSERT/UPDATE/DELETE + CREATE on schema for future tenant
--      provisioning).
--
-- AFTER APPLYING:
--   * Change MOTO_ConnectionString on the app (web.config / docker .env) to
--     use Username=motorent_app and the password you set below.
--   * Restart the app (recycle IIS App Pool, or `docker compose up -d`).
--   * Verify with:  SELECT usename, COUNT(*) FROM pg_stat_activity
--                   WHERE datname=current_database() GROUP BY usename;
--     Only `motorent_app` connections should come from the app (postgres
--     connections are only for admin / psql sessions).
--
-- SECURITY: Replace '<CHANGE-ME-STRONG-PASSWORD>' below with a strong, unique
-- password before running. The password MUST match what you put in the app's
-- MOTO_ConnectionString.
--
-- This migration is safe to re-run (idempotent).
-- =============================================================================

BEGIN;

-- -----------------------------------------------------------------------------
-- 1. Force RLS on every tenant-scoped table (by column `tenant_id`)
-- -----------------------------------------------------------------------------
DO $$
DECLARE
    rec record;
    forced_count int := 0;
BEGIN
    FOR rec IN
        SELECT DISTINCT c.relname
        FROM pg_class c
        JOIN information_schema.columns col ON col.table_name = c.relname
        WHERE c.relkind = 'r'
          AND col.column_name = 'tenant_id'
          AND col.table_schema = 'public'
          AND NOT c.relforcerowsecurity
        ORDER BY c.relname
    LOOP
        EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', rec.relname);
        EXECUTE format('ALTER TABLE %I FORCE ROW LEVEL SECURITY', rec.relname);
        forced_count := forced_count + 1;
    END LOOP;
    RAISE NOTICE 'Applied FORCE ROW LEVEL SECURITY to % table(s)', forced_count;
END $$;

-- -----------------------------------------------------------------------------
-- 2. Create app role (idempotent: drop & recreate is NOT safe if role owns
--    objects; we use CREATE IF NOT EXISTS semantics via DO block).
-- -----------------------------------------------------------------------------
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'motorent_app') THEN
        CREATE ROLE motorent_app WITH
            LOGIN
            PASSWORD '<CHANGE-ME-STRONG-PASSWORD>'
            NOSUPERUSER
            NOBYPASSRLS
            NOCREATEROLE
            NOCREATEDB
            NOREPLICATION;
        RAISE NOTICE 'Role motorent_app created. REMEMBER to change the default password.';
    ELSE
        RAISE NOTICE 'Role motorent_app already exists — skipping creation.';
        -- If you want to rotate the password, run separately:
        --   ALTER ROLE motorent_app PASSWORD '<NEW-PASSWORD>';
    END IF;
END $$;

-- -----------------------------------------------------------------------------
-- 3. Grant privileges
-- -----------------------------------------------------------------------------
DO $$
BEGIN
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO motorent_app', current_database());
END $$;

GRANT USAGE, CREATE ON SCHEMA public TO motorent_app;

GRANT SELECT, INSERT, UPDATE, DELETE, TRIGGER, REFERENCES
    ON ALL TABLES IN SCHEMA public TO motorent_app;

GRANT USAGE, SELECT, UPDATE
    ON ALL SEQUENCES IN SCHEMA public TO motorent_app;

GRANT EXECUTE
    ON ALL FUNCTIONS IN SCHEMA public TO motorent_app;

-- Privileges for future tables/sequences/functions created by any role:
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO motorent_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO motorent_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT EXECUTE ON FUNCTIONS TO motorent_app;

COMMIT;

-- -----------------------------------------------------------------------------
-- 4. Self-verification (read-only, no changes)
-- -----------------------------------------------------------------------------
SELECT rolname, rolsuper, rolbypassrls, rolcanlogin
FROM pg_roles WHERE rolname = 'motorent_app';

SELECT
  CASE WHEN c.relforcerowsecurity THEN 'FORCE' ELSE 'NOT FORCED' END AS rls_status,
  COUNT(*) AS tables
FROM pg_class c
JOIN information_schema.columns col ON col.table_name = c.relname
WHERE c.relkind = 'r' AND col.column_name = 'tenant_id' AND col.table_schema = 'public'
GROUP BY c.relforcerowsecurity;
