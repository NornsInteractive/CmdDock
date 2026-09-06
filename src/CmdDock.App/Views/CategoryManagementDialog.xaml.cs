using CmdDock.Core.Models;
using CmdDock.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;

namespace CmdDock_App.Views;

public sealed partial class CategoryManagementDialog : ContentDialog
{
    private readonly ICategoryService _categoryService;
    private readonly ICommandService _commandService;
    private readonly ObservableCollection<CategoryDisplayItem> _categories = new();

    private enum PickerTarget
    {
        NewCategory,
        EditCategory,
        DirectItem
    }

    private PickerTarget _activePickerTarget = PickerTarget.DirectItem;
    private CategoryDisplayItem? _currentEditingItem;
    private string _newCategoryIcon = "\uE8EC";
    private string _editingCategoryIcon = "\uE8EC";
    private string _selectedIconCategory = "全部";

    public event EventHandler? CategoriesChanged;

    public CategoryManagementDialog(ICategoryService categoryService, ICommandService commandService)
    {
        InitializeComponent();
        _categoryService = categoryService;
        _commandService = commandService;
        CategoriesListView.ItemsSource = _categories;

        var i18n = I18nService.Instance;
        Title = i18n["Dialog.Category.Title"];
        PrimaryButtonText = i18n["Dialog.Category.Done"];
        DialogDescText.Text = i18n["Dialog.Category.Desc"];
        NewCategoryBox.PlaceholderText = i18n["Dialog.Category.Placeholder"];
        AddCategoryBtnText.Text = i18n["Dialog.Category.Add"];
        ToolTipService.SetToolTip(NewCategoryIconBtn, i18n["Dialog.Category.NewCategoryIconToolTip"]);

        // Edit Category Panel
        BackFromEditBtnText.Text = i18n["Dialog.Category.BackToList"];
        EditCategoryNameBox.Header = i18n["Dialog.Category.Name"];
        EditCategoryNameBox.PlaceholderText = i18n["Dialog.Category.EditNamePlaceholder"];
        EditCategoryIconLabel.Text = i18n["Dialog.Category.Icon"];
        EditCategoryIconDesc.Text = i18n["Dialog.Category.ChangeIconHint"];
        ChangeEditIconBtn.Content = i18n["Dialog.CommandEdit.ChooseIcon"];
        CancelEditCategoryBtn.Content = i18n["Commands.Cancel"];
        SaveEditCategoryBtn.Content = i18n["Dialog.Category.SaveEdit"];

        // Icon Picker Panel
        BackFromPickerBtnText.Text = i18n["IconPicker.Back"];
        IconSearchBox.PlaceholderText = i18n["IconPicker.SearchPlaceholder"];
        BrowseLocalIconBtnText.Text = i18n["IconPicker.LocalImage"];
        ToolTipService.SetToolTip(BrowseLocalIconBtn, i18n["IconPicker.LocalImageToolTip"]);
        CatAllBtn.Content = i18n["IconPicker.All"];
        CatOpsBtn.Content = i18n["IconPicker.Operations"];
        CatHwBtn.Content = i18n["IconPicker.Hardware"];
        CatNetBtn.Content = i18n["IconPicker.Network"];
        CatDevBtn.Content = i18n["IconPicker.DevOps"];

        Loaded += async (_, _) =>
        {
            UpdateNewCategoryIconVisual();
            await LoadCategoriesAsync();
        };
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            var catItems = await _categoryService.GetAllCategoryItemsAsync(forceReload: true);
            var commands = await _commandService.GetAllAsync(forceReload: true);

            var counts = commands
                .GroupBy(c => c.Group, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

            _categories.Clear();
            foreach (var item in catItems)
            {
                counts.TryGetValue(item.Name, out var count);
                _categories.Add(new CategoryDisplayItem
                {
                    Id = item.Id,
                    Name = item.Name,
                    IconGlyph = item.IconGlyph,
                    CommandCount = count
                });
            }

            if (StatusHintText != null)
            {
                StatusHintText.Text = I18nService.Instance.Format("Dialog.Category.StatusHint", _categories.Count);
            }
        }
        catch (Exception ex)
        {
            if (StatusHintText != null)
            {
                StatusHintText.Text = I18nService.Instance.Format("Dialog.Category.StatusLoadFailed", ex.Message);
            }
        }
    }

