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

    [Fact]
    public void I18nService_PrivacyKeys_ShouldReturnValidBilingualContent()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            // Chinese
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal("隐私政策与数据安全声明", service["Settings.PrivacyCardTitle"]);
            Assert.Contains("遵循纯本地运行安全原则", service["Settings.PrivacyCardSubtitle"]);
            Assert.Equal("🛡️ 零数据收集", service["Settings.PrivacyPoint1Title"]);
            Assert.Equal("💾 100% 本地存储", service["Settings.PrivacyPoint2Title"]);
            Assert.Equal("🚫 无遥测与追踪", service["Settings.PrivacyPoint3Title"]);
            Assert.Equal("⚡ 权限与命令受控", service["Settings.PrivacyPoint4Title"]);
            Assert.Equal("🗑️ 卸载彻底清除", service["Settings.PrivacyPoint5Title"]);
            Assert.Equal("展开查看完整条款细则", service["Settings.PrivacyToggleExpand"]);
            Assert.Equal("收起完整条款细则", service["Settings.PrivacyToggleCollapse"]);
            Assert.Contains("数据收集与使用", service["Privacy.FullText"]);

            // English
            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal("Privacy Policy & Data Security", service["Settings.PrivacyCardTitle"]);
            Assert.Contains("CmdDock operates strictly on your local device", service["Settings.PrivacyCardSubtitle"]);
            Assert.Equal("🛡️ Zero Data Collection", service["Settings.PrivacyPoint1Title"]);
            Assert.Equal("💾 100% Local Storage", service["Settings.PrivacyPoint2Title"]);
            Assert.Equal("🚫 Zero Telemetry & Tracking", service["Settings.PrivacyPoint3Title"]);
            Assert.Equal("⚡ Controlled Execution", service["Settings.PrivacyPoint4Title"]);
            Assert.Equal("🗑️ Clean Removal", service["Settings.PrivacyPoint5Title"]);
            Assert.Equal("Expand Full Legal Policy", service["Settings.PrivacyToggleExpand"]);
            Assert.Equal("Collapse Full Legal Policy", service["Settings.PrivacyToggleCollapse"]);
            Assert.Contains("Data Collection and Usage", service["Privacy.FullText"]);
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }

    [Fact]
    public void I18nService_StatusMessages_ShouldSupportBilingualAndFormatting()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            // Chinese
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal("就绪", service["Status.Ready"]);
            Assert.Equal("正在执行: 清理系统临时文件...", service.Format("Status.Executing", "清理系统临时文件"));
            Assert.Equal("✓ 清理系统临时文件 执行成功 (120ms)", service.Format("Status.ExecSuccess", "清理系统临时文件", 120));
            Assert.Equal("✗ 清理系统临时文件 执行失败 (代码 1)", service.Format("Status.ExecFailed", "清理系统临时文件", 1));
            Assert.Equal("已删除命令: 测试", service.Format("Status.Deleted", "测试"));
            Assert.Equal("已添加到小组件: 测试", service.Format("Status.AddedToWidget", "测试"));
            Assert.Equal("已从小组件隐藏: 测试", service.Format("Status.HiddenFromWidget", "测试"));
            Assert.Equal("已添加预设命令: 测试 并同步至小组件", service.Format("Status.PresetAdded", "测试"));
            Assert.Equal("已清空执行日志", service["Status.LogsCleared"]);
            Assert.Equal("已保存命令: 测试", service.Format("Status.Saved", "测试"));

            // English
            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal("Ready", service["Status.Ready"]);
            Assert.Equal("Executing: Clean Temp Files...", service.Format("Status.Executing", "Clean Temp Files"));
            Assert.Equal("✓ Clean Temp Files executed successfully (120ms)", service.Format("Status.ExecSuccess", "Clean Temp Files", 120));
            Assert.Equal("✗ Clean Temp Files failed (code 1)", service.Format("Status.ExecFailed", "Clean Temp Files", 1));
            Assert.Equal("Deleted command: Test", service.Format("Status.Deleted", "Test"));
            Assert.Equal("Added to widget: Test", service.Format("Status.AddedToWidget", "Test"));
            Assert.Equal("Hidden from widget: Test", service.Format("Status.HiddenFromWidget", "Test"));
            Assert.Equal("Preset command added: Test and synced to widget", service.Format("Status.PresetAdded", "Test"));
            Assert.Equal("Execution history logs cleared", service["Status.LogsCleared"]);
            Assert.Equal("Saved command: Test", service.Format("Status.Saved", "Test"));
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }

    [Fact]
    public void CommandItem_WithGuidId_ShouldResolvePresetLocalizedNameAndDescription()
    {
        var service = I18nService.Instance;
        var original = service.CurrentLanguage;
        try
        {
            // Simulate an item created with a GUID but based on preset Clean Temp Files
            var cmd = new CmdDock.Core.Models.CommandItem
            {
                Id = "e90461e131074fbd8e06df7a7f95c1e0",
                Name = "清理系统临时文件",
                Description = "深度清空当前用户的临时垃圾与缓存目录，释放磁盘空间"
            };

            // Chinese
            service.CurrentLanguage = AppSettingsService.LanguageChinese;
            Assert.Equal("清理系统临时文件", cmd.DisplayName);
            Assert.Equal("深度清空当前用户的临时垃圾与缓存目录，释放磁盘空间", cmd.DisplayDescription);

            // English
            service.CurrentLanguage = AppSettingsService.LanguageEnglish;
            Assert.Equal("Clean Temp Files", cmd.DisplayName);
            Assert.Contains("Deep clean current user temporary cache", cmd.DisplayDescription);
        }
        finally
        {
            service.CurrentLanguage = original;
        }
    }
}

