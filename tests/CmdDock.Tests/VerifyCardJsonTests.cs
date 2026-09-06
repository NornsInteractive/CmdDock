using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock.Widget;
using System.Text.Json;

namespace CmdDock.Tests;

public class VerifyCardJsonTests
{
    [Fact]
    public void VerifyGeneratedCard_HasZeroPUACharactersAndValidDataUris()
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets();

        var cardJson = WidgetCardBuilder.BuildCard("medium", presets, "就绪", "grid", layoutMode: WidgetSettings.LayoutSidebar);
        Assert.NotNull(cardJson);

        // Check for any PUA characters (\uE000 to \uF8FF) in the entire JSON string!
        foreach (char c in cardJson)
        {
            if (c >= 0xE000 && c <= 0xF8FF)
            {
                Assert.Fail($"Found forbidden PUA character (causes tofu □ rectangle) in card JSON: U+{(int)c:X4}");
            }
        }

        // Verify it parses as valid JSON
        using var doc = JsonDocument.Parse(cardJson);
        var body = doc.RootElement.GetProperty("body");

        // Verify dual-pane layout: body[0] is the main ColumnSet with Left Sidebar & Right Commands Panel
        var mainColSet = body[0];
        Assert.Equal("ColumnSet", mainColSet.GetProperty("type").GetString());
        var mainCols = mainColSet.GetProperty("columns");
        Assert.Equal(2, mainCols.GetArrayLength());

        var leftCol = mainCols[0];
        Assert.Equal("auto", leftCol.GetProperty("width").GetString());
        var leftItems = leftCol.GetProperty("items");

        // Verify Toolbar is at the top of the left sidebar
        var toolbarRow = leftItems[0];
        Assert.Equal("ColumnSet", toolbarRow.GetProperty("type").GetString());
        var toolbarCols = toolbarRow.GetProperty("columns");

        var toggleCol = toolbarCols.EnumerateArray()
            .First(c => c.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "toggleView");
        var toggleImg = toggleCol.GetProperty("items")[0];
        Assert.Equal("Image", toggleImg.GetProperty("type").GetString());
        Assert.StartsWith("data:image/png;base64,", toggleImg.GetProperty("url").GetString());

        var manageCol = toolbarCols.EnumerateArray()
            .First(c => c.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "openApp");
        var manageImg = manageCol.GetProperty("items")[0];
        Assert.Equal("Image", manageImg.GetProperty("type").GetString());
        Assert.StartsWith("data:image/png;base64,", manageImg.GetProperty("url").GetString());

        // Verify vertical category items in left sidebar
        var catItems = leftItems.EnumerateArray()
            .Where(i => i.TryGetProperty("selectAction", out var sa) &&
                        sa.GetProperty("verb").GetString() == "filterCategory")
            .ToList();
        Assert.NotEmpty(catItems);

        // Verify right column: stretch width, separator, commands
        var rightCol = mainCols[1];
        Assert.Equal("stretch", rightCol.GetProperty("width").GetString());
        Assert.True(rightCol.GetProperty("separator").GetBoolean());
        var rightItems = rightCol.GetProperty("items");

