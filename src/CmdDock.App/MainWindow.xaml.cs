using CmdDock.Core.Services;
using Microsoft.UI.Xaml;

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

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }

    private void UpdateTitle()
    {
        var title = I18nService.Instance["App.Title"];
        Title = title;
        AppTitleBar.Title = title;
    }
}
