#web.core
$cpwd = $PWD
Get-ChildItem .\output\web.motorent | Remove-Item -Force -Recurse
& dotnet publish ".\src\MotoRent.Server\MotoRent.Server.csproj" -c "Debug" -o "$cpwd\output\web.motorent" -p:UseAppHost=false

# Remove-Item .\output\web.motorent\wwwroot\assets\ -Recurse -Force
Robocopy.exe .\database .\output\database.motorent /S /xo

explorer .\output\