        // Verify commands have clean titles, zero PUA glyphs, and valid iconUrls
        for (int i = 0; i < rightItems.GetArrayLength(); i++)
        {
            var row = rightItems[i];
            if (row.GetProperty("type").GetString() == "ColumnSet")
            {
                var cols = row.GetProperty("columns");
                foreach (var col in cols.EnumerateArray())
                {
                    if (col.TryGetProperty("items", out var items) && items.GetArrayLength() > 0)
                    {
                        var container = items[0];
                        if (container.GetProperty("type").GetString() != "Container") continue;

                        if (container.TryGetProperty("selectAction", out var sa))
                        {
                            var verb = sa.GetProperty("verb").GetString();
                            Assert.Equal("runCommand", verb);
                        }

                        var innerColSet = container.GetProperty("items")[0];
                        var innerCols = innerColSet.GetProperty("columns");
                        var img = innerCols[0].GetProperty("items")[0];
                        Assert.Equal("Image", img.GetProperty("type").GetString());
                        Assert.StartsWith("data:image/png;base64,", img.GetProperty("url").GetString());

                        var textBlock = innerCols[1].GetProperty("items")[0];
                        Assert.Equal("TextBlock", textBlock.GetProperty("type").GetString());
                        var title = textBlock.GetProperty("text").GetString();
                        Assert.False(string.IsNullOrWhiteSpace(title));
                        foreach (char tc in title!)
                        {
                            Assert.False(tc >= 0xE000 && tc <= 0xF8FF, $"Command title '{title}' has PUA glyph!");
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void VerifyCard_WithImageIconCommand_HasValidImageAndZeroPUA()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"cmddock_card_test_{Guid.NewGuid():N}.png");
        try
        {
            using (var bmp = new System.Drawing.Bitmap(64, 64))
            {
                using (var g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.Blue);
                }
                bmp.Save(tempFile, System.Drawing.Imaging.ImageFormat.Png);
            }

            var commands = new List<CommandItem>
            {
                new CommandItem
                {
                    Id = "cmd_img_test",
                    Name = "Image App",
                    IconGlyph = tempFile,
                    WidgetBackgroundColor = "#1E293B"
                }
            };

            var cardJson = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "list", layoutMode: WidgetSettings.LayoutSidebar);
            Assert.NotNull(cardJson);

            // Zero PUA glyphs
            foreach (char c in cardJson)
            {
                Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Found PUA character: U+{(int)c:X4}");
            }

            using var doc = JsonDocument.Parse(cardJson);
            var body = doc.RootElement.GetProperty("body");
            Assert.True(body.GetArrayLength() >= 1);

            // Verify the command container has the image data URI in right column
            var rightItems = body[0].GetProperty("columns")[1].GetProperty("items");
            var commandContainer = rightItems.EnumerateArray()
                .First(e => e.GetProperty("type").GetString() == "Container" &&
                            e.TryGetProperty("selectAction", out var sa) &&
                            sa.GetProperty("verb").GetString() == "runCommand");
            var colSet = commandContainer.GetProperty("items")[0];
            var iconImg = colSet.GetProperty("columns")[0].GetProperty("items")[0];
            var url = iconImg.GetProperty("url").GetString();
            Assert.NotNull(url);
            Assert.StartsWith("data:image/png;base64,", url);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void VerifyCurrentCommandsJson_IfPresent_HasZeroPUAAndBuildsCard()
    {
        if (File.Exists(AppPaths.CommandsFilePath))
        {
            var json = File.ReadAllText(AppPaths.CommandsFilePath);
            var commands = JsonSerializer.Deserialize<List<CommandItem>>(json);
            Assert.NotNull(commands);

            var cardList = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "list");
            Assert.NotNull(cardList);
            foreach (char c in cardList)
            {
                Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Found PUA in list card: U+{(int)c:X4}");
            }

            var cardGrid = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid");
            Assert.NotNull(cardGrid);
            foreach (char c in cardGrid)
            {
                Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Found PUA in grid card: U+{(int)c:X4}");
            }
        }
    }

    [Fact]
    public void VerifyCard_CategoryFiltering_WorksCorrectly()
    {
        var commands = new List<CommandItem>
        {
            new CommandItem { Id = "c1", Name = "Ping", Group = "网络", IconGlyph = "E701" },
            new CommandItem { Id = "c2", Name = "Clean", Group = "系统", IconGlyph = "E74D" }
        };

        var categories = new List<string> { "全部", "网络", "系统", "开发" };

        // 1. Filter by "网络" -> should contain Ping, but not Clean
        var cardJson = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid", selectedCategory: "网络");
        Assert.NotNull(cardJson);
        Assert.Contains("Ping", cardJson);
        Assert.DoesNotContain("Clean", cardJson);
        Assert.True(cardJson.Contains("filterCategory") || cardJson.Contains("selectCategory"));

        // 2. Filter by "系统" -> 1 item (Clean)
        var sysCardJson = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid", selectedCategory: "系统");
        Assert.NotNull(sysCardJson);
        Assert.Contains("Clean", sysCardJson);
        Assert.DoesNotContain("Ping", sysCardJson);

        // 3. Filter by "全部"
        var allCardJson = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid", selectedCategory: "全部");
        Assert.NotNull(allCardJson);
        Assert.Contains("Ping", allCardJson);
        Assert.Contains("Clean", allCardJson);
    }

    [Theory]
    [InlineData(WidgetSettings.LayoutSidebar)]
    [InlineData(WidgetSettings.LayoutDropdown)]
    public void VerifyAllLayoutModes_GenerateZeroPUA_AndValidJson(string layoutMode)
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets();

        var cardJson = WidgetCardBuilder.BuildCard("medium", presets, "就绪", "grid", "全部", layoutMode: layoutMode);
        Assert.NotNull(cardJson);

        // Check for any PUA characters
        foreach (char c in cardJson)
        {
            Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Layout '{layoutMode}' generated PUA glyph: U+{(int)c:X4}");
        }

        using var doc = JsonDocument.Parse(cardJson);
        Assert.Equal("AdaptiveCard", doc.RootElement.GetProperty("type").GetString());
        Assert.Equal("1.5", doc.RootElement.GetProperty("version").GetString());
        var body = doc.RootElement.GetProperty("body");
        Assert.True(body.GetArrayLength() > 0);
    }

    [Theory]
    [InlineData(WidgetSettings.PaginationInline)]
    [InlineData(WidgetSettings.PaginationBottom)]
    public void VerifyBothPaginationStyles_GenerateChangePageActions(string paginationStyle)
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets(); // 19 commands, multiple pages

        var cardJson = WidgetCardBuilder.BuildCard(
            "medium", presets, "就绪", "grid", "全部",
            layoutMode: WidgetSettings.LayoutDropdown,
            paginationStyle: paginationStyle,
            pageIndex: 0);

        Assert.NotNull(cardJson);
        Assert.Contains("changePage", cardJson);
        Assert.Contains("\"verb\":\"changePage\"", cardJson.Replace(" ", ""));

        // Page 0 next action should target page 1
        Assert.Contains("\"page\":1", cardJson.Replace(" ", ""));
    }

