param (
    [Parameter(Mandatory=$true)]
    [string]$WorktreeName
)

$OriginalDir = Get-Location
# Define the new worktree directory path: ..\motorent.<WorktreeName>
$WorktreePath = Join-Path (Join-Path $OriginalDir "..") "motorent.$WorktreeName"
# Resolve to absolute path
$WorktreePath = [System.IO.Path]::GetFullPath($WorktreePath)

Write-Host "Creating worktree at: $WorktreePath"

# Create the worktree
git worktree add $WorktreePath -b $WorktreeName

if ($LASTEXITCODE -eq 0) {
    Write-Host "Worktree '$WorktreeName' created successfully."
    
    # Copy env files
    Copy-Item "$OriginalDir\env.motorent.*" -Destination $WorktreePath -Force
    Write-Host "Copied env.motorent.* files."

    # Find available port starting from 7104
    $port = 7104
    while ($true) {
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $port)
        try {
            $listener.Start()
            $listener.Stop()
            break
        } catch {
            $port++
        }
    }
    Write-Host "Found available port: $port"

    # Update env files
    foreach ($file in @("env.motorent.template.ps1", "env.motorent.ps1")) {
        $path = Join-Path $WorktreePath $file
        if (Test-Path $path) {
            $content = Get-Content $path -Raw
            # Regex to replace the BaseUrl
            $newContent = $content -replace '\$env:MOTO_BaseUrl\s*=\s*"[^"]+"', ('$env:MOTO_BaseUrl = "https://localhost:{0}"' -f $port)
            Set-Content $path -Value $newContent
            Write-Host "Updated $file with port $port"
        }
    }

    # Update launchSettings.json
    $launchSettingsPath = Join-Path $WorktreePath "src\MotoRent.Server\Properties\launchSettings.json"
    if (Test-Path $launchSettingsPath) {
        $content = Get-Content $launchSettingsPath -Raw
        # Regex to replace the applicationUrl
        $newContent = $content -replace '"applicationUrl":\s*"[^"]+"', ('"applicationUrl": "https://localhost:{0}"' -f $port)
        Set-Content $launchSettingsPath -Value $newContent
        Write-Host "Updated launchSettings.json with port $port"
    }

    # Change location to new worktree
    Set-Location $WorktreePath
    
    # Restore dependencies
    Write-Host "Restoring dependencies..."
    dotnet restore .\MotoRent.sln

    # Load Environment Variables
    if (Test-Path .\env.motorent.ps1) {
        Write-Host "Loading environment variables..."
        . .\env.motorent.ps1
    }

    # Color Selection
    $colors = @(
        @{ Name = "Vue Green"; Hex = "#42b883" },
        @{ Name = "Angular Red"; Hex = "#dd1b16" },
        @{ Name = "React Blue"; Hex = "#61dafb" },
        @{ Name = "JavaScript Yellow"; Hex = "#f1e05a" },
        @{ Name = "Mandalorian Blue"; Hex = "#1857a4" },
        @{ Name = "Node Green"; Hex = "#215732" },
        @{ Name = "Salmon Orange"; Hex = "#ff6b6b" },
        @{ Name = "Cyber Purple"; Hex = "#9a9999" },
        @{ Name = "Sky Blue"; Hex = "#00b0ff" },
        @{ Name = "Neon Pink"; Hex = "#ff00cc" },
        @{ Name = "Navy Blue"; Hex = "#001f3f" },
        @{ Name = "Forest Green"; Hex = "#2ecc40" },
        @{ Name = "Ruby Red"; Hex = "#cc342d" },
        @{ Name = "Purple Rain"; Hex = "#800080" },
        @{ Name = "Teal"; Hex = "#008080" }
    )

    Write-Host "`nSelect a VS Code window color:" -ForegroundColor Cyan
    for ($i = 0; $i -lt $colors.Count; $i++) {
        Write-Host "[$($i + 1)] $($colors[$i].Name)"
    }
    
    $selection = Read-Host "Enter number (default: Random)"
    $selectedColor = ""
    
    if ([int]::TryParse($selection, [ref]$null) -and $selection -ge 1 -and $selection -le $colors.Count) {
        $selectedColor = $colors[$selection - 1].Hex
        Write-Host "Selected: $($colors[$selection - 1].Name) ($selectedColor)" -ForegroundColor Green
    } else {
        $selectedColor = "#{0:X6}" -f (Get-Random -Maximum 0xFFFFFF)
        Write-Host "Using Random Color: $selectedColor" -ForegroundColor Yellow
    }

    # Update VS Code Settings
    $vscodeDir = Join-Path $WorktreePath ".vscode"
    if (-not (Test-Path $vscodeDir)) { New-Item -ItemType Directory -Path $vscodeDir | Out-Null }
    $vscodeSettingsPath = Join-Path $vscodeDir "settings.json"

    # Read existing or create new JSON object
    $json = if (Test-Path $vscodeSettingsPath) { Get-Content $vscodeSettingsPath -Raw | ConvertFrom-Json } else { @{} }
    
    # Ensure workbench.colorCustomizations exists
    if ($null -eq $json."workbench.colorCustomizations") {
        # Using Add-Member to ensure it works on PSCustomObject or Hashtable
        $json | Add-Member -NotePropertyName "workbench.colorCustomizations" -NotePropertyValue @{} -Force
    }

    # Set colors
    $c = $json."workbench.colorCustomizations"
    $c | Add-Member -NotePropertyName "activityBar.background" -NotePropertyValue $selectedColor -Force
    $c | Add-Member -NotePropertyName "activityBar.activeBackground" -NotePropertyValue $selectedColor -Force
    $c | Add-Member -NotePropertyName "activityBar.foreground" -NotePropertyValue "#ffffff" -Force
    $c | Add-Member -NotePropertyName "titleBar.activeBackground" -NotePropertyValue $selectedColor -Force
    $c | Add-Member -NotePropertyName "titleBar.activeForeground" -NotePropertyValue "#ffffff" -Force
    $c | Add-Member -NotePropertyName "statusBar.background" -NotePropertyValue $selectedColor -Force
    $c | Add-Member -NotePropertyName "statusBar.foreground" -NotePropertyValue "#ffffff" -Force
    $c | Add-Member -NotePropertyName "statusBarItem.remoteBackground" -NotePropertyValue $selectedColor -Force
    $c | Add-Member -NotePropertyName "statusBarItem.remoteForeground" -NotePropertyValue "#ffffff" -Force

    # Peacock compatibility (if previously used or to support it)
    if ($null -eq $json."peacock.color") {
        $json | Add-Member -NotePropertyName "peacock.color" -NotePropertyValue $selectedColor -Force
    } else {
        $json."peacock.color" = $selectedColor
    }

    # Save settings
    $json | ConvertTo-Json -Depth 10 | Set-Content $vscodeSettingsPath
    Write-Host "Updated VS Code window color settings."
    
    # Start VS Code
    Write-Host "Opening VS Code..."
    code .
} else {
    Write-Error "Failed to create worktree."
}