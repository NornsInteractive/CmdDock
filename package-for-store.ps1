# CmdDock Microsoft Store Packaging Script
param (
    [string]$Configuration = "Release",
    [string]$Platform = "x64"
)

$ErrorActionPreference = "Stop"
$repoRoot = $PSScriptRoot
$dotnet = "D:\Data\Env\dotnet\dotnet.exe"

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 1. Building CmdDock.Core ($Configuration)..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
& $dotnet build "$repoRoot\src\CmdDock.Core\CmdDock.Core.csproj" -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "CmdDock.Core build failed" }

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 2. Building CmdDock.Widget ($Configuration / $Platform)..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
& $dotnet build "$repoRoot\src\CmdDock.Widget\CmdDock.Widget.csproj" -c $Configuration -p:Platform=$Platform -r win-$Platform
if ($LASTEXITCODE -ne 0) { throw "CmdDock.Widget build failed" }

Write-Host "==============================================" -ForegroundColor Cyan
Write-Host " 3. Publishing CmdDock.App MSIX Store package..." -ForegroundColor Cyan
Write-Host "==============================================" -ForegroundColor Cyan
# Generate MSIX package without local code signing (Microsoft Store will sign during ingestion)
& $dotnet publish "$repoRoot\src\CmdDock.App\CmdDock.App.csproj" `
    -c $Configuration `
    -p:Platform=$Platform `
    -p:WindowsPackageType=MSIX `
    -p:GenerateAppxPackageOnBuild=true `
    -p:AppxPackageSigningEnabled=false `
    -p:UapAppxPackageBuildMode=StoreUpload

if ($LASTEXITCODE -ne 0) {
    Write-Warning "StoreUpload build mode failed, falling back to standard MSIX generation..."
    & $dotnet publish "$repoRoot\src\CmdDock.App\CmdDock.App.csproj" `
        -c $Configuration `
        -p:Platform=$Platform `
        -p:WindowsPackageType=MSIX `
        -p:GenerateAppxPackageOnBuild=true `
        -p:AppxPackageSigningEnabled=false
}

Write-Host "==============================================" -ForegroundColor Green
Write-Host " Store packaging process completed!" -ForegroundColor Green
Write-Host "==============================================" -ForegroundColor Green

$packagesDir = "$repoRoot\src\CmdDock.App\AppPackages"
if (Test-Path $packagesDir) {
    Write-Host "Generated packages located in: $packagesDir" -ForegroundColor Cyan
    Get-ChildItem -Recurse $packagesDir -Include *.msix, *.msixupload, *.appx, *.msixbundle | Select-Object FullName, Length, LastWriteTime
}