    [Fact]
    public void VerifyPagination_PageNavigation_SlicesCommandsProperly()
    {
        var commands = new List<CommandItem>();
        for (int i = 1; i <= 25; i++)
        {
            commands.Add(new CommandItem { Id = $"cmd_{i}", Name = $"Command {i:D2}" });
        }

        // Page size on Medium for grid view is 10 (5 rows x 2 cols).
        // Page 0 should have Command 01 to 10
        var page0 = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid", "全部",
            layoutMode: WidgetSettings.LayoutSidebar, paginationStyle: WidgetSettings.PaginationInline, pageIndex: 0);
        Assert.Contains("Command 01", page0);
        Assert.Contains("Command 10", page0);
        Assert.DoesNotContain("Command 11", page0);

        // Page 1 should have Command 11 to 20
        var page1 = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid", "全部",
            layoutMode: WidgetSettings.LayoutSidebar, paginationStyle: WidgetSettings.PaginationInline, pageIndex: 1);
        Assert.DoesNotContain("Command 10", page1);
        Assert.Contains("Command 11", page1);
        Assert.Contains("Command 20", page1);
        Assert.DoesNotContain("Command 21", page1);

        // Page 2 should have Command 21 to 25
        var page2 = WidgetCardBuilder.BuildCard("medium", commands, "就绪", "grid", "全部",
            layoutMode: WidgetSettings.LayoutSidebar, paginationStyle: WidgetSettings.PaginationInline, pageIndex: 2);
        Assert.DoesNotContain("Command 20", page2);
        Assert.Contains("Command 21", page2);
        Assert.Contains("Command 25", page2);
    }

    [Fact]
    public void VerifyCategoryCapacityMatrix_MatchesDesignSpecification()
    {
        // Sidebar rail layout: Small = 0 (no categories), Medium = 5, Large = 8
        Assert.Equal(0, WidgetSettings.GetCategoryPageSize(WidgetSettings.LayoutSidebar, "small"));
        Assert.Equal(5, WidgetSettings.GetCategoryPageSize(WidgetSettings.LayoutSidebar, "medium"));
        Assert.Equal(8, WidgetSettings.GetCategoryPageSize(WidgetSettings.LayoutSidebar, "large"));

        // Dropdown layout: all categories supported in combobox
        Assert.Equal(int.MaxValue, WidgetSettings.GetCategoryPageSize(WidgetSettings.LayoutDropdown, "small"));
        Assert.Equal(int.MaxValue, WidgetSettings.GetCategoryPageSize(WidgetSettings.LayoutDropdown, "medium"));
        Assert.Equal(int.MaxValue, WidgetSettings.GetCategoryPageSize(WidgetSettings.LayoutDropdown, "large"));
    }

    [Theory]
    [InlineData(WidgetSettings.LayoutSidebar, "medium")]
    [InlineData(WidgetSettings.LayoutSidebar, "large")]
    public void VerifyCategoryPagination_SidebarLayout_PaginatesAndGeneratesActions(string layoutMode, string size)
    {
        // Generate 10 categories: "全部" + 9 categories "Cat1" to "Cat9"
        var commands = new List<CommandItem>();
        for (int i = 1; i <= 9; i++)
        {
            commands.Add(new CommandItem
            {
                Id = $"c_{i}",
                Name = $"Item {i}",
                Group = $"Cat{i}",
                IconGlyph = "E700"
            });
        }

        // Page 0
        var cardPage0 = WidgetCardBuilder.BuildCard(
            size, commands, "就绪", "grid", "全部",
            layoutMode: layoutMode,
            pageIndex: 0,
            categoryPageIndex: 0);

        Assert.NotNull(cardPage0);

        // Zero PUA glyphs
        foreach (char c in cardPage0)
        {
            Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Category card {layoutMode}-{size} has PUA: U+{(int)c:X4}");
        }

        // With 10 categories and pageSize 5 (medium) or 8 (large), totalCatPages > 1
        Assert.Contains("changeCategoryPage", cardPage0);
        Assert.Contains("\"verb\":\"changeCategoryPage\"", cardPage0.Replace(" ", ""));

        // Page 0 next action should target catPage: 1
        Assert.Contains("\"catPage\":1", cardPage0.Replace(" ", ""));

        // Page 1
        var cardPage1 = WidgetCardBuilder.BuildCard(
            size, commands, "就绪", "grid", "全部",
            layoutMode: layoutMode,
            pageIndex: 0,
            categoryPageIndex: 1);

        Assert.NotNull(cardPage1);
        Assert.Contains("changeCategoryPage", cardPage1);

        // Page 1 prev action should target catPage: 0
        Assert.Contains("\"catPage\":0", cardPage1.Replace(" ", ""));
    }

    [Fact]
    public void VerifyDropdownLayout_GeneratesInteractiveDropdown_AndSelectCategoryAction_NoSwitchButton()
    {
        var commands = new List<CommandItem>
        {
            new CommandItem { Id = "c1", Name = "Ping", Group = "网络" },
            new CommandItem { Id = "c2", Name = "Clean", Group = "系统" }
        };

        var cardJson = WidgetCardBuilder.BuildCard(
            "medium", commands, "就绪", "grid", "全部",
            layoutMode: WidgetSettings.LayoutDropdown);

        Assert.NotNull(cardJson);

        // Check for any PUA characters
        foreach (char c in cardJson)
        {
            Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Dropdown layout has PUA: U+{(int)c:X4}");
        }

        using var doc = JsonDocument.Parse(cardJson);
        var body = doc.RootElement.GetProperty("body");
        Assert.True(body.GetArrayLength() > 0);

        // Verify Action.ToggleVisibility exists targeting categoryDropdownMenu
        Assert.Contains("\"type\":\"Action.ToggleVisibility\"", cardJson.Replace(" ", ""));
        Assert.Contains("\"targetElements\":[\"categoryDropdownMenu\"]", cardJson.Replace(" ", ""));

        // Verify category dropdown menu container exists
        Assert.Contains("\"id\":\"categoryDropdownMenu\"", cardJson.Replace(" ", ""));
        Assert.Contains("\"isVisible\":false", cardJson.Replace(" ", ""));

        // Verify selectCategory action exists for 1-click immediate switching
        Assert.Contains("\"verb\":\"selectCategory\"", cardJson.Replace(" ", ""));

        // Verify NO "切换" button exists in the card JSON!
        Assert.DoesNotContain("\"title\":\"切换\"", cardJson.Replace(" ", ""));
        Assert.DoesNotContain("\"title\": \"切换\"", cardJson);
    }

    [Fact]
    public void VerifySmallWidget_RendersOnly4Buttons_In2x2Grid_NoHeaders()
    {
        var presetService = new PresetService();
        var presets = presetService.GetBuiltinPresets(); // 19 commands

        var cardJson = WidgetCardBuilder.BuildCard(
            "small", presets, "就绪", "grid", "全部",
            layoutMode: WidgetSettings.LayoutDropdown);

        Assert.NotNull(cardJson);

        // Zero PUA glyphs
        foreach (char c in cardJson)
        {
            Assert.False(c >= 0xE000 && c <= 0xF8FF, $"Small widget card has PUA: U+{(int)c:X4}");
        }

        // Must NOT contain headers, categories, dropdowns, view toggle, or openApp
        Assert.DoesNotContain("Input.ChoiceSet", cardJson);
        Assert.DoesNotContain("filterCategory", cardJson);
        Assert.DoesNotContain("selectCategory", cardJson);
        Assert.DoesNotContain("openApp", cardJson);
        Assert.DoesNotContain("toggleView", cardJson);
        Assert.DoesNotContain("changePage", cardJson);
        Assert.DoesNotContain("切换", cardJson);

        using var doc = JsonDocument.Parse(cardJson);
        var body = doc.RootElement.GetProperty("body");

        // Small Widget must have exactly 2 rows of ColumnSets (2 rows x 2 columns = 4 buttons)
        Assert.Equal(2, body.GetArrayLength());
        int buttonCount = 0;
        for (int r = 0; r < 2; r++)
        {
            var row = body[r];
            Assert.Equal("ColumnSet", row.GetProperty("type").GetString());
            var cols = row.GetProperty("columns");
            Assert.Equal(2, cols.GetArrayLength()); // 2 columns per row
            for (int c = 0; c < 2; c++)
            {
                var col = cols[c];
                var items = col.GetProperty("items");
                if (items.GetArrayLength() > 0)
                {
                    var container = items[0];
                    Assert.Equal("Container", container.GetProperty("type").GetString());
                    Assert.True(container.TryGetProperty("selectAction", out var sa));
                    Assert.Equal("runCommand", sa.GetProperty("verb").GetString());
                    buttonCount++;
                }
            }
        }

        Assert.Equal(4, buttonCount);
    }

    [Fact]
    public void VerifyGridCapacities_SmallMediumLarge()
    {
        // Small: 2 rows x 2 cols = 4
        Assert.Equal(4, WidgetSettings.GetPageSize(WidgetSettings.LayoutSidebar, "small"));
        Assert.Equal(4, WidgetSettings.GetPageSize(WidgetSettings.LayoutDropdown, "small"));

        // Medium: 5 rows x 2 cols = 10 (grid), 5 (list)
        Assert.Equal(10, WidgetSettings.GetPageSize(WidgetSettings.LayoutSidebar, "medium", "grid"));
        Assert.Equal(5, WidgetSettings.GetPageSize(WidgetSettings.LayoutSidebar, "medium", "list"));
        Assert.Equal(10, WidgetSettings.GetPageSize(WidgetSettings.LayoutDropdown, "medium", "grid"));

        // Large: 9 rows x 2 cols = 18 (grid), 9 (list)
        Assert.Equal(18, WidgetSettings.GetPageSize(WidgetSettings.LayoutSidebar, "large", "grid"));
        Assert.Equal(9, WidgetSettings.GetPageSize(WidgetSettings.LayoutSidebar, "large", "list"));
        Assert.Equal(18, WidgetSettings.GetPageSize(WidgetSettings.LayoutDropdown, "large", "grid"));
    }

    [Fact]
    public void VerifyConfirmationCard_GeneratesValidConfirmationView_ForAllSizes()
    {
        var testCmd = new CommandItem
        {
            Id = "test_confirm_cmd",
            Name = "清理系统临时文件",
            CommandText = "del /f /s /q %TEMP%\\*",
            Group = "运维",
            ShellType = ShellType.Cmd,
            ExecutionMode = ExecutionMode.Silent,
            RequireConfirmation = true
        };

        var commands = new List<CommandItem> { testCmd };

        foreach (var size in new[] { "small", "medium", "large" })
        {
            var cardJson = WidgetCardBuilder.BuildCard(
                size, 
                commands, 
                confirmingCommandId: testCmd.Id, 
                confirmingCommand: testCmd);

            Assert.NotNull(cardJson);
            Assert.Contains("confirmRunCommand", cardJson);
            Assert.Contains("cancelConfirm", cardJson);
            Assert.Contains(testCmd.Name, cardJson);

            using var doc = JsonDocument.Parse(cardJson);
            Assert.Equal("AdaptiveCard", doc.RootElement.GetProperty("type").GetString());
            var body = doc.RootElement.GetProperty("body");
            Assert.True(body.GetArrayLength() >= 3);
        }
    }
}


