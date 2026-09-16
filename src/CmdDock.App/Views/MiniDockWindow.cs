using System;
using CmdDock.Core.Models;
using CmdDock.Core.Services;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Graphics;

namespace CmdDock_App.Views;

public sealed class MiniDockWindow : Window
{
    private static MiniDockWindow? _instance;
    public static MiniDockWindow? Instance => _instance;

    private readonly MiniDockPage _dockPage;
    public MiniDockPage DockPage => _dockPage;

    public MiniDockWindow()
    {
        _instance = this;

        try
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        catch { }

        this.Title = "CmdDock Mini";

        var appWindow = this.AppWindow;
        try
        {
            appWindow.SetIcon("Assets/AppIcon.ico");
        }
        catch { }

        // Configure Presenter:
        // SetBorderAndTitleBar(true, false): hasBorder = true gives native sizing frame (resizable borders on all 4 sides & 4 corners); hasTitleBar = false completely removes the native titlebar!
        // IsMaximizable = false: prevents Windows 11 Snap Layouts from appearing when moving near top edge
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(true, false);
            presenter.IsResizable = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        // Hide from taskbar and Alt+Tab when in mini dock mode (only shown in system tray)
        appWindow.IsShownInSwitchers = false;

        appWindow.Changed += OnAppWindowChanged;

        _dockPage = new MiniDockPage();
        this.Content = _dockPage;

        this.Closed += MiniDockWindow_Closed;
    }

    private void OnAppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidSizeChange)
        {
            var settings = MiniDockSettingsService.Instance.LoadSettings();
            if (settings.DockMode == MiniDockMode.CardDeck)
            {
                if (sender.Size.Width >= 200 && sender.Size.Height >= 150)
                {
                    settings.MiniWidth = sender.Size.Width;
                    settings.MiniHeight = sender.Size.Height;
                    MiniDockSettingsService.Instance.SaveSettings(settings);
                }
            }
        }
    }

    private void MiniDockWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
