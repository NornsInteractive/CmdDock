using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public class PresetService : IPresetService
{
    public IReadOnlyList<string> GetDefaultGroups() => new[] { "常用", "系统", "网络", "开发", "运维" };

    public IReadOnlyList<CommandItem> GetBuiltinPresets()
    {
        return new List<CommandItem>
        {
            // === 系统类 ===
            new()
            {
                Id = "preset_restart_explorer",
                Name = "重启资源管理器",
                Description = "重启 Windows Explorer 进程，解决任务栏冻结或桌面图标卡死",
                Group = "系统",
                IconGlyph = "\uE777", // Sync / Restart
                ShellType = ShellType.PowerShell,
                CommandText = "Stop-Process -Name explorer -Force; Start-Process explorer",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 1
            },
            new()
            {
                Id = "preset_clean_temp",
                Name = "清理系统临时文件",
                Description = "深度清空当前用户的临时垃圾与缓存目录，释放磁盘空间",
                Group = "系统",
                IconGlyph = "\uE74D", // Delete / Clear
                ShellType = ShellType.PowerShell,
                CommandText = "Get-ChildItem -Path $env:TEMP -Force -ErrorAction SilentlyContinue | ForEach-Object { try { Remove-Item $_.FullName -Recurse -Force -ErrorAction Stop } catch { } }; exit 0",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 2
            },
            new()
            {
                Id = "preset_clear_clipboard",
                Name = "清空系统剪贴板",
                Description = "清理当前系统剪贴板中的敏感复制内容与图片数据",
                Group = "系统",
                IconGlyph = "\uE77F", // Clipboard
                ShellType = ShellType.PowerShell,
                CommandText = "Set-Clipboard -Value ''",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 3
            },
            new()
            {
                Id = "preset_lock_screen",
                Name = "一键锁屏",
                Description = "立即锁定当前 Windows 桌面工作会话",
                Group = "系统",
                IconGlyph = "\uE72E", // Lock
                ShellType = ShellType.Cmd,
                CommandText = "rundll32.exe user32.dll,LockWorkStation",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 4
            },
            new()
            {
                Id = "preset_taskmgr",
                Name = "快速打开任务管理器",
                Description = "即时唤起 Windows 任务管理器查看 CPU、内存与后台进程",
                Group = "系统",
                IconGlyph = "\uE9D9", // TaskView / Diagnostic
                ShellType = ShellType.Executable,
                CommandText = "taskmgr.exe",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 5
            },
            new()
            {
                Id = "preset_env_vars",
                Name = "系统环境变量设置",
                Description = "直接调出 Windows 高级系统环境变量配置窗口",
                Group = "系统",
                IconGlyph = "\uE7F8", // PC / System
                ShellType = ShellType.Cmd,
                CommandText = "start rundll32.exe sysdm.cpl,EditEnvironmentVariables",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 6
            },
            new()
            {
                Id = "preset_disk_health",
                Name = "磁盘驱动器健康状态",
                Description = "查询本机所有物理硬盘的健康与运行状态（基于 Win 原生 PowerShell）",
                Group = "系统",
                IconGlyph = "\uEDA2", // HardDrive
                ShellType = ShellType.PowerShell,
                CommandText = "Get-PhysicalDisk | Select-Object DeviceId, FriendlyName, MediaType, OperationalStatus, HealthStatus | Format-Table -AutoSize",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 7
            },
            new()
            {
                Id = "preset_defender_scan",
                Name = "Windows 安全中心",
                Description = "快速打开 Windows Defender 安全中心防护面板",
                Group = "系统",
                IconGlyph = "\uE72B", // Shield
                ShellType = ShellType.UrlProtocol,
                CommandText = "windowsdefender:",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = false,
                Order = 8
            },

            // === 网络类 ===
            new()
            {
                Id = "preset_flush_dns",
                Name = "刷新 DNS 缓存",
                Description = "清除本地 DNS 解析缓存，解决域名访问失效与解析异常",
                Group = "网络",
                IconGlyph = "\uE774", // Globe
                ShellType = ShellType.Cmd,
                CommandText = "ipconfig /flushdns",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 9
            },
            new()
            {
                Id = "preset_port_listening",
                Name = "查看正在监听端口",
                Description = "在终端中展示当前所有正在监听的 TCP 端口及所属进程 PID",
                Group = "网络",
                IconGlyph = "\uE8B0", // Cable / Connect
                ShellType = ShellType.Cmd,
                CommandText = "netstat -ano | findstr LISTENING",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 10
            },
            new()
            {
                Id = "preset_release_8080",
                Name = "快速释放 8080 端口",
                Description = "查找并强制终止占用 8080 端口的后台进程",
                Group = "网络",
                IconGlyph = "\uE7BA", // Warning / Action
                ShellType = ShellType.PowerShell,
                CommandText = "Get-NetTCPConnection -LocalPort 8080 -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }",
                ExecutionMode = ExecutionMode.Silent,
                RequireConfirmation = true,
                ShowInWidget = true,
                Order = 11
            },
            new()
            {
                Id = "preset_public_ip",
                Name = "测试公网 IP 与连通性",
                Description = "查询本机当前出口公网 IP 地址并测试网络延迟",
                Group = "网络",
                IconGlyph = "\uE774", // Globe
                ShellType = ShellType.PowerShell,
                CommandText = "curl.exe -s https://ifconfig.me/ip; ping -n 3 8.8.8.8",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 12
            },
            new()
            {
                Id = "preset_reset_winsock",
                Name = "重置 Winsock 协议栈",
                Description = "重置系统网络套接字协议栈，解决严重断网或代理残留问题",
                Group = "网络",
                IconGlyph = "\uE706", // Wifi / Network
                ShellType = ShellType.Cmd,
                CommandText = "netsh winsock reset",
                ExecutionMode = ExecutionMode.Elevated,
                RequireConfirmation = true,
                ShowInWidget = false,
                Order = 13
            },

            // === 开发与运维类 ===
            new()
            {
                Id = "preset_docker_ps",
                Name = "Docker 运行容器状态",
                Description = "查看正在运行的 Docker 容器列表、状态和端口映射（需先安装 Docker）",
                Group = "开发",
                IconGlyph = "\uE74C", // Cloud
                ShellType = ShellType.PowerShell,
                CommandText = "docker ps --format \"table {{.Names}}\t{{.Status}}\t{{.Ports}}\"",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 14
            },
            new()
            {
                Id = "preset_docker_prune",
                Name = "清理 Docker 虚悬缓存",
                Description = "一键清理无用的 Docker 悬空镜像、容器和网络构建缓存（需先安装 Docker）",
                Group = "开发",
                IconGlyph = "\uE7F4", // Package
                ShellType = ShellType.PowerShell,
                CommandText = "docker system prune -f",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = true,
                ShowInWidget = false,
                Order = 15
            },
            new()
            {
                Id = "preset_python_env",
                Name = "Python 依赖包列表",
                Description = "列出当前 Python 环境下所有已安装的第三方包版本（需先安装 Python）",
                Group = "开发",
                IconGlyph = "\uE943", // Code
                ShellType = ShellType.PowerShell,
                CommandText = "python -m pip list",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 16
            },
            new()
            {
                Id = "preset_http_server",
                Name = "启动临时 HTTP 服务器",
                Description = "在 8080 端口启动 Windows 原生 HTTP 静态服务器（免装 Python，自动打开浏览器）",
                Group = "开发",
                IconGlyph = "\uE8A7", // Server
                ShellType = ShellType.PowerShell,
                CommandText = "$port = 8080; $listener = New-Object System.Net.HttpListener; $listener.Prefixes.Add('http://localhost:8080/'); $listener.Start(); Write-Host '===================================================' -ForegroundColor Cyan; Write-Host ' CmdDock 原生 HTTP 文件服务器已启动 (端口: 8080)' -ForegroundColor Green; Write-Host ' 本地访问地址: http://localhost:8080/' -ForegroundColor Yellow; Write-Host ' 当前托管目录: ' (Get-Location) -ForegroundColor White; Write-Host ' 按 Ctrl + C 可随时终止服务器' -ForegroundColor Gray; Write-Host '===================================================' -ForegroundColor Cyan; Start-Process 'http://localhost:8080/'; while ($listener.IsListening) { $ctx = $listener.GetContext(); $req = $ctx.Request; $res = $ctx.Response; $rel = $req.Url.LocalPath.TrimStart('/'); if ([string]::IsNullOrEmpty($rel)) { $rel = 'index.html' }; $path = Join-Path (Get-Location) $rel; if (Test-Path $path -PathType Leaf) { $bytes = [System.IO.File]::ReadAllBytes($path); $res.ContentLength64 = $bytes.Length; $res.OutputStream.Write($bytes, 0, $bytes.Length) } else { $items = Get-ChildItem | ForEach-Object { '<li><a href=' + $_.Name + '>' + $_.Name + '</a></li>' }; $html = '<html><head><meta charset=utf-8><title>CmdDock HTTP Server</title></head><body><h2>CmdDock 本地目录文件列表</h2><p>当前目录: ' + (Get-Location) + '</p><ul>' + ($items -join '') + '</ul></body></html>'; $bytes = [System.Text.Encoding]::UTF8.GetBytes($html); $res.ContentType = 'text/html; charset=utf-8'; $res.ContentLength64 = $bytes.Length; $res.OutputStream.Write($bytes, 0, $bytes.Length) }; $res.OutputStream.Close() }",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 17
            },
            new()
            {
                Id = "preset_wsl_terminal",
                Name = "进入 WSL Linux 环境",
                Description = "一键打开并进入 Windows Subsystem for Linux 终端环境（需先安装 WSL）",
                Group = "开发",
                IconGlyph = "\uE756", // Terminal
                ShellType = ShellType.Executable,
                CommandText = "wsl.exe",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = true,
                Order = 18
            },
            new()
            {
                Id = "preset_git_config",
                Name = "Git 全局配置查看",
                Description = "在终端中展示 Git 的全局配置项与来源文件（需先安装 Git）",
                Group = "开发",
                IconGlyph = "\uE9E9", // Branch
                ShellType = ShellType.Cmd,
                CommandText = "git config --list --show-origin",
                ExecutionMode = ExecutionMode.Terminal,
                RequireConfirmation = false,
                ShowInWidget = false,
                Order = 19
            }
        };
    }
}
