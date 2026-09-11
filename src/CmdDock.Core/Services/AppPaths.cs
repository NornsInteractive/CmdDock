namespace CmdDock.Core.Services;

public static class AppPaths
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "CmdDock");

    public static string BaseDirectory
    {
        get
        {
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
            }
            return AppDataFolder;
        }
    }

    public static string CommandsFilePath => Path.Combine(BaseDirectory, "commands.json");

    public static string CategoriesFilePath => Path.Combine(BaseDirectory, "categories.json");

    public static string WidgetSettingsFilePath => Path.Combine(BaseDirectory, "widget_settings.json");

    public static string AppSettingsFilePath => Path.Combine(BaseDirectory, "app_settings.json");

    public static string DockSettingsFilePath => Path.Combine(BaseDirectory, "dock_settings.json");

    public static string LogsDirectory
    {
        get
        {
            var logsDir = Path.Combine(BaseDirectory, "logs");
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            return logsDir;
        }
    }

    public static string ExecutionLogFilePath => Path.Combine(LogsDirectory, "execution.jsonl");
}
