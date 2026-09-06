using CmdDock.Core.Services;
using System.Drawing;

namespace CmdDock.Tests;

public class WidgetIconServiceTests
{
    [Fact]
    public void GetIconDataUri_ReturnsValidPngDataUri()
    {
        var uri = WidgetIconService.GetIconDataUri("\uE756", 24);
        Assert.NotNull(uri);
        Assert.StartsWith("data:image/png;base64,", uri);

        var base64 = uri.Substring("data:image/png;base64,".Length);
        var bytes = Convert.FromBase64String(base64);
        Assert.True(bytes.Length > 50);

        // Verify it's a valid PNG (PNG header: 89 50 4E 47 0D 0A 1A 0A)
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);
    }

    [Fact]
    public void GetToggleAndSettingsIcons_ReturnValidDataUris()
    {
        var listToggle = WidgetIconService.GetToggleIconDataUri("list");
        var gridToggle = WidgetIconService.GetToggleIconDataUri("grid");
        var settings = WidgetIconService.GetSettingsIconDataUri();

        Assert.StartsWith("data:image/png;base64,", listToggle);
        Assert.StartsWith("data:image/png;base64,", gridToggle);
        Assert.StartsWith("data:image/png;base64,", settings);

        // List and grid toggles should be different icons
        Assert.NotEqual(listToggle, gridToggle);
    }

    [Fact]
    public void GetIconDataUri_CachesResults()
    {
        var uri1 = WidgetIconService.GetIconDataUri("\uE713", 24);
        var uri2 = WidgetIconService.GetIconDataUri("\uE713", 24);

        Assert.Same(uri1, uri2); // Same instance from cache
    }

    [Fact]
    public void GetCommandIconDataUri_WithFontGlyph_ReturnsValidDataUri()
    {
        var cmd = new CmdDock.Core.Models.CommandItem
        {
            Name = "Terminal",
            IconGlyph = "\uE756"
        };

        var uri = WidgetIconService.GetCommandIconDataUri(cmd, 24);
        Assert.NotNull(uri);
        Assert.StartsWith("data:image/png;base64,", uri);
    }

    [Fact]
    public void GetCommandIconDataUri_WithLocalImageFile_ReturnsOptimizedDataUri()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"cmddock_test_{Guid.NewGuid():N}.png");
        try
        {
            using (var bmp = new Bitmap(100, 100))
            {
                using (var g = Graphics.FromImage(bmp))
                {
                    g.Clear(Color.Red);
                }
                bmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
            }

            var cmd = new CmdDock.Core.Models.CommandItem
            {
                Name = "Custom App",
                IconGlyph = tempFile
            };

            Assert.True(cmd.IsImageIcon);
            Assert.Equal("Visible", cmd.ImageVisibility);
            Assert.Equal("Collapsed", cmd.GlyphVisibility);

            var uri = WidgetIconService.GetCommandIconDataUri(cmd, 20);
            Assert.NotNull(uri);
            Assert.StartsWith("data:image/png;base64,", uri);

            var base64 = uri.Substring("data:image/png;base64,".Length);
            var bytes = Convert.FromBase64String(base64);
            // Verify valid PNG header
            Assert.Equal(0x89, bytes[0]);
            Assert.Equal(0x50, bytes[1]);
            Assert.Equal(0x4E, bytes[2]);
            Assert.Equal(0x47, bytes[3]);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void GetCommandIconDataUri_WithNonExistentFile_GracefullyFallsBack()
    {
        var cmd = new CmdDock.Core.Models.CommandItem
        {
            Name = "Missing App",
            IconGlyph = @"C:\non_existent_folder_xyz\icon.png"
        };

        var uri = WidgetIconService.GetCommandIconDataUri(cmd, 24);
        Assert.NotNull(uri);
        Assert.StartsWith("data:image/png;base64,", uri);
    }

    [Fact]
    public void CommandItem_IconGlyphChange_NotifiesDisplayIconAndVisibility()
    {
        var cmd = new CmdDock.Core.Models.CommandItem
        {
            IconGlyph = "\uE756"
        };

        var notifiedProperties = new List<string>();
        cmd.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != null) notifiedProperties.Add(e.PropertyName);
        };

        cmd.IconGlyph = @"C:\test.png";

        Assert.Contains(nameof(cmd.IconGlyph), notifiedProperties);
        Assert.Contains(nameof(cmd.DisplayIcon), notifiedProperties);
        Assert.Contains(nameof(cmd.IsImageIcon), notifiedProperties);
        Assert.Contains(nameof(cmd.GlyphVisibility), notifiedProperties);
        Assert.Contains(nameof(cmd.ImageVisibility), notifiedProperties);
    }
}
