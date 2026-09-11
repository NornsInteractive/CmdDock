using CmdDock.Core.Models;
using CmdDock.Core.Services;
using Xunit;

namespace CmdDock.Tests;

public class MiniDockSettingsTests
{
    [Fact]
    public void MiniDockSettings_Defaults_AreValid()
    {
        var settings = new MiniDockSettings();

        Assert.Equal(MiniDockMode.CardDeck, settings.DockMode);
        Assert.Equal(DesktopPinMode.AlwaysOnTop, settings.PinMode);
        Assert.Equal(DockOrientation.Auto, settings.Orientation);
        Assert.Equal(0.95, settings.Opacity);
        Assert.Equal(CardScale.Medium, settings.CardScale);
        Assert.False(settings.EnableAutoHide);
        Assert.Equal("full", settings.StartupView);
        Assert.Equal(360, settings.MiniWidth);
        Assert.Equal(500, settings.MiniHeight);
    }

    [Fact]
    public void MiniDockSettingsService_SaveAndLoad_PersistsCorrectly()
    {
        var service = MiniDockSettingsService.Instance;
        var original = new MiniDockSettings
        {
            DockMode = MiniDockMode.DockBar,
            PinMode = DesktopPinMode.PinToDesktop,
            Orientation = DockOrientation.Vertical,
            Opacity = 0.85,
            CardScale = CardScale.Large,
            EnableAutoHide = true,
            StartupView = "mini",
            WindowX = 120,
            WindowY = 240
        };

        service.SaveSettings(original);
        var loaded = service.LoadSettings();

        Assert.Equal(MiniDockMode.DockBar, loaded.DockMode);
        Assert.Equal(DesktopPinMode.PinToDesktop, loaded.PinMode);
        Assert.Equal(DockOrientation.Vertical, loaded.Orientation);
        Assert.Equal(0.85, loaded.Opacity);
        Assert.Equal(CardScale.Large, loaded.CardScale);
        Assert.True(loaded.EnableAutoHide);
        Assert.Equal("mini", loaded.StartupView);
        Assert.Equal(120, loaded.WindowX);
        Assert.Equal(240, loaded.WindowY);
    }

    [Fact]
    public void CommandItem_StatusIndicators_ReflectStatus()
    {
        var item = new CommandItem
        {
            Name = "Ping Gateway",
            CommandText = "ping 192.168.1.1"
        };

        Assert.False(item.IsRunning);
        Assert.False(item.ShowSuccessIcon);
        Assert.False(item.ShowFailedIcon);
        Assert.False(item.HasStatusBadge);
        Assert.False(item.HasParameters);

        item.LastRunStatus = ExecutionStatus.Running;
        Assert.True(item.IsRunning);
        Assert.True(item.HasStatusBadge);

        item.LastRunStatus = ExecutionStatus.Success;
        Assert.True(item.ShowSuccessIcon);
        Assert.False(item.IsRunning);

        item.LastRunStatus = ExecutionStatus.Failed;
        Assert.True(item.ShowFailedIcon);
        Assert.False(item.IsRunning);
    }

    [Fact]
    public void CommandItem_HasParameters_DetectsPlaceholders()
    {
        var normal = new CommandItem { CommandText = "ipconfig /flushdns" };
        var withParam = new CommandItem { CommandText = "ping {target_ip}" };
        var withArgParam = new CommandItem { CommandText = "git checkout", Arguments = "{branch_name}" };

        Assert.False(normal.HasParameters);
        Assert.True(withParam.HasParameters);
        Assert.True(withArgParam.HasParameters);
    }

    [Fact]
    public void I18nService_MiniDockKeys_ReturnNonEmptyStrings()
    {
        var i18n = I18nService.Instance;

        var switchKey = i18n["MiniDock.SwitchToMini"];
        var modeCardKey = i18n["MiniDock.ModeCard"];
        var modeBarKey = i18n["MiniDock.ModeBar"];
        var pinTopKey = i18n["MiniDock.PinTop"];

        Assert.False(string.IsNullOrWhiteSpace(switchKey));
        Assert.False(string.IsNullOrWhiteSpace(modeCardKey));
        Assert.False(string.IsNullOrWhiteSpace(modeBarKey));
        Assert.False(string.IsNullOrWhiteSpace(pinTopKey));
    }
}
