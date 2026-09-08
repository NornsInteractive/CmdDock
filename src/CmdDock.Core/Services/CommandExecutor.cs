using CmdDock.Core.Models;
using System.Diagnostics;
using System.Text;

namespace CmdDock.Core.Services;

public class CommandExecutor : ICommandExecutor
{
    private readonly ICommandService _commandService;
    private readonly ILogService _logService;

    public CommandExecutor(ICommandService commandService, ILogService logService)
    {
        _commandService = commandService;
        _logService = logService;
    }

    public async Task<ExecutionResult> ExecuteAsync(CommandItem item, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await _commandService.UpdateExecutionStatusAsync(item.Id, ExecutionStatus.Running, null, null);

        ExecutionResult result;

        try
        {
            if (item.ShellType == ShellType.UrlProtocol)
            {
                result = ExecuteUrlProtocol(item, stopwatch);
            }
            else
            {
                result = await ExecuteProcessAsync(item, stopwatch, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result = ExecutionResult.Failed(
                item.Id,
                item.DisplayName,
                I18nService.Instance.Format("Executor.LaunchException", ex.Message),
                stopwatch.ElapsedMilliseconds,
                -1);
        }

        // Update command item status
        var status = result.Success ? ExecutionStatus.Success : ExecutionStatus.Failed;
        await _commandService.UpdateExecutionStatusAsync(item.Id, status, result.ExitCode, result.DurationMs);

        // Record execution log
        await _logService.AppendLogAsync(new ExecutionLogEntry
        {
            CommandId = item.Id,
            CommandName = item.DisplayName,
            ShellType = item.ShellType.ToString(),
            CommandText = item.CommandText,
            ExecutionMode = item.ExecutionMode.ToString(),
            Success = result.Success,
            ExitCode = result.ExitCode,
            Output = string.IsNullOrWhiteSpace(result.StdErr) ? result.StdOut : $"{result.StdOut}\n[Error]: {result.StdErr}".Trim(),
            DurationMs = result.DurationMs,
            Timestamp = result.ExecutedAt
        });

        return result;
    }

    private ExecutionResult ExecuteUrlProtocol(CommandItem item, Stopwatch stopwatch)
    {
        var psi = new ProcessStartInfo
        {
            FileName = item.CommandText,
            UseShellExecute = true
        };

        Process.Start(psi);
        stopwatch.Stop();
        return ExecutionResult.Succeeded(item.Id, item.DisplayName, I18nService.Instance["Executor.UrlProtocolOpened"], stopwatch.ElapsedMilliseconds);
    }

    private async Task<ExecutionResult> ExecuteProcessAsync(CommandItem item, Stopwatch stopwatch, CancellationToken cancellationToken)
    {
        // For Executables (e.g. taskmgr.exe, notepad.exe, or any desktop app):
        // Always launch via ShellExecute so Windows handles UAC elevation handoff,
        // and avoid blocking or failing on GUI apps.
        if (item.ShellType == ShellType.Executable)
        {
            var exeArgs = string.IsNullOrWhiteSpace(item.Arguments) ? "" : item.Arguments;
            var psiExe = new ProcessStartInfo
            {
                FileName = item.CommandText,
                Arguments = exeArgs,
                UseShellExecute = true,
                WorkingDirectory = string.IsNullOrWhiteSpace(item.WorkingDirectory) ? Environment.CurrentDirectory : item.WorkingDirectory
            };

            if (item.ExecutionMode == ExecutionMode.Elevated)
            {
                psiExe.Verb = "runas";
            }

            using var process = Process.Start(psiExe);
            stopwatch.Stop();
            return ExecutionResult.Succeeded(
                item.Id,
                item.DisplayName,
                I18nService.Instance["Executor.TerminalLaunched"],
                stopwatch.ElapsedMilliseconds,
                0);
        }

        var (fileName, arguments, useShellExecute, createNoWindow) = ResolveLaunchParameters(item);

        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = useShellExecute,
            CreateNoWindow = createNoWindow,
            WorkingDirectory = string.IsNullOrWhiteSpace(item.WorkingDirectory) ? Environment.CurrentDirectory : item.WorkingDirectory
        };

        if (item.ExecutionMode == ExecutionMode.Elevated)
        {
            psi.Verb = "runas";
            psi.UseShellExecute = true;
            psi.CreateNoWindow = false;
        }

        // Silent mode captures stdout / stderr
        if (item.ExecutionMode == ExecutionMode.Silent)
        {
            psi.RedirectStandardOutput = true;
            psi.RedirectStandardError = true;
            psi.StandardOutputEncoding = Encoding.UTF8;
            psi.StandardErrorEncoding = Encoding.UTF8;

            using var process = new Process { StartInfo = psi };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) outputBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) errorBuilder.AppendLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            stopwatch.Stop();

            var stdout = outputBuilder.ToString().Trim();
            var stderr = errorBuilder.ToString().Trim();

            // Taskmgr and some Windows elevation broker stubs return -2147467260 (0x80004004 E_ABORT) upon successful handoff
            bool isSuccess = process.ExitCode == 0 ||
                (process.ExitCode == -2147467260 && item.CommandText.Contains("taskmgr", StringComparison.OrdinalIgnoreCase));

            if (isSuccess)
            {
                return ExecutionResult.Succeeded(item.Id, item.DisplayName, stdout, stopwatch.ElapsedMilliseconds, 0);
            }
            else
            {
                var errorMsg = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                return ExecutionResult.Failed(item.Id, item.DisplayName, errorMsg, stopwatch.ElapsedMilliseconds, process.ExitCode, stdout);
            }
        }
        else
        {
            // Terminal or Elevated mode: start directly
            using var process = Process.Start(psi);
            stopwatch.Stop();

            return ExecutionResult.Succeeded(
                item.Id,
                item.DisplayName,
                I18nService.Instance["Executor.TerminalLaunched"],
                stopwatch.ElapsedMilliseconds,
                0);
        }
    }

    private (string FileName, string Arguments, bool UseShellExecute, bool CreateNoWindow) ResolveLaunchParameters(CommandItem item)
    {
        bool isTerminal = item.ExecutionMode == ExecutionMode.Terminal;
        string cmd = item.CommandText;

        switch (item.ShellType)
        {
            case ShellType.PowerShell:
                if (isTerminal)
                {
                    return ("powershell.exe", $"-NoExit -ExecutionPolicy Bypass -Command \"{cmd}\"", true, false);
                }
                else
                {
                    return ("powershell.exe", $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"{cmd}\"", false, true);
                }

            case ShellType.Cmd:
                if (isTerminal)
                {
                    return ("cmd.exe", $"/k \"{cmd}\"", true, false);
                }
                else
                {
                    return ("cmd.exe", $"/c \"{cmd}\"", false, true);
                }

            case ShellType.Wsl:
                if (isTerminal)
                {
                    return ("wsl.exe", $"-e bash -c \"{cmd}; exec bash\"", true, false);
                }
                else
                {
                    return ("wsl.exe", $"-e bash -c \"{cmd}\"", false, true);
                }

            case ShellType.Executable:
            default:
                var args = string.IsNullOrWhiteSpace(item.Arguments) ? "" : item.Arguments;
                return (cmd, args, isTerminal, !isTerminal);
        }
    }
}
