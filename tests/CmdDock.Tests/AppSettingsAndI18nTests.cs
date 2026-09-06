using CmdDock.Core.Services;
using Xunit;

namespace CmdDock.Tests;

public class AppSettingsAndI18nTests
{
    [Fact]
    public void AppSettingsService_ThemeReadWrite_ShouldPersistCorrectly()
    {
        var original = AppSettingsService.GetTheme();
        try
        {
            AppSettingsService.SetTheme(AppSettingsService.ThemeDark);
            Assert.Equal(AppSettingsService.ThemeDark, AppSettingsService.GetTheme());

            AppSettingsService.SetTheme(AppSettingsService.ThemeLight);
            Assert.Equal(AppSettingsService.ThemeLight, AppSettingsService.GetTheme());

            AppSettingsService.SetTheme(AppSettingsService.ThemeSystem);
            Assert.Equal(AppSettingsService.ThemeSystem, AppSettingsService.GetTheme());
        }
        finally
        {
            AppSettingsService.SetTheme(original);
        }
    }

    [Fact]
    public void AppSettingsService_LanguageReadWrite_ShouldPersistCorrectly()
    {
        var original = AppSettingsService.GetLanguage();
        try
        {
            AppSettingsService.SetLanguage(AppSettingsService.LanguageChinese);
            Assert.Equal(AppSettingsService.LanguageChinese, AppSettingsService.GetLanguage());

            AppSettingsService.SetLanguage(AppSettingsService.LanguageEnglish);
            Assert.Equal(AppSettingsService.LanguageEnglish, AppSettingsService.GetLanguage());

            AppSettingsService.SetLanguage(AppSettingsService.LanguageSystem);
            Assert.Equal(AppSettingsService.LanguageSystem, AppSettingsService.GetLanguage());
        }
        finally
        {
            AppSettingsService.SetLanguage(original);
        }
    }

    [Fact]
    public void I18nService_ShouldSwitchLanguagesAndRaiseEvent()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        bool eventFired = false;
        Action handler = () => { eventFired = true; };

        service.LanguageChanged += handler;
        try
        {
            // Switch to English
            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal(AppSettingsService.LanguageEnglish, service.EffectiveLanguage);
            Assert.Equal("Commands", service["Nav.Commands"]);
            Assert.Equal("New Command", service["Commands.New"]);
            Assert.True(eventFired);

            // Switch to Chinese
            eventFired = false;
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal(AppSettingsService.LanguageChinese, service.EffectiveLanguage);
            Assert.Equal("快捷命令", service["Nav.Commands"]);
            Assert.Equal("新建快捷命令", service["Commands.New"]);
            Assert.True(eventFired);

            // Format check
            var formatted = service.Format("Commands.DeleteConfirmContent", "TestCmd");
            Assert.Contains("TestCmd", formatted);
        }
        finally
        {
            service.LanguageChanged -= handler;
            service.CurrentLanguage = original;
        }
    }

    [Fact]
    public void I18nService_SystemDefault_ShouldResolveToValidLanguage()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            service.CurrentLanguage = AppSettingsService.LanguageSystem;
            var effective = service.EffectiveLanguage;
            Assert.True(effective == AppSettingsService.LanguageChinese || effective == AppSettingsService.LanguageEnglish);
            Assert.NotEmpty(service["Nav.Settings"]);
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }

    [Fact]
    public void I18nService_Presets_ShouldReturnBilingualContent()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal("刷新 DNS 缓存", service.GetPresetLocalizedName("preset_flush_dns", "fallback"));
            Assert.Contains("清除本地 DNS 解析缓存", service.GetPresetLocalizedDescription("preset_flush_dns", "fallback"));

            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal("Flush DNS Cache", service.GetPresetLocalizedName("preset_flush_dns", "fallback"));
            Assert.Contains("Flush local DNS resolver cache", service.GetPresetLocalizedDescription("preset_flush_dns", "fallback"));
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }

    [Fact]
    public void I18nService_Categories_ShouldTranslateProperly()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal("全部", service.TranslateCategory("全部"));
            Assert.Equal("网络", service.TranslateCategory("网络"));
            Assert.Equal("开发", service.TranslateCategory("开发"));

            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal("All", service.TranslateCategory("全部"));
            Assert.Equal("Network", service.TranslateCategory("网络"));
            Assert.Equal("Development", service.TranslateCategory("开发"));
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }

    [Fact]
    public void CommandItem_And_CategoryItem_ShouldReactToLanguageChange()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            var cmd = new CmdDock.Core.Models.CommandItem
            {
                Id = "preset_flush_dns",
                Name = "刷新 DNS 缓存",
                Description = "清除本地 DNS 解析缓存，解决域名访问失效与解析异常",
                Group = "网络",
                ShellType = CmdDock.Core.Models.ShellType.Cmd,
                ExecutionMode = CmdDock.Core.Models.ExecutionMode.Silent
            };

            var cat = new CmdDock.Core.Models.CategoryItem
            {
                Name = "网络"
            };

            var log = new CmdDock.Core.Models.ExecutionLogEntry
            {
                Success = true
            };

            // In Chinese
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal("刷新 DNS 缓存", cmd.DisplayName);
            Assert.Equal("网络", cmd.DisplayGroup);
            Assert.Equal("网络", cat.DisplayName);
            Assert.Equal("成功", log.DisplaySuccess);

            // In English
            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal("Flush DNS Cache", cmd.DisplayName);
            Assert.Equal("Network", cmd.DisplayGroup);
            Assert.Equal("Network", cat.DisplayName);
            Assert.Equal("Success", log.DisplaySuccess);
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }
}

