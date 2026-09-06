using CmdDock.Core.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CmdDock.Core.Services;

public class CommandService : ICommandService
{
    private readonly IPresetService _presetService;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private List<CommandItem>? _cachedCommands;
    private DateTime _lastLoadedTime = DateTime.MinValue;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        PropertyNameCaseInsensitive = true
    };

    public event EventHandler? CommandsChanged;

    public CommandService(IPresetService? presetService = null)
    {
        _presetService = presetService ?? new PresetService();
    }

    public async Task<IReadOnlyList<CommandItem>> GetAllAsync(bool forceReload = false)
    {
        var fileInfo = new FileInfo(AppPaths.CommandsFilePath);
        var lastWrite = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.MinValue;

        if (_cachedCommands != null && !forceReload && lastWrite <= _lastLoadedTime && fileInfo.Exists)
        {
            return _cachedCommands.OrderBy(c => c.Order).ToList();
        }

        await _fileLock.WaitAsync();
        try
        {
            if (!File.Exists(AppPaths.CommandsFilePath))
            {
                // Seed with built-in presets
                var presets = _presetService.GetBuiltinPresets().ToList();
                var json = JsonSerializer.Serialize(presets, JsonOptions);
                await File.WriteAllTextAsync(AppPaths.CommandsFilePath, json);
                _cachedCommands = presets;
                _lastLoadedTime = File.GetLastWriteTimeUtc(AppPaths.CommandsFilePath);
                return _cachedCommands.OrderBy(c => c.Order).ToList();
            }

            var text = await File.ReadAllTextAsync(AppPaths.CommandsFilePath);
            var loaded = JsonSerializer.Deserialize<List<CommandItem>>(text, JsonOptions) ?? new List<CommandItem>();
            _cachedCommands = loaded;
            _lastLoadedTime = File.GetLastWriteTimeUtc(AppPaths.CommandsFilePath);
            return _cachedCommands.OrderBy(c => c.Order).ToList();
        }
        catch
        {
            _cachedCommands ??= new List<CommandItem>();
            return _cachedCommands;
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<IReadOnlyList<CommandItem>> GetWidgetCommandsAsync()
    {
        var all = await GetAllAsync();
        return all.Where(c => c.ShowInWidget).OrderBy(c => c.Order).ToList();
    }

    public async Task<CommandItem?> GetByIdAsync(string id)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(c => c.Id == id);
    }

    public async Task AddOrUpdateAsync(CommandItem item)
    {
        var all = (await GetAllAsync()).ToList();
        var index = all.FindIndex(c => c.Id == item.Id);
        if (index >= 0)
        {
            all[index] = item;
        }
        else
        {
            if (item.Order == 0)
            {
                item.Order = all.Count > 0 ? all.Max(c => c.Order) + 1 : 1;
            }
            all.Add(item);
        }

        await SaveAllAsync(all);
    }

    public async Task DeleteAsync(string id)
    {
        var all = (await GetAllAsync()).ToList();
        var removed = all.RemoveAll(c => c.Id == id);
        if (removed > 0)
        {
            await SaveAllAsync(all);
        }
    }

    public async Task SaveAllAsync(IEnumerable<CommandItem> items)
    {
        await _fileLock.WaitAsync();
        try
        {
            var list = items.OrderBy(c => c.Order).ToList();
            var json = JsonSerializer.Serialize(list, JsonOptions);
            await File.WriteAllTextAsync(AppPaths.CommandsFilePath, json);
            _cachedCommands = list;
            _lastLoadedTime = File.GetLastWriteTimeUtc(AppPaths.CommandsFilePath);
        }
        finally
        {
            _fileLock.Release();
        }

        CommandsChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task UpdateExecutionStatusAsync(string id, ExecutionStatus status, int? exitCode, long? durationMs)
    {
        var all = (await GetAllAsync()).ToList();
        var item = all.FirstOrDefault(c => c.Id == id);
        if (item != null)
        {
            item.LastRunStatus = status;
            item.LastRunTime = DateTime.UtcNow;
            item.LastExitCode = exitCode;
            item.LastRunDurationMs = durationMs;
            await SaveAllAsync(all);
        }
    }
}
