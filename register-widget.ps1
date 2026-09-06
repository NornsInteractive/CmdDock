$manifestPath = "D:\Data\Projects\cmddock\src\CmdDock.App\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\AppxManifest.xml"

if (-not (Test-Path $manifestPath)) {
    Write-Host "Building x64 layout..." -ForegroundColor Cyan
    & "D:\Data\Env\dotnet\dotnet.exe" build "src\CmdDock.App\CmdDock.App.csproj" -p:Platform=x64 -p:WindowsPackageType=MSIX
}

Write-Host "Registering CmdDock Widget Provider to Windows 11..." -ForegroundColor Cyan
try {
    Add-AppxPackage -Register $manifestPath -ErrorAction Stop
    Write-Host "SUCCESS: CmdDock Widget successfully registered!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Steps to pin to Widget Board:" -ForegroundColor Yellow
    Write-Host "1. Press [Win + W] to open Windows 11 Widgets Board"
    Write-Host "2. Click [+] on the top-right corner"
    Write-Host "3. Find [CmdDock] in the list and click Pin"
    Write-Host "4. Click buttons on the widget to trigger commands!"
}
catch {
    Write-Host "Registration failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    Write-Host "Developer Mode is required for local widget testing." -ForegroundColor Yellow
}