    #region Add Category
    private void NewCategoryIconBtn_Click(object sender, RoutedEventArgs e)
    {
        _activePickerTarget = PickerTarget.NewCategory;
        OpenIconPicker(I18nService.Instance["Dialog.Category.NewCategoryIconToolTip"]);
    }

    private void UpdateNewCategoryIconVisual()
    {
        if (NewCategoryFontIcon == null || NewCategoryImgIcon == null) return;
        if (File.Exists(_newCategoryIcon))
        {
            NewCategoryFontIcon.Visibility = Visibility.Collapsed;
            NewCategoryImgIcon.Visibility = Visibility.Visible;
            try
            {
                NewCategoryImgIcon.Source = new BitmapImage(new Uri(_newCategoryIcon));
            }
            catch { }
        }
        else
        {
            NewCategoryImgIcon.Visibility = Visibility.Collapsed;
            NewCategoryFontIcon.Visibility = Visibility.Visible;
            NewCategoryFontIcon.Glyph = CommandItem.NormalizeToSegoeGlyph(_newCategoryIcon);
        }
    }

    private async void AddCategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        await AddCategoryInternalAsync();
    }

    private async void NewCategoryBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            await AddCategoryInternalAsync();
        }
    }

    private async Task AddCategoryInternalAsync()
    {
        var name = NewCategoryBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        if (_categories.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            if (StatusHintText != null)
            {
                StatusHintText.Text = I18nService.Instance.Format("Dialog.Category.StatusExists", name);
            }
            return;
        }

        await _categoryService.AddCategoryAsync(name, _newCategoryIcon);
        NewCategoryBox.Text = string.Empty;
        _newCategoryIcon = "\uE8EC";
        UpdateNewCategoryIconVisual();
        await LoadCategoriesAsync();
        CategoriesChanged?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region Edit Category
    private void EditCategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CategoryDisplayItem item)
        {
            _currentEditingItem = item;
            _editingCategoryIcon = item.IconGlyph;

            EditCategoryNameBox.Text = item.Name;
            UpdateEditCategoryIconVisual();

            MainCategoriesPanel.Visibility = Visibility.Collapsed;
            IconPickerPanel.Visibility = Visibility.Collapsed;
            EditCategoryPanel.Visibility = Visibility.Visible;

            Title = I18nService.Instance.Format("Dialog.Category.EditCategoryTitle", item.DisplayName);
            EditPanelTitleText.Text = Title?.ToString() ?? I18nService.Instance["Dialog.Category.EditTitle"];
            IsPrimaryButtonEnabled = false;
        }
    }

    private void ChangeEditIconBtn_Click(object sender, RoutedEventArgs e)
    {
        _activePickerTarget = PickerTarget.EditCategory;
        OpenIconPicker(I18nService.Instance["IconPicker.SelectCategoryIcon"]);
    }

    private void UpdateEditCategoryIconVisual()
    {
        if (EditCategoryFontIcon == null || EditCategoryImgIcon == null) return;
        if (File.Exists(_editingCategoryIcon))
        {
            EditCategoryFontIcon.Visibility = Visibility.Collapsed;
            EditCategoryImgIcon.Visibility = Visibility.Visible;
            try
            {
                EditCategoryImgIcon.Source = new BitmapImage(new Uri(_editingCategoryIcon));
                if (EditCategoryIconNameText != null)
                {
                    EditCategoryIconNameText.Text = Path.GetFileName(_editingCategoryIcon);
                }
            }
            catch { }
        }
        else
        {
            EditCategoryImgIcon.Visibility = Visibility.Collapsed;
            EditCategoryFontIcon.Visibility = Visibility.Visible;
            var glyph = CommandItem.NormalizeToSegoeGlyph(_editingCategoryIcon);
            EditCategoryFontIcon.Glyph = glyph;

            var found = IconItem.FindByGlyph(glyph);
            if (EditCategoryIconNameText != null)
            {
                EditCategoryIconNameText.Text = found != null ? found.LocalizedName : I18nService.Instance["IconPicker.OfficialIcon"];
            }
        }
    }

    private async void SaveEditCategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_currentEditingItem == null) return;

        var newName = EditCategoryNameBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            return;
        }

        var oldName = _currentEditingItem.Name;
        await _categoryService.UpdateCategoryAsync(oldName, newName, _editingCategoryIcon);

        EditCategoryPanel.Visibility = Visibility.Collapsed;
        MainCategoriesPanel.Visibility = Visibility.Visible;
        Title = I18nService.Instance["Dialog.Category.Title"];
        IsPrimaryButtonEnabled = true;

        await LoadCategoriesAsync();
        CategoriesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BackFromEditBtn_Click(object sender, RoutedEventArgs e)
    {
        EditCategoryPanel.Visibility = Visibility.Collapsed;
        IconPickerPanel.Visibility = Visibility.Collapsed;
        MainCategoriesPanel.Visibility = Visibility.Visible;
        Title = I18nService.Instance["Dialog.Category.Title"];
        IsPrimaryButtonEnabled = true;
    }
    #endregion

    #region Direct Icon Change from Item
    private void ChangeCategoryIconBtn_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CategoryDisplayItem item)
        {
            _currentEditingItem = item;
            _activePickerTarget = PickerTarget.DirectItem;
            OpenIconPicker(I18nService.Instance.Format("Dialog.Category.ChangeIconFor", item.DisplayName));
        }
    }
    #endregion

    #region Icon Picker Panel
    private void OpenIconPicker(string pickerTitle)
    {
        MainCategoriesPanel.Visibility = Visibility.Collapsed;
        EditCategoryPanel.Visibility = Visibility.Collapsed;
        IconPickerPanel.Visibility = Visibility.Visible;

        Title = pickerTitle;
        IsPrimaryButtonEnabled = false;

        _selectedIconCategory = "全部";
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
        if (_activePickerTarget == PickerTarget.EditCategory)
        {
            EditCategoryPanel.Visibility = Visibility.Visible;
            Title = _currentEditingItem != null 
                ? I18nService.Instance.Format("Dialog.Category.EditCategoryTitle", _currentEditingItem.DisplayName) 
                : I18nService.Instance["Dialog.Category.EditTitle"];
            IsPrimaryButtonEnabled = false;
        }
        else
        {
            MainCategoriesPanel.Visibility = Visibility.Visible;
            Title = I18nService.Instance["Dialog.Category.Title"];
            IsPrimaryButtonEnabled = true;
        }
    }

    private void IconSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        RefreshIconsList();
    }

    private void CategoryBtn_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string cat)
        {
            _selectedIconCategory = cat;
            UpdateCategoryButtonsVisual();
            RefreshIconsList();
        }
    }

    private void UpdateCategoryButtonsVisual()
    {
        var accentStyle = Application.Current.Resources["AccentButtonStyle"] as Style;
        var defaultStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;

        SetButtonStyle(CatAllBtn, _selectedIconCategory == "全部", accentStyle, defaultStyle);
        SetButtonStyle(CatOpsBtn, _selectedIconCategory == "常用与操作", accentStyle, defaultStyle);
        SetButtonStyle(CatHwBtn, _selectedIconCategory == "系统与硬件", accentStyle, defaultStyle);
        SetButtonStyle(CatNetBtn, _selectedIconCategory == "网络通信", accentStyle, defaultStyle);
        SetButtonStyle(CatDevBtn, _selectedIconCategory == "开发与运维", accentStyle, defaultStyle);
    }

    private static void SetButtonStyle(Button? btn, bool isSelected, Style? accent, Style? def)
    {
        if (btn == null) return;
        if (isSelected && accent != null) btn.Style = accent;
        else if (!isSelected && def != null) btn.Style = def;
    }

    private void RefreshIconsList()
    {
        var query = IconSearchBox?.Text?.Trim();
        var results = IconItem.Filter(query, _selectedIconCategory);

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
            IconSearchHint.Text = results.Count == 0
                ? I18nService.Instance["IconPicker.HintEmpty"]
                : I18nService.Instance["IconPicker.HintSelect"];
        }
    }

    private async void IconsGridView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is IconItem selected)
        {
            await ApplySelectedIconAsync(selected.Glyph);
        }
    }

    private async void BrowseLocalIconBtn_Click(object sender, RoutedEventArgs e)
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
                var destFileName = $"cat_{Guid.NewGuid():N}{ext}";
                var destPath = Path.Combine(iconsDir, destFileName);
                File.Copy(file.Path, destPath, overwrite: true);

                await ApplySelectedIconAsync(destPath);
            }
        }
        catch (Exception ex)
        {
            if (StatusHintText != null)
            {
                StatusHintText.Text = I18nService.Instance.Format("Dialog.Category.StatusImportFailed", ex.Message);
            }
        }
    }

    private async Task ApplySelectedIconAsync(string iconGlyphOrPath)
    {
        switch (_activePickerTarget)
        {
            case PickerTarget.NewCategory:
                _newCategoryIcon = iconGlyphOrPath;
                UpdateNewCategoryIconVisual();
                CloseIconPicker();
                break;

            case PickerTarget.EditCategory:
                _editingCategoryIcon = iconGlyphOrPath;
                UpdateEditCategoryIconVisual();
                CloseIconPicker();
                break;

            case PickerTarget.DirectItem:
                if (_currentEditingItem != null)
                {
                    _currentEditingItem.IconGlyph = iconGlyphOrPath;
                    await _categoryService.UpdateCategoryIconAsync(_currentEditingItem.Name, iconGlyphOrPath);
                    CloseIconPicker();
                    CategoriesChanged?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    CloseIconPicker();
                }
                break;
        }
    }
    #endregion

    #region Delete & Reorder
    private async void DeleteBtn_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CategoryDisplayItem item)
        {
            if (_categories.Count <= 1)
            {
                if (StatusHintText != null)
                {
                    StatusHintText.Text = I18nService.Instance["Dialog.Category.StatusDeleteMin"];
                }
                return;
            }

            await _categoryService.DeleteCategoryAsync(item.Name, "常用");
            await LoadCategoriesAsync();
            CategoriesChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private async void MoveUpBtn_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CategoryDisplayItem item)
        {
            int index = _categories.IndexOf(item);
            if (index > 0)
            {
                _categories.Move(index, index - 1);
                var ordered = _categories.Select(c => c.Name).ToList();
                await _categoryService.ReorderCategoriesAsync(ordered);
                CategoriesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private async void MoveDownBtn_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is CategoryDisplayItem item)
        {
            int index = _categories.IndexOf(item);
            if (index >= 0 && index < _categories.Count - 1)
            {
                _categories.Move(index, index + 1);
                var ordered = _categories.Select(c => c.Name).ToList();
                await _categoryService.ReorderCategoriesAsync(ordered);
                CategoriesChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
    #endregion
}

public partial class CategoryDisplayItem : ObservableObject
{
    public CategoryDisplayItem()
    {
        I18nService.Instance.LanguageChanged += RefreshLocalizedProperties;
    }

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(ChangeIconToolTip));
        OnPropertyChanged(nameof(MoveUpToolTip));
        OnPropertyChanged(nameof(MoveDownToolTip));
        OnPropertyChanged(nameof(EditToolTip));
        OnPropertyChanged(nameof(DeleteToolTip));
    }

    private string _name = string.Empty;
    private string _iconGlyph = "\uE8EC";
    private int _commandCount;

    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public string Name
    {
        get => _name;
        set
        {
            if (SetProperty(ref _name, value))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
        }
    }

    public string DisplayName => I18nService.Instance.TranslateCategory(Name);

    public string IconGlyph
    {
        get => _iconGlyph;
        set
        {
            if (SetProperty(ref _iconGlyph, value))
            {
                OnPropertyChanged(nameof(DisplayIcon));
                OnPropertyChanged(nameof(IsImageIcon));
                OnPropertyChanged(nameof(GlyphVisibility));
                OnPropertyChanged(nameof(ImageVisibility));
            }
        }
    }

    public int CommandCount
    {
        get => _commandCount;
        set
        {
            if (SetProperty(ref _commandCount, value))
            {
                OnPropertyChanged(nameof(CountText));
            }
        }
    }

    public string CountText => I18nService.Instance.EffectiveLanguage == "en-US" 
        ? $"{CommandCount} {(CommandCount == 1 ? "command" : "commands")}" 
        : $"{CommandCount} 个命令";

    public string ChangeIconToolTip => I18nService.Instance["Dialog.Category.ChangeIconToolTip"];
    public string MoveUpToolTip => I18nService.Instance["Dialog.Category.MoveUp"];
    public string MoveDownToolTip => I18nService.Instance["Dialog.Category.MoveDown"];
    public string EditToolTip => I18nService.Instance["Dialog.Category.Edit"];
    public string DeleteToolTip => I18nService.Instance["Dialog.Category.Delete"];

    public string DisplayIcon => File.Exists(IconGlyph) ? string.Empty : CommandItem.NormalizeToSegoeGlyph(IconGlyph);
    public bool IsImageIcon => File.Exists(IconGlyph);
    public Visibility GlyphVisibility => !IsImageIcon ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ImageVisibility => IsImageIcon ? Visibility.Visible : Visibility.Collapsed;
}
