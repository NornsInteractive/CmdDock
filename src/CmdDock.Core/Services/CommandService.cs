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
            bool modified = false;
            foreach (var cmd in loaded)
            {
                if (cmd.CommandText != null && cmd.CommandText.Contains("$env:TEMP") && (cmd.CommandText.Contains("Remove-Item") || cmd.Id == "preset_clean_temp" || cmd.Name == "清理系统临时文件" || cmd.Name == "Clean Temp Files"))
                {
                    cmd.CommandText = "Get-ChildItem -Path $env:TEMP -Force -ErrorAction SilentlyContinue | ForEach-Object { try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop } catch { } }; exit 0";
                    modified = true;
                }
                if (cmd.CommandText != null && (cmd.CommandText.Contains("wmic diskdrive") || cmd.Id == "preset_disk_health" || cmd.Name == "磁盘驱动器健康状态" || cmd.Name == "Disk Health Check"))
                {
                    cmd.ShellType = ShellType.PowerShell;
                    cmd.CommandText = "Get-PhysicalDisk | Select-Object DeviceId, FriendlyName, MediaType, OperationalStatus, HealthStatus | Format-Table -AutoSize";
                    modified = true;
                }
                if (cmd.CommandText != null && (cmd.CommandText.Contains("python -m http.server") || cmd.Id == "preset_http_server" || cmd.Name == "启动临时 HTTP 服务器" || cmd.Name == "Start Local HTTP Server"))
                {
                    cmd.ShellType = ShellType.PowerShell;
                    cmd.CommandText = "$port = 8080; $listener = New-Object System.Net.HttpListener; $listener.Prefixes.Add('http://localhost:8080/'); $listener.Start(); Write-Host '===================================================' -ForegroundColor Cyan; Write-Host ' CmdDock 原生 HTTP 文件服务器已启动 (端口: 8080)' -ForegroundColor Green; Write-Host ' 本地访问地址: http://localhost:8080/' -ForegroundColor Yellow; Write-Host ' 当前托管目录: ' (Get-Location) -ForegroundColor White; Write-Host ' 按 Ctrl + C 可随时终止服务器' -ForegroundColor Gray; Write-Host '===================================================' -ForegroundColor Cyan; Start-Process 'http://localhost:8080/'; while ($listener.IsListening) { $ctx = $listener.GetContext(); $req = $ctx.Request; $res = $ctx.Response; $rel = $req.Url.LocalPath.TrimStart('/'); if ([string]::IsNullOrEmpty($rel)) { $rel = 'index.html' }; $path = Join-Path (Get-Location) $rel; if (Test-Path $path -PathType Leaf) { $bytes = [System.IO.File]::ReadAllBytes($path); $res.ContentLength64 = $bytes.Length; $res.OutputStream.Write($bytes, 0, $bytes.Length) } else { $items = Get-ChildItem | ForEach-Object { '<li><a href=' + $_.Name + '>' + $_.Name + '</a></li>' }; $html = '<html><head><meta charset=utf-8><title>CmdDock HTTP Server</title></head><body><h2>CmdDock 本地目录文件列表</h2><p>当前目录: ' + (Get-Location) + '</p><ul>' + ($items -join '') + '</ul></body></html>'; $bytes = [System.Text.Encoding]::UTF8.GetBytes($html); $res.ContentType = 'text/html; charset=utf-8'; $res.ContentLength64 = $bytes.Length; $res.OutputStream.Write($bytes, 0, $bytes.Length) }; $res.OutputStream.Close() }";
                    modified = true;
                }
            }
            if (modified)
            {
                try
                {
                    var updatedJson = JsonSerializer.Serialize(loaded, JsonOptions);
                    await File.WriteAllTextAsync(AppPaths.CommandsFilePath, updatedJson);
                }
                catch { }
            }
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
