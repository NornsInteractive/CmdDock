using CmdDock.Core.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CmdDock.Core.Services;

public class CategoryService : ICategoryService
{
    private readonly string _filePath;
    private readonly ICommandService? _commandService;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private List<CategoryItem>? _cachedCategoryItems;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public static readonly IReadOnlyList<string> DefaultCategories = new[]
    {
        "常用", "系统", "网络", "开发", "运维"
    };

    public static readonly IReadOnlyList<(string Name, string IconGlyph)> DefaultCategoryDefinitions = new[]
    {
        ("常用", "\uE735"), // Favorite Star
        ("系统", "\uE770"), // System Laptop
        ("网络", "\uE701"), // Globe / Network
        ("开发", "\uE943"), // Code
        ("运维", "\uE912")  // Server / Cloud
    };

    public static string GetDefaultIconForCategory(string categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName)) return "\uE8EC";
        var name = categoryName.Trim();
        foreach (var (defName, icon) in DefaultCategoryDefinitions)
        {
            if (string.Equals(defName, name, StringComparison.OrdinalIgnoreCase))
            {
                return icon;
            }
        }

        if (name.Contains("全")) return "\uE8B9";
        if (name.Contains("星") || name.Contains("常") || name.Contains("收藏")) return "\uE735";
        if (name.Contains("系统") || name.Contains("硬件") || name.Contains("电脑") || name.Contains("机")) return "\uE770";
        if (name.Contains("网") || name.Contains("wifi") || name.Contains("ip") || name.Contains("通信")) return "\uE701";
        if (name.Contains("开发") || name.Contains("代码") || name.Contains("dev") || name.Contains("git")) return "\uE943";
        if (name.Contains("运维") || name.Contains("服务器") || name.Contains("云") || name.Contains("容器") || name.Contains("docker")) return "\uE912";
        if (name.Contains("工具") || name.Contains("实用")) return "\uE8B9";

        return "\uE8EC"; // Folder
    }

    public CategoryService(string? customFilePath = null, ICommandService? commandService = null)
    {
        _filePath = string.IsNullOrWhiteSpace(customFilePath) ? AppPaths.CategoriesFilePath : customFilePath;
        _commandService = commandService;
    }

    public async Task<List<CategoryItem>> GetAllCategoryItemsAsync(bool forceReload = false)
    {
        if (_cachedCategoryItems != null && !forceReload)
        {
            return _cachedCategoryItems.Select(c => c.Clone()).ToList();
        }

        await _lock.WaitAsync();
        try
        {
            if (File.Exists(_filePath))
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                var text = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(text))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(text);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            var elements = doc.RootElement.EnumerateArray().ToList();
                            if (elements.Count > 0 && elements[0].ValueKind == JsonValueKind.String)
                            {
                                // Legacy string[] format: ["常用", "系统", ...] -> Migrate to CategoryItem[]
                                var oldStrings = JsonSerializer.Deserialize<List<string>>(text, JsonOptions) ?? new();
                                var migrated = new List<CategoryItem>();
                                int order = 0;
                                foreach (var s in oldStrings)
                                {
                                    if (string.IsNullOrWhiteSpace(s)) continue;
                                    var clean = s.Trim();
                                    if (migrated.Any(m => string.Equals(m.Name, clean, StringComparison.OrdinalIgnoreCase))) continue;
                                    migrated.Add(new CategoryItem
                                    {
                                        Id = Guid.NewGuid().ToString("N"),
                                        Name = clean,
                                        IconGlyph = GetDefaultIconForCategory(clean),
                                        Order = order++
                                    });
                                }
                                _cachedCategoryItems = migrated;
                                await SaveCategoriesInternalAsync(migrated);
                                return _cachedCategoryItems.Select(c => c.Clone()).ToList();
                            }
                            else if (elements.Count > 0 && elements[0].ValueKind == JsonValueKind.Object)
                            {
                                var loaded = JsonSerializer.Deserialize<List<CategoryItem>>(text, JsonOptions);
                                if (loaded != null && loaded.Count > 0)
                                {
                                    bool normalizedAny = false;
                                    foreach (var c in loaded)
                                    {
                                        if (!string.IsNullOrWhiteSpace(c.IconGlyph) && !File.Exists(c.IconGlyph))
                                        {
                                            var norm = CommandItem.NormalizeToSegoeGlyph(c.IconGlyph);
                                            if (norm != c.IconGlyph)
                                            {
                                                c.IconGlyph = norm;
                                                normalizedAny = true;
                                            }
                                        }
                                    }
                                    if (normalizedAny)
                                    {
                                        await SaveCategoriesInternalAsync(loaded);
                                    }
                                    _cachedCategoryItems = loaded.OrderBy(c => c.Order).ToList();
                                    return _cachedCategoryItems.Select(c => c.Clone()).ToList();
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Fall through to defaults if json corrupted
                    }
                }
            }

            // Seed defaults
            var initial = new List<CategoryItem>();
            int seedOrder = 0;
            foreach (var (name, icon) in DefaultCategoryDefinitions)
            {
                initial.Add(new CategoryItem
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Name = name,
                    IconGlyph = icon,
                    Order = seedOrder++
                });
            }

            // Also check commands.json for any existing categories
            try
            {
                if (File.Exists(AppPaths.CommandsFilePath))
                {
                    using var cmdFs = new FileStream(AppPaths.CommandsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var cmdReader = new StreamReader(cmdFs, System.Text.Encoding.UTF8);
                    var cmdText = await cmdReader.ReadToEndAsync();
                    var cmds = JsonSerializer.Deserialize<List<CommandItem>>(cmdText, JsonOptions);
                    if (cmds != null)
                    {
                        foreach (var c in cmds)
                        {
                            if (!string.IsNullOrWhiteSpace(c.Group) &&
                                !initial.Any(i => string.Equals(i.Name, c.Group.Trim(), StringComparison.OrdinalIgnoreCase)))
                            {
                                var groupName = c.Group.Trim();
                                initial.Add(new CategoryItem
                                {
                                    Id = Guid.NewGuid().ToString("N"),
                                    Name = groupName,
                                    IconGlyph = GetDefaultIconForCategory(groupName),
                                    Order = seedOrder++
                                });
                            }
                        }
                    }
                }
            }
            catch { }

            _cachedCategoryItems = initial;
            await SaveCategoriesInternalAsync(initial);
            return _cachedCategoryItems.Select(c => c.Clone()).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<List<string>> GetAllCategoriesAsync(bool forceReload = false)
    {
        var items = await GetAllCategoryItemsAsync(forceReload);
        return items.Select(i => i.Name).ToList();
    }

    public async Task<CategoryItem?> GetCategoryByNameAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        var items = await GetAllCategoryItemsAsync();
        return items.FirstOrDefault(c => string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public async Task AddCategoryAsync(string category, string? iconGlyph = null)
    {
        if (string.IsNullOrWhiteSpace(category)) return;
        var clean = category.Trim();

        var items = await GetAllCategoryItemsAsync();
        if (items.Any(c => string.Equals(c.Name, clean, StringComparison.OrdinalIgnoreCase))) return;

        items.Add(new CategoryItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = clean,
            IconGlyph = string.IsNullOrWhiteSpace(iconGlyph) ? GetDefaultIconForCategory(clean) : iconGlyph.Trim(),
            Order = items.Count
        });

        await SaveCategoriesAsync(items);
    }

    public async Task RenameCategoryAsync(string oldName, string newName)
    {
        await UpdateCategoryAsync(oldName, newName, null);
    }

    public async Task UpdateCategoryAsync(string oldName, string newName, string? newIconGlyph = null)
    {
        if (string.IsNullOrWhiteSpace(oldName)) return;
        var oldClean = oldName.Trim();
        var newClean = string.IsNullOrWhiteSpace(newName) ? oldClean : newName.Trim();

        var items = await GetAllCategoryItemsAsync();
        var target = items.FirstOrDefault(c => string.Equals(c.Name, oldClean, StringComparison.OrdinalIgnoreCase));
        if (target == null)
        {
            await AddCategoryAsync(newClean, newIconGlyph);
            return;
        }

        bool nameChanged = !string.Equals(oldClean, newClean, StringComparison.OrdinalIgnoreCase);
        target.Name = newClean;
        if (!string.IsNullOrWhiteSpace(newIconGlyph))
        {
            target.IconGlyph = newIconGlyph.Trim();
        }

        await SaveCategoriesAsync(items);

        if (nameChanged)
        {
            await UpdateCommandsCategoryAsync(oldClean, newClean);
        }
    }

    public async Task UpdateCategoryIconAsync(string name, string iconGlyph)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(iconGlyph)) return;
        var clean = name.Trim();

        var items = await GetAllCategoryItemsAsync();
        var target = items.FirstOrDefault(c => string.Equals(c.Name, clean, StringComparison.OrdinalIgnoreCase));
        if (target != null)
        {
            target.IconGlyph = iconGlyph.Trim();
            await SaveCategoriesAsync(items);
        }
    }

    public async Task DeleteCategoryAsync(string category, string fallbackCategory = "常用")
    {
        if (string.IsNullOrWhiteSpace(category)) return;
        var clean = category.Trim();
        var fallbackClean = string.IsNullOrWhiteSpace(fallbackCategory) ? "常用" : fallbackCategory.Trim();

        var items = await GetAllCategoryItemsAsync();
        items.RemoveAll(c => string.Equals(c.Name, clean, StringComparison.OrdinalIgnoreCase));
        if (items.Count == 0)
        {
            items.Add(new CategoryItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = fallbackClean,
                IconGlyph = GetDefaultIconForCategory(fallbackClean),
                Order = 0
            });
        }
        else if (!items.Any(c => string.Equals(c.Name, fallbackClean, StringComparison.OrdinalIgnoreCase)))
        {
            items.Insert(0, new CategoryItem
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = fallbackClean,
                IconGlyph = GetDefaultIconForCategory(fallbackClean),
                Order = 0
            });
        }

        // Re-index orders
        for (int i = 0; i < items.Count; i++)
        {
            items[i].Order = i;
        }

        await SaveCategoriesAsync(items);
        await UpdateCommandsCategoryAsync(clean, fallbackClean);
    }

    public async Task ReorderCategoriesAsync(List<string> orderedCategories)
    {
        if (orderedCategories == null || orderedCategories.Count == 0) return;
        var cleanNames = orderedCategories.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => c.Trim()).Distinct().ToList();

        var items = await GetAllCategoryItemsAsync();
        var reordered = new List<CategoryItem>();
        int order = 0;

        foreach (var name in cleanNames)
        {
            var found = items.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            if (found != null)
            {
                found.Order = order++;
                reordered.Add(found);
            }
        }

        // Any remaining items not in the list
        foreach (var item in items)
        {
            if (!reordered.Any(r => string.Equals(r.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
            {
                item.Order = order++;
                reordered.Add(item);
            }
        }

        await SaveCategoriesAsync(reordered);
    }

    public async Task SaveCategoriesAsync(List<CategoryItem> categories)
    {
        await _lock.WaitAsync();
        try
        {
            await SaveCategoriesInternalAsync(categories);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task SaveCategoriesInternalAsync(List<CategoryItem> categories)
    {
        _cachedCategoryItems = categories.OrderBy(c => c.Order).ToList();
        var json = JsonSerializer.Serialize(_cachedCategoryItems, JsonOptions);
        var dir = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        using var fs = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        using var writer = new StreamWriter(fs, System.Text.Encoding.UTF8);
        await writer.WriteAsync(json);
    }

    private async Task UpdateCommandsCategoryAsync(string targetCategory, string newCategory)
    {
        try
        {
            if (_commandService != null)
            {
                var commands = await _commandService.GetAllAsync(forceReload: true);
                bool anyChanged = false;
                foreach (var cmd in commands)
                {
                    if (string.Equals(cmd.Group, targetCategory, StringComparison.OrdinalIgnoreCase))
                    {
                        cmd.Group = newCategory;
                        anyChanged = true;
                    }
                }
                if (anyChanged)
                {
                    foreach (var cmd in commands)
                    {
                        await _commandService.AddOrUpdateAsync(cmd);
                    }
                }
            }
            else if (File.Exists(AppPaths.CommandsFilePath))
            {
                using var fs = new FileStream(AppPaths.CommandsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
                var cmdText = await reader.ReadToEndAsync();
                var cmds = JsonSerializer.Deserialize<List<CommandItem>>(cmdText, JsonOptions);
                if (cmds != null)
                {
                    bool anyChanged = false;
                    foreach (var cmd in cmds)
                    {
                        if (string.Equals(cmd.Group, targetCategory, StringComparison.OrdinalIgnoreCase))
                        {
                            cmd.Group = newCategory;
                            anyChanged = true;
                        }
                    }
                    if (anyChanged)
                    {
                        var json = JsonSerializer.Serialize(cmds, JsonOptions);
                        using var outFs = new FileStream(AppPaths.CommandsFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                        using var writer = new StreamWriter(outFs, System.Text.Encoding.UTF8);
                        await writer.WriteAsync(json);
                    }
                }
            }
        }
        catch { }
    }
}
