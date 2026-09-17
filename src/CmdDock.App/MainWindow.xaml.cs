using CmdDock.Core.Services;
using Microsoft.UI.Xaml;

using CmdDock_App.Services;

namespace CmdDock_App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        App.MainWindowInstance = this;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        UpdateTitle();
        I18nService.Instance.LanguageChanged += () =>
        {
            DispatcherQueue.TryEnqueue(UpdateTitle);
        };

        WindowMorphService.Instance.Initialize(this);

        var dockSettings = MiniDockSettingsService.Instance.LoadSettings();
        var fullWidth = dockSettings.FullWidth >= 1320 ? dockSettings.FullWidth : 1320;
        var fullHeight = dockSettings.FullHeight >= 820 ? dockSettings.FullHeight : 820;
        AppWindow.Resize(new Windows.Graphics.SizeInt32(fullWidth, fullHeight));
        RootFrame.Navigate(typeof(MainPage));

        this.Closed += (s, e) =>
        {
            if (WindowMorphService.Instance.MiniDockWindow != null)
            {
                try
                {
                    WindowMorphService.Instance.MiniDockWindow.Close();
                }
                catch { }
            }
        };
    }

    public UIElement TitleBarControl => AppTitleBar;
    public Microsoft.UI.Xaml.Controls.Frame ContentFrame => RootFrame;

    private void UpdateTitle()
    {
        var title = I18nService.Instance["App.Title"];
        Title = title;
        AppTitleBar.Title = title;
    }
}
