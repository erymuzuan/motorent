# MotoRent — PostgreSQL Backup Guide

> Audience: server team / ops / future maintainer.
> Reader assumed to know **Microsoft SQL Server** and need a PostgreSQL translation.

## 1. Overview

MotoRent uses **PostgreSQL 18** (running in a Docker container locally as `postgres18_dev`, image `postgres:18.1-alpine`). The database holds:

- **Today**: a single `public` schema with ~66 tables (Organization, User, Vehicle, Rental, etc.).
- **Intended (per `CLAUDE.md`)**: a shared `Core` schema + one per-tenant schema per `AccountNo` (e.g. `KrabiBeachRentals`). The migration to that layout is not yet done.

A single `pg_dump` of the `motorent` database captures **everything** — every schema currently in the DB. The backup procedure does not need to change when the multi-tenant migration happens; only the verification queries do.

### Recovery objectives (defaults)

| Objective | Target |
|---|---|
| **RPO** (Recovery Point Objective — how much data you can afford to lose) | **24 hours** |
| **RTO** (Recovery Time Objective — how quickly you need to be back up) | **< 1 hour** for the `motorent` database |
| Retention | **30 days** of nightly backups, plus monthly archives indefinitely |

If the business needs an RPO measured in minutes, see [§5 — When you outgrow nightly dumps](#5-when-you-outgrow-nightly-dumps).

---

## 2. MS SQL → PostgreSQL Concept Map

If you're coming from SQL Server, this is the mental model:

| MS SQL concept | PostgreSQL equivalent | Notes |
|---|---|---|
| **Full backup** (`.bak`) | `pg_dump -Fc` (logical) **or** `pg_basebackup` (physical) | Logical = portable, smaller, slower restore. Physical = byte-exact, supports PITR. |
| **Differential backup** | *No native equivalent.* | The closest is "base backup + WAL since base" using physical backup + PITR. |
| **Transaction log backup** | **WAL archiving** (Write-Ahead Log) | Configured via `archive_mode=on` + `archive_command` in `postgresql.conf`. |
| **Point-in-time recovery (PITR)** | Base backup + WAL replay (`recovery_target_time`) | Same idea, different mechanics. |
| **Recovery model FULL** | WAL archiving enabled + base backups | Required for PITR. |
| **Recovery model SIMPLE** | No WAL archive, `wal_level = minimal` | Only the most recent backup is restorable. |
| **`RESTORE … WITH REPLACE`** | `pg_restore --clean --if-exists -d motorent file.dump` | Drops existing objects before restoring. |
| **SQL Server Agent backup job** | Windows Task Scheduler → PowerShell script | PostgreSQL has no built-in scheduler on Windows. |
| `.bak` file | `.dump` file (custom format) | Same idea: a single binary file you hand to the restore tool. |
| `BACKUP DATABASE … TO DISK` | `pg_dump -Fc -f file.dump` | Both produce a single restorable file. |
| `tail-log backup` | n/a (closest: WAL up to crash time) | Requires WAL archiving to be useful. |

**Key mental shift**: in PostgreSQL the word **backup** can mean two very different things:

1. **Logical backup** — produced by `pg_dump`. It is *SQL plus data*. Portable across machines, OS, and (mostly) versions. Slower to restore on huge databases. **This is the default for MotoRent.**
2. **Physical backup** — produced by `pg_basebackup`. It is a *byte-exact copy* of the database files. Fast restore, supports PITR via WAL replay, but locked to the same major version and roughly the same OS/architecture.

---

## 3. The three backup types

PostgreSQL supports the same conceptual *types* of backup as SQL Server, just by different names:

### 3a. Full backup

The whole database as of one moment in time. PostgreSQL gives you two choices:

```powershell
# Logical full backup (PORTABLE, recommended for MotoRent today)
pg_dump -h localhost -p 5432 -U postgres -Fc -f motorent.dump motorent
```

```bash
# Physical full backup (byte-exact, required if you want PITR)
pg_basebackup -h localhost -p 5432 -U postgres -D /var/backups/base -Ft -z -P
```

### 3b. Daily backup

PostgreSQL has **no native differential backup**. The standard practice is one of:

- **Daily logical full** — re-run `pg_dump -Fc` every night. Simple, sufficient for small/medium DBs (anything under ~50 GB). **This is what MotoRent does.**
- **Weekly base + continuous WAL** — for large DBs where re-dumping daily is too slow.

### 3c. Transaction log backup

PostgreSQL's transaction log is called the **Write-Ahead Log (WAL)**. There is no separate "transaction log backup" command — instead, you turn on **WAL archiving** in `postgresql.conf` and PostgreSQL streams every committed transaction to your archive directory continuously:

```ini
# postgresql.conf
wal_level = replica            # 'minimal' disables WAL archiving
archive_mode = on
archive_command = 'copy "%p" "D:\\pg_wal_archive\\%f"'   # Windows
# archive_command = 'test ! -f /var/lib/pgsql/wal_archive/%f && cp %p /var/lib/pgsql/wal_archive/%f'   # Linux
```

After a `SELECT pg_reload_conf()` (and a restart if `wal_level` changed), every WAL segment is copied to your archive directory the moment it's filled. Combined with a base backup, this gives you **point-in-time recovery** — the closest equivalent to MS SQL's Full + Differential + TLog scheme.

WAL archiving is **not enabled** by default in MotoRent. See [§5](#5-when-you-outgrow-nightly-dumps) for how to turn it on when you need it.

---

## 4. Primary strategy — nightly logical dump (recommended)

This is what we do today. Single command, single file, easy to hand over.

### 4a. Run a backup manually

From the repo root:

```powershell
# Load env vars (connection string, etc.)
. .\env.motorent.ps1

# Run the script
.\database\scripts\backup-motorent.ps1
```

The script:

1. Reads `MOTO_ConnectionString` (or accepts `-PgHost / -Port / -Database / -Username` overrides).
2. Detects whether `pg_dump` is on `PATH`. If not, falls back to running `pg_dump` inside the `postgres18_dev` Docker container and copies the file out via `docker cp`.
3. Writes `database\backups\motorent_<yyyyMMdd_HHmmss>.dump`.
4. Verifies the file is non-zero and prints the path + size.

Example output:

```
===== MotoRent PostgreSQL Backup =====
  Host    : localhost:5432
  Database: motorent
  Username: postgres
  Output  : E:\project\work\motorent.jaleos\database\backups\motorent_20260407_140532.dump

===== Backup complete =====
  File: ...\motorent_20260407_140532.dump
  Size: 4.21 MB (4413952 bytes)
```

### 4b. Schedule it nightly (Windows Task Scheduler)

Save the following XML as `motorent-backup.xml` and import it via `schtasks /Create /XML motorent-backup.xml /TN "MotoRent Backup"`:

```xml
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo>
    <Description>Nightly logical backup of the MotoRent PostgreSQL database.</Description>
  </RegistrationInfo>
  <Triggers>
    <CalendarTrigger>
      <StartBoundary>2026-04-08T02:00:00</StartBoundary>
      <ScheduleByDay>
        <DaysInterval>1</DaysInterval>
      </ScheduleByDay>
      <Enabled>true</Enabled>
    </CalendarTrigger>
  </Triggers>
  <Principals>
    <Principal id="Author">
      <UserId>S-1-5-18</UserId>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <ExecutionTimeLimit>PT1H</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context="Author">
    <Exec>
      <Command>powershell.exe</Command>
      <Arguments>-NoProfile -ExecutionPolicy Bypass -File "E:\project\work\motorent.jaleos\database\scripts\backup-motorent.ps1"</Arguments>
      <WorkingDirectory>E:\project\work\motorent.jaleos</WorkingDirectory>
    </Exec>
  </Actions>
</Task>
```

The task runs daily at 02:00 as `LocalSystem`. Adjust `StartBoundary`, the `WorkingDirectory`, and the script path to match the server.

### 4c. The connection string

The script reads from `MOTO_ConnectionString` (Npgsql format):

```
Host=localhost;Port=5432;Database=motorent;Username=postgres;Password=postgres;Include Error Detail=true
```

For a scheduled task running as `LocalSystem`, set the env var system-wide via:

```powershell
[Environment]::SetEnvironmentVariable(
  "MOTO_ConnectionString",
  "Host=localhost;Port=5432;Database=motorent;Username=postgres;Password=...",
  "Machine")
```

Or store the password in `%APPDATA%\postgresql\pgpass.conf` (the standard `~/.pgpass` location on Windows) so `pg_dump` picks it up without an env var:

```
localhost:5432:motorent:postgres:YourPasswordHere
```

File must be readable only by the running user.

---

## 5. When you outgrow nightly dumps

Switch to **base backup + WAL archiving** when any of these become true:

- The database is larger than ~50 GB and `pg_dump` takes longer than your maintenance window.
- The business needs an RPO better than 24 hours (e.g., "no more than 5 minutes of lost transactions").
- You need to roll back the database to an exact point in time after a bad migration.

This is the closest match to MS SQL's Full + Differential + TLog model.

### 5a. Enable WAL archiving on the server

Edit `postgresql.conf`:

```ini
wal_level = replica
archive_mode = on
archive_command = 'copy "%p" "D:\\pg_wal_archive\\%f"'
max_wal_senders = 3       # required by pg_basebackup with -X stream
```

Create the archive directory and make sure the PostgreSQL service account can write to it:

```powershell
New-Item -ItemType Directory -Path "D:\pg_wal_archive"
icacls "D:\pg_wal_archive" /grant "NT AUTHORITY\NetworkService:(OI)(CI)F"
```

Restart the PostgreSQL service. Verify archiving works:

```sql
SELECT pg_switch_wal();   -- forces a WAL segment to close
\! dir D:\pg_wal_archive  -- you should see a new file
```

### 5b. Take a base backup

```powershell
pg_basebackup -h localhost -p 5432 -U postgres `
  -D D:\backups\base_2026-04-07 `
  -Ft -z -P -X stream
```

This produces a `base.tar.gz` (the data files) and `pg_wal.tar.gz` (the WAL needed to make it consistent). Take a fresh base backup at least once a week.

### 5c. Rotate WAL archives

WAL files accumulate fast. After a new base backup is taken and verified, remove WAL segments older than the *oldest* base backup you still want to restore from. The supported tool is **`pg_archivecleanup`**:

```powershell
pg_archivecleanup D:\pg_wal_archive 000000010000000000000042
```

(The argument is the oldest WAL filename you still need — usually read from `pg_wal.tar.gz` of the oldest base backup you keep.)

For a scripted rotation use the PostgreSQL community tool **pgBackRest** or **Barman**. Both handle base + WAL + retention end-to-end and are the de facto standard for production PostgreSQL backup.

---

## 6. Retention & rotation

Default policy for the nightly dump strategy:

| Tier | What | How long | Where |
|---|---|---|---|
| Daily | nightly `motorent_<ts>.dump` | 30 days, then deleted | `database\backups\` (or a network share) |
| Monthly | first dump of each month | 12 months, then deleted | a separate "archive" folder |
| Yearly | first dump of each year | indefinitely | offline / cold storage |

A simple PowerShell prune you can run after the nightly task:

```powershell
$backupDir = "E:\project\work\motorent.jaleos\database\backups"
Get-ChildItem $backupDir -Filter "motorent_*.dump" |
  Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } |
  Remove-Item -Force
```

---

## 7. Version compatibility

| Component | Version |
|---|---|
| Local PostgreSQL server | **18.1** (Docker `postgres:18.1-alpine`) |
| Production server | **18.x** |
| `pg_dump` used to take the backup | Must be **≥** the source server version |

**Rule of thumb**: always use the **newer** of the two `pg_dump` binaries you have access to. PostgreSQL supports loading dumps into the same or newer servers, but not into older ones.

If the production server is ever upgraded to PG19+, also upgrade the local Docker image so that `pg_dump` keeps up. Edit the local container image tag (`postgres:19-alpine` etc.) and recreate the container.

---

## 8. Verifying a backup

A backup file you've never tested is not a backup. After every full backup the script runs through this checklist; you should also run it manually after any `postgresql.conf` change that affects WAL.

```powershell
# 1. The file exists and is non-zero
Get-Item .\database\backups\motorent_<ts>.dump

# 2. pg_restore can read its table-of-contents (proves the file isn't corrupt)
pg_restore --list .\database\backups\motorent_<ts>.dump | Select-Object -First 30

# 3. You should see entries for the expected tables
pg_restore --list .\database\backups\motorent_<ts>.dump | Select-String 'TABLE DATA'
# Today: every table sits in 'public' (e.g. 'public Organization', 'public Vehicle').
# After the multi-tenant migration: also expect '"Core"' and per-tenant schema names.
```

For a real "is this restorable?" check, restore the dump into a throwaway database — see `database-restore.md` §1 for the smoke-test procedure.

---

## 9. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `pg_dump: error: server version: 18.x; pg_dump version: 16.x` | Local `pg_dump` is older than the server | Use a newer `pg_dump`. The script falls back to `docker exec postgres18_dev pg_dump`, which is always the right version. |
| `pg_dump: error: connection to server ... failed: FATAL: password authentication failed` | Wrong password / no `pgpass` / no `PGPASSWORD` env var | Set `PGPASSWORD`, or create `%APPDATA%\postgresql\pgpass.conf`, or use the docker-exec path (no password needed). |
| `docker exec ... no such container: postgres18_dev` | The Docker container is stopped | `docker start postgres18_dev` |
| Output file is 0 bytes | `pg_dump` wrote to stdout but the redirect was mangled (Unicode line endings, BOM, etc.) | Don't pipe binary output through PowerShell. The provided script writes to `/tmp/` inside the container and uses `docker cp` to copy out — this is byte-clean. |
| Backup takes hours | Database is too large for nightly logical dumps | Switch to base backup + WAL ([§5](#5-when-you-outgrow-nightly-dumps)). |
| Disk full on `D:\pg_wal_archive` | WAL archiving is on but no rotation | Run `pg_archivecleanup` periodically, or adopt pgBackRest / Barman. |
| `pg_dump: error: query failed: ERROR: permission denied for table …` | The role doing the dump can't read every table | Use the `postgres` superuser, or grant `pg_read_all_data` to the dump role. |

---

## See also

- [`database-restore.md`](./database-restore.md) — how to restore a `.dump` file on the server.
- PostgreSQL official docs: <https://www.postgresql.org/docs/18/backup.html>
- pgBackRest (recommended for production WAL-based backup): <https://pgbackrest.org>
