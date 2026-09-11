using System.Text.Json;
using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public class MiniDockSettingsService
{
    private static readonly object _lock = new();
    private static MiniDockSettingsService? _instance;
    public static MiniDockSettingsService Instance => _instance ??= new MiniDockSettingsService();

    private MiniDockSettings? _cachedSettings;

    public event Action<MiniDockSettings>? SettingsChanged;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public MiniDockSettings LoadSettings()
    {
        lock (_lock)
        {
            if (_cachedSettings != null)
            {
                return _cachedSettings;
            }

            try
            {
                if (File.Exists(AppPaths.DockSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.DockSettingsFilePath);
                    var settings = JsonSerializer.Deserialize<MiniDockSettings>(json, _jsonOptions);
                    if (settings != null)
                    {
                        _cachedSettings = settings;
                        return _cachedSettings;
                    }
                }
            }
            catch
            {
                // Fall back to default
            }

            _cachedSettings = new MiniDockSettings();
            return _cachedSettings;
        }
    }

    public void SaveSettings(MiniDockSettings settings)
    {
        lock (_lock)
        {
            _cachedSettings = settings;
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.DockSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(AppPaths.DockSettingsFilePath, json);
            }
            catch
            {
                // Ignore transient write errors
            }
        }

        SettingsChanged?.Invoke(settings);
    }
}
