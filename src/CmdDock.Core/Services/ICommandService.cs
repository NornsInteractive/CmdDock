using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public interface ICommandService
{
    Task<IReadOnlyList<CommandItem>> GetAllAsync(bool forceReload = false);
    Task<IReadOnlyList<CommandItem>> GetWidgetCommandsAsync();
    Task<CommandItem?> GetByIdAsync(string id);
    Task AddOrUpdateAsync(CommandItem item);
    Task DeleteAsync(string id);
    Task SaveAllAsync(IEnumerable<CommandItem> items);
    Task UpdateExecutionStatusAsync(string id, ExecutionStatus status, int? exitCode, long? durationMs);
    event EventHandler? CommandsChanged;
}
