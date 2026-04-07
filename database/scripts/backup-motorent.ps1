<#
.SYNOPSIS
    Creates a logical backup of the MotoRent PostgreSQL database using pg_dump.

.DESCRIPTION
    Produces a compressed custom-format dump (.dump) suitable for pg_restore.
    A single .dump file contains the entire database, including the [Core] schema
    and all per-tenant schemas. The file is the recommended hand-off format for
    the server team.

    Connection details are taken from (in order of precedence):
      1. Explicit -PgHost/-Port/-Database/-Username parameters
      2. The MOTO_ConnectionString environment variable
      3. Defaults: localhost:5432 / motorent / postgres

    The script auto-detects how to invoke pg_dump:
      1. If pg_dump is on PATH, it is used directly.
      2. Otherwise, if a Docker container named 'postgres18_dev' is running,
         pg_dump is invoked inside that container and the file is copied out.
      3. Otherwise, the script aborts with an actionable error.

.PARAMETER PgHost
    PostgreSQL host. Default: parsed from MOTO_ConnectionString or 'localhost'.

.PARAMETER Port
    PostgreSQL port. Default: parsed from MOTO_ConnectionString or 5432.

.PARAMETER Database
    Database name to back up. Default: parsed from MOTO_ConnectionString or 'motorent'.

.PARAMETER Username
    PostgreSQL user. Default: parsed from MOTO_ConnectionString or 'postgres'.

.PARAMETER OutputDir
    Directory where the .dump file will be written.
    Default: <repo>/database/backups

.PARAMETER ContainerName
    Docker container name to use when pg_dump is not on PATH.
    Default: postgres18_dev

.EXAMPLE
    .\backup-motorent.ps1
    Uses the env var connection string and writes to database/backups/.

.EXAMPLE
    .\backup-motorent.ps1 -Database motorent -OutputDir D:\backups
    Override target database and output folder.

.NOTES
    Password handling:
      * If using the docker-exec path, no password is needed (the container's
        local socket trusts the postgres user).
      * If using a host pg_dump, set the PGPASSWORD environment variable before
        running, or rely on a ~/.pgpass file.

    The output file is named: motorent_<yyyyMMdd_HHmmss>.dump
#>

[CmdletBinding()]
param(
    [string]$PgHost,
    [int]$Port = 0,
    [string]$Database,
    [string]$Username,
    [string]$OutputDir,
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

# ---------- Resolve connection details ----------
$conn = Parse-ConnectionString $env:MOTO_ConnectionString

if (-not $PgHost)    { $PgHost    = if ($conn.host)     { $conn.host }     else { "localhost" } }
if ($Port -le 0)     { $Port      = if ($conn.port)     { [int]$conn.port } else { 5432 } }
if (-not $Database)  { $Database  = if ($conn.database) { $conn.database } else { "motorent" } }
if (-not $Username)  { $Username  = if ($conn.username) { $conn.username } else { "postgres" } }

# ---------- Resolve output directory ----------
if (-not $OutputDir) {
    $repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $OutputDir = Join-Path $repoRoot "database\backups"
}
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$fileName  = "motorent_${timestamp}.dump"
$outFile   = Join-Path $OutputDir $fileName

Write-Host ""
Write-Host "===== MotoRent PostgreSQL Backup =====" -ForegroundColor Cyan
Write-Host ("  Host    : {0}:{1}" -f $PgHost, $Port)
Write-Host ("  Database: {0}" -f $Database)
Write-Host ("  Username: {0}" -f $Username)
Write-Host ("  Output  : {0}" -f $outFile)
Write-Host ""

# ---------- Choose execution path ----------
$hostPgDump = Get-Command pg_dump -ErrorAction SilentlyContinue

if ($hostPgDump) {
    Write-Host "Using host pg_dump: $($hostPgDump.Source)" -ForegroundColor Gray
    & pg_dump --version
    Write-Host ""

    $args = @(
        "-h", $PgHost,
        "-p", $Port,
        "-U", $Username,
        "-d", $Database,
        "-Fc",
        "--no-owner",
        "--no-privileges",
        "--verbose",
        "-f", $outFile
    )
    & pg_dump @args
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: pg_dump failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }
}
else {
    # Fall back to Docker
    $dockerCmd = Get-Command docker -ErrorAction SilentlyContinue
    if (-not $dockerCmd) {
        Write-Host "ERROR: pg_dump is not on PATH and 'docker' was not found." -ForegroundColor Red
        Write-Host "Install PostgreSQL 18 client tools, OR start the postgres18_dev container." -ForegroundColor Red
        exit 1
    }

    $running = docker ps --filter "name=^${ContainerName}$" --format "{{.Names}}"
    if ($running -ne $ContainerName) {
        Write-Host "ERROR: Docker container '$ContainerName' is not running." -ForegroundColor Red
        Write-Host "Start it with: docker start $ContainerName" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Using pg_dump inside container: $ContainerName" -ForegroundColor Gray
    docker exec $ContainerName pg_dump --version
    Write-Host ""

    # When dumping via the container, prefer connecting through localhost inside the
    # container rather than the host's loopback IP, since 'localhost' inside the container
    # uses the local Unix socket / trust auth.
    $tmpInside = "/tmp/$fileName"

    $dumpArgs = @(
        "exec", $ContainerName,
        "pg_dump",
        "-U", $Username,
        "-d", $Database,
        "-Fc",
        "--no-owner",
        "--no-privileges",
        "--verbose",
        "-f", $tmpInside
    )
    & docker @dumpArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: pg_dump (inside container) failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }

    # Copy out
    & docker cp "${ContainerName}:$tmpInside" $outFile
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: docker cp failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }

    # Cleanup inside container
    & docker exec $ContainerName rm -f $tmpInside | Out-Null
}

# ---------- Verify output ----------
if (-not (Test-Path $outFile)) {
    Write-Host "ERROR: Expected output file was not created: $outFile" -ForegroundColor Red
    exit 1
}
$fileInfo = Get-Item $outFile
if ($fileInfo.Length -le 0) {
    Write-Host "ERROR: Output file is zero bytes: $outFile" -ForegroundColor Red
    exit 1
}

$sizeMb = [Math]::Round($fileInfo.Length / 1MB, 2)
Write-Host ""
Write-Host "===== Backup complete =====" -ForegroundColor Green
Write-Host ("  File: {0}" -f $outFile) -ForegroundColor Green
Write-Host ("  Size: {0} MB ({1} bytes)" -f $sizeMb, $fileInfo.Length) -ForegroundColor Green
Write-Host ""
Write-Host "Hand this file to the server team along with docs/operations/database-restore.md"
