using CmdDock.Core.Models;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CmdDock.Core.Services;

public class LogService : ILogService
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public event EventHandler? LogsUpdated;

    public async Task AppendLogAsync(ExecutionLogEntry entry)
    {
        await _lock.WaitAsync();
        try
        {
            var line = JsonSerializer.Serialize(entry, JsonOptions);
            await File.AppendAllLinesAsync(AppPaths.ExecutionLogFilePath, new[] { line });
        }
        catch
        {
            // Ignore logging file IO failure to prevent impacting execution flow
        }
        finally
        {
            _lock.Release();
        }

        LogsUpdated?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<ExecutionLogEntry>> GetRecentLogsAsync(int maxCount = 100)
    {
        if (!File.Exists(AppPaths.ExecutionLogFilePath))
        {
            return Array.Empty<ExecutionLogEntry>();
        }

        await _lock.WaitAsync();
        try
        {
            var lines = await File.ReadAllLinesAsync(AppPaths.ExecutionLogFilePath);
            var results = new List<ExecutionLogEntry>();

            for (int i = lines.Length - 1; i >= 0 && results.Count < maxCount; i--)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var item = JsonSerializer.Deserialize<ExecutionLogEntry>(line, JsonOptions);
                    if (item != null)
                    {
                        results.Add(item);
                    }
                }
                catch
                {
                    // Skip malformed line
                }
            }

            return results;
        }
        catch
        {
            return Array.Empty<ExecutionLogEntry>();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearLogsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (File.Exists(AppPaths.ExecutionLogFilePath))
            {
                File.Delete(AppPaths.ExecutionLogFilePath);
            }
        }
        catch
        {
            // Best-effort
        }
        finally
        {
            _lock.Release();
        }

        LogsUpdated?.Invoke(this, EventArgs.Empty);
    }
}
