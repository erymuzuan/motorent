Stop-Process -Name MotoRent.Server -Force -ErrorAction SilentlyContinue
Stop-Process -Id 37928 -Force -ErrorAction SilentlyContinue
Write-Host "Server stopped"
