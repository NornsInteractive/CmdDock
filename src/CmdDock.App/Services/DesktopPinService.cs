using System.Runtime.InteropServices;
using CmdDock.Core.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace CmdDock_App.Services;

public static class DesktopPinService
{
    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new(-2);
    private static readonly IntPtr HWND_BOTTOM = new(1);

    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string? lclassName, string? windowTitle);

    [DllImport("user32.dll")]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam, uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    private static IntPtr _originalParent = IntPtr.Zero;

    public static void ApplyPinMode(Window? window, DesktopPinMode mode)
    {
        if (window == null) return;
        try
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            if (hWnd == IntPtr.Zero) return;

            var appWindow = window.AppWindow;
            var presenter = appWindow?.Presenter as OverlappedPresenter;

            switch (mode)
            {
                case DesktopPinMode.AlwaysOnTop:
                    RestoreParentIfNeeded(hWnd);
                    if (presenter != null)
                    {
                        presenter.IsAlwaysOnTop = true;
                    }
                    SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    break;

                case DesktopPinMode.Normal:
                    RestoreParentIfNeeded(hWnd);
                    if (presenter != null)
                    {
                        presenter.IsAlwaysOnTop = false;
                    }
                    SetWindowPos(hWnd, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    break;

                case DesktopPinMode.PinToDesktop:
                    RestoreParentIfNeeded(hWnd);
                    if (presenter != null)
                    {
                        presenter.IsAlwaysOnTop = false;
                    }
                    SetWindowPos(hWnd, HWND_BOTTOM, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                    break;
            }
        }
        catch
        {
            // Graceful fallback
        }
    }

    private static void RestoreParentIfNeeded(IntPtr hWnd)
    {
        if (_originalParent != IntPtr.Zero)
        {
            SetParent(hWnd, IntPtr.Zero);
            _originalParent = IntPtr.Zero;
        }
    }

    private static IntPtr GetDesktopWorkerW()
    {
        IntPtr progman = FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return IntPtr.Zero;

        // Send 0x052C to Progman to spawn a WorkerW behind icons
        SendMessageTimeout(progman, 0x052C, new IntPtr(0xD), new IntPtr(0), 0, 1000, out _);

        IntPtr workerw = IntPtr.Zero;
        // Enum windows to find the WorkerW behind icons
        IntPtr defView = IntPtr.Zero;
        IntPtr temp = IntPtr.Zero;
        while ((temp = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null)) != IntPtr.Zero)
        {
            defView = FindWindowEx(temp, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView != IntPtr.Zero)
            {
                // The WorkerW immediately following this one is the target
                workerw = FindWindowEx(IntPtr.Zero, temp, "WorkerW", null);
                break;
            }
        }

        return workerw != IntPtr.Zero ? workerw : progman;
    }
}
