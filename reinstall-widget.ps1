# CmdDock Widget Full Clean Reinstall Script
$ErrorActionPreference = "Stop"

$repoRoot = $PSScriptRoot
$dotnet = "D:\Data\Env\dotnet\dotnet.exe"
$appBinDir = Join-Path $repoRoot "src\CmdDock.App\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64"
$manifestPath = Join-Path $appBinDir "AppxManifest.xml"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 1. Terminating running CmdDock and Widgets processes..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Get-Process -Name "CmdDock.App", "CmdDock.Widget", "Widgets", "WidgetService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 2. Removing existing Appx packages..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
$existingPkgs = Get-AppxPackage | Where-Object { $_.Name -like "*5309BBD2*" -or $_.Name -like "*CmdDock*" }
if ($existingPkgs) {
    foreach ($pkg in $existingPkgs) {
        Write-Host "Removing package: $($pkg.PackageFullName)" -ForegroundColor Yellow
        Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "No registered CmdDock package found." -ForegroundColor Gray
}
Start-Sleep -Seconds 1

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 3. Cleaning stale Widget publish folders..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Remove-Item -Recurse -Force "$repoRoot\src\CmdDock.Widget\publish" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$repoRoot\src\CmdDock.Widget\publish_test" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$repoRoot\src\CmdDock.Widget\bin\Debug\net8.0-windows10.0.26100.0\win-x64\publish" -ErrorAction SilentlyContinue
Remove-Item -Recurse -Force "$repoRoot\src\CmdDock.Widget\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\publish" -ErrorAction SilentlyContinue

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 4. Rebuilding CmdDock.Core / Widget / App..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Write-Host "Building CmdDock.Core..." -ForegroundColor Gray
& $dotnet build "$repoRoot\src\CmdDock.Core\CmdDock.Core.csproj" -c Debug
if ($LASTEXITCODE -ne 0) { throw "CmdDock.Core build failed" }

Write-Host "Building CmdDock.Widget..." -ForegroundColor Gray
& $dotnet build "$repoRoot\src\CmdDock.Widget\CmdDock.Widget.csproj" -c Debug -p:Platform=x64 -r win-x64
if ($LASTEXITCODE -ne 0) { throw "CmdDock.Widget build failed" }

Write-Host "Building CmdDock.App (MSIX layout)..." -ForegroundColor Gray
& $dotnet build "$repoRoot\src\CmdDock.App\CmdDock.App.csproj" -c Debug -p:Platform=x64 -p:WindowsPackageType=MSIX
if ($LASTEXITCODE -ne 0) { throw "CmdDock.App build failed" }

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 5. Verifying and synchronizing Widget binaries..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
$widgetBinDir = "$repoRoot\src\CmdDock.Widget\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64"
if (Test-Path $widgetBinDir) {
    Copy-Item "$widgetBinDir\*.*" -Destination $appBinDir -Force
    Write-Host "Copied fresh CmdDock.Widget binaries to $appBinDir" -ForegroundColor Green
}

$coreBinDir = "$repoRoot\src\CmdDock.Core\bin\Debug\net8.0"
if (Test-Path $coreBinDir) {
    Copy-Item "$coreBinDir\*.*" -Destination $appBinDir -Force
    Write-Host "Copied fresh CmdDock.Core binaries to $appBinDir" -ForegroundColor Green
}

$targetWidgetDll = Join-Path $appBinDir "CmdDock.Widget.dll"
$item = Get-Item $targetWidgetDll
Write-Host "Target Widget DLL Info:" -ForegroundColor Cyan
Write-Host "  Path: $($item.FullName)"
Write-Host "  Size: $($item.Length) bytes"
Write-Host "  LastWriteTime: $($item.LastWriteTime)"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 6. Registering fresh package to Windows 11..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Add-AppxPackage -Register $manifestPath -ForceApplicationShutdown
Write-Host "Add-AppxPackage completed successfully!" -ForegroundColor Green

$installed = Get-AppxPackage | Where-Object { $_.Name -like "*5309BBD2*" }
if ($installed) {
    Write-Host "Installed Package: $($installed.PackageFullName)" -ForegroundColor Green
    Write-Host "Version: $($installed.Version)" -ForegroundColor Green
}

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 7. Launching Windows 11 Widgets Board..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
Start-Process "widgets:" -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "==============================================" -ForegroundColor Green
Write-Host " SUCCESS: Widget reinstalled and registered!" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green
Write-Host "Press [Win + W] to open Windows 11 Widgets Board." -ForegroundColor Yellow
Write-Host "If the widget was already pinned, it has now been updated to the latest version." -ForegroundColor Yellow
Write-Host "You can also click '...' on the widget to Reload or unpin/pin if needed." -ForegroundColor Yellow
