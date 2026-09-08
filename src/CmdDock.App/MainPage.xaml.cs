using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CmdDock_App.Services;
using CmdDock_App.ViewModels;
using CmdDock_App.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace CmdDock_App;

public sealed partial class MainPage : Page
{
    public MainViewModel ViewModel { get; } = new();
    private bool _isPageLoaded = false;
    private readonly System.Collections.ObjectModel.ObservableCollection<CommandItem> _smallWidgetCommands = new();

    public MainPage()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Loaded += MainPage_Loaded;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.StatusMessage))
            {
                StatusBarText.Text = ViewModel.StatusMessage;
            }
            if (e.PropertyName == nameof(MainViewModel.FilteredCommands))
            {
                UpdateEmptyState();
            }
        };
    }

    private async void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.InitializeAsync();
        UpdateGroupFilterItems();
        BindCollections();
        UpdateEmptyState();
        InitializeWidgetSettingsUI();
        InitializeSettingsUI();
        UpdateLocalizedTexts();
        I18nService.Instance.LanguageChanged += OnLanguageChanged;
        _isPageLoaded = true;
    }

    private void OnLanguageChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateLocalizedTexts();
            UpdateGroupFilterItems();
            UpdateSettingsDescription();
            UpdateSmallWidgetControls();
        });
    }

    private void BindCollections()
    {
        CommandsListView.ItemsSource = ViewModel.FilteredCommands;
        PresetsListView.ItemsSource = ViewModel.Presets;
        LogsListView.ItemsSource = ViewModel.Logs;
    }

    private async void UpdateGroupFilterItems()
    {
        try
        {
            var catItems = await ViewModel.CategoryService.GetAllCategoryItemsAsync();
            var list = new List<CategoryItem>
            {
                new CategoryItem
                {
                    Name = "全部",
                    IconGlyph = "\uE8B9",
                    Order = -1
                }
            };

            foreach (var item in catItems)
            {
                if (!list.Any(c => string.Equals(c.Name, item.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add(item);
                }
            }

            GroupFilterCombo.ItemsSource = list;
            var current = ViewModel.SelectedGroup;
            var target = list.FirstOrDefault(c => string.Equals(c.Name, current, StringComparison.OrdinalIgnoreCase)) ?? list[0];
            GroupFilterCombo.SelectedItem = target;
        }
        catch { }
    }

    private void UpdateEmptyState()
    {
        EmptyCommandsView.Visibility = ViewModel.FilteredCommands.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        CommandsListView.Visibility = ViewModel.FilteredCommands.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (CommandsPanel == null || PresetsPanel == null || LogsPanel == null || WidgetPanel == null || SettingsPanel == null)
        {
            return;
        }

        if (args.IsSettingsSelected)
        {
            CommandsPanel.Visibility = Visibility.Collapsed;
            PresetsPanel.Visibility = Visibility.Collapsed;
            LogsPanel.Visibility = Visibility.Collapsed;
            WidgetPanel.Visibility = Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Visible;
            return;
        }

        if (args.SelectedItemContainer is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            CommandsPanel.Visibility = tag == "commands" ? Visibility.Visible : Visibility.Collapsed;
            PresetsPanel.Visibility = tag == "presets" ? Visibility.Visible : Visibility.Collapsed;
            LogsPanel.Visibility = tag == "logs" ? Visibility.Visible : Visibility.Collapsed;
            WidgetPanel.Visibility = tag == "widget" ? Visibility.Visible : Visibility.Collapsed;
            SettingsPanel.Visibility = Visibility.Collapsed;
        }
    }

    private static CommandItem? GetCommandItem(object sender)
    {
        if (sender is FrameworkElement fe)
        {
            if (fe.Tag is CommandItem tagItem) return tagItem;
            if (fe.DataContext is CommandItem dcItem) return dcItem;
        }
        return null;
    }

    private async void AddCommandBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var categories = await ViewModel.CategoryService.GetAllCategoryItemsAsync();
            var dialog = new CommandEditDialog(null, categoryItems: categories)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var item = dialog.GetUpdatedItem();
                await ViewModel.SaveCommandAsync(item);
                UpdateGroupFilterItems();
                UpdateEmptyState();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AddCommandBtn_Click] Error: {ex}");
        }
    }

    private async void ManageCategoriesBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new CategoryManagementDialog(ViewModel.CategoryService, ViewModel.CommandService)
            {
                XamlRoot = this.XamlRoot
            };

            dialog.CategoriesChanged += async (_, _) =>
            {
                await ViewModel.RefreshCommandsAsync();
                UpdateGroupFilterItems();
                UpdateEmptyState();
                WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            };

            await dialog.ShowAsync();
            await ViewModel.RefreshCommandsAsync();
            UpdateGroupFilterItems();
            UpdateEmptyState();
            WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ManageCategoriesBtn_Click] Error: {ex}");
        }
    }

    private async void EditCommandBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var item = GetCommandItem(sender);
            if (item != null)
            {
                var categories = await ViewModel.CategoryService.GetAllCategoryItemsAsync();
                var dialog = new CommandEditDialog(item, categoryItems: categories)
                {
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var updated = dialog.GetUpdatedItem();
                    await ViewModel.SaveCommandAsync(updated);
                    UpdateGroupFilterItems();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[EditCommandBtn_Click] Error: {ex}");
        }
    }

    private async void DeleteCommandBtn_Click(object sender, RoutedEventArgs e)
    {
        var item = GetCommandItem(sender);
        if (item != null)
        {
            var confirmDialog = new ContentDialog
            {
                Title = I18nService.Instance["Commands.DeleteConfirmTitle"],
                Content = I18nService.Instance.Format("Commands.DeleteConfirmContent", item.DisplayName),
                PrimaryButtonText = I18nService.Instance["Commands.Delete"],
                CloseButtonText = I18nService.Instance["Commands.Cancel"],
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var res = await confirmDialog.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await ViewModel.DeleteCommandAsync(item);
                UpdateEmptyState();
            }
        }
    }

    private async void RunCommandBtn_Click(object sender, RoutedEventArgs e)
    {
        var item = GetCommandItem(sender);
        if (item != null)
        {
            if (item.RequireConfirmation)
            {
                var confirmDialog = new ContentDialog
                {
                    Title = I18nService.Instance["Dialog.RunConfirm.Title"],
                    Content = I18nService.Instance.Format("Dialog.RunConfirm.Content", item.DisplayName, item.CommandText),
                    PrimaryButtonText = I18nService.Instance["Dialog.RunConfirm.Execute"],
                    CloseButtonText = I18nService.Instance["Commands.Cancel"],
                    XamlRoot = this.XamlRoot
                };

                var res = await confirmDialog.ShowAsync();
                if (res != ContentDialogResult.Primary) return;
            }

            await ViewModel.RunCommandAsync(item);
        }
    }

    private async void WidgetToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (!_isPageLoaded) return;

        var item = GetCommandItem(sender);
        if (item != null && sender is ToggleSwitch ts)
        {
            if (item.ShowInWidget != ts.IsOn)
            {
                item.ShowInWidget = ts.IsOn;
                await ViewModel.ToggleWidgetVisibilityAsync(item);
            }
        }
    }

    private async void RefreshBtn_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.RefreshCommandsAsync();
        UpdateEmptyState();
        RefreshSmallWidgetList();
    }

    private void GroupFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageLoaded) return;
        if (GroupFilterCombo.SelectedItem is CategoryItem catItem)
        {
            ViewModel.SelectedGroup = catItem.Name;
            UpdateEmptyState();
        }
        else if (GroupFilterCombo.SelectedItem is string group)
        {
            ViewModel.SelectedGroup = group;
            UpdateEmptyState();
        }
    }

    private async void AddPresetBtn_Click(object sender, RoutedEventArgs e)
    {
        var preset = GetCommandItem(sender);
        if (preset != null)
        {
            await ViewModel.AddPresetAsync(preset);
            UpdateEmptyState();

            if (sender is Button btn)
            {
                btn.Content = I18nService.Instance["Presets.Installed"];
                btn.IsEnabled = false;
            }
        }
    }

    private async void ClearLogsBtn_Click(object sender, RoutedEventArgs e)
    {
        var confirmDialog = new ContentDialog
        {
            Title = I18nService.Instance["Logs.ClearConfirmTitle"],
            Content = I18nService.Instance["Logs.ClearConfirmContent"],
            PrimaryButtonText = I18nService.Instance["Logs.Clear"],
            CloseButtonText = I18nService.Instance["Commands.Cancel"],
            XamlRoot = this.XamlRoot
        };

        if (await confirmDialog.ShowAsync() == ContentDialogResult.Primary)
        {
            await ViewModel.ClearLogsAsync();
        }
    }

    private void InitializeWidgetSettingsUI()
    {
        try
        {
            var currentLayout = WidgetSettings.GetLayoutMode();
            var currentPagination = WidgetSettings.GetPaginationStyle();

            foreach (var itemObj in WidgetLayoutCombo.Items)
            {
                if (itemObj is ComboBoxItem item && item.Tag?.ToString() == currentLayout)
                {
                    WidgetLayoutCombo.SelectedItem = item;
                    break;
                }
            }

            foreach (var itemObj in WidgetPaginationCombo.Items)
            {
                if (itemObj is ComboBoxItem item && item.Tag?.ToString() == currentPagination)
                {
                    WidgetPaginationCombo.SelectedItem = item;
                    break;
                }
            }

            UpdateSettingsDescription();
            InitializeSmallWidgetUI();
        }
        catch { }
    }

    private void InitializeSmallWidgetUI()
    {
        try
        {
            SmallWidgetSelectedListView.ItemsSource = _smallWidgetCommands;
            RefreshSmallWidgetList();
        }
        catch { }
    }

    private void RefreshSmallWidgetList()
    {
        var savedIds = WidgetSettings.GetSmallWidgetCommandIds();
        _smallWidgetCommands.Clear();

        if (savedIds.Count > 0)
        {
            foreach (var id in savedIds)
            {
                var found = ViewModel.Commands.FirstOrDefault(c => c.Id == id);
                if (found != null && !_smallWidgetCommands.Any(c => c.Id == found.Id))
                {
                    _smallWidgetCommands.Add(found);
                }
            }
        }

        // If fewer than 4, autofill from enabled widget commands
        if (_smallWidgetCommands.Count < 4)
        {
            foreach (var cmd in ViewModel.Commands.Where(c => c.ShowInWidget))
            {
                if (!_smallWidgetCommands.Any(c => c.Id == cmd.Id))
                {
                    _smallWidgetCommands.Add(cmd);
                    if (_smallWidgetCommands.Count == 4) break;
                }
            }
            WidgetSettings.SetSmallWidgetCommandIds(_smallWidgetCommands.Select(c => c.Id));
        }

        UpdateSmallWidgetControls();
    }

    private void UpdateSmallWidgetControls()
    {
        SmallWidgetCountBadge.Text = I18nService.Instance.Format("Widget.SmallCountBadge", _smallWidgetCommands.Count, 4);

        var available = ViewModel.Commands.Where(c => !_smallWidgetCommands.Any(s => s.Id == c.Id)).ToList();
        AvailableCommandsCombo.ItemsSource = available;
        if (available.Count > 0)
        {
            AvailableCommandsCombo.SelectedIndex = 0;
        }
    }

    private void SmallWidgetAddBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_smallWidgetCommands.Count >= 4)
        {
            ViewModel.StatusMessage = I18nService.Instance["Widget.SmallMaxTip"];
            return;
        }

        if (AvailableCommandsCombo.SelectedItem is CommandItem item)
        {
            _smallWidgetCommands.Add(item);
            WidgetSettings.SetSmallWidgetCommandIds(_smallWidgetCommands.Select(c => c.Id));
            UpdateSmallWidgetControls();
            WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            ViewModel.StatusMessage = I18nService.Instance.Format("Widget.SmallAdded", item.DisplayName);
        }
    }

    private void SmallWidgetRemoveBtn_Click(object sender, RoutedEventArgs e)
    {
        var item = GetCommandItem(sender);
        if (item != null)
        {
            _smallWidgetCommands.Remove(item);
            WidgetSettings.SetSmallWidgetCommandIds(_smallWidgetCommands.Select(c => c.Id));
            UpdateSmallWidgetControls();
            WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            ViewModel.StatusMessage = I18nService.Instance.Format("Widget.SmallRemoved", item.DisplayName);
        }
    }

    private void SmallWidgetMoveUpBtn_Click(object sender, RoutedEventArgs e)
    {
        var item = GetCommandItem(sender);
        if (item != null)
        {
            int idx = _smallWidgetCommands.IndexOf(item);
            if (idx > 0)
            {
                _smallWidgetCommands.Move(idx, idx - 1);
                WidgetSettings.SetSmallWidgetCommandIds(_smallWidgetCommands.Select(c => c.Id));
                WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            }
        }
    }

    private void SmallWidgetMoveDownBtn_Click(object sender, RoutedEventArgs e)
    {
        var item = GetCommandItem(sender);
        if (item != null)
        {
            int idx = _smallWidgetCommands.IndexOf(item);
            if (idx >= 0 && idx < _smallWidgetCommands.Count - 1)
            {
                _smallWidgetCommands.Move(idx, idx + 1);
                WidgetSettings.SetSmallWidgetCommandIds(_smallWidgetCommands.Select(c => c.Id));
                WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            }
        }
    }

    private void UpdateSettingsDescription()
    {
        var i18n = I18nService.Instance;
        var layoutTag = (WidgetLayoutCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? WidgetSettings.GetLayoutMode();
        LayoutDescText.Text = layoutTag switch
        {
            WidgetSettings.LayoutDropdown => i18n["Widget.LayoutDescDropdown"],
            _ => i18n["Widget.LayoutDescSidebar"]
        };

        var paginationTag = (WidgetPaginationCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? WidgetSettings.GetPaginationStyle();
        PaginationDescText.Text = paginationTag switch
        {
            WidgetSettings.PaginationBottom => i18n["Widget.PaginationDescBottom"],
            _ => i18n["Widget.PaginationDescInline"]
        };
    }

    private void WidgetLayoutCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageLoaded) return;
        if (WidgetLayoutCombo.SelectedItem is ComboBoxItem item && item.Tag is string layout)
        {
            WidgetSettings.SetLayoutMode(layout);
            UpdateSettingsDescription();
            WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            ViewModel.StatusMessage = I18nService.Instance.Format("Widget.StatusLayoutChanged", item.Content);
        }
    }

    private void WidgetPaginationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageLoaded) return;
        if (WidgetPaginationCombo.SelectedItem is ComboBoxItem item && item.Tag is string style)
        {
            WidgetSettings.SetPaginationStyle(style);
            UpdateSettingsDescription();
            WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
            ViewModel.StatusMessage = I18nService.Instance.Format("Widget.StatusPaginationChanged", item.Content);
        }
    }

    private void ApplyWidgetSettingsBtn_Click(object sender, RoutedEventArgs e)
    {
        WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
        ViewModel.StatusMessage = I18nService.Instance["Widget.StatusSynced"];
    }

    private void InitializeSettingsUI()
    {
        try
        {
            var currentTheme = AppSettingsService.GetTheme();
            foreach (ComboBoxItem item in ThemeSettingCombo.Items)
            {
                if (string.Equals(item.Tag?.ToString(), currentTheme, StringComparison.OrdinalIgnoreCase))
                {
                    ThemeSettingCombo.SelectedItem = item;
                    break;
                }
            }

            var currentLang = AppSettingsService.GetLanguage();
            foreach (ComboBoxItem item in LanguageSettingCombo.Items)
            {
                if (string.Equals(item.Tag?.ToString(), currentLang, StringComparison.OrdinalIgnoreCase))
                {
                    LanguageSettingCombo.SelectedItem = item;
                    break;
                }
            }

            SettingsDataFolderPath.Text = AppPaths.BaseDirectory;
        }
        catch { }
    }

    private void ThemeSettingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageLoaded) return;
        if (ThemeSettingCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            ThemeService.ApplyTheme(tag, saveSetting: true);
        }
    }

    private void LanguageSettingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isPageLoaded) return;
        if (LanguageSettingCombo.SelectedItem is ComboBoxItem item && item.Tag is string tag)
        {
            I18nService.Instance.CurrentLanguage = tag;
            WidgetNotificationService.NotifyWidgets(ViewModel.Commands);
        }
    }

    private void OpenDataFolderBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppPaths.BaseDirectory,
                UseShellExecute = true
            });
        }
        catch { }
    }

    private bool _isPolicyExpanded = false;

    private void ToggleFullPolicyBtn_Click(object sender, RoutedEventArgs e)
    {
        var i18n = I18nService.Instance;
        _isPolicyExpanded = !_isPolicyExpanded;
        FullPolicyBox.Visibility = _isPolicyExpanded ? Visibility.Visible : Visibility.Collapsed;
        ToggleFullPolicyIcon.Glyph = _isPolicyExpanded ? "\uE70E" : "\uE70D";
        ToggleFullPolicyBtnText.Text = _isPolicyExpanded
            ? i18n["Settings.PrivacyToggleCollapse"]
            : i18n["Settings.PrivacyToggleExpand"];
    }

    private void UpdateLocalizedTexts()
    {
        var i18n = I18nService.Instance;

        // Nav Pane
        NavPaneHeaderTitle.Text = i18n["App.ShortTitle"];
        NavCommandsItem.Content = i18n["Nav.Commands"];
        NavPresetsItem.Content = i18n["Nav.Presets"];
        NavLogsItem.Content = i18n["Nav.Logs"];
        NavWidgetItem.Content = i18n["Nav.Widgets"];

        // Commands
        AddCommandBtnText.Text = i18n["Commands.New"];
        RefreshBtnText.Text = i18n["Commands.Refresh"];
        ManageCategoriesBtnText.Text = i18n["Commands.CategoryManage"];
        ToolTipService.SetToolTip(GroupFilterCombo, i18n["Commands.CategoryFilterToolTip"]);
        ToolTipService.SetToolTip(ManageCategoriesBtn, i18n["Commands.CategoryManageToolTip"]);
        EmptyCommandsTitle.Text = i18n["Commands.EmptyTip"];
        EmptyCommandsSubtitle.Text = i18n["Commands.EmptySubtitle"];

        // Presets
        PresetsTitleText.Text = i18n["Presets.Title"];
        PresetsSubtitleText.Text = i18n["Presets.Subtitle"];

        // Logs
        LogsTitleText.Text = i18n["Logs.Title"];
        LogsSubtitleText.Text = i18n["Logs.Subtitle"];
        ClearLogsBtnText.Text = i18n["Logs.Clear"];

        // Widget Panel
        WidgetTitleText.Text = i18n["Widget.Title"];
        WidgetSubtitleText.Text = i18n["Widget.Subtitle"];
        WidgetLayoutCardTitle.Text = i18n["Widget.LayoutCardTitle"];
        WidgetLayoutCardDesc.Text = i18n["Widget.LayoutCardDesc"];
        ApplyWidgetSettingsBtnText.Text = i18n["Widget.ApplyBtnSync"];
        WidgetLayoutHeader.Text = i18n["Widget.LayoutHeader"];
        WidgetLayoutSidebarItem.Content = i18n["Widget.LayoutSidebar"];
        WidgetLayoutDropdownItem.Content = i18n["Widget.LayoutDropdown"];
        WidgetPaginationHeader.Text = i18n["Widget.PaginationHeader"];
        WidgetPaginationInlineItem.Content = i18n["Widget.PaginationInline"];
        WidgetPaginationBottomItem.Content = i18n["Widget.PaginationBottom"];
        UpdateSettingsDescription();

        SmallWidgetConfigTitle.Text = i18n["Widget.SmallConfigTitle"];
        SmallWidgetConfigDesc.Text = i18n["Widget.SmallConfigDesc"];
        SmallWidgetOrderTip.Text = i18n["Widget.SmallOrderTip"];
        AvailableCommandsCombo.Header = i18n["Widget.SmallAddHeader"];
        AvailableCommandsCombo.PlaceholderText = i18n["Widget.SmallPlaceholder"];
        SmallWidgetAddBtnText.Text = i18n["Widget.SmallAddBtn"];
        UpdateSmallWidgetControls();

        // Widget Mockups
        WidgetPreviewTitle.Text = i18n["Widget.PreviewTitle"];
        WidgetPreviewSmallTitle.Text = i18n["Widget.PreviewSmallTitle"];
        MockupSmallBtn1.Content = i18n["Mockup.FlushDns"];
        MockupSmallBtn2.Content = i18n["Mockup.RestartExplorer"];
        MockupSmallBtn3.Content = i18n["Mockup.ClearClipboardShort"];
        MockupSmallBtn4.Content = i18n["Mockup.ViewPorts"];
        WidgetPreviewMedTitle.Text = i18n["Widget.PreviewMediumTitle"];
        WidgetPreviewManageText.Text = i18n["Widget.PreviewManage"];
        MockupMedBtn1.Content = i18n["Mockup.FlushDns"];
        MockupMedBtn2.Content = i18n["Mockup.ClearClipboard"];
        MockupMedBtn3.Content = i18n["Mockup.ViewPorts"];
        MockupMedBtn4.Content = i18n["Mockup.LockScreen"];
        MockupMedBtn5.Content = i18n["Mockup.RestartExplorer"];
        MockupMedBtn6.Content = i18n["Mockup.PingTest"];
        MockupMedBtn7.Content = i18n["Mockup.GitStatus"];
        MockupMedBtn8.Content = i18n["Mockup.CleanTemp"];
        MockupMedBtn9.Content = i18n["Mockup.IpDetails"];
        MockupMedBtn10.Content = i18n["Mockup.NodeVersion"];

        // Widget Guide
        WidgetGuideTitle.Text = i18n["Widget.GuideTitle"];
        WidgetGuideStep1Text.Text = i18n["Widget.GuideStep1"];
        WidgetGuideStep2Text.Text = i18n["Widget.GuideStep2"];
        WidgetGuideStep3Text.Text = i18n["Widget.GuideStep3"];
        WidgetGuideStep4Text.Text = i18n["Widget.GuideStep4"];
        WidgetAboutTitle.Text = i18n["Settings.About"];
        WidgetAboutVersion.Text = i18n["Settings.Version"];
        WidgetAboutTech.Text = i18n["Settings.TechStack"];
        WidgetAboutStore.Text = i18n["Widget.AboutStoreReady"];

        // Settings Panel
        SettingsTitleText.Text = i18n["Settings.Title"];
        SettingsSubtitleText.Text = i18n["Settings.Subtitle"];
        SettingsAppearanceGroupText.Text = i18n["Settings.Appearance"];
        SettingsThemeLabel.Text = i18n["Settings.Theme"];
        SettingsThemeDesc.Text = i18n["Settings.ThemeDesc"];
        ThemeItemSystem.Content = i18n["Settings.ThemeSystem"];
        ThemeItemLight.Content = i18n["Settings.ThemeLight"];
        ThemeItemDark.Content = i18n["Settings.ThemeDark"];

        SettingsLanguageLabel.Text = i18n["Settings.Language"];
        SettingsLanguageDesc.Text = i18n["Settings.LanguageDesc"];
        LangItemSystem.Content = i18n["Settings.LangSystem"];
        LangItemChinese.Content = i18n["Settings.LangChinese"];
        LangItemEnglish.Content = i18n["Settings.LangEnglish"];

        SettingsAboutGroupText.Text = i18n["Settings.About"];
        SettingsAboutDescText.Text = i18n["Settings.AboutDesc"];
        SettingsVersionText.Text = i18n["Settings.Version"];
        SettingsTechStackText.Text = i18n["Settings.TechStack"];
        SettingsDataFolderLabel.Text = i18n["Settings.DataFolder"];
        OpenDataFolderBtnText.Text = i18n["Settings.OpenDataFolder"];
        SettingsPrivacyCardTitle.Text = i18n["Settings.PrivacyCardTitle"];
        SettingsPrivacyCardSubtitle.Text = i18n["Settings.PrivacyCardSubtitle"];
        PrivacyPoint1Title.Text = i18n["Settings.PrivacyPoint1Title"];
        PrivacyPoint1Desc.Text = i18n["Settings.PrivacyPoint1Desc"];
        PrivacyPoint2Title.Text = i18n["Settings.PrivacyPoint2Title"];
        PrivacyPoint2Desc.Text = i18n["Settings.PrivacyPoint2Desc"];
        PrivacyPoint3Title.Text = i18n["Settings.PrivacyPoint3Title"];
        PrivacyPoint3Desc.Text = i18n["Settings.PrivacyPoint3Desc"];
        PrivacyPoint4Title.Text = i18n["Settings.PrivacyPoint4Title"];
        PrivacyPoint4Desc.Text = i18n["Settings.PrivacyPoint4Desc"];
        PrivacyPoint5Title.Text = i18n["Settings.PrivacyPoint5Title"];
        PrivacyPoint5Desc.Text = i18n["Settings.PrivacyPoint5Desc"];
        ToggleFullPolicyBtnText.Text = _isPolicyExpanded
            ? i18n["Settings.PrivacyToggleCollapse"]
            : i18n["Settings.PrivacyToggleExpand"];
        FullPolicyContentText.Text = i18n["Privacy.FullText"];

        // Status bar
        if (string.Equals(ViewModel.StatusMessage, "就绪", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ViewModel.StatusMessage, "Ready", StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.StatusMessage = i18n["Status.Ready"];
            StatusBarText.Text = i18n["Status.Ready"];
        }
    }
}
