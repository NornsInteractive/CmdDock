using CmdDock.Core.Models;
using CmdDock.Core.Services;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CmdDock.Widget;

public static class WidgetCardBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver()
    };

    /// <summary>
    /// Builds a fully-realized Adaptive Card 1.5 JSON for the given widget size.
    /// Supports a 2-column grid layout for Medium/Large sizes, and vertical stacking for Small.
    /// </summary>
    /// <summary>
    /// Builds a fully-realized Adaptive Card 1.5 JSON for the given widget size.
    /// Features a sleek icon-only toolbar (Manage ⚙️ & View Toggle 🔲/📑),
    /// and renders ALL commands without truncation to support full scrollability.
    /// </summary>
    public static string BuildCard(
        string widgetSize,
        IReadOnlyList<CommandItem> commands,
        string statusMessage = "就绪",
        string viewMode = "grid",
        string selectedCategory = "全部",
        string? layoutMode = null,
        string? paginationStyle = null,
        int pageIndex = 0,
        int categoryPageIndex = 0,
        string? confirmingCommandId = null,
        CommandItem? confirmingCommand = null)
    {
        var size = widgetSize.ToLowerInvariant();
        var card = new JsonObject
        {
            ["type"] = "AdaptiveCard",
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["version"] = "1.5"
        };

        var body = new JsonArray();

        // 0. If a command is being confirmed, build the in-widget confirmation card!
        var cmdToConfirm = confirmingCommand ?? (string.IsNullOrEmpty(confirmingCommandId) ? null : commands.FirstOrDefault(c => c.Id == confirmingCommandId));
        if (cmdToConfirm != null)
        {
            BuildConfirmationLayout(body, size, cmdToConfirm);
            card["body"] = body;
            return card.ToJsonString(JsonOptions);
        }

        // Load available categories and icon mappings
        var availableCategories = new List<string> { "全部" };
        var categoryIconMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["全部"] = "\uE8B9"
        };

        try
        {
            if (File.Exists(AppPaths.CategoriesFilePath))
            {
                var catJson = File.ReadAllText(AppPaths.CategoriesFilePath);
                if (!string.IsNullOrWhiteSpace(catJson))
                {
                    using var doc = JsonDocument.Parse(catJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        var first = doc.RootElement.EnumerateArray().FirstOrDefault();
                        if (first.ValueKind == JsonValueKind.Object)
                        {
                            var loadedItems = JsonSerializer.Deserialize<List<CategoryItem>>(catJson);
                            if (loadedItems != null)
                            {
                                foreach (var item in loadedItems)
                                {
                                    if (!string.IsNullOrWhiteSpace(item.Name))
                                    {
                                        var clean = item.Name.Trim();
                                        if (!availableCategories.Contains(clean, StringComparer.OrdinalIgnoreCase))
                                        {
                                            availableCategories.Add(clean);
                                        }
                                        categoryIconMap[clean] = CommandItem.NormalizeToSegoeGlyph(item.IconGlyph);
                                    }
                                }
                            }
                        }
                        else if (first.ValueKind == JsonValueKind.String)
                        {
                            var loadedStrings = JsonSerializer.Deserialize<List<string>>(catJson);
                            if (loadedStrings != null)
                            {
                                foreach (var s in loadedStrings)
                                {
                                    if (!string.IsNullOrWhiteSpace(s))
                                    {
                                        var clean = s.Trim();
                                        if (!availableCategories.Contains(clean, StringComparer.OrdinalIgnoreCase))
                                        {
                                            availableCategories.Add(clean);
                                        }
                                        categoryIconMap[clean] = CategoryService.GetDefaultIconForCategory(clean);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        catch { }

        foreach (var cmd in commands)
        {
            if (!string.IsNullOrWhiteSpace(cmd.Group))
            {
                var grp = cmd.Group.Trim();
                if (!availableCategories.Contains(grp, StringComparer.OrdinalIgnoreCase))
                {
                    availableCategories.Add(grp);
                }
                if (!categoryIconMap.ContainsKey(grp))
                {
                    categoryIconMap[grp] = CategoryService.GetDefaultIconForCategory(grp);
                }
            }
        }

        // Configuration resolution
        var effectiveLayout = string.IsNullOrWhiteSpace(layoutMode) ? WidgetSettings.GetLayoutMode() : layoutMode;
        var effectivePagination = string.IsNullOrWhiteSpace(paginationStyle) ? WidgetSettings.GetPaginationStyle() : paginationStyle;

        // Small Widget Mode: Pure 3 rows x 2 cols (6 shortcut buttons) with user customization
        if (string.Equals(size, "small", StringComparison.OrdinalIgnoreCase))
        {
            var smallCmdIds = WidgetSettings.GetSmallWidgetCommandIds();
            var smallCommands = new List<CommandItem>();
            if (smallCmdIds.Count > 0)
            {
                foreach (var id in smallCmdIds)
                {
                    var found = commands.FirstOrDefault(c => c.Id == id);
                    if (found != null && !smallCommands.Any(c => c.Id == found.Id))
                    {
                        smallCommands.Add(found);
                    }
                }
            }

            if (smallCommands.Count < 4)
            {
                foreach (var cmd in commands)
                {
                    if (!smallCommands.Any(c => c.Id == cmd.Id))
                    {
                        smallCommands.Add(cmd);
                        if (smallCommands.Count == 4) break;
                    }
                }
            }

            BuildSmallWidgetLayout(body, smallCommands);
            card["body"] = body;
            return card.ToJsonString(JsonOptions);
        }

        var effectiveStatusMessage = (!string.IsNullOrWhiteSpace(statusMessage) && statusMessage != "就绪")
            ? statusMessage
            : I18nService.Instance["Commands.StatusReady"];

        // Filter commands by selected category
        var isAllCategory = string.IsNullOrWhiteSpace(selectedCategory)
            || string.Equals(selectedCategory, "全部", StringComparison.OrdinalIgnoreCase)
            || string.Equals(selectedCategory, "All", StringComparison.OrdinalIgnoreCase)
            || string.Equals(selectedCategory, I18nService.Instance["Category.全部"], StringComparison.OrdinalIgnoreCase);

        var activeCommands = isAllCategory
            ? commands
            : commands.Where(c => string.Equals(c.Group, selectedCategory, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(c.DisplayGroup, selectedCategory, StringComparison.OrdinalIgnoreCase)
                               || string.Equals(I18nService.Instance.TranslateCategory(c.Group), selectedCategory, StringComparison.OrdinalIgnoreCase)).ToList();

        // Pagination calculation
        int pageSize = WidgetSettings.GetPageSize(effectiveLayout, size, viewMode);
        int totalCommands = activeCommands.Count;
        int totalPages = Math.Max(1, (int)Math.Ceiling(totalCommands / (double)pageSize));
        int currentPage = Math.Clamp(pageIndex, 0, totalPages - 1);
        var pagedCommands = activeCommands.Skip(currentPage * pageSize).Take(pageSize).ToList();

        // Category pagination calculation
        int catPageSize = WidgetSettings.GetCategoryPageSize(effectiveLayout, size);
        int totalCatPages = Math.Max(1, (int)Math.Ceiling(availableCategories.Count / (double)catPageSize));
        int currentCatPage = Math.Clamp(categoryPageIndex, 0, totalCatPages - 1);
        var pagedCategories = availableCategories.Skip(currentCatPage * catPageSize).Take(catPageSize).ToList();

        // Reusable Actions
        var toggleDataUri = WidgetIconService.GetToggleIconDataUri(viewMode);
        var toggleTooltip = viewMode == "list" 
            ? I18nService.Instance["Widget.ToggleToGrid"] 
            : I18nService.Instance["Widget.ToggleToList"];
        JsonObject CreateToggleAction() => new()
        {
            ["type"] = "Action.Execute",
            ["verb"] = "toggleView",
            ["tooltip"] = toggleTooltip,
            ["data"] = new JsonObject
            {
                ["currentMode"] = viewMode
            }
        };

        var settingsDataUri = WidgetIconService.GetSettingsIconDataUri();
        var manageTooltip = I18nService.Instance["Widget.ManageTooltip"];
        JsonObject CreateManageAction() => new()
        {
            ["type"] = "Action.Execute",
            ["verb"] = "openApp",
            ["tooltip"] = manageTooltip
        };

        // Render based on selected layout mode (SidebarRail or DropdownTop)
        switch (effectiveLayout)
        {
            case WidgetSettings.LayoutDropdown:
                BuildDropdownTopLayout(body, size, availableCategories, categoryIconMap,
                    selectedCategory, pagedCommands, totalCommands, viewMode, effectiveStatusMessage,
                    effectivePagination, currentPage, totalPages, CreateToggleAction, toggleDataUri, toggleTooltip,
                    CreateManageAction, settingsDataUri);
                break;

            case WidgetSettings.LayoutSidebar:
            default:
                BuildSidebarRailLayout(body, size, availableCategories, pagedCategories, currentCatPage, totalCatPages,
                    categoryIconMap, selectedCategory, pagedCommands, totalCommands, viewMode, effectiveStatusMessage,
                    effectivePagination, currentPage, totalPages, CreateToggleAction, toggleDataUri, toggleTooltip,
                    CreateManageAction, settingsDataUri);
                break;
        }

        card["body"] = body;
        return card.ToJsonString(JsonOptions);
    }

    #region Layout Builders

    // --- In-Widget Secondary Confirmation Screen (For Commands with RequireConfirmation = true) ---
    private static void BuildConfirmationLayout(JsonArray body, string size, CommandItem cmd)
    {
        var i18n = I18nService.Instance;
        bool isSmall = string.Equals(size, "small", StringComparison.OrdinalIgnoreCase);

        // 1. Header with Warning Glyph and Title
        var headerCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Image",
                        ["url"] = WidgetIconService.GetIconDataUri("\uE7BA", isSmall ? 18 : 22),
                        ["width"] = isSmall ? "18px" : "22px",
                        ["height"] = isSmall ? "18px" : "22px",
                        ["verticalAlignment"] = "Center"
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["spacing"] = "Small",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = i18n["Widget.ConfirmTitle"],
                        ["weight"] = "Bolder",
                        ["size"] = isSmall ? "Small" : "Default",
                        ["color"] = "Warning",
                        ["verticalAlignment"] = "Center"
                    }
                }
            }
        };

        body.Add(new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "None",
            ["columns"] = headerCols
        });

        // 2. Command Details Card (Emphasis style container)
        var cmdCardItems = new JsonArray();

        var infoCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Image",
                        ["url"] = WidgetIconService.GetCommandIconDataUri(cmd, isSmall ? 22 : 28),
                        ["width"] = isSmall ? "22px" : "28px",
                        ["height"] = isSmall ? "22px" : "28px",
                        ["verticalAlignment"] = "Center"
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["spacing"] = "Small",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = cmd.DisplayName,
                        ["weight"] = "Bolder",
                        ["size"] = isSmall ? "Small" : "Medium",
                        ["textTrimming"] = "CharacterEllipsis",
                        ["verticalAlignment"] = "Center"
                    },
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = $"{cmd.DisplayShellType} · {cmd.DisplayGroup}",
                        ["size"] = "Small",
                        ["isSubtle"] = true,
                        ["textTrimming"] = "CharacterEllipsis",
                        ["spacing"] = "None"
                    }
                }
            }
        };

        cmdCardItems.Add(new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "Small",
            ["columns"] = infoCols
        });

        if (!string.IsNullOrWhiteSpace(cmd.CommandText))
        {
            cmdCardItems.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = cmd.CommandText,
                ["fontType"] = "Monospace",
                ["size"] = "Small",
                ["isSubtle"] = true,
                ["spacing"] = "Small",
                ["textTrimming"] = "CharacterEllipsis",
                ["maxLines"] = isSmall ? 1 : 2
            });
        }

        body.Add(new JsonObject
        {
            ["type"] = "Container",
            ["style"] = "emphasis",
            ["spacing"] = "Small",
            ["items"] = cmdCardItems
        });

        if (!isSmall)
        {
            body.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = i18n["Widget.ConfirmPrompt"],
                ["size"] = "Small",
                ["spacing"] = "Small",
                ["isSubtle"] = true,
                ["horizontalAlignment"] = "Center"
            });
        }

        // 3. Confirm and Cancel Buttons
        var actionCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "50",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "ActionSet",
                        ["actions"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "Action.Execute",
                                ["title"] = i18n["Widget.ConfirmExecute"],
                                ["style"] = "positive",
                                ["verb"] = "confirmRunCommand",
                                ["data"] = new JsonObject
                                {
                                    ["commandId"] = cmd.Id
                                }
                            }
                        }
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "50",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "ActionSet",
                        ["actions"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["type"] = "Action.Execute",
                                ["title"] = i18n["Widget.ConfirmCancel"],
                                ["verb"] = "cancelConfirm"
                            }
                        }
                    }
                }
            }
        };

        body.Add(new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = isSmall ? "Small" : "Medium",
            ["columns"] = actionCols
        });
    }

    // --- 0. Small Widget Minimalist Layout (Pure 3 rows x 2 cols, 6 shortcut buttons only) ---
    private static void BuildSmallWidgetLayout(JsonArray body, List<CommandItem> commands)
    {
        if (commands.Count == 0)
        {
            body.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = I18nService.Instance["Widget.EmptyCommands"],
                ["isSubtle"] = true,
                ["horizontalAlignment"] = "Center",
                ["verticalAlignment"] = "Center"
            });
            return;
        }

        for (int i = 0; i < commands.Count && i < 4; i += 2)
        {
            var cmdLeft = commands[i];
            var cmdRight = (i + 1 < commands.Count && i + 1 < 4) ? commands[i + 1] : null;

            var rowCols = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "50",
                    ["items"] = new JsonArray { CreateSmallWidgetButtonItem(cmdLeft) }
                }
            };

            var rightCol = new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "50"
            };

            if (cmdRight != null)
            {
                rightCol["items"] = new JsonArray { CreateSmallWidgetButtonItem(cmdRight) };
            }
            else
            {
                rightCol["items"] = new JsonArray();
            }

            rowCols.Add(rightCol);

            body.Add(new JsonObject
            {
                ["type"] = "ColumnSet",
                ["spacing"] = (i == 0) ? "None" : "Small",
                ["columns"] = rowCols
            });
        }
    }

    /// <summary>
    /// Determines whether the status icon (running spinner, success check, failed cross)
    /// should be displayed. Running status is always shown while active.
    /// Success or Failed status automatically expires after 2.5 seconds to keep UI clean.
    /// </summary>
    public static bool ShouldShowStatusIcon(CommandItem cmd)
    {
        if (cmd.LastRunStatus == ExecutionStatus.Idle)
        {
            return false;
        }

        // Running status is always shown while in progress
        if (cmd.LastRunStatus == ExecutionStatus.Running)
        {
            return true;
        }

        // Success or Failed status automatically expires after 2.5 seconds
        if (cmd.LastRunTime.HasValue)
        {
            var elapsed = (DateTime.UtcNow - cmd.LastRunTime.Value).TotalMilliseconds;
            return elapsed >= 0 && elapsed < 2500;
        }

        // If no timestamp is present (e.g. freshly set in memory test), show it
        return true;
    }

    public static JsonObject CreateSmallWidgetButtonItem(CommandItem cmd)
    {
        bool hasCustomBg = !string.IsNullOrWhiteSpace(cmd.WidgetBackgroundColor);

        var container = new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "None",
            ["minHeight"] = "42px",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "runCommand",
                ["data"] = new JsonObject
                {
                    ["commandId"] = cmd.Id
                }
            }
        };

        if (hasCustomBg)
        {
            container["backgroundImage"] = new JsonObject
            {
                ["url"] = WidgetIconService.GetColorPngDataUri(cmd.WidgetBackgroundColor),
                ["fillMode"] = "cover"
            };
        }

        string iconDataUri = WidgetIconService.GetCommandIconDataUri(cmd, 20);
        string? textColor = hasCustomBg
            ? (WidgetIconService.IsDarkBackground(cmd.WidgetBackgroundColor) ? "Light" : "Dark")
            : null;

        var iconImage = new JsonObject
        {
            ["type"] = "Image",
            ["url"] = iconDataUri,
            ["width"] = "20px",
            ["height"] = "20px",
            ["verticalAlignment"] = "Center"
        };

        var titleBlock = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = cmd.DisplayName,
            ["weight"] = "Bolder",
            ["size"] = "Small",
            ["verticalAlignment"] = "Center",
            ["textTrimming"] = "CharacterEllipsis"
        };
        if (!string.IsNullOrEmpty(textColor))
        {
            titleBlock["color"] = textColor;
        }

        var cardColumns = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["spacing"] = "None",
                ["items"] = new JsonArray { iconImage }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["spacing"] = "Small",
                ["items"] = new JsonArray { titleBlock }
            }
        };

        if (ShouldShowStatusIcon(cmd))
        {
            var statusDataUri = WidgetIconService.GetExecutionStatusIconDataUri(cmd.LastRunStatus, 14);
            if (!string.IsNullOrEmpty(statusDataUri))
            {
                cardColumns.Add(new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "auto",
                    ["verticalContentAlignment"] = "Center",
                    ["spacing"] = "ExtraSmall",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "Image",
                            ["url"] = statusDataUri,
                            ["width"] = "14px",
                            ["height"] = "14px",
                            ["verticalAlignment"] = "Center"
                        }
                    }
                });
            }
        }

        container["items"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "ColumnSet",
                ["spacing"] = "None",
                ["columns"] = cardColumns
            }
        };

        return container;
    }

    // --- 1. Dropdown Category Selection Layout (Interactive Dropdown Trigger + Auto-Switch Category Menu) ---
    private static void BuildDropdownTopLayout(
        JsonArray body, string size, List<string> allCategories, Dictionary<string, string> iconMap,
        string selectedCat, List<CommandItem> pagedCommands, int totalCommands, string viewMode,
        string statusMsg, string paginationStyle, int currentPage, int totalPages,
        Func<JsonObject> createToggleAction, string toggleUri, string toggleTooltip,
        Func<JsonObject> createManageAction, string manageUri)
    {
        var currentCatRaw = string.IsNullOrWhiteSpace(selectedCat) ? "全部" : selectedCat;
        var currentCatDisplay = I18nService.Instance.TranslateCategory(currentCatRaw);
        var currentIconUri = WidgetIconService.GetCategoryIconDataUri(iconMap.GetValueOrDefault(currentCatRaw, "E8EC"), 14);
        var chevronUri = WidgetIconService.GetDropdownChevronIconDataUri(10);

        var dropdownTrigger = new JsonObject
        {
            ["type"] = "Container",
            ["minHeight"] = "28px",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.ToggleVisibility",
                ["targetElements"] = new JsonArray { "categoryDropdownMenu" }
            },
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "None",
                    ["columns"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "auto",
                            ["verticalContentAlignment"] = "Center",
                            ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "Image",
                                    ["url"] = currentIconUri,
                                    ["width"] = "14px",
                                    ["height"] = "14px"
                                }
                            }
                        },
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["spacing"] = "Small",
                            ["verticalContentAlignment"] = "Center",
                            ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "TextBlock",
                                    ["text"] = currentCatDisplay,
                                    ["size"] = "Small",
                                    ["weight"] = "Bolder",
                                    ["textTrimming"] = "CharacterEllipsis"
                                }
                            }
                        },
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "auto",
                            ["spacing"] = "Small",
                            ["verticalContentAlignment"] = "Center",
                            ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "Image",
                                    ["url"] = chevronUri,
                                    ["width"] = "10px",
                                    ["height"] = "10px"
                                }
                            }
                        }
                    }
                }
            }
        };

        var headerCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray { dropdownTrigger }
            }
        };

        if (paginationStyle == WidgetSettings.PaginationInline && totalPages > 1)
        {
            headerCols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["spacing"] = "Small",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray { BuildInlinePager(currentPage, totalPages) }
            });
        }

        headerCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = createToggleAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = toggleUri,
                    ["width"] = "20px",
                    ["height"] = "20px",
                    ["altText"] = toggleTooltip,
                    ["selectAction"] = createToggleAction()
                }
            }
        });

        headerCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = createManageAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = manageUri,
                    ["width"] = "20px",
                    ["height"] = "20px",
                    ["altText"] = I18nService.Instance["Widget.ManageTooltip"],
                    ["selectAction"] = createManageAction()
                }
            }
        });

        body.Add(new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "None",
            ["columns"] = headerCols
        });

        // Interactive Dropdown Menu (Hidden by default, toggled via dropdownTrigger)
        var menuContainer = new JsonObject
        {
            ["type"] = "Container",
            ["id"] = "categoryDropdownMenu",
            ["isVisible"] = false,
            ["spacing"] = "Small"
        };

        var menuItems = new JsonArray();
        for (int i = 0; i < allCategories.Count; i += 2)
        {
            var leftCat = allCategories[i];
            var rightCat = (i + 1 < allCategories.Count) ? allCategories[i + 1] : null;

            var cols = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "50",
                    ["items"] = new JsonArray { CreateCategoryMenuItem(leftCat, selectedCat, iconMap) }
                }
            };

            if (rightCat != null)
            {
                cols.Add(new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "50",
                    ["items"] = new JsonArray { CreateCategoryMenuItem(rightCat, selectedCat, iconMap) }
                });
            }

            menuItems.Add(new JsonObject
            {
                ["type"] = "ColumnSet",
                ["spacing"] = (i == 0) ? "None" : "Small",
                ["columns"] = cols
            });
        }
        menuContainer["items"] = menuItems;
        body.Add(menuContainer);

        // Command Cards Stage
        AppendCommandsToContainer(body, pagedCommands, totalCommands, selectedCat, viewMode, size == "small");

        // Optional Bottom Pager
        if (paginationStyle == WidgetSettings.PaginationBottom && totalPages > 1)
        {
            body.Add(BuildBottomPager(currentPage, totalPages, statusMsg));
        }
    }

    private static JsonObject CreateCategoryMenuItem(string cat, string selectedCat, Dictionary<string, string> iconMap)
    {
        bool isSelected = string.Equals(cat, selectedCat, StringComparison.OrdinalIgnoreCase)
            || string.Equals(I18nService.Instance.TranslateCategory(cat), selectedCat, StringComparison.OrdinalIgnoreCase);
        var iconUri = WidgetIconService.GetCategoryIconDataUri(iconMap.GetValueOrDefault(cat, "E8EC"), 14);

        var item = new JsonObject
        {
            ["type"] = "Container",
            ["minHeight"] = "28px",
            ["verticalContentAlignment"] = "Center",
            ["spacing"] = "None",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "selectCategory",
                ["data"] = new JsonObject
                {
                    ["category"] = cat
                }
            },
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "None",
                    ["columns"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "auto",
                            ["verticalContentAlignment"] = "Center",
                            ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "Image",
                                    ["url"] = iconUri,
                                    ["width"] = "14px",
                                    ["height"] = "14px"
                                }
                            }
                        },
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "stretch",
                            ["spacing"] = "Small",
                            ["verticalContentAlignment"] = "Center",
                            ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "TextBlock",
                                    ["text"] = I18nService.Instance.TranslateCategory(cat),
                                    ["size"] = "Small",
                                    ["weight"] = isSelected ? "Bolder" : "Default",
                                    ["color"] = isSelected ? "Accent" : "Default",
                                    ["textTrimming"] = "CharacterEllipsis"
                                }
                            }
                        }
                    }
                }
            }
        };

        if (isSelected)
        {
            item["backgroundImage"] = new JsonObject
            {
                ["url"] = WidgetIconService.GetCategoryPillBackgroundDataUri(true),
                ["fillMode"] = "cover"
            };
        }

        return item;
    }

    // --- 2. Sidebar Rail Layout (Left vertical rail + right stage) ---
    private static void BuildSidebarRailLayout(
        JsonArray body, string size, List<string> categories, List<string> pagedCategories,
        int currentCatPage, int totalCatPages, Dictionary<string, string> iconMap,
        string selectedCat, List<CommandItem> pagedCommands, int totalCommands, string viewMode,
        string statusMsg, string paginationStyle, int currentPage, int totalPages,
        Func<JsonObject> createToggleAction, string toggleUri, string toggleTooltip,
        Func<JsonObject> createManageAction, string manageUri)
    {
        var mainColSet = new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "None"
        };
        var mainCols = new JsonArray();

        // Left Sidebar Column
        var leftItems = new JsonArray();

        // Toolbar buttons on top of left sidebar
        var toolbarCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["selectAction"] = createToggleAction(),
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Image",
                        ["url"] = toggleUri,
                        ["width"] = "20px",
                        ["height"] = "20px",
                        ["altText"] = toggleTooltip,
                        ["selectAction"] = createToggleAction()
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["spacing"] = "Medium",
                ["verticalContentAlignment"] = "Center",
                ["selectAction"] = createManageAction(),
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Image",
                        ["url"] = manageUri,
                        ["width"] = "20px",
                        ["height"] = "20px",
                        ["altText"] = I18nService.Instance["Widget.ManageTooltip"],
                        ["selectAction"] = createManageAction()
                    }
                }
            }
        };

        leftItems.Add(new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "None",
            ["columns"] = toolbarCols
        });

        // Vertical Category Menu (Paged)
        bool isSmall = size == "small";
        foreach (var cat in pagedCategories)
        {
            leftItems.Add(BuildVerticalCategoryItem(cat, selectedCat, iconMap, isSmall));
        }

        if (totalCatPages > 1)
        {
            leftItems.Add(BuildVerticalCategoryPager(currentCatPage, totalCatPages, isSmall));
        }

        mainCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["verticalContentAlignment"] = "Top",
            ["items"] = leftItems
        });

        // Right Content Panel
        var rightItems = new JsonArray();

        // Right Top Header if inline pagination is active
        if (paginationStyle == WidgetSettings.PaginationInline && totalPages > 1)
        {
            var headerCols = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "stretch",
                    ["verticalContentAlignment"] = "Center",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "TextBlock",
                            ["text"] = I18nService.Instance.TranslateCategory(selectedCat),
                            ["size"] = "Small",
                            ["weight"] = "Bolder",
                            ["isSubtle"] = true,
                            ["verticalAlignment"] = "Center"
                        }
                    }
                },
                new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "auto",
                    ["verticalContentAlignment"] = "Center",
                    ["items"] = new JsonArray { BuildInlinePager(currentPage, totalPages) }
                }
            };

            rightItems.Add(new JsonObject
            {
                ["type"] = "ColumnSet",
                ["spacing"] = "None",
                ["columns"] = headerCols
            });
        }

        AppendCommandsToContainer(rightItems, pagedCommands, totalCommands, selectedCat, viewMode, isSmall);

        // Optional Bottom Pager inside right panel
        if (paginationStyle == WidgetSettings.PaginationBottom && totalPages > 1)
        {
            rightItems.Add(BuildBottomPager(currentPage, totalPages, statusMsg));
        }

        mainCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "stretch",
            ["separator"] = true,
            ["spacing"] = "Medium",
            ["items"] = rightItems
        });

        mainColSet["columns"] = mainCols;
        body.Add(mainColSet);
    }

    // --- 3. Floating Island Layout (Compact pill island on top + clean stage) ---
    private static void BuildFloatingIslandLayout(
        JsonArray body, string size, List<string> categories, List<string> pagedCategories,
        int currentCatPage, int totalCatPages, Dictionary<string, string> iconMap,
        string selectedCat, List<CommandItem> pagedCommands, int totalCommands, string viewMode,
        string statusMsg, string paginationStyle, int currentPage, int totalPages,
        Func<JsonObject> createToggleAction, string toggleUri, string toggleTooltip,
        Func<JsonObject> createManageAction, string manageUri)
    {
        // Top Floating Island Container
        var islandCols = new JsonArray();

        if (categories.Count > 1)
        {
            var catCols = new JsonArray();
            if (totalCatPages > 1)
            {
                catCols.Add(BuildCategoryPrevButton(currentCatPage > 0, currentCatPage - 1));
            }

            foreach (var cat in pagedCategories)
            {
                catCols.Add(BuildHorizontalCategoryPill(cat, selectedCat, iconMap));
            }

            if (totalCatPages > 1)
            {
                catCols.Add(BuildCategoryNextButton(currentCatPage < totalCatPages - 1, currentCatPage + 1));
            }

            islandCols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "ColumnSet",
                        ["spacing"] = "Small",
                        ["columns"] = catCols
                    }
                }
            });
        }
        else
        {
            islandCols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = $"★ {I18nService.Instance.TranslateCategory(selectedCat)}",
                        ["size"] = "Small",
                        ["weight"] = "Bolder",
                        ["color"] = "Light",
                        ["verticalAlignment"] = "Center"
                    }
                }
            });
        }

        if (paginationStyle == WidgetSettings.PaginationInline && totalPages > 1)
        {
            islandCols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray { BuildInlinePager(currentPage, totalPages) }
            });
        }

        islandCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = createToggleAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = toggleUri,
                    ["width"] = "18px",
                    ["height"] = "18px",
                    ["altText"] = toggleTooltip,
                    ["selectAction"] = createToggleAction()
                }
            }
        });

        islandCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = createManageAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = manageUri,
                    ["width"] = "18px",
                    ["height"] = "18px",
                    ["altText"] = I18nService.Instance["Widget.ManageTooltip"],
                    ["selectAction"] = createManageAction()
                }
            }
        });

        var islandContainer = new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "None",
            ["minHeight"] = "28px",
            ["verticalContentAlignment"] = "Center",
            ["backgroundImage"] = new JsonObject
            {
                ["url"] = WidgetIconService.GetSidebarCategoryItemBackgroundDataUri(true),
                ["fillMode"] = "cover"
            },
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "None",
                    ["columns"] = islandCols
                }
            }
        };

        body.Add(islandContainer);

        // Command Cards Stage
        AppendCommandsToContainer(body, pagedCommands, totalCommands, selectedCat, viewMode, size == "small");

        // Optional Bottom Pager
        if (paginationStyle == WidgetSettings.PaginationBottom && totalPages > 1)
        {
            body.Add(BuildBottomPager(currentPage, totalPages, statusMsg));
        }
    }

    // --- 4. LaunchPad Layout (High density app tiles grid) ---
    private static void BuildLaunchPadLayout(
        JsonArray body, string size, List<string> categories, List<string> pagedCategories,
        int currentCatPage, int totalCatPages, Dictionary<string, string> iconMap,
        string selectedCat, List<CommandItem> pagedCommands, int totalCommands, string viewMode,
        string statusMsg, string paginationStyle, int currentPage, int totalPages,
        Func<JsonObject> createManageAction, string manageUri)
    {
        // Header: Category Filter Buttons + Pager + Manage
        var headerCols = new JsonArray();

        // Horizontal Category Micro Pills with Pager
        var catPillCols = new JsonArray();
        if (totalCatPages > 1)
        {
            catPillCols.Add(BuildCategoryPrevButton(currentCatPage > 0, currentCatPage - 1));
        }

        foreach (var cat in pagedCategories)
        {
            catPillCols.Add(BuildHorizontalCategoryPill(cat, selectedCat, iconMap));
        }

        if (totalCatPages > 1)
        {
            catPillCols.Add(BuildCategoryNextButton(currentCatPage < totalCatPages - 1, currentCatPage + 1));
        }

        headerCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "stretch",
            ["verticalContentAlignment"] = "Center",
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "Small",
                    ["columns"] = catPillCols
                }
            }
        });

        if (paginationStyle == WidgetSettings.PaginationInline && totalPages > 1)
        {
            headerCols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray { BuildInlinePager(currentPage, totalPages) }
            });
        }

        headerCols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = createManageAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = manageUri,
                    ["width"] = "20px",
                    ["height"] = "20px",
                    ["altText"] = I18nService.Instance["Widget.ManageTooltip"],
                    ["selectAction"] = createManageAction()
                }
            }
        });

        body.Add(new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "None",
            ["columns"] = headerCols
        });

        // 4-Column App Tiles Matrix
        if (pagedCommands.Count == 0)
        {
            body.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = I18nService.Instance["Widget.EmptyCommands"],
                ["isSubtle"] = true,
                ["horizontalAlignment"] = "Center",
                ["spacing"] = "Medium"
            });
        }
        else
        {
            int colsPerRow = 4;
            for (int i = 0; i < pagedCommands.Count; i += colsPerRow)
            {
                var rowCols = new JsonArray();
                for (int c = 0; c < colsPerRow; c++)
                {
                    int idx = i + c;
                    var col = new JsonObject
                    {
                        ["type"] = "Column",
                        ["width"] = "25"
                    };

                    if (idx < pagedCommands.Count)
                    {
                        col["items"] = new JsonArray { CreateLaunchPadTileElement(pagedCommands[idx]) };
                    }
                    else
                    {
                        col["items"] = new JsonArray();
                    }

                    rowCols.Add(col);
                }

                body.Add(new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "Small",
                    ["columns"] = rowCols
                });
            }
        }

        // Optional Bottom Pager
        if (paginationStyle == WidgetSettings.PaginationBottom && totalPages > 1)
        {
            body.Add(BuildBottomPager(currentPage, totalPages, statusMsg));
        }
    }

    #endregion

    #region Helper Builders

    private static JsonObject BuildHorizontalCategoryPill(string cat, string selectedCat, Dictionary<string, string> iconMap)
    {
        bool isSelected = string.Equals(cat, selectedCat, StringComparison.OrdinalIgnoreCase)
            || string.Equals(I18nService.Instance.TranslateCategory(cat), selectedCat, StringComparison.OrdinalIgnoreCase);

        iconMap.TryGetValue(cat, out var catIconGlyph);
        var catIconDataUri = WidgetIconService.GetCategoryIconDataUri(catIconGlyph, size: 14);

        var pillCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Image",
                        ["url"] = catIconDataUri,
                        ["width"] = "13px",
                        ["height"] = "13px"
                    }
                }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["spacing"] = "Small",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = I18nService.Instance.TranslateCategory(cat),
                        ["size"] = "Small",
                        ["weight"] = isSelected ? "Bolder" : "Normal",
                        ["color"] = isSelected ? "Light" : null,
                        ["isSubtle"] = !isSelected,
                        ["verticalAlignment"] = "Center"
                    }
                }
            }
        };

        var pillContainer = new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "None",
            ["minHeight"] = "26px",
            ["verticalContentAlignment"] = "Center",
            ["horizontalAlignment"] = "Center",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "filterCategory",
                ["data"] = new JsonObject { ["category"] = cat }
            },
            ["backgroundImage"] = new JsonObject
            {
                ["url"] = WidgetIconService.GetCategoryPillBackgroundDataUri(isSelected),
                ["fillMode"] = "cover"
            },
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "None",
                    ["columns"] = pillCols
                }
            }
        };

        return new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["items"] = new JsonArray { pillContainer }
        };
    }

    private static JsonObject BuildCategoryPrevButton(bool isEnabled, int targetPage)
    {
        JsonObject? CreateAction() => isEnabled
            ? new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "changeCategoryPage",
                ["data"] = new JsonObject { ["catPage"] = targetPage }
            }
            : null;

        return new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "None",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = CreateAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetCategoryPrevIconDataUri(isEnabled, 12),
                    ["width"] = "12px",
                    ["height"] = "12px",
                    ["selectAction"] = CreateAction()
                }
            }
        };
    }

    private static JsonObject BuildCategoryNextButton(bool isEnabled, int targetPage)
    {
        JsonObject? CreateAction() => isEnabled
            ? new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "changeCategoryPage",
                ["data"] = new JsonObject { ["catPage"] = targetPage }
            }
            : null;

        return new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = CreateAction(),
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetCategoryNextIconDataUri(isEnabled, 12),
                    ["width"] = "12px",
                    ["height"] = "12px",
                    ["selectAction"] = CreateAction()
                }
            }
        };
    }

    private static JsonObject BuildVerticalCategoryItem(string cat, string selectedCat, Dictionary<string, string> iconMap, bool isSmall)
    {
        bool isSelected = string.Equals(cat, selectedCat, StringComparison.OrdinalIgnoreCase)
            || string.Equals(I18nService.Instance.TranslateCategory(cat), selectedCat, StringComparison.OrdinalIgnoreCase);

        iconMap.TryGetValue(cat, out var catIconGlyph);
        var catIconDataUri = WidgetIconService.GetCategoryIconDataUri(catIconGlyph, size: 14);

        var itemCols = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Image",
                        ["url"] = catIconDataUri,
                        ["width"] = "13px",
                        ["height"] = "13px",
                        ["verticalAlignment"] = "Center"
                    }
                }
            }
        };

        if (!isSmall)
        {
            itemCols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["spacing"] = "Small",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = I18nService.Instance.TranslateCategory(cat),
                        ["size"] = "Small",
                        ["weight"] = isSelected ? "Bolder" : "Normal",
                        ["color"] = isSelected ? "Light" : null,
                        ["isSubtle"] = !isSelected,
                        ["verticalAlignment"] = "Center",
                        ["textTrimming"] = "CharacterEllipsis"
                    }
                }
            });
        }

        return new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "Small",
            ["minHeight"] = "28px",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "filterCategory",
                ["data"] = new JsonObject { ["category"] = cat }
            },
            ["backgroundImage"] = new JsonObject
            {
                ["url"] = WidgetIconService.GetSidebarCategoryItemBackgroundDataUri(isSelected),
                ["fillMode"] = "cover"
            },
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "None",
                    ["columns"] = itemCols
                }
            }
        };
    }

    private static JsonObject BuildVerticalCategoryPager(int currentCatPage, int totalCatPages, bool isSmall)
    {
        bool hasPrev = currentCatPage > 0;
        bool hasNext = currentCatPage < totalCatPages - 1;

        JsonObject CreateCatPageAction(int target) => new()
        {
            ["type"] = "Action.Execute",
            ["verb"] = "changeCategoryPage",
            ["data"] = new JsonObject { ["catPage"] = target }
        };

        var cols = new JsonArray();

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = hasPrev ? CreateCatPageAction(currentCatPage - 1) : null,
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetCategoryUpIconDataUri(hasPrev, 12),
                    ["width"] = "12px",
                    ["height"] = "12px",
                    ["selectAction"] = hasPrev ? CreateCatPageAction(currentCatPage - 1) : null
                }
            }
        });

        if (!isSmall)
        {
            cols.Add(new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["spacing"] = "Small",
                ["verticalContentAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = $"{currentCatPage + 1}/{totalCatPages}",
                        ["size"] = "Small",
                        ["isSubtle"] = true,
                        ["verticalAlignment"] = "Center"
                    }
                }
            });
        }

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = hasNext ? CreateCatPageAction(currentCatPage + 1) : null,
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetCategoryDownIconDataUri(hasNext, 12),
                    ["width"] = "12px",
                    ["height"] = "12px",
                    ["selectAction"] = hasNext ? CreateCatPageAction(currentCatPage + 1) : null
                }
            }
        });

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "Small",
            ["columns"] = cols
        };
    }

    private static void AppendCommandsToContainer(
        JsonArray targetContainer, List<CommandItem> commands, int totalCommands,
        string selectedCat, string viewMode, bool isSmall)
    {
        if (totalCommands == 0)
        {
            targetContainer.Add(new JsonObject
            {
                ["type"] = "TextBlock",
                ["text"] = I18nService.Instance["Widget.EmptyWidgetCommands"],
                ["isSubtle"] = true,
                ["horizontalAlignment"] = "Center",
                ["spacing"] = "Medium"
            });
        }
        else if (commands.Count == 0)
        {
            var catDisplay = I18nService.Instance.TranslateCategory(selectedCat);
            targetContainer.Add(new JsonObject
            {
                ["type"] = "Container",
                ["spacing"] = "Medium",
                ["horizontalAlignment"] = "Center",
                ["items"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "TextBlock",
                        ["text"] = string.Format(I18nService.Instance["Widget.EmptyCategory"], catDisplay),
                        ["isSubtle"] = true,
                        ["horizontalAlignment"] = "Center",
                        ["size"] = "Small"
                    }
                }
            });
        }
        else if (viewMode == "list")
        {
            for (int i = 0; i < commands.Count; i++)
            {
                var el = CreateCommandItemElement(commands[i], isGrid: false);
                el["spacing"] = (i == 0) ? "None" : "ExtraSmall";
                targetContainer.Add(el);
            }
        }
        else
        {
            for (int i = 0; i < commands.Count; i += 2)
            {
                var cmdLeft = commands[i];
                var cmdRight = (i + 1 < commands.Count) ? commands[i + 1] : null;

                var columns = new JsonArray
                {
                    new JsonObject
                    {
                        ["type"] = "Column",
                        ["width"] = "50",
                        ["items"] = new JsonArray { CreateCommandItemElement(cmdLeft, isGrid: true) }
                    }
                };

                var rightCol = new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "50"
                };

                if (cmdRight != null)
                {
                    rightCol["items"] = new JsonArray { CreateCommandItemElement(cmdRight, isGrid: true) };
                }
                else
                {
                    rightCol["items"] = new JsonArray();
                }

                columns.Add(rightCol);

                targetContainer.Add(new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["spacing"] = "Small",
                    ["columns"] = columns
                });
            }
        }
    }

    private static JsonObject BuildInlinePager(int currentPage, int totalPages)
    {
        var cols = new JsonArray();
        bool hasPrev = currentPage > 0;
        bool hasNext = currentPage < totalPages - 1;

        JsonObject CreatePageAction(int targetPage) => new()
        {
            ["type"] = "Action.Execute",
            ["verb"] = "changePage",
            ["data"] = new JsonObject { ["page"] = targetPage }
        };

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = hasPrev ? CreatePageAction(currentPage - 1) : null,
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetPagerPrevIconDataUri(hasPrev, 14),
                    ["width"] = "14px",
                    ["height"] = "14px",
                    ["selectAction"] = hasPrev ? CreatePageAction(currentPage - 1) : null
                }
            }
        });

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = $"{currentPage + 1}/{totalPages}",
                    ["size"] = "Small",
                    ["isSubtle"] = true,
                    ["verticalAlignment"] = "Center"
                }
            }
        });

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = hasNext ? CreatePageAction(currentPage + 1) : null,
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetPagerNextIconDataUri(hasNext, 14),
                    ["width"] = "14px",
                    ["height"] = "14px",
                    ["selectAction"] = hasNext ? CreatePageAction(currentPage + 1) : null
                }
            }
        });

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "Small",
            ["columns"] = cols
        };
    }

    private static JsonObject BuildBottomPager(int currentPage, int totalPages, string statusMsg)
    {
        var cols = new JsonArray();

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "stretch",
            ["verticalContentAlignment"] = "Center",
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "TextBlock",
                    ["text"] = !string.IsNullOrWhiteSpace(statusMsg) && statusMsg != "就绪" && statusMsg != I18nService.Instance["Commands.StatusReady"]
                        ? statusMsg
                        : string.Format(I18nService.Instance["Widget.PageFormat"], currentPage + 1, totalPages),
                    ["size"] = "Small",
                    ["isSubtle"] = true,
                    ["verticalAlignment"] = "Center"
                }
            }
        });

        bool hasPrev = currentPage > 0;
        bool hasNext = currentPage < totalPages - 1;

        JsonObject CreatePageAction(int targetPage) => new()
        {
            ["type"] = "Action.Execute",
            ["verb"] = "changePage",
            ["data"] = new JsonObject { ["page"] = targetPage }
        };

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Small",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = hasPrev ? CreatePageAction(currentPage - 1) : null,
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetPagerPrevIconDataUri(hasPrev, 16),
                    ["width"] = "16px",
                    ["height"] = "16px",
                    ["selectAction"] = hasPrev ? CreatePageAction(currentPage - 1) : null
                }
            }
        });

        cols.Add(new JsonObject
        {
            ["type"] = "Column",
            ["width"] = "auto",
            ["spacing"] = "Medium",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = hasNext ? CreatePageAction(currentPage + 1) : null,
            ["items"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "Image",
                    ["url"] = WidgetIconService.GetPagerNextIconDataUri(hasNext, 16),
                    ["width"] = "16px",
                    ["height"] = "16px",
                    ["selectAction"] = hasNext ? CreatePageAction(currentPage + 1) : null
                }
            }
        });

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "Small",
            ["columns"] = cols
        };
    }

    private static JsonObject CreateLaunchPadTileElement(CommandItem cmd)
    {
        var tileItems = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Image",
                ["url"] = WidgetIconService.GetCommandIconDataUri(cmd, 22),
                ["width"] = "22px",
                ["height"] = "22px",
                ["horizontalAlignment"] = "Center"
            }
        };

        var titleBlock = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = cmd.DisplayName,
            ["size"] = "Small",
            ["horizontalAlignment"] = "Center",
            ["verticalAlignment"] = "Center",
            ["textTrimming"] = "CharacterEllipsis"
        };

        if (ShouldShowStatusIcon(cmd))
        {
            var statusUri = WidgetIconService.GetExecutionStatusIconDataUri(cmd.LastRunStatus, 12);
            if (!string.IsNullOrEmpty(statusUri))
            {
                tileItems.Add(new JsonObject
                {
                    ["type"] = "ColumnSet",
                    ["horizontalAlignment"] = "Center",
                    ["columns"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "auto",
                            ["verticalContentAlignment"] = "Center",
                            ["items"] = new JsonArray { titleBlock }
                        },
                        new JsonObject
                        {
                            ["type"] = "Column",
                            ["width"] = "auto",
                            ["verticalContentAlignment"] = "Center",
                            ["spacing"] = "ExtraSmall",
                            ["items"] = new JsonArray
                            {
                                new JsonObject
                                {
                                    ["type"] = "Image",
                                    ["url"] = statusUri,
                                    ["width"] = "12px",
                                    ["height"] = "12px",
                                    ["verticalAlignment"] = "Center"
                                }
                            }
                        }
                    }
                });
            }
            else
            {
                tileItems.Add(titleBlock);
            }
        }
        else
        {
            tileItems.Add(titleBlock);
        }

        return new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "Small",
            ["minHeight"] = "48px",
            ["verticalContentAlignment"] = "Center",
            ["horizontalAlignment"] = "Center",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "runCommand",
                ["data"] = new JsonObject { ["commandId"] = cmd.Id }
            },
            ["items"] = tileItems
        };
    }

    #endregion

    /// <summary>
    /// Creates a command element that supports customized background colors,
    /// transparency, or default button appearance.
    /// </summary>
    public static JsonObject CreateCommandItemElement(CommandItem cmd, bool isGrid = false)
    {
        bool hasCustomBg = !string.IsNullOrWhiteSpace(cmd.WidgetBackgroundColor);

        // Command Card Tile: Transparent by default, or Custom Color Background
        var container = new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = isGrid ? "Small" : "None",
            ["minHeight"] = isGrid ? "38px" : "28px",
            ["verticalContentAlignment"] = "Center",
            ["selectAction"] = new JsonObject
            {
                ["type"] = "Action.Execute",
                ["verb"] = "runCommand",
                ["data"] = new JsonObject
                {
                    ["commandId"] = cmd.Id
                }
            }
        };

        if (hasCustomBg)
        {
            container["backgroundImage"] = new JsonObject
            {
                ["url"] = WidgetIconService.GetColorPngDataUri(cmd.WidgetBackgroundColor),
                ["fillMode"] = "cover"
            };
        }

        // Determine icon and text color for optimal contrast
        int iconPx = isGrid ? 20 : 16;
        string iconDataUri = WidgetIconService.GetCommandIconDataUri(cmd, iconPx);
        string? textColor = hasCustomBg
            ? (WidgetIconService.IsDarkBackground(cmd.WidgetBackgroundColor) ? "Light" : "Dark")
            : null;

        var iconImage = new JsonObject
        {
            ["type"] = "Image",
            ["url"] = iconDataUri,
            ["width"] = $"{iconPx}px",
            ["height"] = $"{iconPx}px",
            ["verticalAlignment"] = "Center"
        };

        var titleBlock = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = cmd.DisplayName,
            ["weight"] = isGrid ? "Bolder" : "Default",
            ["size"] = isGrid ? "Small" : "Default",
            ["verticalAlignment"] = "Center",
            ["textTrimming"] = "CharacterEllipsis"
        };
        if (!string.IsNullOrEmpty(textColor))
        {
            titleBlock["color"] = textColor;
        }

        var cardColumns = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "auto",
                ["verticalContentAlignment"] = "Center",
                ["spacing"] = "None",
                ["items"] = new JsonArray { iconImage }
            },
            new JsonObject
            {
                ["type"] = "Column",
                ["width"] = "stretch",
                ["verticalContentAlignment"] = "Center",
                ["spacing"] = "Small",
                ["items"] = new JsonArray { titleBlock }
            }
        };

        if (ShouldShowStatusIcon(cmd))
        {
            var statusDataUri = WidgetIconService.GetExecutionStatusIconDataUri(cmd.LastRunStatus, 14);
            if (!string.IsNullOrEmpty(statusDataUri))
            {
                cardColumns.Add(new JsonObject
                {
                    ["type"] = "Column",
                    ["width"] = "auto",
                    ["verticalContentAlignment"] = "Center",
                    ["spacing"] = "ExtraSmall",
                    ["items"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["type"] = "Image",
                            ["url"] = statusDataUri,
                            ["width"] = "14px",
                            ["height"] = "14px",
                            ["verticalAlignment"] = "Center"
                        }
                    }
                });
            }
        }

        container["items"] = new JsonArray
        {
            new JsonObject
            {
                ["type"] = "ColumnSet",
                ["spacing"] = "None",
                ["columns"] = cardColumns
            }
        };

        return container;
    }

    private static JsonObject CreateExecuteAction(CommandItem cmd)
    {
        // CRITICAL: NEVER include PUA glyph characters in title!
        // PUA characters have no font fallback in Windows Widgets Board and display as tofu rectangles (□).
        var action = new JsonObject
        {
            ["type"] = "Action.Execute",
            ["title"] = cmd.DisplayName,
            ["verb"] = "runCommand",
            ["data"] = new JsonObject
            {
                ["commandId"] = cmd.Id
            }
        };

        action["iconUrl"] = WidgetIconService.GetCommandIconDataUri(cmd, 24);

        return action;
    }

    public static string GetTemplateForSize(string widgetSize)
    {
        var rawTemplate = widgetSize.ToLowerInvariant() switch
        {
            "small" => SmallTemplate,
            "large" => LargeTemplate,
            _ => MediumTemplate // Default to Medium
        };

        var appTitle = I18nService.Instance["App.ShortTitle"];
        var manageText = I18nService.Instance["Widget.ManageTooltip"];

        return rawTemplate
            .Replace("CmdDock 快捷命令", appTitle)
            .Replace("CmdDock 快捷启动坞", appTitle)
            .Replace("\"title\": \"管理\"", $"\"title\": \"{manageText}\"")
            .Replace("\"title\": \"管理面板\"", $"\"title\": \"{manageText}\"");
    }

    public static string BuildDataPayload(IReadOnlyList<CommandItem> commands, string statusMessage = "就绪")
    {
        var effectiveStatus = (!string.IsNullOrWhiteSpace(statusMessage) && statusMessage != "就绪")
            ? statusMessage
            : I18nService.Instance["Commands.StatusReady"];

        var items = commands.Select(c => new
        {
            id = c.Id,
            name = c.DisplayName,
            description = c.DisplayDescription,
            icon = c.DisplayIcon,
            group = c.DisplayGroup,
            status = c.LastRunStatus.ToString(),
            lastRun = c.FormattedLastRun,
            duration = c.FormattedDuration
        }).ToList();

        var payload = new
        {
            statusText = effectiveStatus,
            commandCount = items.Count,
            commands = items
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    // Small Widget Template fallback
    private const string SmallTemplate = """
    {
        "type": "AdaptiveCard",
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "version": "1.5",
        "body": [
            {
                "type": "TextBlock",
                "text": "CmdDock",
                "weight": "Bolder",
                "size": "Medium"
            },
            {
                "type": "TextBlock",
                "text": "${statusText}",
                "isSubtle": true,
                "size": "Small",
                "spacing": "None"
            },
            {
                "type": "ActionSet",
                "actionsOrientation": "Vertical",
                "spacing": "Small",
                "actions": [
                    {
                        "$data": "${commands}",
                        "type": "Action.Execute",
                        "title": "${name}",
                        "verb": "runCommand",
                        "data": {
                            "commandId": "${id}"
                        }
                    }
                ]
            }
        ]
    }
    """;

    // Medium Widget Template fallback
    private const string MediumTemplate = """
    {
        "type": "AdaptiveCard",
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "version": "1.5",
        "body": [
            {
                "type": "ColumnSet",
                "columns": [
                    {
                        "type": "Column",
                        "width": "stretch",
                        "items": [
                            {
                                "type": "TextBlock",
                                "text": "CmdDock 快捷命令",
                                "weight": "Bolder",
                                "size": "Medium"
                            },
                            {
                                "type": "TextBlock",
                                "text": "${statusText}",
                                "isSubtle": true,
                                "size": "Small",
                                "spacing": "None"
                            }
                        ]
                    },
                    {
                        "type": "Column",
                        "width": "auto",
                        "items": [
                            {
                                "type": "ActionSet",
                                "actions": [
                                    {
                                        "type": "Action.Execute",
                                        "title": "管理",
                                        "verb": "openApp"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "type": "ActionSet",
                "actionsOrientation": "Vertical",
                "spacing": "Small",
                "actions": [
                    {
                        "$data": "${commands}",
                        "type": "Action.Execute",
                        "title": "${name}",
                        "verb": "runCommand",
                        "data": {
                            "commandId": "${id}"
                        }
                    }
                ]
            }
        ]
    }
    """;

    // Large Widget Template fallback
    private const string LargeTemplate = """
    {
        "type": "AdaptiveCard",
        "$schema": "http://adaptivecards.io/schemas/adaptive-card.json",
        "version": "1.5",
        "body": [
            {
                "type": "ColumnSet",
                "columns": [
                    {
                        "type": "Column",
                        "width": "stretch",
                        "items": [
                            {
                                "type": "TextBlock",
                                "text": "CmdDock 快捷启动坞",
                                "weight": "Bolder",
                                "size": "Large"
                            },
                            {
                                "type": "TextBlock",
                                "text": "${statusText}",
                                "isSubtle": true,
                                "size": "Small",
                                "spacing": "None"
                            }
                        ]
                    },
                    {
                        "type": "Column",
                        "width": "auto",
                        "items": [
                            {
                                "type": "ActionSet",
                                "actions": [
                                    {
                                        "type": "Action.Execute",
                                        "title": "管理面板",
                                        "verb": "openApp"
                                    }
                                ]
                            }
                        ]
                    }
                ]
            },
            {
                "type": "ActionSet",
                "actionsOrientation": "Vertical",
                "spacing": "Small",
                "actions": [
                    {
                        "$data": "${commands}",
                        "type": "Action.Execute",
                        "title": "${name}",
                        "verb": "runCommand",
                        "data": {
                            "commandId": "${id}"
                        }
                    }
                ]
            }
        ]
    }
    """;
}
