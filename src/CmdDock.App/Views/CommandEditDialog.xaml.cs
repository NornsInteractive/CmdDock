using CmdDock.Core.Models;
using CmdDock.Core.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;

namespace CmdDock_App.Views;

public sealed partial class CommandEditDialog : ContentDialog
{
    private readonly CommandItem _targetItem;
    private readonly IEnumerable<CategoryItem>? _categoryItems;
    private readonly IEnumerable<string>? _availableCategories;
    private string _currentIcon = "\uE756";
    private string _selectedCategory = "全部";
    private bool _isInitialized = false;
    public bool IsNew { get; }

    public CommandEditDialog(CommandItem? item = null, IEnumerable<CategoryItem>? categoryItems = null, IEnumerable<string>? categories = null)
    {
        InitializeComponent();
        _isInitialized = true;
        IsNew = item == null;
        _targetItem = item ?? new CommandItem();
        _categoryItems = categoryItems;
        _availableCategories = categories;

        var i18n = I18nService.Instance;
        Title = IsNew ? i18n["Dialog.CommandEdit.NewTitle"] : i18n["Dialog.CommandEdit.EditTitle"];
        PrimaryButtonText = i18n["Dialog.CommandEdit.Save"];
        CloseButtonText = i18n["Dialog.CommandEdit.Cancel"];
        NameBox.Header = i18n["Dialog.CommandEdit.Name"];
        NameBox.PlaceholderText = i18n["Dialog.CommandEdit.NamePlaceholder"];
        DescriptionBox.Header = i18n["Dialog.CommandEdit.Desc"];
        DescriptionBox.PlaceholderText = i18n["Dialog.CommandEdit.DescPlaceholder"];
        ShellTypeCombo.Header = i18n["Dialog.CommandEdit.ShellType"];
        ShellTypeCmdItem.Content = i18n["ShellType.Cmd"];
        ShellTypeWslItem.Content = i18n["ShellType.Wsl"];
        ShellTypeExeItem.Content = i18n["ShellType.Executable"];
        ShellTypeUrlItem.Content = i18n["ShellType.UrlProtocol"];
        ExecutionModeCombo.Header = i18n["Dialog.CommandEdit.ExecMode"];
        ExecModeSilentItem.Content = i18n["ExecutionMode.Silent"];
        ExecModeTerminalItem.Content = i18n["ExecutionMode.Terminal"];
        CategoryCombo.Header = i18n["Dialog.CommandEdit.Category"];
        CategoryCombo.PlaceholderText = i18n["Dialog.CommandEdit.CategoryPlaceholder"];
        IconSettingsLabel.Text = i18n.GetString("Dialog.CommandEdit.IconSettings", "图标设置");
        OpenIconPickerBtnText.Text = i18n["Dialog.CommandEdit.ChooseIcon"];
        BrowseIconBtnText.Text = i18n["Dialog.CommandEdit.PickPicture"];
        ToolTipService.SetToolTip(BrowseIconBtn, i18n["Dialog.CommandEdit.PickPictureToolTip"]);
        CommandTextBox.Header = i18n["Dialog.CommandEdit.Script"];
        CommandTextBox.PlaceholderText = i18n["Dialog.CommandEdit.ScriptPlaceholder"];
        ArgumentsBox.Header = i18n["Dialog.CommandEdit.Arguments"];
        ArgumentsBox.PlaceholderText = i18n["Dialog.CommandEdit.ArgumentsPlaceholder"];
        WorkingDirectoryBox.Header = i18n["Dialog.CommandEdit.WorkDir"];
        WorkingDirectoryBox.PlaceholderText = i18n["Dialog.CommandEdit.WorkDirPlaceholder"];
        ShowInWidgetCheck.Content = i18n["Dialog.CommandEdit.ShowInWidget"];
        RequireConfirmCheck.Content = i18n["Dialog.CommandEdit.RequireConfirm"];
        WidgetAppearanceTitle.Text = i18n["Dialog.CommandEdit.WidgetAppearance"];
        WidgetStyleCombo.Header = i18n["Dialog.CommandEdit.BgMode"];
        WidgetStyleTransparentItem.Content = i18n["Dialog.CommandEdit.BgTransparent"];
        WidgetStyleCustomItem.Content = i18n["Dialog.CommandEdit.BgCustom"];
        BgColorLabel.Text = i18n["Dialog.CommandEdit.BgColor"];
        WidgetColorHexBox.PlaceholderText = i18n["Dialog.CommandEdit.BgColorPlaceholder"];

        // Icon Picker controls
        BackFromPickerBtnText.Text = i18n["Dialog.CommandEdit.BackToForm"];
        IconPickerTitleText.Text = i18n["IconPicker.SelectSystemIcon"];
        IconSearchBox.PlaceholderText = i18n["IconPicker.SearchPlaceholder"];
        CatAllBtn.Content = i18n["IconPicker.All"];
        CatOpsBtn.Content = i18n["IconPicker.Operations"];
        CatHwBtn.Content = i18n["IconPicker.Hardware"];
        CatNetBtn.Content = i18n["IconPicker.Network"];
        CatDevBtn.Content = i18n["IconPicker.DevOps"];
        IconPickerBottomBackBtn.Content = i18n["IconPicker.Back"];

        this.Closing += CommandEditDialog_Closing;
        LoadData();
    }

