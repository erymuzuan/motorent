# MotoRent — PostgreSQL Restore Guide

> Audience: server team performing the actual restore on the production server.
> Reader assumed to know **Microsoft SQL Server** and need a PostgreSQL translation.

If you've never restored a PostgreSQL backup before, jump to [§1 — Quick restore](#1-quick-restore-the-hand-off-file). Everything else is reference material.

---

## MS SQL → PostgreSQL restore vocabulary

| MS SQL | PostgreSQL | Notes |
|---|---|---|
| `RESTORE DATABASE motorent FROM DISK = '...\motorent.bak'` | `pg_restore -d motorent motorent.dump` | Loads a `.dump` file produced by `pg_dump -Fc`. |
| `RESTORE DATABASE … WITH REPLACE` | `pg_restore --clean --if-exists -d motorent motorent.dump` | Drops existing objects before restoring. |
| `WITH MOVE` (relocating data files) | not needed — logical restore creates objects via SQL | Physical restore (PITR) does need attention to data dir paths. |
| `WITH NORECOVERY` (apply more logs after) | n/a for logical | Physical restore uses `recovery.signal` + `recovery_target_*`. |
| `WITH STANDBY` (read-only restore) | n/a for logical | Use a streaming replica for that. |
| `RESTORE LOG … WITH STOPAT='2026-04-07 14:00'` | edit `recovery_target_time` in `postgresql.conf`, then start the server | PITR. Requires base backup + WAL archive. |
| `RESTORE VERIFYONLY` | `pg_restore --list file.dump` | Reads the table-of-contents without restoring. |
| `RESTORE FILELISTONLY` | `pg_restore --list file.dump` | Same command, same purpose. |
| `RESTORE HEADERONLY` | `pg_restore --list file.dump | head` | Approximation. |
| `sqlcmd -Q "DROP DATABASE motorent"` | `psql -d postgres -c 'DROP DATABASE motorent'` | Must connect to a *different* database first — you can't drop the one you're connected to. |

---

## 1. Quick restore (the hand-off file)

You've been given a `.dump` file (e.g. `motorent_20260407_140532.dump`) and need to load it into a PostgreSQL 18 server. Here's the entire procedure.

### Pre-flight

```bash
# Confirm pg_restore is the right version
pg_restore --version
# Should print: pg_restore (PostgreSQL) 18.x

# Confirm you can reach the server
psql -h <server-host> -p 5432 -U postgres -d postgres -c "SELECT version();"
```

If the server runs in Docker:

```bash
docker exec <container-name> psql -U postgres -d postgres -c "SELECT version();"
```

### Restore into a NEW database (recommended first step — verifies the dump)

```bash
# 1. Create a fresh, empty database
psql -h <server-host> -U postgres -d postgres -c 'CREATE DATABASE motorent_restored;'

# 2. Restore the dump into it
pg_restore -h <server-host> -U postgres -d motorent_restored \
  --no-owner --no-privileges --verbose \
  motorent_20260407_140532.dump
```

`pg_restore` exit codes:

- **0** — clean success.
- **1** — completed with warnings (usually missing roles or privileges that we deliberately skip via `--no-owner --no-privileges`). **Data is restored, this is fine.**
- **2** — fatal error. Read the output above the failure.

### Verify

```bash
psql -h <server-host> -U postgres -d motorent_restored

motorent_restored=# \dn
-- Today: should list only 'public'.
-- After the multi-tenant migration: should also list 'Core' and tenant schemas.

motorent_restored=# SELECT COUNT(*) FROM public."Organization";
-- Should return >= 1.

motorent_restored=# SELECT COUNT(*) FROM public."User";
-- Should return >= 1.

motorent_restored=# SELECT COUNT(*) FROM public."Vehicle";
-- Should return >= 1.

motorent_restored=# \q
```

### Promote to the live database

Once you're satisfied that `motorent_restored` is intact, swap it in:

```bash
# 1. Stop the application(s) connecting to the database
#    (so nothing holds open connections to the old DB)

# 2. Rename old → backup, new → live
psql -h <server-host> -U postgres -d postgres <<SQL
ALTER DATABASE motorent RENAME TO motorent_old;
ALTER DATABASE motorent_restored RENAME TO motorent;
SQL

# 3. Start the application(s)

# 4. Once you're confident everything works, drop the old one:
psql -h <server-host> -U postgres -d postgres -c 'DROP DATABASE motorent_old;'
```

If a rename fails with "database is being accessed by other users", find the holders:

```sql
SELECT pid, usename, application_name, client_addr, state
FROM pg_stat_activity
WHERE datname IN ('motorent', 'motorent_restored');

-- Forcefully terminate them if needed:
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname IN ('motorent', 'motorent_restored') AND pid <> pg_backend_pid();
```

---

## 2. Restore via the helper script

