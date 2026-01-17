


$launchSettingsPath = Join-Path $PSScriptRoot "src\MotoRent.Server\Properties\launchSettings.json"
$launchSettings = Get-Content $launchSettingsPath -Raw | ConvertFrom-Json
$applicationUrl = $launchSettings.profiles.https.applicationUrl
Write-Host "Starting MotoRent Web Application... $applicationUrl"

$envUrl = $env:MOTO_BaseUrl
Write-Host "env:MOTO_BaseUrl ... $envUrl"

if (-not [string]::IsNullOrWhiteSpace($envUrl)) {
    try {
        $uri = [Uri]$envUrl
        $port = $uri.Port
        
        $connections = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        foreach ($conn in $connections) {
            $pidToKill = $conn.OwningProcess
            $proc = Get-Process -Id $pidToKill -ErrorAction SilentlyContinue
            if ($proc) {
                Write-Host "Killing process $($proc.ProcessName) (PID: $pidToKill) listening on port $port..."
                Stop-Process -Id $pidToKill -Force -ErrorAction SilentlyContinue
            }
        }
    }
    catch {
        Write-Host "No process found or unable to parse URL/kill process."
    }
}

dotnet watch --project .\src\MotoRent.Server\MotoRent.Server.csproj