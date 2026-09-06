using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock_App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace CmdDock_App.ViewModels;

public partial class MainViewModel : ObservableObject
{

    [ObservableProperty]
    private ObservableCollection<CommandItem> _commands = new();

    [ObservableProperty]
    private ObservableCollection<CommandItem> _filteredCommands = new();

    [ObservableProperty]
    private ObservableCollection<ExecutionLogEntry> _logs = new();

    [ObservableProperty]
    private ObservableCollection<CommandItem> _presets = new();

    [ObservableProperty]
    private ObservableCollection<string> _groups = new();

    [ObservableProperty]
    private string _selectedGroup = "全部";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    private readonly IPresetService _presetService;
    private readonly ICommandService _commandService;
    private readonly ICategoryService _categoryService;
    private readonly ILogService _logService;
    private readonly CommandExecutor _commandExecutor;

    public ICategoryService CategoryService => _categoryService;
    public ICommandService CommandService => _commandService;

    public MainViewModel()
    {
        _presetService = new PresetService();
        _commandService = new CommandService(_presetService);
        _categoryService = new CategoryService(commandService: _commandService);
        _logService = new LogService();
        _commandExecutor = new CommandExecutor(_commandService, _logService);

        _logService.LogsUpdated += (_, _) => _ = RefreshLogsAsync();
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            // Load presets
            Presets.Clear();
            foreach (var preset in _presetService.GetBuiltinPresets())
            {
                Presets.Add(preset);
            }

            await RefreshCategoriesAsync();
            await RefreshCommandsAsync();
            await RefreshLogsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RefreshCategoriesAsync()
    {
        var cats = await _categoryService.GetAllCategoriesAsync(forceReload: true);
        Groups.Clear();
        Groups.Add("全部");
        foreach (var c in cats)
        {
            if (!Groups.Contains(c))
            {
                Groups.Add(c);
            }
        }
    }

    [RelayCommand]
    public async Task RefreshCommandsAsync()
    {
        await RefreshCategoriesAsync();
        var items = await _commandService.GetAllAsync(forceReload: true);

        // Smooth in-place synchronization without tearing down UI containers
        SyncCollection(Commands, items);

        foreach (var item in items)
        {
            if (!Groups.Contains(item.Group) && !string.IsNullOrWhiteSpace(item.Group))
            {
                Groups.Add(item.Group);
            }
        }

        FilterCommands();
    }

    [RelayCommand]
    public async Task RefreshLogsAsync()
    {
        var recentLogs = await _logService.GetRecentLogsAsync(50);
        if (recentLogs.Count == 0 && Logs.Count == 0) return;

        if (Logs.Count == recentLogs.Count && Logs.Count > 0 && Logs[0].Timestamp == recentLogs[0].Timestamp)
        {
            return;
        }

        Logs.Clear();
        foreach (var log in recentLogs)
        {
            Logs.Add(log);
        }
    }

    public void FilterCommands()
    {
        bool isAll = string.IsNullOrWhiteSpace(SelectedGroup) || 
                     SelectedGroup == "全部" || 
                     SelectedGroup == "All" || 
                     string.Equals(SelectedGroup, I18nService.Instance["Commands.AllCategories"], StringComparison.OrdinalIgnoreCase);

        var query = (isAll
            ? Commands.OrderBy(c => c.Order) 
            : Commands.Where(c => c.Group == SelectedGroup).OrderBy(c => c.Order)).ToList();

        // 1. Remove items that no longer match the filter
        for (int i = FilteredCommands.Count - 1; i >= 0; i--)
        {
            var item = FilteredCommands[i];
            if (!query.Any(q => q.Id == item.Id))
            {
                FilteredCommands.RemoveAt(i);
            }
        }

        // 2. Add or reorder matching items without destroying existing containers
        for (int i = 0; i < query.Count; i++)
        {
            var item = query[i];
            var currentIdx = FilteredCommands.IndexOf(item);
            if (currentIdx >= 0)
            {
                if (currentIdx != i && i < FilteredCommands.Count)
                {
                    FilteredCommands.Move(currentIdx, i);
                }
            }
            else
            {
                if (i < FilteredCommands.Count)
                {
                    FilteredCommands.Insert(i, item);
                }
                else
                {
                    FilteredCommands.Add(item);
                }
            }
        }
    }

    private static void SyncCollection(ObservableCollection<CommandItem> target, IReadOnlyList<CommandItem> source)
    {
        // 1. Remove items no longer in source
        for (int i = target.Count - 1; i >= 0; i--)
        {
            var existing = target[i];
            if (!source.Any(s => s.Id == existing.Id))
            {
                target.RemoveAt(i);
            }
        }

        // 2. In-place property updates or insert new items
        for (int i = 0; i < source.Count; i++)
        {
            var src = source[i];
            var existingIdx = -1;
            for (int j = 0; j < target.Count; j++)
            {
                if (target[j].Id == src.Id)
                {
                    existingIdx = j;
                    break;
                }
            }

            if (existingIdx >= 0)
            {
                var existing = target[existingIdx];
                if (!ReferenceEquals(existing, src))
                {
                    existing.Name = src.Name;
                    existing.Description = src.Description;
                    existing.Group = src.Group;
                    existing.IconGlyph = src.IconGlyph;
                    existing.ShellType = src.ShellType;
                    existing.CommandText = src.CommandText;
                    existing.Arguments = src.Arguments;
                    existing.WorkingDirectory = src.WorkingDirectory;
                    existing.ExecutionMode = src.ExecutionMode;
                    existing.RequireConfirmation = src.RequireConfirmation;
                    existing.Order = src.Order;
                    existing.ShowInWidget = src.ShowInWidget;
                    existing.WidgetSizePreference = src.WidgetSizePreference;
                    existing.LastRunTime = src.LastRunTime;
                    existing.LastRunStatus = src.LastRunStatus;
                    existing.LastRunDurationMs = src.LastRunDurationMs;
                    existing.LastExitCode = src.LastExitCode;
                }

                if (existingIdx != i && i < target.Count)
                {
                    target.Move(existingIdx, i);
                }
            }
            else
            {
                if (i < target.Count)
                {
                    target.Insert(i, src);
                }
                else
                {
                    target.Add(src);
                }
            }
        }
    }

    partial void OnSelectedGroupChanged(string value)
    {
        FilterCommands();
    }

    [RelayCommand]
    public async Task RunCommandAsync(CommandItem item)
    {
        if (item == null) return;

        StatusMessage = $"正在执行: {item.Name}...";
        item.LastRunStatus = ExecutionStatus.Running;

        var result = await _commandExecutor.ExecuteAsync(item);

        StatusMessage = result.Success 
            ? $"✓ {item.Name} 执行成功 ({result.DurationMs}ms)" 
            : $"✗ {item.Name} 执行失败 (代码 {result.ExitCode})";

        await RefreshLogsAsync();
        WidgetNotificationService.NotifyWidgets(Commands);
    }

    [RelayCommand]
    public async Task DeleteCommandAsync(CommandItem item)
    {
        if (item == null) return;

        Commands.Remove(item);
        FilteredCommands.Remove(item);

        await _commandService.DeleteAsync(item.Id);

        StatusMessage = $"已删除命令: {item.Name}";
        WidgetNotificationService.NotifyWidgets(Commands);
    }

    [RelayCommand]
    public async Task ToggleWidgetVisibilityAsync(CommandItem item)
    {
        if (item == null) return;

        await _commandService.AddOrUpdateAsync(item);

        StatusMessage = item.ShowInWidget ? $"已添加到小组件: {item.Name}" : $"已从小组件隐藏: {item.Name}";
        WidgetNotificationService.NotifyWidgets(Commands);
    }

    [RelayCommand]
    public async Task AddPresetAsync(CommandItem preset)
    {
        if (preset == null) return;

        var newItem = new CommandItem
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = preset.Name,
            Description = preset.Description,
            Group = preset.Group,
            IconGlyph = preset.IconGlyph,
            ShellType = preset.ShellType,
            CommandText = preset.CommandText,
            Arguments = preset.Arguments,
            WorkingDirectory = preset.WorkingDirectory,
            ExecutionMode = preset.ExecutionMode,
            RequireConfirmation = preset.RequireConfirmation,
            ShowInWidget = true,
            Order = Commands.Count > 0 ? Commands.Max(c => c.Order) + 1 : 1
        };

        // Add to in-memory collections seamlessly
        Commands.Add(newItem);
        if (SelectedGroup == "全部" || SelectedGroup == newItem.Group)
        {
            FilteredCommands.Add(newItem);
        }

        if (!Groups.Contains(newItem.Group) && !string.IsNullOrWhiteSpace(newItem.Group))
        {
            Groups.Add(newItem.Group);
        }

        await _commandService.AddOrUpdateAsync(newItem);

        StatusMessage = $"已添加预设命令: {newItem.Name} 并同步至小组件";
        WidgetNotificationService.NotifyWidgets(Commands);
    }

