# CmdDock 取消注册小组件脚本

Write-Host "正在停止相关进程..." -ForegroundColor Cyan
Get-Process -Name "CmdDock.App", "CmdDock.Widget", "Widgets", "WidgetService" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

Write-Host "正在从 Windows 11 注销 CmdDock 小组件..." -ForegroundColor Cyan
$pkgs = Get-AppxPackage | Where-Object { $_.Name -like "*5309BBD2*" -or $_.Name -like "*CmdDock*" }

if ($pkgs) {
    foreach ($pkg in $pkgs) {
        Remove-AppxPackage -Package $pkg.PackageFullName -ErrorAction SilentlyContinue
        Write-Host "✓ 已成功注销应用包: $($pkg.PackageFullName)" -ForegroundColor Green
    }
} else {
    Write-Host "系统中未找到已注册的 CmdDock 应用包。" -ForegroundColor Yellow
}
