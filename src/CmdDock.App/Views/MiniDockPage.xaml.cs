using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using WinRT.Interop;

namespace CmdDock_App.Views;

public sealed partial class MiniDockPage : Page
{
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    private const uint WM_SYSCOMMAND = 0x0112;
    private const uint SC_MOVE = 0xF010;
    private const uint HTCAPTION = 0x0002;

    private readonly ICommandService _commandService;
    private readonly ICategoryService _categoryService;
    private readonly ILogService _logService;
    private readonly CommandExecutor _commandExecutor;
    private readonly EdgeSnapService _edgeSnapService;

    private MiniDockSettings _settings;
    private string _selectedCategory = "全部";
    private string _searchFilter = string.Empty;

    public ObservableCollection<CommandItem> AllCommands { get; } = new();
    public ObservableCollection<CommandItem> DisplayedCommands { get; } = new();
    public ObservableCollection<string> Categories { get; } = new();

    public MiniDockPage()
    {
        InitializeComponent();

        var presetService = new PresetService();
        _commandService = new CommandService(presetService);
        _categoryService = new CategoryService(commandService: _commandService);
        _logService = new LogService();
        _commandExecutor = new CommandExecutor(_commandService, _logService);

        _settings = MiniDockSettingsService.Instance.LoadSettings();
        _edgeSnapService = new EdgeSnapService(App.MainWindowInstance!);

        CardsItemsControl.ItemsSource = DisplayedCommands;
        DockBarItemsControl.ItemsSource = DisplayedCommands;

        Loaded += MiniDockPage_Loaded;
    }

    private async void MiniDockPage_Loaded(object sender, RoutedEventArgs e)
    {
        ApplySettingsToUI();
        await LoadDataAsync();
        _edgeSnapService.CheckAndSnap(applySnap: false);
    }

    private void ApplySettingsToUI()
    {
        // 1. Opacity
        BackgroundBackdrop.Opacity = Math.Clamp(_settings.Opacity, 0.3, 1.0);
        OpacitySlider.Value = BackgroundBackdrop.Opacity * 100;

        // 2. Dock Mode (Card vs Bar)
        UpdateDockModeVisuals(_settings.DockMode);

        // 3. Pin Mode Visuals
        UpdatePinModeVisuals(_settings.PinMode);

        // 4. Auto Hide
        AutoHideSwitch.IsOn = _settings.EnableAutoHide;
        _edgeSnapService.SetupAutoHide(_settings.EnableAutoHide);
    }

    private async Task LoadDataAsync()
    {
        // Load commands
        var cmds = await _commandService.GetAllAsync();
        AllCommands.Clear();
        foreach (var c in cmds)
        {
            AllCommands.Add(c);
        }

        // Load categories
        var cats = await _categoryService.GetAllCategoriesAsync();
        Categories.Clear();
        Categories.Add("全部");
        foreach (var cat in cats)
        {
            if (!Categories.Contains(cat))
            {
                Categories.Add(cat);
            }
        }

        BuildCategoryPills();
        FilterCommands();
    }

    private void BuildCategoryPills()
    {
        CategoryPillsPanel.Children.Clear();
        foreach (var cat in Categories)
        {
            var btn = new Button
            {
                Content = I18nService.Instance.TranslateCategory(cat),
                Tag = cat,
                Padding = new Thickness(10, 4, 10, 4),
                CornerRadius = new CornerRadius(12),
                FontSize = 11,
                Margin = new Thickness(0, 0, 4, 0),
                Style = cat == _selectedCategory 
                    ? (Style)Application.Current.Resources["AccentButtonStyle"] 
                    : (Style)Application.Current.Resources["DefaultButtonStyle"]
            };

            btn.Click += (s, _) =>
            {
                if (s is Button b && b.Tag is string chosen)
                {
                    _selectedCategory = chosen;
                    BuildCategoryPills();
                    FilterCommands();
                }
            };

            CategoryPillsPanel.Children.Add(btn);
        }
    }

    private void FilterCommands()
    {
        DisplayedCommands.Clear();
        var query = AllCommands.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(_selectedCategory) && _selectedCategory != "全部" && _selectedCategory != "All")
        {
            query = query.Where(c => c.Group == _selectedCategory);
        }

