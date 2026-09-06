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
    public const string LanguageEnglish = "en-US";

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
                        if (val == LanguageChinese || val == LanguageEnglish || val == LanguageSystem)
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

                string normalized;
                if (string.Equals(language, LanguageChinese, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = LanguageChinese;
                }
                else if (string.Equals(language, LanguageEnglish, StringComparison.OrdinalIgnoreCase))
                {
                    normalized = LanguageEnglish;
                }
                else
                {
                    normalized = LanguageSystem;
                }

                obj["language"] = normalized;
                File.WriteAllText(AppPaths.AppSettingsFilePath, obj.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }
    }
}
