using System.Text.Json.Serialization;

namespace CmdDock.Core.Models;

public enum MiniDockMode
{
    CardDeck,
    DockBar
}

public enum DesktopPinMode
{
    AlwaysOnTop,
    PinToDesktop,
    Normal
}

public enum DockOrientation
{
    Auto,
    Horizontal,
    Vertical
}

public enum CardScale
{
    Small,
    Medium,
    Large
}

public class MiniDockSettings
{
    [JsonPropertyName("dockMode")]
    public MiniDockMode DockMode { get; set; } = MiniDockMode.CardDeck;

    [JsonPropertyName("pinMode")]
    public DesktopPinMode PinMode { get; set; } = DesktopPinMode.AlwaysOnTop;

    [JsonPropertyName("orientation")]
    public DockOrientation Orientation { get; set; } = DockOrientation.Auto;

    [JsonPropertyName("opacity")]
    public double Opacity { get; set; } = 0.95;

    [JsonPropertyName("cardScale")]
    public CardScale CardScale { get; set; } = CardScale.Medium;

    [JsonPropertyName("enableAutoHide")]
    public bool EnableAutoHide { get; set; } = false;

    [JsonPropertyName("startupView")]
    public string StartupView { get; set; } = "full"; // "full" or "mini"

    [JsonPropertyName("windowX")]
    public int? WindowX { get; set; }

    [JsonPropertyName("windowY")]
    public int? WindowY { get; set; }

    [JsonPropertyName("miniWidth")]
    public int MiniWidth { get; set; } = 360;

    [JsonPropertyName("miniHeight")]
    public int MiniHeight { get; set; } = 500;

    [JsonPropertyName("fullWidth")]
    public int FullWidth { get; set; } = 1000;

    [JsonPropertyName("fullHeight")]
    public int FullHeight { get; set; } = 680;
}
