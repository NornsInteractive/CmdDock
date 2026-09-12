using Microsoft.Windows.AppLifecycle;
using System;
using System.Threading.Tasks;

namespace CmdDock_App;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var logDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "CmdDock");
        if (!System.IO.Directory.Exists(logDir)) System.IO.Directory.CreateDirectory(logDir);
        var logFile = System.IO.Path.Combine(logDir, "app_startup.log");
        System.IO.File.AppendAllText(logFile, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Main started. Args: {string.Join(" ", args)}\n");

        WinRT.ComWrappersSupport.InitializeComWrappers();

        var mainInstance = AppInstance.FindOrRegisterForKey("CmdDock_Main_SingleInstance");
        System.IO.File.AppendAllText(logFile, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] FindOrRegisterForKey: IsCurrent = {mainInstance.IsCurrent}\n");

        if (!mainInstance.IsCurrent)
        {
            var activatedArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            System.IO.File.AppendAllText(logFile, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Redirecting to existing instance...\n");
            mainInstance.RedirectActivationToAsync(activatedArgs).AsTask().Wait();
            System.IO.File.AppendAllText(logFile, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] Redirected. Exiting secondary process.\n");
            return;
        }

        mainInstance.Activated += (sender, activatedArgs) =>
        {
            App.BringMainWindowToForeground();
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            try
            {
                System.IO.File.AppendAllText(logFile, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] DOMAIN UNHANDLED: {e.ExceptionObject}\n");
            }
            catch { }
        };

        try
        {
            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                System.Threading.SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        catch (Exception ex)
        {
            System.IO.File.AppendAllText(logFile, $"[{System.DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] APPLICATION START CRASH: {ex}\n");
        }
    }
}
