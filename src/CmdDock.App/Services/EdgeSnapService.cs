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

    private const int SnapDistance = 45; // Generous snap threshold so pushing towards any edge catches easily
    private const int HiddenMargin = 4;  // 4 pixels visible when auto-hidden (like QQ)

    private Window? _window;
    public Window? TargetWindow => _window ?? (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance;

    private DispatcherTimer? _monitorTimer;
    private DispatcherTimer? _slideAnimTimer;
    private bool _isCurrentlyHidden;
    private bool _isAnimating;
    private SnappedEdge _lastSnappedEdge = SnappedEdge.None;
    private PointInt32 _restoredPosition;
    private int _mouseLeaveCount = 0;
    private bool _autoHideEnabled = true;
    public bool IsDragging { get; set; } = false;

    // Animation state
    private PointInt32 _animStart;
    private PointInt32 _animTarget;
    private int _animFrame;
    private const int AnimTotalFrames = 10; // 10 frames * 16ms = ~160ms smooth slide
    private Action? _onAnimComplete;

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
                Interval = TimeSpan.FromMilliseconds(75) // ~13 polls/sec, imperceptible CPU
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
            RestoreFromAutoHide(animate: true);
        }
    }

    public void OnPointerEntered()
    {
        if (_isCurrentlyHidden)
        {
            RestoreFromAutoHide(animate: true);
        }
        _mouseLeaveCount = 0;
    }

    public void OnPointerExited(bool autoHideEnabled)
    {
        // Handled by continuous monitor timer
    }

    public SnappedEdge CheckAndSnap(bool applySnap = true)
    {
        var window = TargetWindow;
        if (window == null) return SnappedEdge.None;

        var appWindow = window.AppWindow;
        if (appWindow == null) return SnappedEdge.None;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return SnappedEdge.None;

        var outer = displayArea.OuterBounds;
        var work = displayArea.WorkArea;
        var pos = appWindow.Position;
        var size = appWindow.Size;

        // Detect taskbar location: taskbar is at the edge where workArea does NOT reach outerBounds
        bool taskbarAtBottom = (work.Y + work.Height) < (outer.Y + outer.Height);
        bool taskbarAtTop = work.Y > outer.Y;
        bool taskbarAtLeft = work.X > outer.X;
        bool taskbarAtRight = (work.X + work.Width) < (outer.X + outer.Width);

        // Calculate distances to all 3 available non-taskbar edges
        // If user drags to/past an edge, distance is 0
        int distTop = taskbarAtTop ? int.MaxValue : (pos.Y <= work.Y + SnapDistance ? Math.Max(0, pos.Y - work.Y) : int.MaxValue);
        int distLeft = taskbarAtLeft ? int.MaxValue : (pos.X <= work.X + SnapDistance ? Math.Max(0, pos.X - work.X) : int.MaxValue);
        int distRight = taskbarAtRight ? int.MaxValue : ((pos.X + size.Width >= work.X + work.Width - SnapDistance) ? Math.Max(0, (work.X + work.Width) - (pos.X + size.Width)) : int.MaxValue);

        int newX = pos.X;
        int newY = pos.Y;
        var edge = SnappedEdge.None;

        int minDist = Math.Min(distTop, Math.Min(distLeft, distRight));

        if (minDist <= SnapDistance)
        {
            if (minDist == distLeft)
            {
                edge = SnappedEdge.Left;
                newX = work.X;
                // Keep Y within work area
                newY = Math.Clamp(pos.Y, work.Y, Math.Max(work.Y, work.Y + work.Height - size.Height));
            }
            else if (minDist == distRight)
            {
                edge = SnappedEdge.Right;
                newX = work.X + work.Width - size.Width;
                newY = Math.Clamp(pos.Y, work.Y, Math.Max(work.Y, work.Y + work.Height - size.Height));
            }
            else if (minDist == distTop)
            {
                edge = SnappedEdge.Top;
                newY = work.Y;
                newX = Math.Clamp(pos.X, work.X, Math.Max(work.X, work.X + work.Width - size.Width));
            }
        }

        if (edge != SnappedEdge.None)
        {
            _restoredPosition = new PointInt32(newX, newY);
        }

        if (applySnap)
        {
            if (newX != pos.X || newY != pos.Y)
            {
                appWindow.Move(new PointInt32(newX, newY));
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
        if (!_autoHideEnabled || IsDragging || _isAnimating) return;

        var window = TargetWindow;
        if (window == null) return;
        var appWindow = window.AppWindow;
        if (appWindow == null || !appWindow.IsVisible) return;

        if (_lastSnappedEdge == SnappedEdge.None)
        {
            if (_isCurrentlyHidden)
            {
                RestoreFromAutoHide(animate: true);
            }
            return;
        }

        if (!GetCursorPos(out var cursor)) return;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;
        var work = displayArea.WorkArea;
        var pos = appWindow.Position;
        var size = appWindow.Size;

        if (_isCurrentlyHidden)
        {
            // Check if mouse touches the edge where the window is docked
            bool isMouseOverEdge = false;
            switch (_lastSnappedEdge)
            {
                case SnappedEdge.Top:
                    isMouseOverEdge = cursor.Y <= work.Y + 12 &&
                                      cursor.X >= _restoredPosition.X - 25 &&
                                      cursor.X <= _restoredPosition.X + size.Width + 25;
                    break;
                case SnappedEdge.Left:
                    isMouseOverEdge = cursor.X <= work.X + 12 &&
                                      cursor.Y >= _restoredPosition.Y - 25 &&
                                      cursor.Y <= _restoredPosition.Y + size.Height + 25;
                    break;
                case SnappedEdge.Right:
                    isMouseOverEdge = cursor.X >= work.X + work.Width - 12 &&
                                      cursor.Y >= _restoredPosition.Y - 25 &&
                                      cursor.Y <= _restoredPosition.Y + size.Height + 25;
                    break;
            }

            if (isMouseOverEdge)
            {
                RestoreFromAutoHide(animate: true);
                _mouseLeaveCount = 0;
            }
        }
        else
        {
            // Check if mouse is inside the restored window rect (with 15px buffer)
            bool isInsideWindow = cursor.X >= pos.X - 15 && cursor.X <= pos.X + size.Width + 15 &&
                                  cursor.Y >= pos.Y - 15 && cursor.Y <= pos.Y + size.Height + 15;

            if (isInsideWindow)
            {
                _mouseLeaveCount = 0;
            }
            else
            {
                _mouseLeaveCount++;
                // After mouse leaves for ~300ms (4 ticks of 75ms), slide to hide
                if (_mouseLeaveCount >= 4)
                {
                    AnimateToHidden(animate: true);
                }
            }
        }
    }

    public void AnimateToHidden(bool animate = true)
    {
        var window = TargetWindow;
        if (window == null) return;
        var appWindow = window.AppWindow;
        if (appWindow == null || _lastSnappedEdge == SnappedEdge.None || _isCurrentlyHidden || _isAnimating) return;

        var displayArea = DisplayArea.GetFromWindowId(appWindow.Id, DisplayAreaFallback.Primary);
        if (displayArea == null) return;

        var work = displayArea.WorkArea;
        var size = appWindow.Size;

        // Only update restored position if the window is currently positioned inside visible work area
        var curPos = appWindow.Position;
        if (curPos.X >= work.X - 10 && curPos.X <= work.X + work.Width &&
            curPos.Y >= work.Y - 10 && curPos.Y <= work.Y + work.Height)
        {
            _restoredPosition = curPos;
        }

        var targetX = _restoredPosition.X;
        var targetY = _restoredPosition.Y;

        switch (_lastSnappedEdge)
        {
            case SnappedEdge.Left:
                targetX = work.X - size.Width + HiddenMargin;
                break;
            case SnappedEdge.Right:
                targetX = work.X + work.Width - HiddenMargin;
                break;
            case SnappedEdge.Top:
                targetY = work.Y - size.Height + HiddenMargin;
                break;
        }

        var targetPos = new PointInt32(targetX, targetY);

        // Turn off window resize border so mouse hovering over edge does NOT turn into resize cursor
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.IsResizable = false;
        }

        _isCurrentlyHidden = true;
        AutoHideStateChanged?.Invoke(true);

        if (animate)
        {
            _isAnimating = true;
            StartSlideAnimation(targetPos, () =>
            {
                _isAnimating = false;
                EnsureTopmost(window);
            });
        }
        else
        {
            appWindow.Move(targetPos);
            EnsureTopmost(window);
        }
    }

    public void RestoreFromAutoHide(bool animate = true)
    {
        var window = TargetWindow;
        if (window == null) return;
        var appWindow = window.AppWindow;
        if (appWindow == null || !_isCurrentlyHidden || _isAnimating) return;

        _isCurrentlyHidden = false;
        AutoHideStateChanged?.Invoke(false);

        if (animate)
        {
            _isAnimating = true;
            StartSlideAnimation(_restoredPosition, () =>
            {
                _isAnimating = false;
                var presenter = appWindow.Presenter as OverlappedPresenter;
                if (presenter != null)
                {
                    presenter.IsResizable = true;
                }
            });
        }
        else
        {
            appWindow.Move(_restoredPosition);
            var presenter = appWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.IsResizable = true;
            }
        }
    }

    private void StartSlideAnimation(PointInt32 target, Action? onComplete = null)
    {
        var window = TargetWindow;
        if (window?.AppWindow == null) return;

        _slideAnimTimer?.Stop();
        _animStart = window.AppWindow.Position;
        _animTarget = target;
        _animFrame = 0;
        _onAnimComplete = onComplete;

        if (_slideAnimTimer == null)
        {
            _slideAnimTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // 60 fps
            };
            _slideAnimTimer.Tick += OnSlideAnimTick;
        }
        _slideAnimTimer.Start();
    }

    private void OnSlideAnimTick(object? sender, object e)
    {
        _animFrame++;
        float t = Math.Clamp((float)_animFrame / AnimTotalFrames, 0f, 1f);
        // EaseOutCubic: 1 - (1-t)^3
        float eased = 1f - (float)Math.Pow(1f - t, 3);

        int curX = (int)(_animStart.X + (_animTarget.X - _animStart.X) * eased);
        int curY = (int)(_animStart.Y + (_animTarget.Y - _animStart.Y) * eased);

        var window = TargetWindow;
        if (window != null)
        {
            var hWnd = WindowNative.GetWindowHandle(window);
            if (hWnd != IntPtr.Zero)
            {
                SetWindowPos(hWnd, IntPtr.Zero, curX, curY, 0, 0, 0x0001 | 0x0004 | 0x0010); // SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE
            }
        }

        if (_animFrame >= AnimTotalFrames)
        {
            _slideAnimTimer?.Stop();
            if (window?.AppWindow != null)
            {
                window.AppWindow.Move(_animTarget);
            }
            _onAnimComplete?.Invoke();
        }
    }

    private void EnsureTopmost(Window window)
    {
        var hWnd = WindowNative.GetWindowHandle(window);
        if (hWnd != IntPtr.Zero)
        {
            SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
        }
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
