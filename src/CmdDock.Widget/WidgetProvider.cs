using CmdDock.Core.Models;
using CmdDock.Core.Services;
using Microsoft.Windows.Widgets;
using Microsoft.Windows.Widgets.Providers;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace CmdDock.Widget;

public class CompactWidgetContext
{
    public string WidgetId { get; set; } = string.Empty;
    public WidgetSize Size { get; set; }
    public bool IsActive { get; set; }
    public string ViewMode { get; set; } = "grid";
    public int CurrentPage { get; set; } = 0;
    public int CategoryPageIndex { get; set; } = 0;
    public string? ConfirmingCommandId { get; set; }
}

[Guid("B6A68D64-323A-4E38-A372-2D99BE1F1E85")]
public class WidgetProvider : IWidgetProvider, IWidgetProvider2
{
    private static readonly Dictionary<string, CompactWidgetContext> ActiveWidgets = new();
    private static readonly ManualResetEvent EmptyWidgetListEvent = new(false);
    private static readonly object SyncRoot = new();

    private static readonly ICommandService _commandService = new CommandService();
    private static readonly ICommandExecutor _commandExecutor = new CommandExecutor(_commandService, new LogService());

    private static FileSystemWatcher? _fileWatcher;
    private static System.Threading.Timer? _debounceTimer;

    static WidgetProvider()
    {
        InitializeFileWatcher();
    }

    public WidgetProvider()
    {
    }

