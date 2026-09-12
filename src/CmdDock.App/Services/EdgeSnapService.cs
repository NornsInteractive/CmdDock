using CmdDock.Core.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace CmdDock_App.Services;

public enum SnappedEdge
{
    None,
    Left,
    Right,
    Top,
    Bottom
}

public class EdgeSnapService
{
    private const int SnapDistance = 25; // Snap threshold in pixels
    private const int HiddenMargin = 6;  // Pixels visible when auto-hidden

    private Window? _window;
    public Window? TargetWindow => _window ?? App.MainWindowInstance ?? WindowMorphService.Instance.MainWindow;
    private DispatcherTimer? _autoHideTimer;
    private bool _isCurrentlyHidden;
    private SnappedEdge _lastSnappedEdge = SnappedEdge.None;
    private PointInt32 _restoredPosition;

    public event Action<SnappedEdge>? SnappedEdgeChanged;
    public event Action<bool>? AutoHideStateChanged;

    public SnappedEdge CurrentSnappedEdge => _lastSnappedEdge;
    public bool IsCurrentlyHidden => _isCurrentlyHidden;

    public EdgeSnapService(Window? window = null)
    {
        _window = window;
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

        // Check Left
        if (Math.Abs(pos.X - workArea.X) <= SnapDistance)
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

        // Check Top
        if (Math.Abs(pos.Y - workArea.Y) <= SnapDistance)
        {
            newY = workArea.Y;
            if (edge == SnappedEdge.None) edge = SnappedEdge.Top;
        }
        // Check Bottom
        else if (Math.Abs((pos.Y + size.Height) - (workArea.Y + workArea.Height)) <= SnapDistance)
        {
            newY = workArea.Y + workArea.Height - size.Height;
            if (edge == SnappedEdge.None) edge = SnappedEdge.Bottom;
        }

        if (applySnap && (newX != pos.X || newY != pos.Y))
        {
            appWindow.Move(new PointInt32(newX, newY));
        }

        if (_lastSnappedEdge != edge)
        {
            _lastSnappedEdge = edge;
            SnappedEdgeChanged?.Invoke(edge);
        }

        return edge;
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

    public void SetupAutoHide(bool enable)
    {
        if (_autoHideTimer == null)
        {
            _autoHideTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _autoHideTimer.Tick += OnAutoHideTimerTick;
        }

        if (!enable)
        {
            _autoHideTimer.Stop();
            RestoreFromAutoHide();
        }
    }

    public void OnPointerEntered()
    {
        _autoHideTimer?.Stop();
        if (_isCurrentlyHidden)
        {
            RestoreFromAutoHide();
        }
    }

    public void OnPointerExited(bool autoHideEnabled)
    {
        if (autoHideEnabled && _lastSnappedEdge != SnappedEdge.None && !_isCurrentlyHidden)
        {
            _autoHideTimer?.Stop();
            _autoHideTimer?.Start();
        }
    }

    private void OnAutoHideTimerTick(object? sender, object e)
    {
        _autoHideTimer?.Stop();
        AnimateToHidden();
    }

    private void AnimateToHidden()
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
            case SnappedEdge.Bottom:
                targetY = workArea.Y + workArea.Height - HiddenMargin;
                break;
        }

        appWindow.Move(new PointInt32(targetX, targetY));
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
}
