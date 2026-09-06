Add-Type -AssemblyName System.Drawing
$assetsDir = "D:\Data\Projects\cmddock\src\CmdDock.App\ProviderAssets"
if (-not (Test-Path $assetsDir)) {
    New-Item -ItemType Directory -Path $assetsDir -Force | Out-Null
}

# 1. WidgetIcon.png (48x48)
$iconBmp = New-Object System.Drawing.Bitmap(48, 48)
$g = [System.Drawing.Graphics]::FromImage($iconBmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0, 120, 215))
$g.FillEllipse($brush, 2, 2, 44, 44)
$font = New-Object System.Drawing.Font('Segoe UI Symbol', 20, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$sf = New-Object System.Drawing.StringFormat
$sf.Alignment = [System.Drawing.StringAlignment]::Center
$sf.LineAlignment = [System.Drawing.StringAlignment]::Center
$g.DrawString([char]0x26A1, $font, $whiteBrush, [System.Drawing.RectangleF]::new(0, 0, 48, 48), $sf)
$g.Dispose()
$iconBmp.Save((Join-Path $assetsDir "WidgetIcon.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$iconBmp.Dispose()

# 2. WidgetScreenshot.png (300x304 - official size)
$shotBmp = New-Object System.Drawing.Bitmap(300, 304)
$g = [System.Drawing.Graphics]::FromImage($shotBmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias

# Background dark card with rounded rect
$bgBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(32, 32, 32))
$g.FillRectangle($bgBrush, 0, 0, 300, 304)

# Header
$titleFont = New-Object System.Drawing.Font('Segoe UI', 13, [System.Drawing.FontStyle]::Bold)
$subFont = New-Object System.Drawing.Font('Segoe UI', 9)
$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$gray = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(180, 180, 180))

$g.DrawString('CmdDock', $titleFont, $white, 16, 16)
$g.DrawString('Quick Commands Widget', $subFont, $gray, 16, 42)

# Buttons mock
$btnBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(48, 48, 48))
$btnPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 70, 70), 1)
$btnFont = New-Object System.Drawing.Font('Segoe UI', 9, [System.Drawing.FontStyle]::Regular)

$buttons = @('Flush DNS', 'Restart Explorer', 'Clear Clipboard', 'Port Listening', 'Docker Status', 'Lock Screen')
$row = 0; $col = 0
for ($i = 0; $i -lt $buttons.Count; $i++) {
    $x = 16 + $col * 136
    $y = 76 + $row * 68
    $g.FillRectangle($btnBrush, $x, $y, 128, 54)
    $g.DrawRectangle($btnPen, $x, $y, 128, 54)
    $g.DrawString($buttons[$i], $btnFont, $white, [System.Drawing.RectangleF]::new($x + 4, $y + 16, 120, 30), $sf)
    
    $col++
    if ($col -ge 2) { $col = 0; $row++ }
}

$g.Dispose()
$shotBmp.Save((Join-Path $assetsDir "WidgetScreenshot.png"), [System.Drawing.Imaging.ImageFormat]::Png)
$shotBmp.Dispose()

Write-Host "Successfully created ProviderAssets/WidgetIcon.png and ProviderAssets/WidgetScreenshot.png"