If you have access to this repo on the server, the wrapper script does the same thing with safer defaults:

```powershell
cd <repo-root>
.\database\scripts\restore-motorent.ps1 -DumpFile .\database\backups\motorent_20260407_140532.dump
```

Default behaviour:

- Restores into a NEW database called `motorent_restored` (won't touch your live DB).
- Uses `--no-owner --no-privileges` so role mismatches don't fail the restore.
- Auto-detects whether to use a host `pg_restore` or to run inside the `postgres18_dev` Docker container.

To restore directly into the live database (drops existing objects first):

```powershell
.\database\scripts\restore-motorent.ps1 `
  -DumpFile .\database\backups\motorent_20260407_140532.dump `
  -TargetDatabase motorent `
  -Clean
```

The script prompts for `YES` confirmation before clobbering `motorent`.

---

## 3. Restore from a logical dump — full reference

```
pg_restore [options] dumpfile
```

Useful options:

| Option | What it does |
|---|---|
| `-d <db>` | Connect to this database and restore into it. **Required** for restoring (instead of just printing SQL). |
| `--clean` | Issue `DROP` for each object before recreating it. The MS SQL `WITH REPLACE` equivalent. |
| `--if-exists` | Pair with `--clean` so the DROPs don't fail when objects don't exist yet. |
| `--no-owner` | Don't try to set object ownership. Use this when the source roles don't exist on the target. |
| `--no-privileges` | Don't restore `GRANT`/`REVOKE` statements. Use this when source roles don't exist. |
| `--verbose` | Print every object being restored. Recommended. |
| `-j N` | Restore N objects in parallel (significantly faster on big DBs). Only with `-Fd` (directory) or `-Fc` dumps. |
| `--list` | Print the table-of-contents instead of restoring. Use to inspect a dump without loading it. |
| `-L list.txt` | Restore only the items listed in `list.txt` (an edited output of `--list`). For partial restores. |
| `--single-transaction` | Wrap the whole restore in one transaction. If anything fails, nothing is committed. Use for production restores. |

A "production-grade" full restore command:

```bash
pg_restore \
  -h <server-host> -p 5432 -U postgres \
  -d motorent \
  --clean --if-exists \
  --no-owner --no-privileges \
  --single-transaction \
  --verbose \
  motorent_20260407_140532.dump
```

---

## 4. Point-in-time restore (physical backup + WAL replay)

Only relevant if WAL archiving is enabled on the server (see `database-backup.md` §5). This is the closest equivalent to MS SQL's "Restore Full → Restore Diff → Restore Logs to point in time".

Outline:

1. **Stop the PostgreSQL service.**
2. **Move the existing data directory aside** (`pg_data` → `pg_data.broken`). Don't delete it until the restore succeeds.
3. **Extract the most recent base backup** into the empty data directory:
   ```bash
   tar -xzf base.tar.gz -C /var/lib/postgresql/18/main
   tar -xzf pg_wal.tar.gz -C /var/lib/postgresql/18/main/pg_wal
   ```
4. **Create `recovery.signal`** in the data directory (its presence tells PostgreSQL to enter recovery mode on next start):
   ```bash
   touch /var/lib/postgresql/18/main/recovery.signal
   ```
5. **Configure recovery target** in `postgresql.conf`:
   ```ini
   restore_command = 'copy "D:\\pg_wal_archive\\%f" "%p"'    # Windows
   # restore_command = 'cp /var/lib/pgsql/wal_archive/%f %p' # Linux
   recovery_target_time = '2026-04-07 13:55:00 +07:00'
   recovery_target_action = 'promote'
   ```
6. **Start the PostgreSQL service.** It will read each WAL segment, replay it, and stop at the configured time.
7. **Verify** with `psql` and a few sanity SELECTs.
8. **Remove `recovery.signal`** (PostgreSQL deletes it automatically when recovery completes successfully).

For anything beyond a one-off, use **pgBackRest** or **Barman** to drive PITR — both wrap this entire procedure in a single command.

---

## 5. Multi-tenant notes

**Current state**: as of this writing, the database has a single `public` schema with all tables. The multi-tenant schema split described in `CLAUDE.md` (a `Core` schema + one schema per tenant `AccountNo`, e.g. `KrabiBeachRentals`, `AdamMotoGolok`) is the **target** design but is not yet in place.

The instructions in this section are written for both today and the post-migration future. Where they differ, both forms are shown.

A `pg_dump` of the whole `motorent` database **always includes every schema**. Restore-time observations:

- `pg_restore` recreates all schemas. There is no per-schema restore from a whole-DB dump.
- If you only need *one tenant* (post-migration), the supported approach is:
  ```bash
  # Take a schema-only dump for that tenant
  pg_dump -h <host> -U postgres -n KrabiBeachRentals -Fc -f krabi.dump motorent
  # And restore it elsewhere
  pg_restore -h <host> -U postgres -d motorent_target krabi.dump
  ```
  The `-n` (schema include) flag was added for exactly this use case.
- Tenant schema **names** are case-sensitive on PostgreSQL (`"KrabiBeachRentals"` ≠ `krabibeachrentals`). Always quote them in SQL.
- Mixed-case table names in `public` are also case-sensitive — `public."User"`, not `public.user`. PostgreSQL folds unquoted identifiers to lowercase.

---

## 6. Post-restore verification checklist

Run this after every restore. Treat anything unexpected as a failed restore until you understand it.

```sql
-- 1. Schemas exist
\dn
-- Today: 'public'. Post-migration: also 'Core' and per-tenant schemas.

-- 2. Core tables have the expected row counts (compare against the source if possible)
SELECT 'Organization' AS t, COUNT(*) FROM public."Organization"
UNION ALL SELECT 'User',     COUNT(*) FROM public."User"
UNION ALL SELECT 'Setting',  COUNT(*) FROM public."Setting";

-- 3. At least one operational table works
SELECT COUNT(*) FROM public."Vehicle";
SELECT COUNT(*) FROM public."Rental";

-- 4. Sequences are healthy (no "currval not yet defined" surprises)
SELECT schemaname, sequencename, last_value
FROM pg_sequences
WHERE schemaname = 'public'
ORDER BY sequencename;

-- 5. Foreign keys are valid
SELECT conrelid::regclass AS table, conname
FROM pg_constraint
WHERE contype = 'f' AND NOT convalidated;
-- expect: 0 rows. Anything here is a NOT VALID FK that was loaded but never re-validated.

-- 6. Inspect a couple of representative tables (computed columns, JSONB, indexes)
\d public."Organization"
\d public."Vehicle"
```

After the smoke checks, **point a non-prod copy of the application at the restored DB** and exercise a critical workflow (login, list vehicles, create a booking). This is the only check that proves the application sees the data the way it expects.

---

## 7. Rollback / undo

The reason §1 restores into `motorent_restored` and renames at the end is so that rollback is a single SQL command:

```sql
-- Something is wrong with the restored DB; revert to the previous live one
ALTER DATABASE motorent RENAME TO motorent_failed;
ALTER DATABASE motorent_old RENAME TO motorent;
```

Then investigate `motorent_failed` at your leisure.

If you restored *over* the live DB with `--clean` and want to undo, your only option is to restore again from a known-good backup. **This is why the rename strategy is recommended.**

---

## 8. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `pg_restore: error: could not connect to database "motorent": FATAL: database "motorent" does not exist` | Target DB doesn't exist | Create it first: `psql -d postgres -c 'CREATE DATABASE motorent;'` |
| `pg_restore: warning: errors ignored on restore: 12` | Some objects (usually `OWNER TO ...` for roles that don't exist) failed | Pass `--no-owner --no-privileges`. Data is fine. |
| `ERROR: permission denied for schema "Core"` | Running pg_restore as a role without CREATE on the target DB | Use `postgres` superuser, or `GRANT CREATE ON DATABASE motorent TO <role>`. |
| `ERROR: extension "pgcrypto" is not available` | Source uses an extension the target doesn't have | Install the extension on the target server (`CREATE EXTENSION pgcrypto`) and re-run, or remove the extension call from the dump (`pg_restore -L`). |
| `pg_restore: error: input file appears to be a text format dump. Please use psql.` | The file is a `pg_dump -Fp` (plain SQL) dump, not custom format | Restore with `psql -f file.sql -d motorent` instead of `pg_restore`. |
| Sequence values are off (next insert collides with existing IDs) | Restore reset sequences but auto-generated IDs grew during the gap | `SELECT setval('"Core"."User_UserId_seq"', (SELECT MAX("UserId") FROM "Core"."User"));` for each affected sequence. |
| Restore is slow on a large DB | Single-threaded restore | Pass `-j 4` (or higher) to `pg_restore` to parallelise. Requires custom-format or directory-format dumps. |
| Rename fails: "database is being accessed by other users" | Application or `psql` session still connected | See [§1 — Promote to live](#promote-to-the-live-database) for the `pg_terminate_backend` snippet. |
| Encoding warnings (`character with byte sequence ... has no equivalent`) | Source DB encoding ≠ target DB encoding | Recreate the target with `CREATE DATABASE motorent ENCODING 'UTF8' TEMPLATE template0;`. The MotoRent DBs should always be UTF8. |

---

## See also

- [`database-backup.md`](./database-backup.md) — how the `.dump` file was produced.
- PostgreSQL official docs: <https://www.postgresql.org/docs/18/app-pgrestore.html>
- PITR walkthrough: <https://www.postgresql.org/docs/18/continuous-archiving.html>
