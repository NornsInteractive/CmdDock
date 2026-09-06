using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock.Widget;
using System.Text.Json;

namespace CmdDock.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CategoryService_DefaultAndCrudOperations()
    {
        var tempCatFile = Path.Combine(Path.GetTempPath(), $"cmddock_cat_{Guid.NewGuid():N}.json");

        try
        {
            var categoryService = new CategoryService(tempCatFile);
            var categories = await categoryService.GetAllCategoriesAsync();

            Assert.NotNull(categories);
            Assert.Contains("常用", categories);
            Assert.Contains("系统", categories);
            Assert.Contains("网络", categories);

            // Add new category
            await categoryService.AddCategoryAsync("测试分类");
            categories = await categoryService.GetAllCategoriesAsync(forceReload: true);
            Assert.Contains("测试分类", categories);

            // Rename category
            await categoryService.RenameCategoryAsync("测试分类", "重命名分类");
            categories = await categoryService.GetAllCategoriesAsync(forceReload: true);
            Assert.DoesNotContain("测试分类", categories);
            Assert.Contains("重命名分类", categories);

            // Delete category
            await categoryService.DeleteCategoryAsync("重命名分类");
            categories = await categoryService.GetAllCategoriesAsync(forceReload: true);
            Assert.DoesNotContain("重命名分类", categories);
        }
        finally
        {
            if (File.Exists(tempCatFile)) File.Delete(tempCatFile);
        }
    }

    [Fact]
    public async Task CategoryService_CategoryItem_IconsAndMigration()
    {
        var tempCatFile = Path.Combine(Path.GetTempPath(), $"cmddock_cat_item_{Guid.NewGuid():N}.json");

        try
        {
            // 1. Write legacy string[] format
            var legacyJson = JsonSerializer.Serialize(new List<string> { "常用", "系统", "开发" });
            await File.WriteAllTextAsync(tempCatFile, legacyJson);

            var service = new CategoryService(tempCatFile);

            // 2. Load should automatically migrate to CategoryItem[] with default icons
            var items = await service.GetAllCategoryItemsAsync();
            Assert.Equal(3, items.Count);
            Assert.Equal("常用", items[0].Name);
            Assert.Equal("\uE735", items[0].IconGlyph);
            Assert.Equal("系统", items[1].Name);
            Assert.Equal("\uE770", items[1].IconGlyph);
            Assert.Equal("开发", items[2].Name);
            Assert.Equal("\uE943", items[2].IconGlyph);

            // 3. Update category icon
            await service.UpdateCategoryIconAsync("开发", "EB9F");
            items = await service.GetAllCategoryItemsAsync(forceReload: true);
            var dev = items.First(i => i.Name == "开发");
            Assert.Equal("\uEB9F", dev.IconGlyph);

            // 4. Update category name and icon together
            await service.UpdateCategoryAsync("系统", "核心系统", "E7F4");
            items = await service.GetAllCategoryItemsAsync(forceReload: true);
            Assert.Null(items.FirstOrDefault(i => i.Name == "系统"));
            var coreSys = items.FirstOrDefault(i => i.Name == "核心系统");
            Assert.NotNull(coreSys);
            Assert.Equal("\uE7F4", coreSys.IconGlyph);

            // 5. Add with custom icon
            await service.AddCategoryAsync("自定义", "E700");
            items = await service.GetAllCategoryItemsAsync(forceReload: true);
            var custom = items.FirstOrDefault(i => i.Name == "自定义");
            Assert.NotNull(custom);
            Assert.Equal("\uE700", custom.IconGlyph);
        }
        finally
        {
            if (File.Exists(tempCatFile)) File.Delete(tempCatFile);
        }
    }

    [Fact]
    public void WidgetCardBuilder_CategoryFilter_FiltersCommandsCorrectly()
    {
        var commands = new List<CommandItem>
        {
            new() { Id = "1", Name = "DNS Flush", Group = "网络" },
            new() { Id = "2", Name = "Ping Test", Group = "网络" },
            new() { Id = "3", Name = "Restart Explorer", Group = "系统" },
            new() { Id = "4", Name = "Clean Temp", Group = "系统" }
        };

        // 1. Filter by "网络"
        var netCard = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "list", "网络", layoutMode: WidgetSettings.LayoutSidebar);
        using var netDoc = JsonDocument.Parse(netCard);
        var netItems = netDoc.RootElement.GetProperty("body")[0].GetProperty("columns")[1].GetProperty("items");

        var netCmds = netItems.EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "Container" &&
                        e.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "runCommand")
            .ToList();

        Assert.Equal(2, netCmds.Count);

        // 2. Filter by "全部"
        var allCard = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "list", "全部", layoutMode: WidgetSettings.LayoutSidebar);
        using var allDoc = JsonDocument.Parse(allCard);
        var allItems = allDoc.RootElement.GetProperty("body")[0].GetProperty("columns")[1].GetProperty("items");

        var allCmds = allItems.EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "Container" &&
                        e.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "runCommand")
            .ToList();

        Assert.Equal(4, allCmds.Count);

        // 3. Filter by non-existent category -> Empty state message
        var emptyCard = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "list", "不存在的分类", layoutMode: WidgetSettings.LayoutSidebar);
        using var emptyDoc = JsonDocument.Parse(emptyCard);
        var emptyItems = emptyDoc.RootElement.GetProperty("body")[0].GetProperty("columns")[1].GetProperty("items");

        var emptyCmds = emptyItems.EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "Container" &&
                        e.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "runCommand")
            .ToList();

        Assert.Empty(emptyCmds);
    }
}
