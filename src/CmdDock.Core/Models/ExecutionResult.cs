namespace CmdDock.Core.Models;

public class ExecutionResult
{
    public string CommandId { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string StdOut { get; set; } = string.Empty;
    public string StdErr { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;

    public static ExecutionResult Succeeded(string id, string name, string stdout, long durationMs, int exitCode = 0)
    {
        return new ExecutionResult
        {
            CommandId = id,
            CommandName = name,
            Success = true,
            ExitCode = exitCode,
            StdOut = stdout,
            StdErr = string.Empty,
            DurationMs = durationMs,
            ExecutedAt = DateTime.UtcNow
        };
    }

    public static ExecutionResult Failed(string id, string name, string stderr, long durationMs, int exitCode = -1, string stdout = "")
    {
        return new ExecutionResult
        {
            CommandId = id,
            CommandName = name,
            Success = false,
            ExitCode = exitCode,
            StdOut = stdout,
            StdErr = stderr,
            DurationMs = durationMs,
            ExecutedAt = DateTime.UtcNow
        };
    }
}
