using System;
using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock_App.Views;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
    private MiniDockWindow? _miniDockWindow;
    private WindowViewMode _currentViewMode = WindowViewMode.FullView;

    public WindowViewMode CurrentViewMode => _currentViewMode;
    public MainWindow? MainWindow => _mainWindow;
    public MiniDockWindow? MiniDockWindow => _miniDockWindow;
    public event Action<WindowViewMode>? ViewModeChanged;

    public void Initialize(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _mainWindow.AppWindow.Changed += OnMainWindowChanged;
    }

    private void OnMainWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange && _currentViewMode == WindowViewMode.FullView)
        {
            if (sender.Size.Width > 500 && sender.Size.Height > 400)
            {
                var settings = MiniDockSettingsService.Instance.LoadSettings();
                settings.FullWidth = sender.Size.Width;
                settings.FullHeight = sender.Size.Height;
                MiniDockSettingsService.Instance.SaveSettings(settings);
            }
        }
    }

    public void SwitchToMiniDock(MiniDockMode? requestedMode = null)
    {
        var settings = MiniDockSettingsService.Instance.LoadSettings();
        if (requestedMode.HasValue)
        {
            settings.DockMode = requestedMode.Value;
        }

        // 1. Hide MainWindow if currently in FullView
        if (_mainWindow != null)
        {
            if (_currentViewMode == WindowViewMode.FullView)
            {
                settings.FullWidth = _mainWindow.AppWindow.Size.Width;
                settings.FullHeight = _mainWindow.AppWindow.Size.Height;
            }
            _mainWindow.AppWindow.Hide();
        }

        _currentViewMode = WindowViewMode.MiniDockView;

        // 2. Ensure MiniDockWindow is instantiated
        if (_miniDockWindow == null)
        {
            _miniDockWindow = new MiniDockWindow();
        }

        var appWindow = _miniDockWindow.AppWindow;

        // 3. Determine target size
        int targetWidth, targetHeight;
        if (settings.DockMode == MiniDockMode.DockBar)
        {
            if (settings.Orientation == DockOrientation.Vertical)
            {
                targetWidth = 64;
                targetHeight = settings.DockBarHeight >= 200 ? settings.DockBarHeight : 480;
            }
            else
            {
                targetWidth = settings.DockBarWidth >= 200 ? settings.DockBarWidth : 480;
                targetHeight = 64;
            }
        }
        else
        {
            targetWidth = settings.MiniWidth > 200 ? settings.MiniWidth : 360;
            targetHeight = settings.MiniHeight > 200 ? settings.MiniHeight : 500;
        }

        // 4. Reposition & Resize MiniDockWindow
        if (settings.WindowX.HasValue && settings.WindowY.HasValue)
        {
            appWindow.MoveAndResize(new RectInt32(settings.WindowX.Value, settings.WindowY.Value, targetWidth, targetHeight));
        }
        else
        {
            appWindow.Resize(new SizeInt32(targetWidth, targetHeight));
        }

        // 5. Update UI mode inside DockPage
        _miniDockWindow.DockPage?.SetDockMode(settings.DockMode);

        // 6. Apply Pin Mode (AlwaysOnTop / PinToDesktop / Normal)
        DesktopPinService.ApplyPinMode(_miniDockWindow, settings.PinMode);

        // 7. Hide from taskbar and show & activate MiniDockWindow
        appWindow.IsShownInSwitchers = false;
        appWindow.Show(true);
        _miniDockWindow.Activate();

        MiniDockSettingsService.Instance.SaveSettings(settings);
        ViewModeChanged?.Invoke(_currentViewMode);
    }

    public void SwitchToFullView()
    {
        var settings = MiniDockSettingsService.Instance.LoadSettings();

        // 1. Save Mini Dock position & size, and hide MiniDockWindow
        if (_miniDockWindow != null)
        {
            settings.WindowX = _miniDockWindow.AppWindow.Position.X;
            settings.WindowY = _miniDockWindow.AppWindow.Position.Y;
            if (settings.DockMode == MiniDockMode.CardDeck)
            {
                settings.MiniWidth = _miniDockWindow.AppWindow.Size.Width;
                settings.MiniHeight = _miniDockWindow.AppWindow.Size.Height;
            }
            else if (settings.DockMode == MiniDockMode.DockBar)
            {
                if (_miniDockWindow.AppWindow.Size.Width >= 160)
                {
                    settings.DockBarWidth = _miniDockWindow.AppWindow.Size.Width;
                }
                if (_miniDockWindow.AppWindow.Size.Height >= 160)
                {
                    settings.DockBarHeight = _miniDockWindow.AppWindow.Size.Height;
                }
            }
            _miniDockWindow.AppWindow.Hide();
        }

        _currentViewMode = WindowViewMode.FullView;

        // 2. Restore MainWindow
        if (_mainWindow != null)
        {
            var fullWidth = settings.FullWidth >= 1320 ? settings.FullWidth : 1320;
            var fullHeight = settings.FullHeight >= 820 ? settings.FullHeight : 820;
            _mainWindow.AppWindow.Resize(new SizeInt32(fullWidth, fullHeight));

            DesktopPinService.ApplyPinMode(_mainWindow, DesktopPinMode.Normal);

            var presenter = _mainWindow.AppWindow.Presenter as OverlappedPresenter;
            if (presenter != null)
            {
                presenter.SetBorderAndTitleBar(true, true);
                presenter.IsResizable = true;
                presenter.IsMaximizable = true;
                presenter.IsMinimizable = true;
                presenter.IsAlwaysOnTop = false;
            }

            _mainWindow.TitleBarControl.Visibility = Visibility.Visible;
            _mainWindow.SetTitleBar(_mainWindow.TitleBarControl);

            _mainWindow.AppWindow.IsShownInSwitchers = true;
            _mainWindow.AppWindow.Show(true);
            _mainWindow.Activate();
        }

        MiniDockSettingsService.Instance.SaveSettings(settings);
        ViewModeChanged?.Invoke(_currentViewMode);
    }

    public void BringCurrentWindowToForeground()
    {
        if (_currentViewMode == WindowViewMode.MiniDockView && _miniDockWindow != null)
        {
            _miniDockWindow.Activate();
        }
        else if (_mainWindow != null)
        {
            _mainWindow.Activate();
        }
    }
}
