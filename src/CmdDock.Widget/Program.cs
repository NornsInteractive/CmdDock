using CmdDock.Widget;
using CmdDock.Widget.COM;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CmdDock.Widget;

internal static class Program
{
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("ole32.dll")]
    private static extern int CoRegisterClassObject(
        [MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
        uint dwClsContext,
        uint flags,
        out uint lpdwRegister);

    [DllImport("ole32.dll")]
    private static extern int CoRevokeClassObject(uint dwRegister);

    private const uint CLSCTX_LOCAL_SERVER = 0x4;
    private const uint REGCLS_MULTIPLEUSE = 0x1;

    public static void Log(string message)
    {
        try
        {
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CmdDock");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "widget_runtime.log");
            File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }

    [STAThread]
    private static void Main(string[] args)
    {
        Log($"Main started. Args: {string.Join(" ", args)}");

        try
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Log("ComWrappersSupport.InitializeComWrappers succeeded.");
        }
        catch (Exception ex)
        {
            Log($"ComWrappersSupport.InitializeComWrappers error: {ex}");
        }

        uint cookie = 0;
        var clsid = Guid.Parse(Guids.WidgetProviderClsid);

        var hr = CoRegisterClassObject(
            clsid,
            new WidgetProviderFactory<WidgetProvider>(),
            CLSCTX_LOCAL_SERVER,
            REGCLS_MULTIPLEUSE,
            out cookie);

        Log($"CoRegisterClassObject result: hr=0x{hr:X8}, cookie={cookie}");

        if (hr != 0)
        {
            Log($"CoRegisterClassObject failed with hr=0x{hr:X8}. Exiting.");
            return;
        }

        if (GetConsoleWindow() != IntPtr.Zero)
        {
            Console.WriteLine("[CmdDock.Widget] Provider registered successfully. Press ENTER to stop.");
            Console.ReadLine();
        }
        else
        {
            Log("Waiting for EmptyWidgetListEvent...");
            var emptyEvent = WidgetProvider.GetEmptyWidgetListEvent();
            emptyEvent.WaitOne();
            Log("EmptyWidgetListEvent signaled. Shutting down.");
        }

        if (cookie != 0)
        {
            CoRevokeClassObject(cookie);
            Log("CoRevokeClassObject completed.");
        }
    }
}
