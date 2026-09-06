using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock.Widget;
using System.Text.Json;
using Xunit;

namespace CmdDock.Tests;

public class CoreTests
{
    [Fact]
    public void PresetService_ShouldProvideBuiltinPresets()
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets();
        var groups = presetService.GetDefaultGroups();

        Assert.NotEmpty(presets);
        Assert.NotEmpty(groups);
        Assert.Contains(presets, p => p.Name.Contains("DNS"));
        Assert.Contains(presets, p => p.Name.Contains("资源管理器"));
        Assert.Contains(groups, g => g == "常用");
        Assert.Contains(groups, g => g == "网络");
    }

    [Fact]
    public async Task CommandExecutor_ShouldExecuteCmdSuccessfully()
    {
        var commandService = new CommandService();
        var logService = new LogService();
        var executor = new CommandExecutor(commandService, logService);

        var testItem = new CommandItem
        {
            Name = "Echo Test",
            ShellType = ShellType.Cmd,
            CommandText = "echo CmdDockTest123",
            ExecutionMode = ExecutionMode.Silent
        };

        var result = await executor.ExecuteAsync(testItem);

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("CmdDockTest123", result.StdOut);
        Assert.True(result.DurationMs >= 0);
    }

    [Fact]
    public async Task CommandExecutor_ShouldHandleNonZeroExitCode()
    {
        var commandService = new CommandService();
        var logService = new LogService();
        var executor = new CommandExecutor(commandService, logService);

        var failItem = new CommandItem
        {
            Name = "Fail Test",
            ShellType = ShellType.Cmd,
            CommandText = "exit 7",
            ExecutionMode = ExecutionMode.Silent
        };

        var result = await executor.ExecuteAsync(failItem);

        Assert.False(result.Success);
        Assert.Equal(7, result.ExitCode);
    }

    [Fact]
    public void WidgetCardBuilder_ShouldGenerateValidAdaptiveCards()
    {
        var smallTemplate = WidgetCardBuilder.GetTemplateForSize("small");
        var mediumTemplate = WidgetCardBuilder.GetTemplateForSize("medium");
        var largeTemplate = WidgetCardBuilder.GetTemplateForSize("large");

        // Verify JSON parse
        using var smallDoc = JsonDocument.Parse(smallTemplate);
        using var mediumDoc = JsonDocument.Parse(mediumTemplate);
        using var largeDoc = JsonDocument.Parse(largeTemplate);

        Assert.Equal("AdaptiveCard", smallDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("AdaptiveCard", mediumDoc.RootElement.GetProperty("type").GetString());
        Assert.Equal("AdaptiveCard", largeDoc.RootElement.GetProperty("type").GetString());

        var testCommands = new List<CommandItem>
        {
            new() { Name = "Test 1", CommandText = "dir" },
            new() { Name = "Test 2", CommandText = "cls" }
        };

        var dataJson = WidgetCardBuilder.BuildDataPayload(testCommands, "就绪测试");
        using var dataDoc = JsonDocument.Parse(dataJson);

        Assert.Equal("就绪测试", dataDoc.RootElement.GetProperty("statusText").GetString());
        Assert.Equal(2, dataDoc.RootElement.GetProperty("commandCount").GetInt32());
    }

    [Fact]
    public void WidgetCardBuilder_BuildCard_ShouldGenerateGridForMediumAndDisplayAllCommands()
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets();
        Assert.Equal(19, presets.Count);

        var cardJson = WidgetCardBuilder.BuildCard("medium", presets, "全部就绪", layoutMode: WidgetSettings.LayoutSidebar);
        using var doc = JsonDocument.Parse(cardJson);

        Assert.Equal("AdaptiveCard", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("1.5", doc.RootElement.GetProperty("version").GetString());

        var body = doc.RootElement.GetProperty("body");
        
        // Check Header toolbar: Top-right transparent icons (View Toggle & Manage App)
        var mainColSet = body[0];
        Assert.Equal("ColumnSet", mainColSet.GetProperty("type").GetString());
        var mainCols = mainColSet.GetProperty("columns");
        Assert.Equal(2, mainCols.GetArrayLength());

        var leftCol = mainCols[0];
        Assert.Equal("auto", leftCol.GetProperty("width").GetString());
        var leftItems = leftCol.GetProperty("items");

        // Toolbar (top of left sidebar)
        var toolbar = leftItems[0];
        var toolbarCols = toolbar.GetProperty("columns");
        var toggleCol = toolbarCols.EnumerateArray().First(c => c.TryGetProperty("selectAction", out var sa) && sa.GetProperty("verb").GetString() == "toggleView");
        Assert.Equal("auto", toggleCol.GetProperty("width").GetString());
        var toggleImage = toggleCol.GetProperty("items")[0];
        Assert.Equal("Image", toggleImage.GetProperty("type").GetString());
        Assert.StartsWith("data:image/png;base64,", toggleImage.GetProperty("url").GetString());

        var manageCol = toolbarCols.EnumerateArray().First(c => c.TryGetProperty("selectAction", out var sa) && sa.GetProperty("verb").GetString() == "openApp");
        Assert.Equal("auto", manageCol.GetProperty("width").GetString());
        var manageImage = manageCol.GetProperty("items")[0];
        Assert.Equal("Image", manageImage.GetProperty("type").GetString());
        Assert.StartsWith("data:image/png;base64,", manageImage.GetProperty("url").GetString());

        // Category items in left sidebar
        var catItem = leftItems.EnumerateArray().First(i => i.TryGetProperty("selectAction", out var sa) && sa.GetProperty("verb").GetString() == "filterCategory");
        Assert.True(catItem.ValueKind != JsonValueKind.Undefined);

        // Right column: Commands
        var rightCol = mainCols[1];
        Assert.Equal("stretch", rightCol.GetProperty("width").GetString());
        Assert.True(rightCol.GetProperty("separator").GetBoolean());
        var rightItems = rightCol.GetProperty("items");

        int rowCount = 0;
        int actionCount = 0;
        for (int i = 0; i < rightItems.GetArrayLength(); i++)
        {
            var element = rightItems[i];
            if (element.GetProperty("type").GetString() == "ColumnSet")
            {
                var columns = element.GetProperty("columns");
                bool isCommandRow = false;
                foreach (var col in columns.EnumerateArray())
                {
                    if (col.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                    {
                        var item = items[0];
                        if (item.GetProperty("type").GetString() == "Container" &&
                            item.TryGetProperty("selectAction", out var sa) &&
                            sa.GetProperty("verb").GetString() == "runCommand")
                        {
                            actionCount++;
                            isCommandRow = true;
                        }
                    }
                }
                if (isCommandRow) rowCount++;
            }
        }

        // With pagination (5 rows x 2 cols = 10 commands per page on Medium)
        Assert.Equal(5, rowCount);
        Assert.Equal(10, actionCount);
    }

    [Fact]
    public void WidgetCardBuilder_BuildCard_ShouldSupportListModeAndCleanTitlesWithIconUrl()
    {
        var testCommands = new List<CommandItem>
        {
            new() { Id = "c1", Name = "刷新 DNS", IconGlyph = "\uE774" },
            new() { Id = "c2", Name = "剪贴板", IconGlyph = "📋" } // Legacy emoji input
        };

        // Test list view
        var cardJson = WidgetCardBuilder.BuildCard("medium", testCommands, "就绪", "list", layoutMode: WidgetSettings.LayoutSidebar);
        using var doc = JsonDocument.Parse(cardJson);

        var body = doc.RootElement.GetProperty("body");
        
        // Left Column: Toolbar
        var leftItems = body[0].GetProperty("columns")[0].GetProperty("items");
        var toolbarCols = leftItems[0].GetProperty("columns");
        var toggleCol = toolbarCols.EnumerateArray().First(c => c.TryGetProperty("selectAction", out var sa) && sa.GetProperty("verb").GetString() == "toggleView");
        var manageCol = toolbarCols.EnumerateArray().First(c => c.TryGetProperty("selectAction", out var sa) && sa.GetProperty("verb").GetString() == "openApp");
        Assert.True(toggleCol.ValueKind != JsonValueKind.Undefined);
        Assert.True(manageCol.ValueKind != JsonValueKind.Undefined);

        // Right Column: Command containers
        var rightItems = body[0].GetProperty("columns")[1].GetProperty("items");
        var cmdContainers = rightItems.EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "Container" &&
                        e.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "runCommand")
            .ToList();

        Assert.Equal(2, cmdContainers.Count);

        // Command 1 in list view: Container (default transparent)
        var cmd1 = cmdContainers[0];
        Assert.Equal("runCommand", cmd1.GetProperty("selectAction").GetProperty("verb").GetString());
        var cmd1Cols = cmd1.GetProperty("items")[0].GetProperty("columns");
        Assert.StartsWith("data:image/png;base64,", cmd1Cols[0].GetProperty("items")[0].GetProperty("url").GetString());
        Assert.Equal("刷新 DNS", cmd1Cols[1].GetProperty("items")[0].GetProperty("text").GetString());

        // Command 2 in list view: legacy emoji 📋
        var cmd2 = cmdContainers[1];
        Assert.Equal("runCommand", cmd2.GetProperty("selectAction").GetProperty("verb").GetString());
        var cmd2Cols = cmd2.GetProperty("items")[0].GetProperty("columns");
        Assert.StartsWith("data:image/png;base64,", cmd2Cols[0].GetProperty("items")[0].GetProperty("url").GetString());
        Assert.Equal("剪贴板", cmd2Cols[1].GetProperty("items")[0].GetProperty("text").GetString());
    }

    [Fact]
    public void WidgetCardBuilder_BuildCard_ShouldSupportCustomBackgroundColorAndTransparency()
    {
        var customCommands = new List<CommandItem>
        {
            new()
            {
                Id = "custom1",
                Name = "完全透明命令",
                IconGlyph = "\uE756",
                WidgetIsTransparent = true
            },
            new()
            {
                Id = "custom2",
                Name = "蓝色卡片命令",
                IconGlyph = "\uE713",
                WidgetBackgroundColor = "#2563EB"
            }
        };

        var cardJson = WidgetCardBuilder.BuildCard("medium", customCommands, "就绪", "list", layoutMode: WidgetSettings.LayoutSidebar);
        using var doc = JsonDocument.Parse(cardJson);

        var body = doc.RootElement.GetProperty("body");

        var rightItems = body[0].GetProperty("columns")[1].GetProperty("items");
        var cmdContainers = rightItems.EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "Container" &&
                        e.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "runCommand")
            .ToList();

        Assert.Equal(2, cmdContainers.Count);

        // Item 1: Transparent Container
        var item1 = cmdContainers[0];
        Assert.Equal("runCommand", item1.GetProperty("selectAction").GetProperty("verb").GetString());
        // Transparent container should not have backgroundImage
        Assert.False(item1.TryGetProperty("backgroundImage", out _));

        // Item 2: Custom Blue Background Container
        var item2 = cmdContainers[1];
        Assert.Equal("runCommand", item2.GetProperty("selectAction").GetProperty("verb").GetString());
        // Should have a valid backgroundImage PNG Data URI
        Assert.True(item2.TryGetProperty("backgroundImage", out var bgProp));
        Assert.StartsWith("data:image/png;base64,", bgProp.GetProperty("url").GetString());
    }

    [Fact]
    public async Task LogService_ShouldAppendAndRetrieveLogs()
    {
        var logService = new LogService();
        await logService.ClearLogsAsync();

        var entry = new ExecutionLogEntry
        {
            CommandName = "UnitTestLog",
            CommandText = "dir",
            Success = true,
            ExitCode = 0,
            Output = "Mock Output"
        };

        await logService.AppendLogAsync(entry);

        var logs = await logService.GetRecentLogsAsync(10);
        Assert.NotEmpty(logs);
        Assert.Contains(logs, l => l.CommandName == "UnitTestLog");

        await logService.ClearLogsAsync();
        var emptyLogs = await logService.GetRecentLogsAsync(10);
        Assert.Empty(emptyLogs);
    }

    [Fact]
    public void IconItem_Filter_ShouldSupportFuzzySearchAndCategories()
    {
        // 1. All icons
        var all = IconItem.Filter(null);
        Assert.True(all.Count >= 85);

        // 2. Exact / keyword search: "cmd" -> should match Terminal
        var cmdMatches = IconItem.Filter("cmd");
        Assert.NotEmpty(cmdMatches);
        Assert.Contains(cmdMatches, i => i.Glyph == "\uE756");

        // 3. Chinese keyword search: "终端" -> should match Terminal
        var termMatches = IconItem.Filter("终端");
        Assert.NotEmpty(termMatches);
        Assert.Contains(termMatches, i => i.Glyph == "\uE756");

        // 4. WiFi search: "wifi" -> should match WiFi icon
        var wifiMatches = IconItem.Filter("wifi");
        Assert.NotEmpty(wifiMatches);
        Assert.Contains(wifiMatches, i => i.Glyph == "\uE706");

        // 5. Git search: "git" -> should match Branch icon
        var gitMatches = IconItem.Filter("git");
        Assert.NotEmpty(gitMatches);
        Assert.Contains(gitMatches, i => i.Glyph == "\uE9E9");

        // 6. Subsequence search: "pwsh" -> should match PowerShell / Console
        var pwshMatches = IconItem.Filter("pwsh");
        Assert.NotEmpty(pwshMatches);
        Assert.Contains(pwshMatches, i => i.Glyph == "\uE9CE");

        // 7. Category filter
        var netIcons = IconItem.Filter(null, "网络通信");
        Assert.NotEmpty(netIcons);
        Assert.All(netIcons, i => Assert.Equal("网络通信", i.Category));
    }
}