        if (!string.IsNullOrWhiteSpace(_searchFilter))
        {
            query = query.Where(c => 
                c.DisplayName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase) ||
                c.CommandText.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var c in query.OrderBy(c => c.Order))
        {
            DisplayedCommands.Add(c);
        }
    }

    // =========================================================================
    // WINDOW DRAG & EXPAND ACTIONS
    // =========================================================================
    private void Header_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var window = App.MainWindowInstance;
            if (window != null)
            {
                var hWnd = WindowNative.GetWindowHandle(window);
                if (hWnd != IntPtr.Zero)
                {
                    ReleaseCapture();
                    SendMessage(hWnd, WM_SYSCOMMAND, (IntPtr)(SC_MOVE + HTCAPTION), IntPtr.Zero);
                    _edgeSnapService.CheckAndSnap(applySnap: true);
                }
            }
        }
    }

    private void Header_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        WindowMorphService.Instance.SwitchToFullView();
    }

    private void ExpandBtn_Click(object sender, RoutedEventArgs e)
    {
        WindowMorphService.Instance.SwitchToFullView();
    }

    // =========================================================================
    // MODE & PIN SWITCHING
    // =========================================================================
    private void ModeSwitchBtn_Click(object sender, RoutedEventArgs e)
    {
        var targetMode = _settings.DockMode == MiniDockMode.CardDeck 
            ? MiniDockMode.DockBar 
            : MiniDockMode.CardDeck;

        WindowMorphService.Instance.SwitchToMiniDock(targetMode);
    }

    private void UpdateDockModeVisuals(MiniDockMode mode)
    {
        if (mode == MiniDockMode.CardDeck)
        {
            CardDeckContainer.Visibility = Visibility.Visible;
            DockBarContainer.Visibility = Visibility.Collapsed;
        }
        else
        {
            CardDeckContainer.Visibility = Visibility.Collapsed;
            DockBarContainer.Visibility = Visibility.Visible;
            UpdateDockBarOrientation();
        }
    }

    private void UpdateDockBarOrientation()
    {
        var orientation = _edgeSnapService.GetSuggestedOrientation(_settings.Orientation);
        if (orientation == DockOrientation.Vertical)
        {
            DockBarStack.Orientation = Orientation.Vertical;
            DockBarSeparator.Width = 18;
            DockBarSeparator.Height = 1;
            DockBarSeparator.Margin = new Thickness(0, 2, 0, 2);
        }
        else
        {
            DockBarStack.Orientation = Orientation.Horizontal;
            DockBarSeparator.Width = 1;
            DockBarSeparator.Height = 18;
            DockBarSeparator.Margin = new Thickness(2, 0, 2, 0);
        }
    }

    private void PinModeBtn_Click(object sender, RoutedEventArgs e)
    {
        _settings.PinMode = _settings.PinMode switch
        {
            DesktopPinMode.AlwaysOnTop => DesktopPinMode.PinToDesktop,
            DesktopPinMode.PinToDesktop => DesktopPinMode.Normal,
            _ => DesktopPinMode.AlwaysOnTop
        };

        UpdatePinModeVisuals(_settings.PinMode);
        if (App.MainWindowInstance != null)
        {
            DesktopPinService.ApplyPinMode(App.MainWindowInstance, _settings.PinMode);
        }
        MiniDockSettingsService.Instance.SaveSettings(_settings);
    }

    private void UpdatePinModeVisuals(DesktopPinMode mode)
    {
        switch (mode)
        {
            case DesktopPinMode.AlwaysOnTop:
                PinModeIcon.Glyph = "\uE718"; // Pin
                ToolTipService.SetToolTip(PinModeBtn, I18nService.Instance["MiniDock.PinTop"]);
                break;
            case DesktopPinMode.PinToDesktop:
                PinModeIcon.Glyph = "\uE748"; // Desktop
                ToolTipService.SetToolTip(PinModeBtn, I18nService.Instance["MiniDock.PinDesktop"]);
                break;
            case DesktopPinMode.Normal:
                PinModeIcon.Glyph = "\uE77A"; // Unpin
                ToolTipService.SetToolTip(PinModeBtn, I18nService.Instance["MiniDock.PinNormal"]);
                break;
        }
    }

    // =========================================================================
    // SEARCH & SETTINGS HANDLERS
    // =========================================================================
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchFilter = SearchBox.Text.Trim();
        FilterCommands();
    }

    private void OpacitySlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (BackgroundBackdrop == null) return;
        var val = e.NewValue / 100.0;
        BackgroundBackdrop.Opacity = val;
        _settings.Opacity = val;
        MiniDockSettingsService.Instance.SaveSettings(_settings);
    }

    private void CardScaleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CardScaleCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            _settings.CardScale = tag switch
            {
                "Small" => CardScale.Small,
                "Large" => CardScale.Large,
                _ => CardScale.Medium
            };
            MiniDockSettingsService.Instance.SaveSettings(_settings);
        }
    }

    private void AutoHideSwitch_Toggled(object sender, RoutedEventArgs e)
    {
        _settings.EnableAutoHide = AutoHideSwitch.IsOn;
        _edgeSnapService.SetupAutoHide(AutoHideSwitch.IsOn);
        MiniDockSettingsService.Instance.SaveSettings(_settings);
    }

    private void RootLayout_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _edgeSnapService.OnPointerEntered();
    }

    private void RootLayout_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _edgeSnapService.OnPointerExited(_settings.EnableAutoHide);
    }

    // =========================================================================
    // COMMAND EXECUTION & DYNAMIC PARAMETERS
    // =========================================================================
    private async void CommandCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CommandItem cmd)
        {
            if (cmd.HasParameters)
            {
                await PromptAndExecuteWithParametersAsync(cmd);
            }
            else
            {
                await ExecuteCommandAsync(cmd);
            }
        }
    }

    private async Task PromptAndExecuteWithParametersAsync(CommandItem cmd)
    {
        ParamDialogPrompt.Text = $"{I18nService.Instance["MiniDock.ParamPrompt"]}\n\n{cmd.CommandText}";
        ParamInputTextBox.Text = string.Empty;
        ParamInputDialog.XamlRoot = this.XamlRoot;

        var dialogResult = await ParamInputDialog.ShowAsync();
        if (dialogResult == ContentDialogResult.Primary)
        {
            var param = ParamInputTextBox.Text.Trim();
            // Clone item with replaced parameters
            var runnable = new CommandItem
            {
                Id = cmd.Id,
                Name = cmd.Name,
                Description = cmd.Description,
                ShellType = cmd.ShellType,
                CommandText = ReplacePlaceholder(cmd.CommandText, param),
                Arguments = ReplacePlaceholder(cmd.Arguments, param),
                WorkingDirectory = cmd.WorkingDirectory,
                ExecutionMode = cmd.ExecutionMode
            };

            await ExecuteCommandAsync(cmd, runnable);
        }
    }

    private static string ReplacePlaceholder(string? text, string replacement)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var start = text.IndexOf('{');
        var end = text.IndexOf('}');
        if (start >= 0 && end > start)
        {
            var placeholder = text.Substring(start, end - start + 1);
            return text.Replace(placeholder, replacement);
        }
        return $"{text} {replacement}";
    }

    private async Task ExecuteCommandAsync(CommandItem statusItem, CommandItem? actualToRun = null)
    {
        statusItem.LastRunStatus = ExecutionStatus.Running;
        var toRun = actualToRun ?? statusItem;
        var result = await _commandExecutor.ExecuteAsync(toRun);

        statusItem.LastRunStatus = result.Success ? ExecutionStatus.Success : ExecutionStatus.Failed;
        statusItem.LastRunDurationMs = result.DurationMs;
        statusItem.LastExitCode = result.ExitCode;
        statusItem.LastRunTime = DateTime.UtcNow;

        // Auto-dismiss status after 2.5 seconds
        var runTime = statusItem.LastRunTime;
        _ = Task.Run(async () =>
        {
            await Task.Delay(2500);
            DispatcherQueue.TryEnqueue(() =>
            {
                if (statusItem.LastRunTime == runTime)
                {
                    statusItem.LastRunStatus = ExecutionStatus.Idle;
                }
            });
        });
    }

    private async void CommandCard_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CommandItem cmd)
        {
            var logs = await _logService.GetRecentLogsAsync(20);
            var latest = logs.FirstOrDefault(l => l.CommandId == cmd.Id);

            var flyout = new MenuFlyout();
            var titleItem = new MenuFlyoutItem
            {
                Text = $"{cmd.DisplayName} ({I18nService.Instance["MiniDock.LatestLogTitle"]})",
                IsEnabled = false
            };
            flyout.Items.Add(titleItem);
            flyout.Items.Add(new MenuFlyoutSeparator());

            if (latest != null)
            {
                var statusText = latest.Success 
                    ? $"✓ 成功 (用时 {latest.DurationMs}ms, 代码 {latest.ExitCode})"
                    : $"✗ 失败 (代码 {latest.ExitCode})";
                flyout.Items.Add(new MenuFlyoutItem { Text = statusText, IsEnabled = false });

                if (!string.IsNullOrWhiteSpace(latest.Output))
                {
                    var outputPreview = latest.Output.Trim();
                    if (outputPreview.Length > 80) outputPreview = outputPreview.Substring(0, 80) + "...";
                    flyout.Items.Add(new MenuFlyoutItem { Text = $"输出: {outputPreview}" });
                }
            }
            else
            {
                flyout.Items.Add(new MenuFlyoutItem { Text = I18nService.Instance["MiniDock.NoLogs"], IsEnabled = false });
            }

            flyout.ShowAt(fe);
        }
    }
}