    [RelayCommand]
    public async Task ClearLogsAsync()
    {
        await _logService.ClearLogsAsync();
        Logs.Clear();
        StatusMessage = "已清空执行日志";
    }

    public async Task SaveCommandAsync(CommandItem item)
    {
        var existing = Commands.FirstOrDefault(c => c.Id == item.Id);
        if (existing != null)
        {
            existing.Name = item.Name;
            existing.Description = item.Description;
            existing.Group = item.Group;
            existing.IconGlyph = item.IconGlyph;
            existing.ShellType = item.ShellType;
            existing.CommandText = item.CommandText;
            existing.Arguments = item.Arguments;
            existing.WorkingDirectory = item.WorkingDirectory;
            existing.ExecutionMode = item.ExecutionMode;
            existing.RequireConfirmation = item.RequireConfirmation;
            existing.Order = item.Order;
            existing.ShowInWidget = item.ShowInWidget;
            existing.WidgetSizePreference = item.WidgetSizePreference;
            existing.WidgetCardStyle = item.WidgetCardStyle;
            existing.WidgetBackgroundColor = item.WidgetBackgroundColor;
            existing.WidgetIsTransparent = item.WidgetIsTransparent;
        }
        else
        {
            Commands.Add(item);
            if (SelectedGroup == "全部" || SelectedGroup == item.Group)
            {
                FilteredCommands.Add(item);
            }
        }

        if (!string.IsNullOrWhiteSpace(item.Group))
        {
            await _categoryService.AddCategoryAsync(item.Group);
            if (!Groups.Contains(item.Group))
            {
                Groups.Add(item.Group);
            }
        }

        await _commandService.AddOrUpdateAsync(item);

        StatusMessage = $"已保存命令: {item.Name}";
        WidgetNotificationService.NotifyWidgets(Commands);
    }
}
