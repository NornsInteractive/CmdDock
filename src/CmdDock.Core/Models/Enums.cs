namespace CmdDock.Core.Models;

public enum ShellType
{
    PowerShell,
    Cmd,
    Wsl,
    Executable,
    UrlProtocol
}

public enum ExecutionMode
{
    Silent,     // Background silent execution, captures output
    Terminal,   // Pop up Windows Terminal or ConHost window
    Elevated    // Run as Administrator (UAC prompt)
}

public enum ExecutionStatus
{
    Idle,
    Running,
    Success,
    Failed
}
