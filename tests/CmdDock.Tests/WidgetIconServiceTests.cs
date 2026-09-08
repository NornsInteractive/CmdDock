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

    [Fact]
    public void GetExecutionStatusIconDataUri_ReturnsCorrectDataUris()
    {
        // Idle should return null (no badge)
        Assert.Null(WidgetIconService.GetExecutionStatusIconDataUri(CmdDock.Core.Models.ExecutionStatus.Idle));

        // Running, Success, Failed should return valid PNG Data URIs
        var runningUri = WidgetIconService.GetExecutionStatusIconDataUri(CmdDock.Core.Models.ExecutionStatus.Running, 16);
        var successUri = WidgetIconService.GetExecutionStatusIconDataUri(CmdDock.Core.Models.ExecutionStatus.Success, 16);
        var failedUri = WidgetIconService.GetExecutionStatusIconDataUri(CmdDock.Core.Models.ExecutionStatus.Failed, 16);

        Assert.NotNull(runningUri);
        Assert.NotNull(successUri);
        Assert.NotNull(failedUri);

        Assert.StartsWith("data:image/png;base64,", runningUri);
        Assert.StartsWith("data:image/png;base64,", successUri);
        Assert.StartsWith("data:image/png;base64,", failedUri);

        // Verify valid PNG header for runningUri
        var bytes = Convert.FromBase64String(runningUri.Substring("data:image/png;base64,".Length));
        Assert.Equal(0x89, bytes[0]);
        Assert.Equal(0x50, bytes[1]);
        Assert.Equal(0x4E, bytes[2]);
        Assert.Equal(0x47, bytes[3]);

        // Distinct icons
        Assert.NotEqual(runningUri, successUri);
        Assert.NotEqual(successUri, failedUri);
    }

    [Fact]
    public void CreateSmallWidgetButtonItem_IncludesStatusIcon_WhenStatusNotIdle()
    {
        var cmd = new CmdDock.Core.Models.CommandItem
        {
            Id = "test_cmd",
            Name = "Test Command",
            LastRunStatus = CmdDock.Core.Models.ExecutionStatus.Running
        };

        var el = CmdDock.Widget.WidgetCardBuilder.CreateSmallWidgetButtonItem(cmd);
        var columns = el["items"]![0]!["columns"]!.AsArray();
        Assert.Equal(3, columns.Count);

        var runningUri = WidgetIconService.GetExecutionStatusIconDataUri(CmdDock.Core.Models.ExecutionStatus.Running, 14);
        Assert.NotNull(runningUri);
        var statusColImage = columns[2]!["items"]![0]!["url"]!.GetValue<string>();
        Assert.Equal(runningUri, statusColImage);
    }

    [Fact]
    public void CreateCommandItemElement_IncludesStatusIcon_WhenStatusNotIdle()
    {
        var cmd = new CmdDock.Core.Models.CommandItem
        {
            Id = "test_cmd_success",
            Name = "Test Command Success",
            LastRunStatus = CmdDock.Core.Models.ExecutionStatus.Success
        };

        var el = CmdDock.Widget.WidgetCardBuilder.CreateCommandItemElement(cmd, isGrid: false);
        var columns = el["items"]![0]!["columns"]!.AsArray();
        Assert.Equal(3, columns.Count);

        var successUri = WidgetIconService.GetExecutionStatusIconDataUri(CmdDock.Core.Models.ExecutionStatus.Success, 14);
        Assert.NotNull(successUri);
        var statusColImage = columns[2]!["items"]![0]!["url"]!.GetValue<string>();
        Assert.Equal(successUri, statusColImage);
    }

    [Fact]
    public void PresetService_DiskHealthAndHttpServer_NativeAndAnnotated()
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets();

        var diskHealth = presets.FirstOrDefault(p => p.Id == "preset_disk_health");
        Assert.NotNull(diskHealth);
        Assert.Equal(CmdDock.Core.Models.ShellType.PowerShell, diskHealth.ShellType);
        Assert.Contains("Get-PhysicalDisk", diskHealth.CommandText);

        var httpServer = presets.FirstOrDefault(p => p.Id == "preset_http_server");
        Assert.NotNull(httpServer);
        Assert.Equal(CmdDock.Core.Models.ShellType.PowerShell, httpServer.ShellType);
        Assert.Contains("HttpListener", httpServer.CommandText);

        // Verify dependencies annotated
        var dockerPs = presets.FirstOrDefault(p => p.Id == "preset_docker_ps");
        Assert.NotNull(dockerPs);
        Assert.Contains("Docker", dockerPs.Description);

        var pythonEnv = presets.FirstOrDefault(p => p.Id == "preset_python_env");
        Assert.NotNull(pythonEnv);
        Assert.Contains("Python", pythonEnv.Description);

        var wslTerm = presets.FirstOrDefault(p => p.Id == "preset_wsl_terminal");
        Assert.NotNull(wslTerm);
        Assert.Contains("WSL", wslTerm.Description);

        var gitCfg = presets.FirstOrDefault(p => p.Id == "preset_git_config");
        Assert.NotNull(gitCfg);
        Assert.Contains("Git", gitCfg.Description);
    }

    [Fact]
    public void ShouldShowStatusIcon_AutoExpiresAfterTwoSeconds()
    {
        var cmd = new CmdDock.Core.Models.CommandItem
        {
            Id = "cmd_idle",
            LastRunStatus = CmdDock.Core.Models.ExecutionStatus.Idle
        };
        Assert.False(CmdDock.Widget.WidgetCardBuilder.ShouldShowStatusIcon(cmd));

        cmd.LastRunStatus = CmdDock.Core.Models.ExecutionStatus.Running;
        Assert.True(CmdDock.Widget.WidgetCardBuilder.ShouldShowStatusIcon(cmd));

        // Recently succeeded (< 2.5s) -> show icon
        cmd.LastRunStatus = CmdDock.Core.Models.ExecutionStatus.Success;
        cmd.LastRunTime = DateTime.UtcNow;
        Assert.True(CmdDock.Widget.WidgetCardBuilder.ShouldShowStatusIcon(cmd));

        // Succeeded more than 2.5 seconds ago -> auto-expired, do NOT show
        cmd.LastRunTime = DateTime.UtcNow.AddSeconds(-5);
        Assert.False(CmdDock.Widget.WidgetCardBuilder.ShouldShowStatusIcon(cmd));

        // Failed more than 2.5 seconds ago -> auto-expired, do NOT show
        cmd.LastRunStatus = CmdDock.Core.Models.ExecutionStatus.Failed;
        cmd.LastRunTime = DateTime.UtcNow.AddSeconds(-10);
        Assert.False(CmdDock.Widget.WidgetCardBuilder.ShouldShowStatusIcon(cmd));
    }

    [Fact]
    public async Task CommandExecutor_ExecutableLaunch_Succeeds()
    {
        var mockService = new CommandService();
        var mockLog = new LogService();
        var executor = new CommandExecutor(mockService, mockLog);

        var taskmgrCmd = new CmdDock.Core.Models.CommandItem
        {
            Id = "test_taskmgr",
            Name = "Taskmgr Test",
            ShellType = CmdDock.Core.Models.ShellType.Executable,
            CommandText = "cmd.exe",
            Arguments = "/c exit 0",
            ExecutionMode = CmdDock.Core.Models.ExecutionMode.Silent
        };

        var result = await executor.ExecuteAsync(taskmgrCmd);
        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }
}
