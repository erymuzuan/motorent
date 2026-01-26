param(
    [string]$SourceDir,
    [string]$OutFile
)

$items = Get-ChildItem -Path $SourceDir -Filter *.md -File
$manifest = @()

foreach ($item in $items) {
    if ($item.Name -eq "AUDIT.md" -or $item.Name -eq "README.md") { continue }
    
    $content = Get-Content $item.FullName -First 5
    $title = $item.Name
    foreach ($line in $content) {
        if ($line.StartsWith("# ")) {
            $title = $line.Substring(2).Trim()
            break
        }
    }
    
    $manifest += @{
        fileName = $item.Name
        title = $title
        order = if ($item.Name -match "^(\d+)") { [int]$matches[1] } else { 99 }
    }
}

$manifest | Sort-Object order, title | ConvertTo-Json | Set-Content $OutFile
