using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock_App.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using WinRT.Interop;

namespace CmdDock_App.Views;

public sealed partial class MiniDockPage : Page
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

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
        _edgeSnapService = new EdgeSnapService((Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance);

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
        OpacitySlider.Minimum = 30;
        OpacitySlider.Maximum = 100;
        OpacitySlider.Value = BackgroundBackdrop.Opacity * 100;
        OpacitySlider.ValueChanged += OpacitySlider_ValueChanged;

        // 2. Dock Mode (Card vs Bar)
        UpdateDockModeVisuals(_settings.DockMode);

        // 3. Pin Mode Visuals
        UpdatePinModeVisuals(_settings.PinMode);

        // 4. Card Scale
        foreach (ComboBoxItem item in CardScaleCombo.Items)
        {
            if (string.Equals(item.Tag?.ToString(), _settings.CardScale.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                CardScaleCombo.SelectedItem = item;
                break;
            }
        }
        CardScaleCombo.SelectionChanged += CardScaleCombo_SelectionChanged;

        // 5. Auto Hide
        AutoHideSwitch.IsOn = _settings.EnableAutoHide;
        _edgeSnapService.SetupAutoHide(_settings.EnableAutoHide);
        AutoHideSwitch.Toggled += AutoHideSwitch_Toggled;

        // 6. Tooltips
        ToolTipService.SetToolTip(AllCategoriesBtn, I18nService.Instance["MiniDock.AllCategories"]);
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
                    SelectCategory(chosen);
                }
            };

            CategoryPillsPanel.Children.Add(btn);
        }
    }

    private void SelectCategory(string chosen)
    {
        _selectedCategory = chosen;
        BuildCategoryPills();
        FilterCommands();

        // Scroll the selected pill smoothly into view
        DispatcherQueue.TryEnqueue(() =>
        {
            foreach (var child in CategoryPillsPanel.Children)
            {
                if (child is Button b && b.Tag as string == chosen)
                {
                    b.StartBringIntoView(new BringIntoViewOptions { AnimationDesired = true });
                    break;
                }
            }
        });
    }

    private void CategoryScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(CategoryScrollViewer).Properties.MouseWheelDelta;
        if (delta != 0)
        {
            var currentOffset = CategoryScrollViewer.HorizontalOffset;
            var targetOffset = Math.Max(0, currentOffset - delta);
            CategoryScrollViewer.ChangeView(targetOffset, null, null, false);
            e.Handled = true;
        }
    }

    private void AllCategoriesBtn_Click(object sender, RoutedEventArgs e)
    {
        var flyout = new MenuFlyout();

        var titleItem = new MenuFlyoutItem
        {
            Text = $"{I18nService.Instance["MiniDock.AllCategories"]} ({Categories.Count})",
            IsEnabled = false
        };
        flyout.Items.Add(titleItem);
        flyout.Items.Add(new MenuFlyoutSeparator());

        foreach (var cat in Categories)
        {
            var isSelected = cat == _selectedCategory;
            var item = new ToggleMenuFlyoutItem
            {
                Text = I18nService.Instance.TranslateCategory(cat),
                IsChecked = isSelected,
                Tag = cat
            };

            item.Click += (s, _) =>
            {
                if (s is FrameworkElement fe && fe.Tag is string chosen)
                {
                    SelectCategory(chosen);
                }
            };

            flyout.Items.Add(item);
        }

        if (sender is FrameworkElement target)
        {
            flyout.ShowAt(target);
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
    private bool _isDraggingWindow = false;
    private POINT _dragStartCursor;
    private PointInt32 _dragStartWindowPos;

    private void Header_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var ptrPt = e.GetCurrentPoint(sender as UIElement);
        if (ptrPt.Properties.IsLeftButtonPressed)
        {
            var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance;
            if (window?.AppWindow != null)
            {
                _isDraggingWindow = true;
                _edgeSnapService.IsDragging = true;
                GetCursorPos(out _dragStartCursor);
                _dragStartWindowPos = window.AppWindow.Position;

                if (sender is UIElement uie)
                {
                    uie.CapturePointer(e.Pointer);
                }
                e.Handled = true;
            }
        }
    }

    private void Header_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isDraggingWindow)
        {
            var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance;
            if (window?.AppWindow != null)
            {
                GetCursorPos(out var cur);
                int newX = _dragStartWindowPos.X + (cur.X - _dragStartCursor.X);
                int newY = _dragStartWindowPos.Y + (cur.Y - _dragStartCursor.Y);

                var hWnd = WindowNative.GetWindowHandle(window);
                if (hWnd != IntPtr.Zero)
                {
                    SetWindowPos(hWnd, IntPtr.Zero, newX, newY, 0, 0, 0x0001 | 0x0004 | 0x0010); // SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE
                }
                e.Handled = true;
            }
        }
    }

    private void Header_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDraggingWindow)
        {
            _isDraggingWindow = false;
            _edgeSnapService.IsDragging = false;
            if (sender is UIElement uie)
            {
                uie.ReleasePointerCapture(e.Pointer);
            }

            var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance;
            if (window?.AppWindow != null)
            {
                _edgeSnapService.CheckAndSnap(applySnap: true);

                _settings.WindowX = window.AppWindow.Position.X;
                _settings.WindowY = window.AppWindow.Position.Y;
                MiniDockSettingsService.Instance.SaveSettings(_settings);
            }
            e.Handled = true;
        }
    }

    private void Header_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
    {
        if (_isDraggingWindow)
        {
            _isDraggingWindow = false;
            _edgeSnapService.IsDragging = false;
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

    private void CloseBtn_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Exit();
    }

    // =========================================================================
    // CARD DECK RESIZING HANDLERS
    // =========================================================================
    private bool _isResizingCard;
    private Windows.Foundation.Point _resizeStartPoint;
    private SizeInt32 _resizeStartSize;

    private void ResizeGrip_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow;
            if (window?.AppWindow != null)
            {
                _isResizingCard = true;
                _resizeStartPoint = e.GetCurrentPoint(null).Position;
                _resizeStartSize = window.AppWindow.Size;
                if (sender is UIElement uie)
                {
                    uie.CapturePointer(e.Pointer);
                }
                e.Handled = true;
            }
        }
    }

    private void ResizeGrip_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizingCard)
        {
            var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow;
            if (window?.AppWindow != null)
            {
                var currentPoint = e.GetCurrentPoint(null).Position;
                var deltaX = currentPoint.X - _resizeStartPoint.X;
                var deltaY = currentPoint.Y - _resizeStartPoint.Y;

                var newW = Math.Max(260, (int)(_resizeStartSize.Width + deltaX));
                var newH = Math.Max(220, (int)(_resizeStartSize.Height + deltaY));

                window.AppWindow.Resize(new SizeInt32(newW, newH));
            }
        }
    }

    private void ResizeGrip_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isResizingCard)
        {
            _isResizingCard = false;
            if (sender is UIElement uie)
            {
                uie.ReleasePointerCapture(e.Pointer);
            }
            var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow;
            if (window?.AppWindow != null)
            {
                _settings.MiniWidth = window.AppWindow.Size.Width;
                _settings.MiniHeight = window.AppWindow.Size.Height;
                MiniDockSettingsService.Instance.SaveSettings(_settings);
            }
            e.Handled = true;
        }
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

    public void SetDockMode(MiniDockMode mode)
    {
        _settings.DockMode = mode;
        UpdateDockModeVisuals(mode);
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
            DockBarControlsStack.Orientation = Orientation.Vertical;
            DockBarSeparator.Width = 18;
            DockBarSeparator.Height = 1;
            DockBarSeparator.Margin = new Thickness(0, 2, 0, 2);

            Grid.SetColumn(DockBarScrollViewer, 0);
            Grid.SetRow(DockBarScrollViewer, 1);

            DockBarScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            DockBarScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            DockBarScrollViewer.HorizontalScrollMode = ScrollMode.Disabled;
            DockBarScrollViewer.VerticalScrollMode = ScrollMode.Enabled;

            try
            {
                DockBarItemsControl.ItemsPanel = (ItemsPanelTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    "<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Orientation='Vertical' Spacing='4'/></ItemsPanelTemplate>");
            }
            catch { }
        }
        else
        {
            DockBarControlsStack.Orientation = Orientation.Horizontal;
            DockBarSeparator.Width = 1;
            DockBarSeparator.Height = 18;
            DockBarSeparator.Margin = new Thickness(2, 0, 2, 0);

            Grid.SetColumn(DockBarScrollViewer, 1);
            Grid.SetRow(DockBarScrollViewer, 0);

            DockBarScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
            DockBarScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
            DockBarScrollViewer.HorizontalScrollMode = ScrollMode.Enabled;
            DockBarScrollViewer.VerticalScrollMode = ScrollMode.Disabled;

            try
            {
                DockBarItemsControl.ItemsPanel = (ItemsPanelTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
                    "<ItemsPanelTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><StackPanel Orientation='Horizontal' Spacing='4'/></ItemsPanelTemplate>");
            }
            catch { }
        }
    }

    private void DockBarScrollViewer_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var delta = e.GetCurrentPoint(DockBarScrollViewer).Properties.MouseWheelDelta;
        if (delta != 0)
        {
            var isVertical = DockBarControlsStack.Orientation == Orientation.Vertical;
            if (isVertical)
            {
                var cur = DockBarScrollViewer.VerticalOffset;
                DockBarScrollViewer.ChangeView(null, Math.Max(0, cur - delta), null, false);
            }
            else
            {
                var cur = DockBarScrollViewer.HorizontalOffset;
                DockBarScrollViewer.ChangeView(Math.Max(0, cur - delta), null, null, false);
            }
            e.Handled = true;
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
        var window = (Window?)MiniDockWindow.Instance ?? WindowMorphService.Instance.MiniDockWindow ?? App.MainWindowInstance;
        if (window != null)
        {
            DesktopPinService.ApplyPinMode(window, _settings.PinMode);
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
            var hasParams = cmd.HasParameters;
            var needsConfirm = cmd.RequireConfirmation;
            var ownerWindow = App.MainWindowInstance ?? WindowMorphService.Instance.MainWindow;

            if (hasParams && needsConfirm)
            {
                var actionRes = await CommandActionWindow.ShowDialogAsync(cmd, CommandActionMode.ParameterAndConfirm, ownerWindow);
                if (!actionRes.IsConfirmed) return;

                var runnable = CloneWithParam(cmd, actionRes.ParameterValue);
                await ExecuteCommandAsync(cmd, runnable);
            }
            else if (hasParams)
            {
                var actionRes = await CommandActionWindow.ShowDialogAsync(cmd, CommandActionMode.ParameterInput, ownerWindow);
                if (!actionRes.IsConfirmed) return;

                var runnable = CloneWithParam(cmd, actionRes.ParameterValue);
                await ExecuteCommandAsync(cmd, runnable);
            }
            else if (needsConfirm)
            {
                var actionRes = await CommandActionWindow.ShowDialogAsync(cmd, CommandActionMode.Confirmation, ownerWindow);
                if (!actionRes.IsConfirmed) return;

                await ExecuteCommandAsync(cmd);
            }
            else
            {
                await ExecuteCommandAsync(cmd);
            }
        }
    }

    private static CommandItem CloneWithParam(CommandItem cmd, string param)
    {
        return new CommandItem
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
