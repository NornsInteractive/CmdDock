using CmdDock.Core.Services;
using Microsoft.UI.Xaml;

using CmdDock_App.Services;

namespace CmdDock_App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
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
        if (dockSettings.StartupView == "mini")
        {
            WindowMorphService.Instance.SwitchToMiniDock();
        }
        else
        {
            RootFrame.Navigate(typeof(MainPage));
        }
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
