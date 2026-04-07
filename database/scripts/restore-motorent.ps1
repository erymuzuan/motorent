<#
.SYNOPSIS
    Restores a MotoRent .dump file into a target PostgreSQL database.

.DESCRIPTION
    Wraps pg_restore to load a custom-format dump produced by backup-motorent.ps1.

    By default, the script restores into a NEW database (motorent_restored) so
    you can verify the dump without touching an existing one. Use -TargetDatabase
    motorent + -Clean to overwrite the live database.

    The script auto-detects how to invoke pg_restore:
      1. If pg_restore is on PATH, it is used directly.
      2. Otherwise, if Docker container 'postgres18_dev' is running, the dump is
         copied INTO the container and pg_restore is invoked there.
      3. Otherwise, the script aborts.

.PARAMETER DumpFile
    Path to the .dump file produced by backup-motorent.ps1. Required.

.PARAMETER TargetDatabase
    Database to restore into. Default: motorent_restored
    The script will CREATE this database if it does not exist.

.PARAMETER PgHost
    PostgreSQL host. Default: parsed from MOTO_ConnectionString or 'localhost'.

.PARAMETER Port
    PostgreSQL port. Default: parsed from MOTO_ConnectionString or 5432.

.PARAMETER Username
    PostgreSQL user. Default: parsed from MOTO_ConnectionString or 'postgres'.

.PARAMETER Clean
    Drop existing objects before restoring (pg_restore --clean --if-exists).
    Use this when restoring back into the original database.

.PARAMETER ContainerName
    Docker container name to use when pg_restore is not on PATH.
    Default: postgres18_dev

.EXAMPLE
    .\restore-motorent.ps1 -DumpFile ..\backups\motorent_20260407_140000.dump
    Restores the dump into 'motorent_restored' (creating it if needed).

.EXAMPLE
    .\restore-motorent.ps1 -DumpFile .\file.dump -TargetDatabase motorent -Clean
    Restores into the live 'motorent' database, dropping existing objects first.

.NOTES
    --no-owner and --no-privileges are always passed so the restore works
    regardless of whether the original role names exist on the target server.
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$DumpFile,

    [string]$TargetDatabase = "motorent_restored",
    [string]$PgHost,
    [int]$Port = 0,
    [string]$Username,
    [switch]$Clean,
    [string]$ContainerName = "postgres18_dev"
)

$ErrorActionPreference = "Stop"

function Parse-ConnectionString {
    param([string]$ConnString)
    $result = @{}
    if ([string]::IsNullOrWhiteSpace($ConnString)) { return $result }
    foreach ($pair in $ConnString.Split(";")) {
        $kv = $pair.Split("=", 2)
        if ($kv.Length -eq 2) {
            $result[$kv[0].Trim().ToLower()] = $kv[1].Trim()
        }
    }
    return $result
}

# ---------- Validate dump file ----------
if (-not (Test-Path $DumpFile)) {
    Write-Host "ERROR: Dump file not found: $DumpFile" -ForegroundColor Red
    exit 1
}
$dumpItem = Get-Item $DumpFile
if ($dumpItem.Length -le 0) {
    Write-Host "ERROR: Dump file is zero bytes: $DumpFile" -ForegroundColor Red
    exit 1
}
$DumpFile = $dumpItem.FullName

# ---------- Resolve connection details ----------
$conn = Parse-ConnectionString $env:MOTO_ConnectionString

if (-not $PgHost)   { $PgHost   = if ($conn.host)     { $conn.host }     else { "localhost" } }
if ($Port -le 0)    { $Port     = if ($conn.port)     { [int]$conn.port } else { 5432 } }
if (-not $Username) { $Username = if ($conn.username) { $conn.username } else { "postgres" } }

$sizeMb = [Math]::Round($dumpItem.Length / 1MB, 2)

Write-Host ""
Write-Host "===== MotoRent PostgreSQL Restore =====" -ForegroundColor Cyan
Write-Host ("  Dump file       : {0} ({1} MB)" -f $DumpFile, $sizeMb)
Write-Host ("  Target host     : {0}:{1}" -f $PgHost, $Port)
Write-Host ("  Target database : {0}" -f $TargetDatabase)
Write-Host ("  Username        : {0}" -f $Username)
Write-Host ("  Clean (drop)    : {0}" -f $Clean.IsPresent)
Write-Host ""

