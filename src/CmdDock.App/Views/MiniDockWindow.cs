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

        // Configure Presenter: completely frameless, no native title bar, no maximize/minimize
        // Disabling maximize prevents Windows 11 Snap Layouts from appearing when moving near top edge
        var presenter = appWindow.Presenter as OverlappedPresenter;
        if (presenter != null)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        _dockPage = new MiniDockPage();
        this.Content = _dockPage;

        this.Closed += MiniDockWindow_Closed;
    }

    private void MiniDockWindow_Closed(object sender, WindowEventArgs args)
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