    private static void InitializeFileWatcher()
    {
        try
        {
            var dir = Path.GetDirectoryName(AppPaths.CommandsFilePath);
            if (!string.IsNullOrEmpty(dir))
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _fileWatcher = new FileSystemWatcher(dir, "*.json")
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
                };

                _fileWatcher.Changed += OnCommandsFileChanged;
                _fileWatcher.Created += OnCommandsFileChanged;
                _fileWatcher.Renamed += OnCommandsFileChanged;
                _fileWatcher.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[CmdDock.Widget] FileWatcher error: {ex.Message}");
        }
    }

    private static void OnCommandsFileChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new System.Threading.Timer(_ =>
        {
            UpdateAllActiveWidgets();
        }, null, 150, Timeout.Infinite);
    }

    public static void UpdateAllActiveWidgets(string? statusMessage = null)
    {
        List<CompactWidgetContext> list;
        lock (SyncRoot)
        {
            list = ActiveWidgets.Values.ToList();
        }

        var status = statusMessage ?? I18nService.Instance["Commands.StatusReady"];
        var savedMode = WidgetSettings.GetViewMode();
        foreach (var ctx in list)
        {
            UpdateWidgetUI(ctx.WidgetId, ctx.Size, status, savedMode);
        }
    }

    public static ManualResetEvent GetEmptyWidgetListEvent() => EmptyWidgetListEvent;

    public void CreateWidget(WidgetContext widgetContext)
    {
        var id = widgetContext.Id;
        var mode = WidgetSettings.GetViewMode();
        Program.Log($"[WidgetProvider] CreateWidget: id={id}, size={widgetContext.Size}, isActive={widgetContext.IsActive}");
        lock (SyncRoot)
        {
            ActiveWidgets[id] = new CompactWidgetContext
            {
                WidgetId = id,
                Size = widgetContext.Size,
                IsActive = widgetContext.IsActive,
                ViewMode = mode
            };
            EmptyWidgetListEvent.Reset();
        }

        UpdateWidgetUI(id, widgetContext.Size, I18nService.Instance["Commands.StatusReady"], mode);
    }

    public void Activate(WidgetContext widgetContext)
    {
        var mode = WidgetSettings.GetViewMode();
        Program.Log($"[WidgetProvider] Activate: id={widgetContext.Id}, size={widgetContext.Size}, isActive={widgetContext.IsActive}");
        lock (SyncRoot)
        {
            if (ActiveWidgets.TryGetValue(widgetContext.Id, out var context))
            {
                context.IsActive = true;
                context.Size = widgetContext.Size;
                context.ViewMode = mode;
            }
            else
            {
                ActiveWidgets[widgetContext.Id] = new CompactWidgetContext
                {
                    WidgetId = widgetContext.Id,
                    Size = widgetContext.Size,
                    IsActive = true,
                    ViewMode = mode
                };
            }
        }

        UpdateWidgetUI(widgetContext.Id, widgetContext.Size, I18nService.Instance["Commands.StatusReady"], mode);
    }

    public void Deactivate(string widgetId)
    {
        Program.Log($"[WidgetProvider] Deactivate: id={widgetId}");
        lock (SyncRoot)
        {
            if (ActiveWidgets.TryGetValue(widgetId, out var context))
            {
                context.IsActive = false;
            }
        }
    }

    public void DeleteWidget(string widgetId, string customState)
    {
        Program.Log($"[WidgetProvider] DeleteWidget: id={widgetId}");
        lock (SyncRoot)
        {
            ActiveWidgets.Remove(widgetId);
            if (ActiveWidgets.Count == 0)
            {
                EmptyWidgetListEvent.Set();
            }
        }
    }

    public void OnWidgetContextChanged(WidgetContextChangedArgs contextChangedArgs)
    {
        var widgetContext = contextChangedArgs.WidgetContext;
        var mode = WidgetSettings.GetViewMode();
        Program.Log($"[WidgetProvider] OnWidgetContextChanged: id={widgetContext.Id}, size={widgetContext.Size}");
        lock (SyncRoot)
        {
            if (ActiveWidgets.TryGetValue(widgetContext.Id, out var context))
            {
                context.Size = widgetContext.Size;
            }
            else
            {
                ActiveWidgets[widgetContext.Id] = new CompactWidgetContext
                {
                    WidgetId = widgetContext.Id,
                    Size = widgetContext.Size,
                    IsActive = true,
                    ViewMode = mode
                };
            }
        }

        UpdateWidgetUI(widgetContext.Id, widgetContext.Size, I18nService.Instance["Commands.StatusReady"], mode);
    }

    public async void OnActionInvoked(WidgetActionInvokedArgs actionInvokedArgs)
    {
        var verb = actionInvokedArgs.Verb;
        var data = actionInvokedArgs.Data;
        var widgetId = actionInvokedArgs.WidgetContext.Id;
        var size = actionInvokedArgs.WidgetContext.Size;
        Program.Log($"[WidgetProvider] OnActionInvoked: verb={verb}, widgetId={widgetId}, size={size}, data={data}");

        if (verb == "openApp")
        {
            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    context.ConfirmingCommandId = null;
                }
            }
            LaunchCompanionApp();
            return;
        }

        if (verb == "toggleView")
        {
            string currentMode = WidgetSettings.GetViewMode();
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("currentMode", out var modeProp))
                    {
                        var m = modeProp.GetString();
                        if (!string.IsNullOrEmpty(m)) currentMode = m;
                    }
                }
            }
            catch { }

            string newMode = currentMode == "grid" ? "list" : "grid";
            WidgetSettings.SetViewMode(newMode);

            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    context.ConfirmingCommandId = null;
                    context.ViewMode = newMode;
                    context.Size = size;
                }
                else
                {
                    ActiveWidgets[widgetId] = new CompactWidgetContext
                    {
                        WidgetId = widgetId,
                        Size = size,
                        IsActive = true,
                        ViewMode = newMode
                    };
                }
            }

            UpdateWidgetUI(widgetId, size, null, newMode);
            return;
        }

        if (verb == "changePage")
        {
            int targetPage = 0;
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("page", out var pageProp))
                    {
                        targetPage = pageProp.GetInt32();
                    }
                }
            }
            catch { }

            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    context.ConfirmingCommandId = null;
                    context.CurrentPage = Math.Max(0, targetPage);
                }
            }

            UpdateWidgetUI(widgetId, size);
            return;
        }

        if (verb == "changeCategoryPage")
        {
            int targetCatPage = 0;
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("catPage", out var catProp))
                    {
                        targetCatPage = catProp.GetInt32();
                    }
                }
            }
            catch { }

            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    context.ConfirmingCommandId = null;
                    context.CategoryPageIndex = Math.Max(0, targetCatPage);
                }
            }

            UpdateWidgetUI(widgetId, size);
            return;
        }

        if (verb == "filterCategory" || verb == "selectCategory")
        {
            string selectedCat = "全部";
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("category", out var catProp))
                    {
                        var c = catProp.GetString();
                        if (!string.IsNullOrWhiteSpace(c)) selectedCat = c.Trim();
                    }
                }
            }
            catch { }

            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    context.ConfirmingCommandId = null;
                    context.CurrentPage = 0;
                }
            }

            WidgetSettings.SetSelectedCategory(selectedCat);
            string currentMode = WidgetSettings.GetViewMode();
            UpdateWidgetUI(widgetId, size, null, currentMode, selectedCat);
            return;
        }

        if (verb == "runCommand")
        {
            string? commandId = null;
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("commandId", out var idProp))
                    {
                        commandId = idProp.GetString();
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(commandId))
            {
                var command = await _commandService.GetByIdAsync(commandId);
                if (command != null)
                {
                    if (command.RequireConfirmation)
                    {
                        lock (SyncRoot)
                        {
                            if (ActiveWidgets.TryGetValue(widgetId, out var context))
                            {
                                context.ConfirmingCommandId = command.Id;
                            }
                        }
                        UpdateWidgetUI(widgetId, size, I18nService.Instance["Widget.ConfirmTitle"], confirmingCommandId: command.Id);
                        return;
                    }

                    lock (SyncRoot)
                    {
                        if (ActiveWidgets.TryGetValue(widgetId, out var context))
                        {
                            context.ConfirmingCommandId = null;
                        }
                    }

                    command.LastRunStatus = ExecutionStatus.Running;
                    await _commandService.UpdateExecutionStatusAsync(command.Id, ExecutionStatus.Running, null, null);
                    UpdateAllActiveWidgets(I18nService.Instance.Format("Widget.ConfirmExecuting", command.DisplayName));

                    var result = await _commandExecutor.ExecuteAsync(command);

                    var resultMsg = result.Success 
                        ? I18nService.Instance.Format("Widget.ExecSuccess", command.DisplayName, result.DurationMs) 
                        : I18nService.Instance.Format("Widget.ExecFailed", command.DisplayName, result.ExitCode);

                    UpdateAllActiveWidgets(resultMsg);
                }
            }
            return;
        }

        if (verb == "confirmRunCommand")
        {
            string? commandId = null;
            try
            {
                if (!string.IsNullOrEmpty(data))
                {
                    using var doc = JsonDocument.Parse(data);
                    if (doc.RootElement.TryGetProperty("commandId", out var idProp))
                    {
                        commandId = idProp.GetString();
                    }
                }
            }
            catch { }

            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    if (string.IsNullOrEmpty(commandId))
                    {
                        commandId = context.ConfirmingCommandId;
                    }
                    context.ConfirmingCommandId = null;
                }
            }

            if (!string.IsNullOrEmpty(commandId))
            {
                var command = await _commandService.GetByIdAsync(commandId);
                if (command != null)
                {
                    command.LastRunStatus = ExecutionStatus.Running;
                    await _commandService.UpdateExecutionStatusAsync(command.Id, ExecutionStatus.Running, null, null);
                    UpdateAllActiveWidgets(I18nService.Instance.Format("Widget.ConfirmExecuting", command.DisplayName));

                    var result = await _commandExecutor.ExecuteAsync(command);

                    var resultMsg = result.Success 
                        ? I18nService.Instance.Format("Widget.ExecSuccess", command.DisplayName, result.DurationMs) 
                        : I18nService.Instance.Format("Widget.ExecFailed", command.DisplayName, result.ExitCode);

                    UpdateAllActiveWidgets(resultMsg);
                }
            }
            return;
        }

        if (verb == "cancelConfirm")
        {
            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var context))
                {
                    context.ConfirmingCommandId = null;
                }
            }
            UpdateWidgetUI(widgetId, size, I18nService.Instance["Widget.ConfirmCancelled"]);
            return;
        }
    }

    public void OnCustomizationRequested(WidgetCustomizationRequestedArgs customizationInvokedArgs)
    {
        Program.Log("[WidgetProvider] OnCustomizationRequested invoked.");
        LaunchCompanionApp();
    }

    private static async void UpdateWidgetUI(string widgetId, WidgetSize size, string? statusMessage = null, string? viewMode = null, string? selectedCategory = null, string? confirmingCommandId = null)
    {
        var effectiveStatus = statusMessage ?? I18nService.Instance["Commands.StatusReady"];
        Program.Log($"[WidgetProvider] UpdateWidgetUI: widgetId={widgetId}, size={size}, status={effectiveStatus}");
        try
        {
            var commands = await _commandService.GetWidgetCommandsAsync();
            var sizeStr = size switch
            {
                WidgetSize.Small => "small",
                WidgetSize.Large => "large",
                _ => "medium"
            };

            string currentViewMode = viewMode ?? WidgetSettings.GetViewMode();
            string currentCategory = selectedCategory ?? WidgetSettings.GetSelectedCategory();
            int currentPage = 0;
            int categoryPageIndex = 0;
            string? confirmingId = confirmingCommandId;

            lock (SyncRoot)
            {
                if (ActiveWidgets.TryGetValue(widgetId, out var ctx))
                {
                    if (viewMode == null) currentViewMode = ctx.ViewMode;
                    currentPage = ctx.CurrentPage;
                    categoryPageIndex = ctx.CategoryPageIndex;
                    if (confirmingId == null) confirmingId = ctx.ConfirmingCommandId;
                }
            }

            CommandItem? cmdToConfirm = null;
            if (!string.IsNullOrEmpty(confirmingId))
            {
                cmdToConfirm = await _commandService.GetByIdAsync(confirmingId);
            }

            var layoutMode = WidgetSettings.GetLayoutMode();
            var paginationStyle = WidgetSettings.GetPaginationStyle();

            var cardJson = WidgetCardBuilder.BuildCard(
                sizeStr, 
                commands, 
                effectiveStatus, 
                currentViewMode, 
                currentCategory, 
                layoutMode, 
                paginationStyle, 
                currentPage, 
                categoryPageIndex,
                confirmingCommandId: confirmingId,
                confirmingCommand: cmdToConfirm);

            var updateOptions = new WidgetUpdateRequestOptions(widgetId)
            {
                Template = cardJson,
                Data = "{}"
            };

            WidgetManager.GetDefault().UpdateWidget(updateOptions);
            Program.Log($"[WidgetProvider] UpdateWidget succeeded for {widgetId}");
        }
        catch (Exception ex)
        {
            Program.Log($"[WidgetProvider] UpdateWidget error: {ex}");
            Debug.WriteLine($"[CmdDock.Widget] UpdateWidget error: {ex.Message}");
        }
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern void SwitchToThisWindow(IntPtr hWnd, bool fAltTab);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    private const int SW_RESTORE = 9;

    private static void LaunchCompanionApp()
    {
        try
        {
            // 1. Check if CmdDock.App is already running, and bring its window to front
            var processes = Process.GetProcessesByName("CmdDock.App");
            if (processes.Length > 0)
            {
                foreach (var proc in processes)
                {
                    try
                    {
                        var hwnd = proc.MainWindowHandle;
                        if (hwnd == IntPtr.Zero)
                        {
                            hwnd = FindWindowForProcess((uint)proc.Id);
                        }

                        if (hwnd != IntPtr.Zero)
                        {
                            ShowWindow(hwnd, SW_RESTORE);
                            BringWindowToTop(hwnd);
                            SetForegroundWindow(hwnd);
                            SwitchToThisWindow(hwnd, true);
                            return;
                        }
                    }
                    catch { }
                }
            }

            // 2. Not running: launch via AUMID or exe
            var aumid = "Norns.CmdDock_sxt4q0tm9x2xr!App";
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"shell:AppsFolder\\{aumid}",
                    UseShellExecute = true
                });
                return;
            }
            catch { }

            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            var appPath = Path.Combine(appDir, "CmdDock.App.exe");
            if (File.Exists(appPath))
            {
                Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = true });
            }
            else
            {
                var altPath = Path.GetFullPath(Path.Combine(appDir, "..", "CmdDock.App", "CmdDock.App.exe"));
                if (File.Exists(altPath))
                {
                    Process.Start(new ProcessStartInfo(altPath) { UseShellExecute = true });
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LaunchCompanionApp] Error: {ex.Message}");
        }
    }

    private static IntPtr FindWindowForProcess(uint targetPid)
    {
        IntPtr result = IntPtr.Zero;
        EnumWindows((hWnd, lParam) =>
        {
            GetWindowThreadProcessId(hWnd, out uint pid);
            if (pid == targetPid && IsWindowVisible(hWnd))
            {
                result = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        return result;
    }
}
