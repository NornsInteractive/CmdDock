using System;
using System.Runtime.InteropServices;
using CmdDock.Core.Models;
using CmdDock_App.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;
using WinRT.Interop;

namespace CmdDock_App.Services;

public enum SnappedEdge
{
    None,
    Left,
    Right,
    Top,
    Bottom
}

[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;
}

public class EdgeSnapService
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    private static readonly IntPtr HWND_TOPMOST = new(-1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_SHOWWINDOW = 0x0040;

    private const int SnapDistance = 35; // Snap threshold in pixels (25-35px feels very natural)
    private const int HiddenMargin = 4;  // 4 pixels visible when auto-hidden (like QQ)

    private Window? _window;
    public Window? TargetWindow => _window ?? (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance;

    private DispatcherTimer? _monitorTimer;
    private bool _isCurrentlyHidden;
    private SnappedEdge _lastSnappedEdge = SnappedEdge.None;
    private PointInt32 _restoredPosition;
    private int _mouseLeaveCount = 0;
    private bool _autoHideEnabled = true;
    public bool IsDragging { get; set; } = false;

    public event Action<SnappedEdge>? SnappedEdgeChanged;
    public event Action<bool>? AutoHideStateChanged;

    public SnappedEdge CurrentSnappedEdge => _lastSnappedEdge;
    public bool IsCurrentlyHidden => _isCurrentlyHidden;

    public EdgeSnapService(Window? window = null)
    {
        _window = window;
        StartMonitor();
    }

    private void StartMonitor()
    {
        if (_monitorTimer == null)
        {
            _monitorTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(80) // 80ms poll provides ~12 updates/sec with zero CPU
            };
            _monitorTimer.Tick += OnMonitorTick;
            _monitorTimer.Start();
        }
    }

    public void SetupAutoHide(bool enable)
    {
        _autoHideEnabled = enable;
        if (!enable && _isCurrentlyHidden)
        {
            RestoreFromAutoHide();
        }
    }

    public void OnPointerEntered()
    {
        if (_isCurrentlyHidden)
        {
            RestoreFromAutoHide();
        }
        _mouseLeaveCount = 0;
    }

    public void OnPointerExited(bool autoHideEnabled)
    {
        // Handled by monitor timer
    }

    public SnappedEdge CheckAndSnap(bool applySnap = true)
    {
        var window = TargetWindow;
        if (window == null) return SnappedEdge.None;

        var appWindow = window.AppWindow;
        if (appWindow == null) return SnappedEdge.None;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return SnappedEdge.None;

        var workArea = displayArea.WorkArea;
        var pos = appWindow.Position;
        var size = appWindow.Size;

        var newX = pos.X;
        var newY = pos.Y;
        var edge = SnappedEdge.None;

        // Check Top first (top docking is most popular like QQ)
        if (Math.Abs(pos.Y - workArea.Y) <= SnapDistance)
        {
            newY = workArea.Y;
            edge = SnappedEdge.Top;
        }
        // Check Left
        else if (Math.Abs(pos.X - workArea.X) <= SnapDistance)
        {
            newX = workArea.X;
            edge = SnappedEdge.Left;
        }
        // Check Right
        else if (Math.Abs((pos.X + size.Width) - (workArea.X + workArea.Width)) <= SnapDistance)
        {
            newX = workArea.X + workArea.Width - size.Width;
            edge = SnappedEdge.Right;
        }

        if (applySnap)
        {
            if (newX != pos.X || newY != pos.Y)
            {
                appWindow.Move(new PointInt32(newX, newY));
            }
            if (edge != SnappedEdge.None)
            {
                _restoredPosition = new PointInt32(newX, newY);
            }
        }

        if (_lastSnappedEdge != edge)
        {
            _lastSnappedEdge = edge;
            SnappedEdgeChanged?.Invoke(edge);
        }

        return edge;
    }

    private void OnMonitorTick(object? sender, object e)
    {
        if (!_autoHideEnabled || IsDragging) return;

        var window = TargetWindow;
        if (window == null) return;
        var appWindow = window.AppWindow;
        if (appWindow == null || !appWindow.IsVisible) return;

        if (_lastSnappedEdge == SnappedEdge.None)
        {
            // Window is free-floating in desktop
            if (_isCurrentlyHidden)
            {
                RestoreFromAutoHide();
            }
            return;
        }

        if (!GetCursorPos(out var cursor)) return;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;
        var workArea = displayArea.WorkArea;
        var pos = appWindow.Position;
        var size = appWindow.Size;

        if (_isCurrentlyHidden)
        {
            // Check if mouse is touching the edge or the visible 4px slice of window
            bool isMouseOverEdge = false;
            switch (_lastSnappedEdge)
            {
                case SnappedEdge.Top:
                    isMouseOverEdge = cursor.Y <= workArea.Y + 8 &&
                                      cursor.X >= _restoredPosition.X - 10 &&
                                      cursor.X <= _restoredPosition.X + size.Width + 10;
                    break;
                case SnappedEdge.Left:
                    isMouseOverEdge = cursor.X <= workArea.X + 8 &&
                                      cursor.Y >= _restoredPosition.Y - 10 &&
                                      cursor.Y <= _restoredPosition.Y + size.Height + 10;
                    break;
                case SnappedEdge.Right:
                    isMouseOverEdge = cursor.X >= workArea.X + workArea.Width - 8 &&
                                      cursor.Y >= _restoredPosition.Y - 10 &&
                                      cursor.Y <= _restoredPosition.Y + size.Height + 10;
                    break;
            }

            if (isMouseOverEdge)
            {
                RestoreFromAutoHide();
                _mouseLeaveCount = 0;
            }
        }
        else
        {
            // Check if mouse is inside the restored window rect (with 8px tolerance)
            bool isInsideWindow = cursor.X >= pos.X - 8 && cursor.X <= pos.X + size.Width + 8 &&
                                  cursor.Y >= pos.Y - 8 && cursor.Y <= pos.Y + size.Height + 8;

            if (isInsideWindow)
            {
                _mouseLeaveCount = 0;
            }
            else
            {
                _mouseLeaveCount++;
                // After mouse leaves for ~320ms (4 ticks of 80ms), hide to edge
                if (_mouseLeaveCount >= 4)
                {
                    AnimateToHidden();
                }
            }
        }
    }

    public void AnimateToHidden()
    {
        var window = TargetWindow;
        if (window == null) return;
        var appWindow = window.AppWindow;
        if (appWindow == null || _lastSnappedEdge == SnappedEdge.None || _isCurrentlyHidden) return;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;

        var workArea = displayArea.WorkArea;
        _restoredPosition = appWindow.Position;
        var size = appWindow.Size;

        var targetX = _restoredPosition.X;
        var targetY = _restoredPosition.Y;

        switch (_lastSnappedEdge)
        {
            case SnappedEdge.Left:
                targetX = workArea.X - size.Width + HiddenMargin;
                break;
            case SnappedEdge.Right:
                targetX = workArea.X + workArea.Width - HiddenMargin;
                break;
            case SnappedEdge.Top:
                targetY = workArea.Y - size.Height + HiddenMargin;
                break;
        }

        appWindow.Move(new PointInt32(targetX, targetY));

        // When hidden, ensure window is topmost so edge slice is not buried by other windows
        var hWnd = WindowNative.GetWindowHandle(window);
        if (hWnd != IntPtr.Zero)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }

        _isCurrentlyHidden = true;
        AutoHideStateChanged?.Invoke(true);
    }

    public void RestoreFromAutoHide()
    {
        var window = TargetWindow;
        if (window == null) return;
        var appWindow = window.AppWindow;
        if (appWindow == null || !_isCurrentlyHidden) return;

        appWindow.Move(_restoredPosition);
        _isCurrentlyHidden = false;
        AutoHideStateChanged?.Invoke(false);
    }

    public DockOrientation GetSuggestedOrientation(DockOrientation currentSetting)
    {
        if (currentSetting != DockOrientation.Auto)
        {
            return currentSetting;
        }

        return _lastSnappedEdge switch
        {
            SnappedEdge.Left or SnappedEdge.Right => DockOrientation.Vertical,
            SnappedEdge.Top or SnappedEdge.Bottom => DockOrientation.Horizontal,
            _ => DockOrientation.Horizontal
        };
    }
}
