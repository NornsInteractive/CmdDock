using CmdDock.Core.Models;
using Microsoft.Win32;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace CmdDock.Core.Services;

/// <summary>
/// Service that renders font glyphs (from Segoe Fluent Icons / Segoe MDL2 Assets)
/// into ultra-compact, high-contrast monochrome PNG Data URIs,
/// converts local image icons into optimized base64 PNG Data URIs,
/// and generates solid/semi-transparent background color textures for custom card tiles.
/// </summary>
public static class WidgetIconService
{
    private static readonly ConcurrentDictionary<string, string> DataUriCache = new();
    private static readonly ConcurrentDictionary<string, string> ColorPngCache = new();
    private static string? _resolvedFontFamily;
    private static readonly object FontLock = new();

    /// <summary>
    /// Gets the appropriate Data URI for a CommandItem's icon.
    /// If the command uses an image file (PNG/JPG/ICO/etc.), it converts the image
    /// to an optimized, high-DPI base64 PNG data URI.
    /// If it uses a font glyph, it renders the glyph with appropriate contrast.
    /// </summary>
    public static string GetCommandIconDataUri(CommandItem cmd, int size = 24)
    {
        if (cmd == null)
        {
            return GetIconDataUri("\uE756", size);
        }

        if (cmd.IsImageIcon)
        {
            return GetImageIconDataUri(cmd.IconGlyph, size);
        }

        bool hasCustomBg = !string.IsNullOrWhiteSpace(cmd.WidgetBackgroundColor);
        if (hasCustomBg)
        {
            return GetContrastIconDataUri(cmd.DisplayIcon, cmd.WidgetBackgroundColor, size);
        }

        return GetIconDataUri(cmd.DisplayIcon, size);
    }

