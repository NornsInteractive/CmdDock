using System.Text.Json;
using System.Text.Json.Nodes;

namespace CmdDock.Core.Services;

public static class WidgetSettings
{
    private static readonly object _lock = new();

    public static string GetViewMode()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("viewMode", out var prop))
                    {
                        var mode = prop.GetString();
                        if (mode == "list" || mode == "grid")
                        {
                            return mode;
                        }
                    }
                }
            }
            catch { }

            return "grid";
        }
    }

    public static void SetViewMode(string mode)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.WidgetSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                obj["viewMode"] = mode;
                File.WriteAllText(AppPaths.WidgetSettingsFilePath, obj.ToJsonString());
            }
            catch { }
        }
    }

    public static string GetSelectedCategory()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("selectedCategory", out var prop))
                    {
                        var cat = prop.GetString();
                        if (!string.IsNullOrWhiteSpace(cat))
                        {
                            return cat;
                        }
                    }
                }
            }
            catch { }

            return "全部";
        }
    }

    public static void SetSelectedCategory(string category)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.WidgetSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                obj["selectedCategory"] = string.IsNullOrWhiteSpace(category) ? "全部" : category.Trim();
                File.WriteAllText(AppPaths.WidgetSettingsFilePath, obj.ToJsonString());
            }
            catch { }
        }
    }

    public const string LayoutSidebar = "sidebar";
    public const string LayoutDropdown = "dropdown";

    // Legacy aliases for backward compatibility
    public const string LayoutSegmented = "dropdown";
    public const string LayoutIsland = "dropdown";
    public const string LayoutLaunchPad = "dropdown";

    public const string PaginationInline = "inline";
    public const string PaginationBottom = "bottom";

    public static string GetLayoutMode()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("layoutMode", out var prop))
                    {
                        var m = prop.GetString();
                        if (m == LayoutSidebar || m == LayoutDropdown)
                        {
                            return m;
                        }
                        if (m == "segmented" || m == "island" || m == "launchpad")
                        {
                            return LayoutDropdown;
                        }
                    }
                }
            }
            catch { }

            return LayoutSidebar; // Default to Left Sidebar Menu
        }
    }

    public static void SetLayoutMode(string layoutMode)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.WidgetSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                obj["layoutMode"] = layoutMode;
                File.WriteAllText(AppPaths.WidgetSettingsFilePath, obj.ToJsonString());
            }
            catch { }
        }
    }

    public static string GetPaginationStyle()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("paginationStyle", out var prop))
                    {
                        var p = prop.GetString();
                        if (p == PaginationInline || p == PaginationBottom)
                        {
                            return p;
                        }
                    }
                }
            }
            catch { }

            return PaginationInline; // Default: space-saving inline pager
        }
    }

    public static void SetPaginationStyle(string paginationStyle)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.WidgetSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                obj["paginationStyle"] = paginationStyle;
                File.WriteAllText(AppPaths.WidgetSettingsFilePath, obj.ToJsonString());
            }
            catch { }
        }
    }

    public static int GetPageSize(string layoutMode, string widgetSize, string viewMode = "grid")
    {
        var size = widgetSize.ToLowerInvariant();
        if (size == "small")
        {
            return 4; // 2 rows x 2 cols
        }

        bool isList = string.Equals(viewMode, "list", StringComparison.OrdinalIgnoreCase);

        if (size == "large")
        {
            return isList ? 9 : 18; // 9 rows x 2 cols
        }

        // Medium
        return isList ? 5 : 10; // 5 rows x 2 cols
    }

    public static int GetCategoryPageSize(string layoutMode, string widgetSize)
    {
        // Dropdown contains all categories in its native combobox
        if (layoutMode == LayoutDropdown)
        {
            return int.MaxValue;
        }

        var size = widgetSize.ToLowerInvariant();
        if (size == "small")
        {
            return 0; // Small widget has no categories
        }

        if (size == "large")
        {
            return 8; // Balances 9 rows of commands
        }

        // Medium
        return 5; // Balances 5 rows of commands
    }

    public static List<string> GetSmallWidgetCommandIds()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var json = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    using var doc = JsonDocument.Parse(json);
                    if (doc.RootElement.TryGetProperty("smallWidgetCommandIds", out var prop) && prop.ValueKind == JsonValueKind.Array)
                    {
                        var list = new List<string>();
                        foreach (var item in prop.EnumerateArray())
                        {
                            var s = item.GetString();
                            if (!string.IsNullOrEmpty(s)) list.Add(s);
                        }
                        return list;
                    }
                }
            }
            catch { }

            return new List<string>();
        }
    }

    public static void SetSmallWidgetCommandIds(IEnumerable<string> ids)
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(AppPaths.WidgetSettingsFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                JsonObject obj;
                if (File.Exists(AppPaths.WidgetSettingsFilePath))
                {
                    var text = File.ReadAllText(AppPaths.WidgetSettingsFilePath);
                    obj = JsonNode.Parse(text)?.AsObject() ?? new JsonObject();
                }
                else
                {
                    obj = new JsonObject();
                }

                var arr = new JsonArray();
                foreach (var id in ids.Take(4))
                {
                    arr.Add(id);
                }

                obj["smallWidgetCommandIds"] = arr;
                File.WriteAllText(AppPaths.WidgetSettingsFilePath, obj.ToJsonString());
            }
            catch { }
        }
    }
}
