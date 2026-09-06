using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public interface ICommandExecutor
{
    Task<ExecutionResult> ExecuteAsync(CommandItem item, CancellationToken cancellationToken = default);
}
