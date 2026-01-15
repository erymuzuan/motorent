$launchSettingsPath = Join-Path $PSScriptRoot "src\MotoRent.Server\Properties\launchSettings.json"
$launchSettings = Get-Content $launchSettingsPath -Raw | ConvertFrom-Json
$applicationUrl = $launchSettings.profiles.https.applicationUrl
Write-Host "Starting MotoRent Web Application... $applicationUrl"

$envUrl = $env:MOTO_BaseUrl
Write-Host "env:MOTO_BaseUrl ... $envUrl"

dotnet watch --project .\src\MotoRent.Server\MotoRent.Server.csproj