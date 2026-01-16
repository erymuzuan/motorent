param(
    [Parameter(Mandatory=$true)]
    [string]$Port
)

$ErrorActionPreference = "Stop"

$LaunchSettingsPath = Join-Path $PSScriptRoot "src\MotoRent.Server\Properties\launchSettings.json"
$EnvScriptPath = Join-Path $PSScriptRoot "env.motorent.local.ps1"
$NewBaseUrl = "https://localhost:$Port"

Write-Host "Updating port to $Port..."

# 1. Update launchSettings.json
if (Test-Path $LaunchSettingsPath) {
    Write-Host "Updating $LaunchSettingsPath"
    $jsonContent = Get-Content -Path $LaunchSettingsPath -Raw | ConvertFrom-Json
    
    # Update applicationUrl
    if ($jsonContent.profiles.'https') {
        $jsonContent.profiles.'https'.applicationUrl = $NewBaseUrl
        
        # Update or Add MOTO_BaseUrl in environmentVariables
        if (-not $jsonContent.profiles.'https'.environmentVariables) {
            $jsonContent.profiles.'https'.environmentVariables = @{}
        }
        # Note: PowerShell object to JSON might differ slightly if environmentVariables is a PSCustomObject vs Hashtable, 
        # but ConvertFrom-Json creates PSCustomObjects.
        
        # We need to ensure environmentVariables is treated as an object with properties
        $envVars = $jsonContent.profiles.'https'.environmentVariables
        if ($envVars -is [System.Management.Automation.PSCustomObject]) {
            $envVars | Add-Member -MemberType NoteProperty -Name "MOTO_BaseUrl" -Value $NewBaseUrl -Force
        }
    }
    
    # Write back to file with indentation
    $jsonContent | ConvertTo-Json -Depth 10 | Set-Content -Path $LaunchSettingsPath
    Write-Host "Updated launchSettings.json"
} else {
    Write-Warning "File not found: $LaunchSettingsPath"
}

# 2. Update env.station.ms.ps1
if (Test-Path $EnvScriptPath) {
    Write-Host "Updating $EnvScriptPath"
    $content = Get-Content -Path $EnvScriptPath -Raw
    
    # Regex to replace the specific line
    # Looking for: $env:MOTO_BaseUrl = "..."
    $pattern = '\$env:MOTO_BaseUrl\s*=\s*"[^"]+"'
    $replacement = '$env:MOTO_BaseUrl = "' + $NewBaseUrl + '"'
    
    if ($content -match $pattern) {
        $newContent = $content -replace $pattern, $replacement
        Set-Content -Path $EnvScriptPath -Value $newContent
        Write-Host "Updated env.motorent.local.ps1"
    } else {
        Write-Warning "Pattern not found in $EnvScriptPath. Appending to end."
        Add-Content -Path $EnvScriptPath -Value "`n$replacement"
    }
} else {
    Write-Warning "File not found: $EnvScriptPath"
}

Write-Host "Port update complete. New Base URL: $NewBaseUrl"
$env:MOTO_BaseUrl = $NewBaseUrl