    private void CommandEditDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
    {
        // When in icon picker view, dismissing or pressing ESC simply goes back to the edit form
        if (IconPickerPanel != null && IconPickerPanel.Visibility == Visibility.Visible)
        {
            args.Cancel = true;
            CloseIconPicker();
        }
    }

    private void LoadData()
    {
        NameBox.Text = _targetItem.Name;
        DescriptionBox.Text = _targetItem.Description;

        // Initialize Categories Dropdown with CategoryItem (supporting icons)
        var catList = new List<CategoryItem>();
        if (_categoryItems != null)
        {
            catList.AddRange(_categoryItems.Where(c => !string.IsNullOrWhiteSpace(c.Name) && c.Name != "全部"));
        }
        else if (_availableCategories != null)
        {
            foreach (var c in _availableCategories.Where(c => !string.IsNullOrWhiteSpace(c) && c != "全部"))
            {
                catList.Add(new CategoryItem
                {
                    Name = c,
                    IconGlyph = CategoryService.GetDefaultIconForCategory(c)
                });
            }
        }

        if (catList.Count == 0)
        {
            foreach (var (name, icon) in CategoryService.DefaultCategoryDefinitions)
            {
                catList.Add(new CategoryItem
                {
                    Name = name,
                    IconGlyph = icon
                });
            }
        }

        var currentGroup = string.IsNullOrWhiteSpace(_targetItem.Group) ? "常用" : _targetItem.Group;
        var selectedItem = catList.FirstOrDefault(c => string.Equals(c.Name, currentGroup, StringComparison.OrdinalIgnoreCase));
        if (selectedItem == null)
        {
            selectedItem = new CategoryItem
            {
                Name = currentGroup,
                IconGlyph = CategoryService.GetDefaultIconForCategory(currentGroup)
            };
            catList.Add(selectedItem);
        }

        CategoryCombo.ItemsSource = catList;
        CategoryCombo.SelectedItem = selectedItem;
        CommandTextBox.Text = _targetItem.CommandText;
        ArgumentsBox.Text = _targetItem.Arguments;
        WorkingDirectoryBox.Text = _targetItem.WorkingDirectory;
        ShowInWidgetCheck.IsChecked = _targetItem.ShowInWidget;
        RequireConfirmCheck.IsChecked = _targetItem.RequireConfirmation;

        // Select shell type & execution mode
        SelectComboByTag(ShellTypeCombo, _targetItem.ShellType.ToString());
        SelectComboByTag(ExecutionModeCombo, _targetItem.ExecutionMode.ToString());

        // Initialize Icon
        _currentIcon = string.IsNullOrWhiteSpace(_targetItem.IconGlyph) ? "\uE756" : _targetItem.IconGlyph;
        UpdateIconPreview(_currentIcon);

        // Initialize Widget Card Style: Default is Transparent, otherwise Custom
        if (!string.IsNullOrWhiteSpace(_targetItem.WidgetBackgroundColor) &&
            string.Equals(_targetItem.WidgetCardStyle, "Custom", StringComparison.OrdinalIgnoreCase))
        {
            SelectComboByTag(WidgetStyleCombo, "Custom");
            if (ColorSettingPanel != null) ColorSettingPanel.Visibility = Visibility.Visible;
            if (WidgetColorHexBox != null) WidgetColorHexBox.Text = _targetItem.WidgetBackgroundColor;
            UpdateColorPreview(_targetItem.WidgetBackgroundColor);
        }
        else
        {
            SelectComboByTag(WidgetStyleCombo, "Transparent");
            if (ColorSettingPanel != null) ColorSettingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void UpdateIconPreview(string icon)
    {
        if (!_isInitialized || IconFontPreview == null || ImageIconPreview == null)
        {
            return;
        }

        if (File.Exists(icon))
        {
            try
            {
                IconFontPreview.Visibility = Visibility.Collapsed;
                ImageIconPreview.Visibility = Visibility.Visible;
                ImageIconPreview.Source = new BitmapImage(new Uri(icon));
                if (IconSelectedNameText != null) IconSelectedNameText.Text = Path.GetFileName(icon);
                if (IconSelectedCategoryText != null) IconSelectedCategoryText.Text = I18nService.Instance["IconPicker.CustomLocalImage"];
                return;
            }
            catch
            {
                // Fallback to font glyph
            }
        }

        ImageIconPreview.Visibility = Visibility.Collapsed;
        IconFontPreview.Visibility = Visibility.Visible;
        var normalized = CommandItem.NormalizeToSegoeGlyph(icon);
        IconFontPreview.Glyph = normalized;

        var found = IconItem.FindByGlyph(normalized);
        if (found != null)
        {
            if (IconSelectedNameText != null) IconSelectedNameText.Text = found.LocalizedName;
            if (IconSelectedCategoryText != null) IconSelectedCategoryText.Text = $"{I18nService.Instance["IconPicker.OfficialIcon"]} · {found.LocalizedCategory}";
        }
        else
        {
            if (IconSelectedNameText != null) IconSelectedNameText.Text = I18nService.Instance["IconPicker.OfficialIcon"];
            if (IconSelectedCategoryText != null) IconSelectedCategoryText.Text = I18nService.Instance["IconPicker.WindowsNative"];
        }
    }

    private void OpenIconPickerBtn_Click(object sender, RoutedEventArgs e)
    {
        EditFormScrollViewer.Visibility = Visibility.Collapsed;
        IconPickerPanel.Visibility = Visibility.Visible;
        Title = I18nService.Instance["IconPicker.SelectSystemIcon"];
        IsPrimaryButtonEnabled = false;

        _selectedCategory = "全部";
        UpdateCategoryButtonsVisual();
        IconSearchBox.Text = string.Empty;
        RefreshIconsList();

        IconSearchBox.Focus(FocusState.Programmatic);
    }

    private void BackFromPickerBtn_Click(object sender, RoutedEventArgs e)
    {
        CloseIconPicker();
    }

    private void CloseIconPicker()
    {
        IconPickerPanel.Visibility = Visibility.Collapsed;
        EditFormScrollViewer.Visibility = Visibility.Visible;
        Title = IsNew ? I18nService.Instance["Dialog.CommandEdit.NewTitle"] : I18nService.Instance["Dialog.CommandEdit.EditTitle"];
        IsPrimaryButtonEnabled = true;
    }

    private void IconSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshIconsList();
    }

    private void CategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cat)
        {
            _selectedCategory = cat;
            UpdateCategoryButtonsVisual();
            RefreshIconsList();
        }
    }

    private void UpdateCategoryButtonsVisual()
    {
        var accentStyle = Application.Current.Resources["AccentButtonStyle"] as Style;
        var defaultStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;

        SetButtonStyle(CatAllBtn, _selectedCategory == "全部", accentStyle, defaultStyle);
        SetButtonStyle(CatOpsBtn, _selectedCategory == "常用与操作", accentStyle, defaultStyle);
        SetButtonStyle(CatHwBtn, _selectedCategory == "系统与硬件", accentStyle, defaultStyle);
        SetButtonStyle(CatNetBtn, _selectedCategory == "网络通信", accentStyle, defaultStyle);
        SetButtonStyle(CatDevBtn, _selectedCategory == "开发与运维", accentStyle, defaultStyle);
    }

    private static void SetButtonStyle(Button? btn, bool isSelected, Style? accent, Style? def)
    {
        if (btn == null) return;
        if (isSelected && accent != null)
        {
            btn.Style = accent;
        }
        else if (!isSelected && def != null)
        {
            btn.Style = def;
        }
    }

    private void RefreshIconsList()
    {
        var query = IconSearchBox?.Text?.Trim();
        var results = IconItem.Filter(query, _selectedCategory);

        if (IconsGridView != null)
        {
            IconsGridView.ItemsSource = results;
        }

        if (IconCountBadge != null)
        {
            IconCountBadge.Text = string.IsNullOrWhiteSpace(query)
                ? I18nService.Instance.Format("IconPicker.CountTotal", results.Count)
                : I18nService.Instance.Format("IconPicker.CountMatched", results.Count);
        }

        if (IconSearchHint != null)
        {
            if (results.Count == 0)
            {
                IconSearchHint.Text = I18nService.Instance["IconPicker.HintEmpty"];
            }
            else
            {
                IconSearchHint.Text = I18nService.Instance["IconPicker.HintSelect"];
            }
        }
    }

    private void IconsGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IconItem selected)
        {
            _currentIcon = selected.Glyph;
            UpdateIconPreview(_currentIcon);
            CloseIconPicker();
        }
    }

    private async void BrowseIconBtn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();

            var window = App.MainWindowInstance;
            if (window != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                if (hwnd != IntPtr.Zero)
                {
                    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);
                }
            }

            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".ico");
            picker.FileTypeFilter.Add(".svg");
            picker.FileTypeFilter.Add(".bmp");

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var iconsDir = Path.Combine(AppPaths.BaseDirectory, "Icons");
                if (!Directory.Exists(iconsDir))
                {
                    Directory.CreateDirectory(iconsDir);
                }

                var ext = Path.GetExtension(file.Path);
                var destPath = Path.Combine(iconsDir, $"{Guid.NewGuid():N}{ext}");
                File.Copy(file.Path, destPath, overwrite: true);

                _currentIcon = destPath;
                UpdateIconPreview(_currentIcon);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[BrowseIconBtn_Click] File picker error: {ex.Message}");
        }
    }

    public CommandItem GetUpdatedItem()
    {
        _targetItem.Name = NameBox.Text.Trim();
        var selectedCat = CategoryCombo.SelectedItem is CategoryItem ci
            ? ci.Name
            : (CategoryCombo.SelectedItem?.ToString() ?? CategoryCombo.Text?.Trim());
        _targetItem.Group = string.IsNullOrWhiteSpace(selectedCat) ? "常用" : selectedCat.Trim();
        _targetItem.IconGlyph = string.IsNullOrWhiteSpace(_currentIcon) ? "\uE756" : _currentIcon;
        _targetItem.CommandText = CommandTextBox.Text.Trim();
        _targetItem.Arguments = ArgumentsBox.Text.Trim();
        _targetItem.WorkingDirectory = WorkingDirectoryBox.Text.Trim();
        _targetItem.ShowInWidget = ShowInWidgetCheck.IsChecked ?? true;
        _targetItem.RequireConfirmation = RequireConfirmCheck.IsChecked ?? false;

        // Save widget card styling (Default Transparent vs Custom Background Color)
        var styleTag = (WidgetStyleCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "Transparent";
        _targetItem.WidgetCardStyle = styleTag;
        if (styleTag == "Custom")
        {
            _targetItem.WidgetIsTransparent = false;
            var hex = WidgetColorHexBox.Text?.Trim();
            _targetItem.WidgetBackgroundColor = string.IsNullOrWhiteSpace(hex) ? "#2563EB" : hex;
        }
        else
        {
            _targetItem.WidgetIsTransparent = true;
            _targetItem.WidgetBackgroundColor = string.Empty;
        }

        if (ShellTypeCombo.SelectedItem is ComboBoxItem shellItem &&
            Enum.TryParse<ShellType>(shellItem.Tag?.ToString(), out var shellType))
        {
            _targetItem.ShellType = shellType;
        }

        if (ExecutionModeCombo.SelectedItem is ComboBoxItem modeItem &&
            Enum.TryParse<ExecutionMode>(modeItem.Tag?.ToString(), out var mode))
        {
            _targetItem.ExecutionMode = mode;
        }

        return _targetItem;
    }

    private void WidgetStyleCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isInitialized || ColorSettingPanel == null) return;
        var tag = (WidgetStyleCombo.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        if (tag == "Custom")
        {
            ColorSettingPanel.Visibility = Visibility.Visible;
            if (string.IsNullOrWhiteSpace(WidgetColorHexBox.Text))
            {
                WidgetColorHexBox.Text = "#2563EB";
            }
            UpdateColorPreview(WidgetColorHexBox.Text);
        }
        else
        {
            ColorSettingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void WidgetColorHexBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_isInitialized) return;
        UpdateColorPreview(WidgetColorHexBox.Text);
    }

    private void ColorSwatch_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string hex)
        {
            WidgetColorHexBox.Text = hex;
            UpdateColorPreview(hex);
        }
    }

    private void UpdateColorPreview(string? hex)
    {
        if (ColorPreviewBorder == null || string.IsNullOrWhiteSpace(hex)) return;
        try
        {
            var c = WidgetIconService.ParseHexColor(hex);
            ColorPreviewBorder.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(
                Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B));
        }
        catch { }
    }

    private static void SelectComboByTag(ComboBox combo, string tag)
    {
        foreach (var item in combo.Items)
        {
            if (item is ComboBoxItem cbi && string.Equals(cbi.Tag?.ToString(), tag, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = cbi;
                return;
            }
        }
    }
}
