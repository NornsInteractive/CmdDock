using System;
using System.IO;
using System.Runtime.InteropServices;
using CmdDock.Core.Services;
using CmdDock_App.Views;
using Microsoft.UI.Xaml;

namespace CmdDock_App.Services;

public class TrayIconService : IDisposable
{
    private static TrayIconService? _instance;
    public static TrayIconService Instance => _instance ??= new TrayIconService();

    private const int WM_USER = 0x0400;
    private const int WM_TRAYICON = WM_USER + 101;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_LBUTTONDBLCLK = 0x0203;
    private const int WM_RBUTTONUP = 0x0205;

    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint MF_SEPARATOR = 0x0800;
    private const uint MF_STRING = 0x0000;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public int dwState;
        public int dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public int uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public int dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpdata);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEX
    {
        public int cbSize;
        public int style;
        public IntPtr lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public IntPtr hInstance;
        public IntPtr hIcon;
        public IntPtr hCursor;
        public IntPtr hbrBackground;
        public string? lpszMenuName;
        public string lpszClassName;
        public IntPtr hIconSm;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(
        uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
        int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

    [DllImport("user32.dll")]
    private static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hWnd);

    private IntPtr _msgHwnd = IntPtr.Zero;
    private IntPtr _hIcon = IntPtr.Zero;
    private WndProcDelegate? _wndProcDelegate;
    private bool _isInitialized = false;

    public void Initialize()
    {
        if (_isInitialized) return;

        try
        {
            // 1. Create a dedicated message-only window for receiving tray messages
            _wndProcDelegate = WndProc;
            var wndClass = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                lpszClassName = "CmdDockTrayListenerClass_" + Guid.NewGuid().ToString("N"),
                hInstance = IntPtr.Zero
            };

            RegisterClassEx(ref wndClass);

            // HWND_MESSAGE is (IntPtr)(-3)
            _msgHwnd = CreateWindowEx(0, wndClass.lpszClassName, "CmdDockTrayListener", 0, 0, 0, 0, 0, new IntPtr(-3), IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            // 2. Load Icon
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
            if (File.Exists(iconPath))
            {
                _hIcon = LoadImage(IntPtr.Zero, iconPath, 1 /* IMAGE_ICON */, 16, 16, 0x0010 /* LR_LOADFROMFILE */);
            }
            if (_hIcon == IntPtr.Zero)
            {
                _hIcon = LoadIcon(IntPtr.Zero, new IntPtr(32512) /* IDI_APPLICATION */);
            }

            // 3. Add to Tray
            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = _msgHwnd,
                uID = 1001,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = WM_TRAYICON,
                hIcon = _hIcon,
                szTip = "CmdDock"
            };

            Shell_NotifyIcon(NIM_ADD, ref nid);
            _isInitialized = true;
        }
        catch { }
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == WM_TRAYICON)
        {
            int mouseMsg = (int)lParam;
            if (mouseMsg == WM_LBUTTONUP || mouseMsg == WM_LBUTTONDBLCLK)
            {
                OnTrayLeftClick();
            }
            else if (mouseMsg == WM_RBUTTONUP)
            {
                OnTrayRightClick();
            }
            return IntPtr.Zero;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    private void OnTrayLeftClick()
    {
        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            if (WindowMorphService.Instance.CurrentViewMode == WindowViewMode.MiniDockView)
            {
                WindowMorphService.Instance.SwitchToMiniDock();
            }
            else
            {
                WindowMorphService.Instance.SwitchToFullView();
            }
        });
    }

    private void OnTrayRightClick()
    {
        App.MainWindowInstance?.DispatcherQueue?.TryEnqueue(() =>
        {
            GetCursorPos(out POINT pt);
            SetForegroundWindow(_msgHwnd);

            IntPtr hMenu = CreatePopupMenu();
            AppendMenu(hMenu, MF_STRING, 101, I18nService.Instance["Tray.OpenMain"]);
            AppendMenu(hMenu, MF_STRING, 102, I18nService.Instance["Tray.OpenMini"]);
            AppendMenu(hMenu, MF_SEPARATOR, 0, string.Empty);
            AppendMenu(hMenu, MF_STRING, 103, I18nService.Instance["Tray.Exit"]);

            uint cmd = TrackPopupMenuEx(hMenu, TPM_RETURNCMD | TPM_RIGHTBUTTON, pt.X, pt.Y, _msgHwnd, IntPtr.Zero);
            DestroyMenu(hMenu);

            switch (cmd)
            {
                case 101:
                    WindowMorphService.Instance.SwitchToFullView();
                    break;
                case 102:
                    WindowMorphService.Instance.SwitchToMiniDock();
                    break;
                case 103:
                    Dispose();
                    Application.Current.Exit();
                    break;
            }
        });
    }

    public void Dispose()
    {
        if (_isInitialized)
        {
            try
            {
                var nid = new NOTIFYICONDATA
                {
                    cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                    hWnd = _msgHwnd,
                    uID = 1001
                };
                Shell_NotifyIcon(NIM_DELETE, ref nid);

                if (_msgHwnd != IntPtr.Zero)
                {
                    DestroyWindow(_msgHwnd);
                    _msgHwnd = IntPtr.Zero;
                }

                if (_hIcon != IntPtr.Zero)
                {
                    DestroyIcon(_hIcon);
                    _hIcon = IntPtr.Zero;
                }
            }
            catch { }
            _isInitialized = false;
        }
    }
}
