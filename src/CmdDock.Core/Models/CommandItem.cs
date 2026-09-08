using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CmdDock.Core.Models;

public partial class CommandItem : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString("N");

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _group = "常用";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayIcon))]
    [NotifyPropertyChangedFor(nameof(IsImageIcon))]
    [NotifyPropertyChangedFor(nameof(GlyphVisibility))]
    [NotifyPropertyChangedFor(nameof(ImageVisibility))]
    private string _iconGlyph = "\uE756"; // Default Windows Terminal icon

    [ObservableProperty]
    private ShellType _shellType = ShellType.PowerShell;

    [ObservableProperty]
    private string _commandText = string.Empty;

    [ObservableProperty]
    private string _arguments = string.Empty;

    [ObservableProperty]
    private string _workingDirectory = string.Empty;

    [ObservableProperty]
    private ExecutionMode _executionMode = ExecutionMode.Silent;

    [ObservableProperty]
    private bool _requireConfirmation = false;

    [ObservableProperty]
    private int _order = 0;

    [ObservableProperty]
    private bool _showInWidget = true;

    [ObservableProperty]
    private string _widgetSizePreference = "All";

    [ObservableProperty]
    private string _widgetCardStyle = "Transparent"; // "Transparent" (default) or "Custom"

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WidgetIsTransparent))]
    private string _widgetBackgroundColor = string.Empty; // Hex like "#2563EB" or "#10B981"

    [ObservableProperty]
    private bool _widgetIsTransparent = true; // Default is transparent!

    [ObservableProperty]
    private DateTime? _lastRunTime;

    [ObservableProperty]
    private ExecutionStatus _lastRunStatus = ExecutionStatus.Idle;

    [ObservableProperty]
    private long? _lastRunDurationMs;

    [ObservableProperty]
    private int? _lastExitCode;

    public CommandItem()
    {
        CmdDock.Core.Services.I18nService.Instance.LanguageChanged += RefreshLocalizedProperties;
    }

    public void RefreshLocalizedProperties()
    {
        OnPropertyChanged(nameof(DisplayName));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(DisplayGroup));
        OnPropertyChanged(nameof(DisplayShellType));
        OnPropertyChanged(nameof(DisplayExecutionMode));
        OnPropertyChanged(nameof(FormattedLastRun));
        OnPropertyChanged(nameof(WidgetSwitchOn));
        OnPropertyChanged(nameof(WidgetSwitchOff));
        OnPropertyChanged(nameof(RunBtnText));
        OnPropertyChanged(nameof(RunToolTip));
        OnPropertyChanged(nameof(EditToolTip));
        OnPropertyChanged(nameof(DeleteToolTip));
        OnPropertyChanged(nameof(AddPresetBtnText));
        OnPropertyChanged(nameof(MoveUpToolTip));
        OnPropertyChanged(nameof(MoveDownToolTip));
        OnPropertyChanged(nameof(RemoveToolTip));
    }

    [JsonIgnore]
    public string FormattedLastRun => LastRunTime.HasValue 
        ? LastRunTime.Value.ToLocalTime().ToString("MM-dd HH:mm:ss") 
        : (CmdDock.Core.Services.I18nService.Instance.EffectiveLanguage == "en-US" ? "Never executed" : "从未执行");

    [JsonIgnore]
    public string FormattedDuration => LastRunDurationMs.HasValue 
        ? $"{LastRunDurationMs.Value} ms" 
        : "-";

    [JsonIgnore]
    public string DisplayName => CmdDock.Core.Services.I18nService.Instance.GetPresetLocalizedName(Id, Name);

    [JsonIgnore]
    public string DisplayDescription => CmdDock.Core.Services.I18nService.Instance.GetPresetLocalizedDescription(Id, Description, Name);

    [JsonIgnore]
    public string DisplayGroup => CmdDock.Core.Services.I18nService.Instance.TranslateCategory(Group);

    [JsonIgnore]
    public string DisplayShellType => CmdDock.Core.Services.I18nService.Instance.TranslateShellType(ShellType);

    [JsonIgnore]
    public string DisplayExecutionMode => CmdDock.Core.Services.I18nService.Instance.TranslateExecutionMode(ExecutionMode);

    [JsonIgnore]
    public string WidgetSwitchOn => CmdDock.Core.Services.I18nService.Instance["Commands.WidgetSwitchOn"];

    [JsonIgnore]
    public string WidgetSwitchOff => CmdDock.Core.Services.I18nService.Instance["Commands.WidgetSwitchOff"];

    [JsonIgnore]
    public string RunBtnText => CmdDock.Core.Services.I18nService.Instance["Commands.Run"];

    [JsonIgnore]
    public string RunToolTip => CmdDock.Core.Services.I18nService.Instance["Commands.RunToolTip"];

    [JsonIgnore]
    public string EditToolTip => CmdDock.Core.Services.I18nService.Instance["Commands.EditToolTip"];

    [JsonIgnore]
    public string DeleteToolTip => CmdDock.Core.Services.I18nService.Instance["Commands.DeleteToolTip"];

    [JsonIgnore]
    public string AddPresetBtnText => CmdDock.Core.Services.I18nService.Instance["Presets.Install"];

    [JsonIgnore]
    public string MoveUpToolTip => CmdDock.Core.Services.I18nService.Instance["Dialog.Category.MoveUp"];

    [JsonIgnore]
    public string MoveDownToolTip => CmdDock.Core.Services.I18nService.Instance["Dialog.Category.MoveDown"];

    [JsonIgnore]
    public string RemoveToolTip => CmdDock.Core.Services.I18nService.Instance["Dialog.Category.Delete"];

    [JsonIgnore]
    public string DisplayIcon => IsImageIcon ? string.Empty : (string.IsNullOrWhiteSpace(IconGlyph) ? "\uE756" : NormalizeToSegoeGlyph(IconGlyph));

    public static string NormalizeToSegoeGlyph(string? icon)
    {
        if (string.IsNullOrWhiteSpace(icon)) return "\uE756";
        var clean = icon.Trim();

        // 1. If it matches known emojis or multi-char emojis
        var mappedEmoji = clean switch
        {
            "🌐" => "\uE774",
            "🔄" => "\uE777",
            "📋" => "\uE77F",
            "💻" => "\uE756",
            "☁️" or "☁" => "\uE74C",
            "🔒" => "\uE72E",
            "⚙️" or "⚙" => "\uE713",
            "🚀" => "\uE895",
            "🧹" => "\uE74D",
            "🛠️" or "🛠" => "\uE90F",
            "⚡" => "\uE749",
            "📁" => "\uE8B7",
            "💾" => "\uEDA2",
            "🛡️" or "🛡" => "\uE72B",
            "🔍" => "\uE721",
            "📡" => "\uE8B0",
            "📶" => "\uE706",
            "⏱️" or "⏱" => "\uE9D9",
            "📦" => "\uE7F4",
            "🔑" => "\uE71D",
            "🐍" => "\uE943",
            "🐧" => "\uE756",
            "🔌" => "\uE7BA",
            "🔋" => "\uEBB5",
            "🔗" => "\uE71B",
            "📄" => "\uE7C3",
            "🐛" => "\uE968",
            "🔲" => "\uE8A9",
            "📑" => "\uE8C0",
            "⭐" or "🌟" => "\uE735",
            "🗑️" or "🗑" => "\uE74D",
            "🛑" or "⏹️" or "⏹" => "\uE71A",
            "▶️" or "▶" => "\uE768",
            "⏸️" or "⏸" => "\uE769",
            "⌨️" or "⌨" => "\uE765",
            "🖱️" or "🖱" => "\uE962",
            _ => null
        };
        if (mappedEmoji != null) return mappedEmoji;

        // 2. If it is already a single char (e.g. '\uE735'), return as is
        if (clean.Length == 1) return clean;

        // 3. Handle hex code formats like "E735", "\uE735", "U+E735", "0xE735", "&#xE735;"
        var hex = clean;
        if (hex.StartsWith("\\u", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
        else if (hex.StartsWith("U+", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
        else if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex.Substring(2);
        else if (hex.StartsWith("&#x", StringComparison.OrdinalIgnoreCase) && hex.EndsWith(";")) hex = hex.Substring(3, hex.Length - 4);

        if ((hex.Length == 4 || hex.Length == 5) &&
            int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int codePoint))
        {
            try
            {
                return char.ConvertFromUtf32(codePoint);
            }
            catch { }
        }

        return clean;
    }

    public static string MapGlyphToWidgetEmoji(string? icon) => NormalizeToSegoeGlyph(icon);


    [JsonIgnore]
    public bool IsImageIcon => !string.IsNullOrWhiteSpace(IconGlyph) && 
        (IconGlyph.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".ico", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
         IconGlyph.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase) ||
         File.Exists(IconGlyph));

    [JsonIgnore]
    public string GlyphVisibility => IsImageIcon ? "Collapsed" : "Visible";

    [JsonIgnore]
    public string ImageVisibility => IsImageIcon ? "Visible" : "Collapsed";
}
