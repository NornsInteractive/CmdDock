using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock_App.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Graphics;

namespace CmdDock_App.Services;

public enum WindowViewMode
{
    FullView,
    MiniDockView
}

public class WindowMorphService
{
    private static WindowMorphService? _instance;
    public static WindowMorphService Instance => _instance ??= new WindowMorphService();

    private MainWindow? _mainWindow;
    private WindowViewMode _currentViewMode = WindowViewMode.FullView;

    public WindowViewMode CurrentViewMode => _currentViewMode;
    public MainWindow? MainWindow => _mainWindow;
    public event Action<WindowViewMode>? ViewModeChanged;

    public void Initialize(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
    }

    public void SwitchToMiniDock(MiniDockMode? requestedMode = null)
    {
        if (_mainWindow == null) return;

        var settings = MiniDockSettingsService.Instance.LoadSettings();
        if (requestedMode.HasValue)
        {
            settings.DockMode = requestedMode.Value;
        }

        var appWindow = _mainWindow.AppWindow;
        if (appWindow == null) return;

        // 1. Save Full View geometry
        if (_currentViewMode == WindowViewMode.FullView)
        {
            settings.FullWidth = appWindow.Size.Width;
            settings.FullHeight = appWindow.Size.Height;
        }

        _currentViewMode = WindowViewMode.MiniDockView;

        // 2. Adjust Presenter
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        // 3. Hide full titlebar
        _mainWindow.TitleBarControl.Visibility = Visibility.Collapsed;

        // 4. Determine target size
        int targetWidth, targetHeight;
        if (settings.DockMode == MiniDockMode.DockBar)
        {
            if (settings.Orientation == DockOrientation.Vertical)
            {
                targetWidth = 64;
                targetHeight = 440;
            }
            else
            {
                targetWidth = 460;
                targetHeight = 64;
            }
        }
        else
        {
            targetWidth = settings.MiniWidth > 0 ? settings.MiniWidth : 360;
            targetHeight = settings.MiniHeight > 0 ? settings.MiniHeight : 500;
        }

        // 5. Reposition & Resize
        if (settings.WindowX.HasValue && settings.WindowY.HasValue)
        {
            appWindow.MoveAndResize(new RectInt32(settings.WindowX.Value, settings.WindowY.Value, targetWidth, targetHeight));
        }
        else
        {
            appWindow.Resize(new SizeInt32(targetWidth, targetHeight));
        }

        // 6. Apply Pin Mode (AlwaysOnTop / PinToDesktop)
        DesktopPinService.ApplyPinMode(_mainWindow, settings.PinMode);

        // 7. Navigate to MiniDockPage
        _mainWindow.ContentFrame.Navigate(typeof(MiniDockPage));

        appWindow.Show(true);
        _mainWindow.Activate();

        MiniDockSettingsService.Instance.SaveSettings(settings);
        ViewModeChanged?.Invoke(_currentViewMode);
    }

    public void SwitchToFullView()
    {
        if (_mainWindow == null) return;

        var settings = MiniDockSettingsService.Instance.LoadSettings();
        var appWindow = _mainWindow.AppWindow;
        if (appWindow == null) return;

        // 1. Save Mini Dock position
        if (_currentViewMode == WindowViewMode.MiniDockView)
        {
            settings.WindowX = appWindow.Position.X;
            settings.WindowY = appWindow.Position.Y;
            if (settings.DockMode == MiniDockMode.CardDeck)
            {
                settings.MiniWidth = appWindow.Size.Width;
                settings.MiniHeight = appWindow.Size.Height;
            }
        }

        _currentViewMode = WindowViewMode.FullView;

        // 2. Restore normal window pin mode
        DesktopPinService.ApplyPinMode(_mainWindow, DesktopPinMode.Normal);

        // 3. Restore Presenter
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(true, true);
            presenter.IsResizable = true;
            presenter.IsMaximizable = true;
            presenter.IsMinimizable = true;
            presenter.IsAlwaysOnTop = false;
        }

        // 4. Show custom AppTitleBar
        _mainWindow.TitleBarControl.Visibility = Visibility.Visible;
        _mainWindow.SetTitleBar(_mainWindow.TitleBarControl);

        // 5. Restore full size
        var fullWidth = settings.FullWidth > 500 ? settings.FullWidth : 1000;
        var fullHeight = settings.FullHeight > 400 ? settings.FullHeight : 680;
        appWindow.Resize(new SizeInt32(fullWidth, fullHeight));

        // 6. Navigate to MainPage
        _mainWindow.ContentFrame.Navigate(typeof(MainPage));

        appWindow.Show(true);
        _mainWindow.Activate();

        MiniDockSettingsService.Instance.SaveSettings(settings);
        ViewModeChanged?.Invoke(_currentViewMode);
    }
}