    /// <summary>
    /// Converts a local image file path or web image into a base64 PNG Data URI.
    /// Automatically resizes high-resolution images down to high-DPI icon size (e.g. 48x48)
    /// to keep Adaptive Card payload small and performance high.
    /// </summary>
    public static string GetImageIconDataUri(string? iconPath, int displaySize = 24)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            return GetIconDataUri("\uE756", displaySize);
        }

        if (iconPath.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) ||
            iconPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            iconPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return iconPath;
        }

        if (!File.Exists(iconPath))
        {
            return GetIconDataUri("\uE756", displaySize);
        }

        try
        {
            var fileInfo = new FileInfo(iconPath);
            int targetPx = Math.Max(32, displaySize * 2); // 2x for High-DPI sharpness
            var cacheKey = $"img:{iconPath}:{fileInfo.LastWriteTimeUtc.Ticks}:{targetPx}";

            if (DataUriCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            using Bitmap? srcBitmap = LoadBitmapFromFile(iconPath);
            if (srcBitmap == null)
            {
                return GetIconDataUri("\uE756", displaySize);
            }

            int srcWidth = srcBitmap.Width;
            int srcHeight = srcBitmap.Height;

            int destWidth, destHeight;
            if (srcWidth <= targetPx && srcHeight <= targetPx)
            {
                destWidth = srcWidth;
                destHeight = srcHeight;
            }
            else
            {
                float ratio = Math.Min((float)targetPx / srcWidth, (float)targetPx / srcHeight);
                destWidth = Math.Max(1, (int)(srcWidth * ratio));
                destHeight = Math.Max(1, (int)(srcHeight * ratio));
            }

            using var destBmp = new Bitmap(destWidth, destHeight);
            using (var g = Graphics.FromImage(destBmp))
            {
                g.Clear(Color.Transparent);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawImage(srcBitmap, new Rectangle(0, 0, destWidth, destHeight));
            }

            using var ms = new MemoryStream();
            destBmp.Save(ms, ImageFormat.Png);
            var uri = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            DataUriCache[cacheKey] = uri;
            return uri;
        }
        catch
        {
            return GetIconDataUri("\uE756", displaySize);
        }
    }

    private static Bitmap? LoadBitmapFromFile(string path)
    {
        try
        {
            if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                using var icon = new Icon(path);
                return icon.ToBitmap();
            }

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var temp = Image.FromStream(fs);
            return new Bitmap(temp);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets a base64 PNG data URI for the specified glyph.
    /// Supports Segoe glyphs (e.g. \uE756), standard Unicode, and custom image paths.
    /// Theme-adaptive: Dark charcoal on light theme, bright white on dark theme.
    /// </summary>
    public static string GetIconDataUri(string? glyph, int size = 24)
    {
        if (string.IsNullOrWhiteSpace(glyph))
        {
            glyph = "\uE756"; // Default Terminal
        }

        if (glyph.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) ||
            glyph.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            glyph.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            glyph.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
        {
            return glyph;
        }

        if (File.Exists(glyph))
        {
            return GetImageIconDataUri(glyph, size);
        }

        bool isDark = IsSystemDarkTheme();
        var cacheKey = $"{isDark}:{size}:{glyph}";
        if (DataUriCache.TryGetValue(cacheKey, out var cachedUri))
        {
            return cachedUri;
        }

        var color = isDark ? Color.FromArgb(240, 240, 240) : Color.FromArgb(32, 32, 32);
        var uri = RenderGlyphToPngDataUriInternal(glyph, color, size);
        DataUriCache[cacheKey] = uri;
        return uri;
    }

    /// <summary>
    /// Returns the icon Data URI for the View Mode toggle button.
    /// If current view is "list", shows Grid icon (\uE8A9).
    /// If current view is "grid", shows List icon (\uE8C0).
    /// </summary>
    public static string GetToggleIconDataUri(string viewMode, int size = 20)
    {
        var glyph = viewMode == "list" ? "\uE8A9" : "\uE8C0";
        return GetIconDataUri(glyph, size);
    }

    /// <summary>
    /// Returns the icon Data URI for the Manage/Settings button (\uE713).
    /// </summary>
    public static string GetSettingsIconDataUri(int size = 20)
    {
        return GetIconDataUri("\uE713", size);
    }

    /// <summary>
    /// Generates a solid/translucent PNG Data URI for the specified hex color (#RRGGBB or #AARRGGBB).
    /// Used as backgroundImage for customized widget card tiles.
    /// </summary>
    public static string GetColorPngDataUri(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor))
        {
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
        }

        var key = hexColor.Trim().ToUpperInvariant();
        if (ColorPngCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var color = ParseHexColor(key);
        using var bmp = new Bitmap(4, 4);
        using (var g = Graphics.FromImage(bmp))
        {
            using var brush = new SolidBrush(color);
            g.FillRectangle(brush, 0, 0, 4, 4);
        }

        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Png);
        var uri = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        ColorPngCache[key] = uri;
        return uri;
    }

    private static string? _selectedPillDataUri;
    private const string Transparent1x1Png = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";

    /// <summary>
    /// Generates a high-DPI smooth anti-aliased capsule pill background for selected category tags,
    /// and transparent texture for unselected tags, eliminating harsh rectangular blocks.
    /// </summary>
    public static string GetCategoryPillBackgroundDataUri(bool isSelected, int width = 120, int height = 48)
    {
        if (!isSelected)
        {
            return Transparent1x1Png;
        }

        if (_selectedPillDataUri != null)
        {
            return _selectedPillDataUri;
        }

        try
        {
            using var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using var path = new GraphicsPath();
                float r = (height - 4) / 2f;
                var rect = new RectangleF(2, 2, width - 4, height - 4);
                path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();

                // Subtle frosted Fluent Blue capsule fill
                using var brush = new SolidBrush(Color.FromArgb(50, 59, 130, 246));
                g.FillPath(brush, path);

                // Subtle accent border
                using var pen = new Pen(Color.FromArgb(180, 96, 165, 250), 2f);
                g.DrawPath(pen, path);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            _selectedPillDataUri = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            return _selectedPillDataUri;
        }
        catch
        {
            return Transparent1x1Png;
        }
    }

    private static string? _selectedSidebarItemDataUri;

    /// <summary>
    /// Generates a rounded rectangle background for the active sidebar category item,
    /// mimicking Windows 11 NavigationViewItem selection style.
    /// </summary>
    public static string GetSidebarCategoryItemBackgroundDataUri(bool isSelected, int width = 100, int height = 36)
    {
        if (!isSelected)
        {
            return Transparent1x1Png;
        }

        if (_selectedSidebarItemDataUri != null)
        {
            return _selectedSidebarItemDataUri;
        }

        try
        {
            using var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);

                using var path = new GraphicsPath();
                float r = 6f;
                var rect = new RectangleF(1, 1, width - 2, height - 2);
                path.AddArc(rect.X, rect.Y, r * 2, r * 2, 180, 90);
                path.AddArc(rect.Right - r * 2, rect.Y, r * 2, r * 2, 270, 90);
                path.AddArc(rect.Right - r * 2, rect.Bottom - r * 2, r * 2, r * 2, 0, 90);
                path.AddArc(rect.X, rect.Bottom - r * 2, r * 2, r * 2, 90, 90);
                path.CloseFigure();

                // Subtle frosted Fluent Blue background
                using var brush = new SolidBrush(Color.FromArgb(60, 59, 130, 246));
                g.FillPath(brush, path);

                // Subtle accent border
                using var pen = new Pen(Color.FromArgb(160, 96, 165, 250), 1.5f);
                g.DrawPath(pen, path);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            _selectedSidebarItemDataUri = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            return _selectedSidebarItemDataUri;
        }
        catch
        {
            return Transparent1x1Png;
        }
    }

    /// <summary>
    /// <summary>
    /// Returns adaptive text/glyph color for widget controls according to current Windows theme.
    /// </summary>
    public static Color GetAdaptiveControlColor(bool isEnabled, bool isSubtle = false)
    {
        bool isDark = IsSystemDarkTheme();
        if (isDark)
        {
            return isEnabled
                ? Color.FromArgb(240, 240, 240)
                : (isSubtle ? Color.FromArgb(80, 80, 80) : Color.FromArgb(100, 100, 100));
        }
        else
        {
            return isEnabled
                ? Color.FromArgb(32, 32, 32)
                : (isSubtle ? Color.FromArgb(190, 190, 190) : Color.FromArgb(160, 160, 160));
        }
    }

    /// <summary>
    /// Gets a 16x16 icon data URI for a category pill.
    /// </summary>
    public static string GetCategoryIconDataUri(string? iconGlyph, int size = 16)
    {
        if (string.IsNullOrWhiteSpace(iconGlyph))
        {
            iconGlyph = "E8EC";
        }

        if (File.Exists(iconGlyph))
        {
            return GetImageIconDataUri(iconGlyph, size);
        }

        var normalized = CommandItem.NormalizeToSegoeGlyph(iconGlyph);
        var isDark = IsSystemDarkTheme();
        var iconColor = isDark ? Color.FromArgb(245, 245, 245) : Color.FromArgb(40, 40, 40);
        return GetIconDataUriWithColor(normalized, iconColor, size);
    }

    /// <summary>
    /// Returns the ChevronLeft icon data URI (\uE76B) for pagination controls.
    /// </summary>
    public static string GetPagerPrevIconDataUri(bool isEnabled = true, int size = 16)
    {
        var color = GetAdaptiveControlColor(isEnabled);
        return GetIconDataUriWithColor("\uE76B", color, size);
    }

    /// <summary>
    /// Returns the ChevronRight icon data URI (\uE76C) for pagination controls.
    /// </summary>
    public static string GetPagerNextIconDataUri(bool isEnabled = true, int size = 16)
    {
        var color = GetAdaptiveControlColor(isEnabled);
        return GetIconDataUriWithColor("\uE76C", color, size);
    }

    /// <summary>
    /// Returns the ChevronLeft icon data URI (\uE76B) for category pagination.
    /// </summary>
    public static string GetCategoryPrevIconDataUri(bool isEnabled = true, int size = 12)
    {
        var color = GetAdaptiveControlColor(isEnabled);
        return GetIconDataUriWithColor("\uE76B", color, size);
    }

    /// <summary>
    /// Returns the ChevronRight icon data URI (\uE76C) for category pagination.
    /// </summary>
    public static string GetCategoryNextIconDataUri(bool isEnabled = true, int size = 12)
    {
        var color = GetAdaptiveControlColor(isEnabled);
        return GetIconDataUriWithColor("\uE76C", color, size);
    }

    /// <summary>
    /// Returns the ChevronUp icon data URI (\uE70E) for vertical category pagination.
    /// </summary>
    public static string GetCategoryUpIconDataUri(bool isEnabled = true, int size = 12)
    {
        var color = GetAdaptiveControlColor(isEnabled);
        return GetIconDataUriWithColor("\uE70E", color, size);
    }

    /// <summary>
    /// Returns the ChevronDown icon data URI (\uE70D) for vertical category pagination.
    /// </summary>
    public static string GetCategoryDownIconDataUri(bool isEnabled = true, int size = 12)
    {
        var color = GetAdaptiveControlColor(isEnabled);
        return GetIconDataUriWithColor("\uE70D", color, size);
    }

    /// <summary>
    /// Returns the ChevronDown icon data URI (\uE70D) for the dropdown category selector.
    /// </summary>
    public static string GetDropdownChevronIconDataUri(int size = 12)
    {
        var color = GetAdaptiveControlColor(true);
        return GetIconDataUriWithColor("\uE70D", color, size);
    }

    /// <summary>
    /// Renders a glyph with a specific custom color (e.g. White on dark cards, Dark on light cards).
    /// </summary>
    public static string GetIconDataUriWithColor(string? glyph, Color iconColor, int size = 24)
    {
        if (string.IsNullOrWhiteSpace(glyph)) glyph = "\uE756";
        var cacheKey = $"c:{iconColor.ToArgb():X8}:{size}:{glyph}";
        if (DataUriCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var uri = RenderGlyphToPngDataUriInternal(glyph, iconColor, size);
        DataUriCache[cacheKey] = uri;
        return uri;
    }

    /// <summary>
    /// Automatically calculates whether the background is dark or light and returns
    /// a high-contrast icon (White on dark colors, Dark charcoal on light colors).
    /// </summary>
    public static string GetContrastIconDataUri(string? glyph, string? hexBackground, int size = 24)
    {
        if (string.IsNullOrWhiteSpace(hexBackground))
        {
            return GetIconDataUri(glyph, size);
        }

        if (File.Exists(glyph))
        {
            return GetImageIconDataUri(glyph, size);
        }

        bool isDarkBg = IsDarkBackground(hexBackground);
        var iconColor = isDarkBg ? Color.FromArgb(245, 245, 245) : Color.FromArgb(30, 30, 30);
        return GetIconDataUriWithColor(glyph, iconColor, size);
    }

    public static bool IsDarkBackground(string? hexColor)
    {
        if (string.IsNullOrWhiteSpace(hexColor)) return false;
        var c = ParseHexColor(hexColor);
        // Standard perceived luminance calculation
        double lum = (0.299 * c.R + 0.587 * c.G + 0.114 * c.B);
        return lum < 150;
    }

    public static Color ParseHexColor(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return Color.Transparent;
        var clean = hex.Trim().TrimStart('#');
        try
        {
            if (clean.Length == 6)
            {
                int r = Convert.ToInt32(clean.Substring(0, 2), 16);
                int g = Convert.ToInt32(clean.Substring(2, 2), 16);
                int b = Convert.ToInt32(clean.Substring(4, 2), 16);
                return Color.FromArgb(255, r, g, b);
            }
            if (clean.Length == 8)
            {
                int a = Convert.ToInt32(clean.Substring(0, 2), 16);
                int r = Convert.ToInt32(clean.Substring(2, 2), 16);
                int g = Convert.ToInt32(clean.Substring(4, 2), 16);
                int b = Convert.ToInt32(clean.Substring(6, 2), 16);
                return Color.FromArgb(a, r, g, b);
            }
        }
        catch { }
        return Color.FromArgb(255, 37, 99, 235); // Default blue fallback
    }

    private static string RenderGlyphToPngDataUriInternal(string glyph, Color iconColor, int size)
    {
        try
        {
            using var bmp = new Bitmap(size, size);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;

                var familyName = GetBestIconFontFamily();
                using var font = new Font(familyName, size * 0.72f, FontStyle.Regular, GraphicsUnit.Pixel);
                using var brush = new SolidBrush(iconColor);
                using var sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };

                var rect = new RectangleF(0, 0, size, size);
                g.DrawString(glyph, font, brush, rect, sf);
            }

            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
        }
        catch
        {
            // Fallback: 1x1 transparent PNG if GDI+ fails
            return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=";
        }
    }

    private static string GetBestIconFontFamily()
    {
        if (_resolvedFontFamily != null)
        {
            return _resolvedFontFamily;
        }

        lock (FontLock)
        {
            if (_resolvedFontFamily != null)
            {
                return _resolvedFontFamily;
            }

            try
            {
                using var fonts = new InstalledFontCollection();
                var families = fonts.Families;

                foreach (var family in families)
                {
                    if (family.Name.Equals("Segoe Fluent Icons", StringComparison.OrdinalIgnoreCase))
                    {
                        _resolvedFontFamily = "Segoe Fluent Icons";
                        return _resolvedFontFamily;
                    }
                }

                foreach (var family in families)
                {
                    if (family.Name.Equals("Segoe MDL2 Assets", StringComparison.OrdinalIgnoreCase))
                    {
                        _resolvedFontFamily = "Segoe MDL2 Assets";
                        return _resolvedFontFamily;
                    }
                }
            }
            catch
            {
                // Ignore
            }

            _resolvedFontFamily = "Segoe UI Symbol";
            return _resolvedFontFamily;
        }
    }

    public static bool IsSystemDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("SystemUsesLightTheme") is int val)
            {
                return val == 0;
            }
        }
        catch
        {
            // Default to light
        }
        return false;
    }
}