if ($Clean -and $TargetDatabase -eq "motorent") {
    Write-Host "WARNING: This will DROP and recreate objects in the live 'motorent' database." -ForegroundColor Yellow
    $confirm = Read-Host "Type 'YES' to continue"
    if ($confirm -ne "YES") {
        Write-Host "Aborted by user." -ForegroundColor Yellow
        exit 0
    }
}

# ---------- Choose execution path ----------
$hostPgRestore = Get-Command pg_restore -ErrorAction SilentlyContinue
$useDocker     = -not $hostPgRestore

if ($useDocker) {
    $dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCmd) {
        Write-Host "ERROR: pg_restore not on PATH and 'docker' was not found." -ForegroundColor Red
        exit 1
    }
    $running = docker ps --filter "name=^${ContainerName}$" --format "{{.Names}}"
    if ($running -ne $ContainerName) {
        Write-Host "ERROR: Docker container '$ContainerName' is not running." -ForegroundColor Red
        Write-Host "Start it with: docker start $ContainerName" -ForegroundColor Yellow
        exit 1
    }
}

# ---------- Helper: run a psql command ----------
function Invoke-Psql {
    param([string]$Db, [string]$Sql)
    if ($useDocker) {
        & docker exec $ContainerName psql -U $Username -d $Db -v ON_ERROR_STOP=1 -c $Sql
    } else {
        & psql -h $PgHost -p $Port -U $Username -d $Db -v ON_ERROR_STOP=1 -c $Sql
    }
    if ($LASTEXITCODE -ne 0) {
        throw "psql command failed: $Sql"
    }
}

# ---------- Create target database if it doesn't exist ----------
Write-Host "Checking target database..." -ForegroundColor Gray
if ($useDocker) {
    $exists = docker exec $ContainerName psql -U $Username -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$TargetDatabase'"
} else {
    $exists = & psql -h $PgHost -p $Port -U $Username -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$TargetDatabase'"
}
$exists = ($exists | Out-String).Trim()

if ($exists -eq "1") {
    Write-Host "  Target database '$TargetDatabase' already exists." -ForegroundColor Yellow
    if (-not $Clean) {
        Write-Host "  Restoring INTO an existing database without -Clean. Existing objects will conflict." -ForegroundColor Yellow
    }
} else {
    Write-Host "  Creating database '$TargetDatabase'..." -ForegroundColor Gray
    Invoke-Psql -Db "postgres" -Sql "CREATE DATABASE `"$TargetDatabase`""
}

# ---------- Run pg_restore ----------
Write-Host ""
Write-Host "Running pg_restore..." -ForegroundColor Gray

$restoreArgs = @(
    "-U", $Username,
    "-d", $TargetDatabase,
    "--no-owner",
    "--no-privileges",
    "--verbose"
)
if ($Clean) {
    $restoreArgs += @("--clean", "--if-exists")
}

if ($useDocker) {
    # Copy dump into container
    $insidePath = "/tmp/" + (Split-Path $DumpFile -Leaf)
    Write-Host "  Copying dump into container at $insidePath..." -ForegroundColor Gray
    & docker cp $DumpFile "${ContainerName}:$insidePath"
    if ($LASTEXITCODE -ne 0) { Write-Host "ERROR: docker cp failed" -ForegroundColor Red; exit 1 }

    & docker exec $ContainerName pg_restore @restoreArgs $insidePath
    $restoreExit = $LASTEXITCODE

    & docker exec $ContainerName rm -f $insidePath | Out-Null
} else {
    $restoreArgs = @("-h", $PgHost, "-p", $Port) + $restoreArgs
    & pg_restore @restoreArgs $DumpFile
    $restoreExit = $LASTEXITCODE
}

# pg_restore exits 1 on warnings (e.g., missing roles), 2 on fatal errors.
if ($restoreExit -eq 0) {
    Write-Host ""
    Write-Host "===== Restore complete =====" -ForegroundColor Green
} elseif ($restoreExit -eq 1) {
    Write-Host ""
    Write-Host "===== Restore completed with warnings (exit 1) =====" -ForegroundColor Yellow
    Write-Host "This usually means a few non-fatal items were skipped (e.g., missing roles)." -ForegroundColor Yellow
    Write-Host "Review the output above. The data is restored." -ForegroundColor Yellow
} else {
    Write-Host ""
    Write-Host "===== Restore FAILED (exit $restoreExit) =====" -ForegroundColor Red
    exit $restoreExit
}

Write-Host ""
Write-Host ("Verify with: psql -U {0} -d {1} -c '\dn'" -f $Username, $TargetDatabase)
