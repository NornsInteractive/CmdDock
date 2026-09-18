using System.Text.Json;
using System.Text.Json.Nodes;

namespace CmdDock.Core.Services;

public static class AppSettingsService
{
    private static readonly object _lock = new();

    public const string ThemeSystem = "system";
    public const string ThemeLight = "light";
    public const string ThemeDark = "dark";

    public const string LanguageSystem = "system";
    public const string LanguageChinese = "zh-CN";
    public const string LanguageTraditionalChinese = "zh-TW";
    public const string LanguageEnglish = "en-US";
    public const string LanguageJapanese = "ja-JP";
    public const string LanguageKorean = "ko-KR";
    public const string LanguageGerman = "de-DE";
    public const string LanguageFrench = "fr-FR";
    public const string LanguageSpanish = "es-ES";
    public const string LanguageItalian = "it-IT";
    public const string LanguagePortuguese = "pt-BR";
    public const string LanguageRussian = "ru-RU";

    public static readonly HashSet<string> SupportedLanguages = new(StringComparer.OrdinalIgnoreCase)
    {
        LanguageSystem,
        LanguageChinese,
        LanguageTraditionalChinese,
        LanguageEnglish,
        LanguageJapanese,
        LanguageKorean,
        LanguageGerman,
        LanguageFrench,
        LanguageSpanish,
        LanguageItalian,
        LanguagePortuguese,
        LanguageRussian
    };

    public static string GetTheme()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.AppSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.AppSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("theme", out var prop))
                    {
                        var val = prop.GetString();
                        if (val == ThemeLight || val == ThemeDark || val == ThemeSystem)
                        {
                            return val;
                        }
                    }
                }
            }
            catch { }

            return ThemeSystem;
        }
    }

    public static void SetTheme(string theme)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.AppSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.AppSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.AppSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                var normalized = theme.ToLowerInvariant();
                if (normalized != ThemeLight && normalized != ThemeDark)
                {
                    normalized = ThemeSystem;
                }

                obj["theme"] = normalized;
                File.WriteAllText(AppPaths.AppSettingsFilePath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }

    public static string GetLanguage()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.AppSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.AppSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("language", out var prop))
                    {
                        var val = prop.GetString();
                        if (!string.IsNullOrEmpty(val) && SupportedLanguages.Contains(val))
                        {
                            return val;
                        }
                    }
                }
            }
            catch { }

            return LanguageSystem;
        }
    }

    public static void SetLanguage(string language)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.AppSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.AppSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.AppSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                var normalized = SupportedLanguages.Contains(language) ? language : LanguageSystem;
                obj["language"] = normalized;
                File.WriteAllText(AppPaths.AppSettingsFilePath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}
