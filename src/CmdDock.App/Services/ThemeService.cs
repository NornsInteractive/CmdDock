using CmdDock.Core.Services;
using Microsoft.UI.Xaml;

namespace CmdDock_App.Services;

public static class ThemeService
{
    public static void InitializeTheme()
    {
        var savedTheme = AppSettingsService.GetTheme();
        ApplyTheme(savedTheme, saveSetting: false);
    }

    public static void ApplyTheme(string theme, bool saveSetting = true)
    {
        if (saveSetting)
        {
            AppSettingsService.SetTheme(theme);
        }

        var elementTheme = theme switch
        {
            AppSettingsService.ThemeLight => ElementTheme.Light,
            AppSettingsService.ThemeDark => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (App.MainWindowInstance?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = elementTheme;
        }
    }

    public static string GetCurrentTheme()
    {
        return AppSettingsService.GetTheme();
    }
}
