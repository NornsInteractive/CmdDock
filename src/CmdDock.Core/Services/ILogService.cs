using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public interface ILogService
{
    Task AppendLogAsync(ExecutionLogEntry entry);
    Task<IReadOnlyList<ExecutionLogEntry>> GetRecentLogsAsync(int maxCount = 100);
    Task ClearLogsAsync();
    event EventHandler? LogsUpdated;
}
