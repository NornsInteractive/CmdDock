using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CmdDock.Core.Models;

public class CategoryItem : ObservableObject
{
    public CategoryItem()
    {
        CmdDock.Core.Services.I18nService.Instance.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(DisplayName));
        };
    }

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayName => CmdDock.Core.Services.I18nService.Instance.TranslateCategory(Name);

    public string IconGlyph { get; set; } = "\uE8EC"; // Default Segoe glyph or local image path
    public int Order { get; set; }

    [JsonIgnore]
    public string DisplayIcon => File.Exists(IconGlyph) ? string.Empty : CommandItem.NormalizeToSegoeGlyph(IconGlyph);

    [JsonIgnore]
    public bool IsImageIcon => !string.IsNullOrWhiteSpace(IconGlyph) &&
        (IconGlyph.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
         File.Exists(IconGlyph));

    [JsonIgnore]
    public string GlyphVisibility => !IsImageIcon ? "Visible" : "Collapsed";

    [JsonIgnore]
    public string ImageVisibility => IsImageIcon ? "Visible" : "Collapsed";

    public CategoryItem Clone()
    {
        return new CategoryItem
        {
            Id = Id,
            Name = Name,
            IconGlyph = IconGlyph,
            Order = Order
        };
    }
}
