using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace CmdDock.Core.Models;

public class ExecutionLogEntry : ObservableObject
{
    public ExecutionLogEntry()
    {
        CmdDock.Core.Services.I18nService.Instance.LanguageChanged += () =>
        {
            OnPropertyChanged(nameof(DisplaySuccess));
            OnPropertyChanged(nameof(DisplayDuration));
        };
    }

    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string CommandId { get; set; } = string.Empty;
    public string CommandName { get; set; } = string.Empty;
    public string ShellType { get; set; } = string.Empty;
    public string CommandText { get; set; } = string.Empty;
    public string ExecutionMode { get; set; } = string.Empty;
    public bool Success { get; set; }
    public int ExitCode { get; set; }
    public string Output { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public string FormattedTimestamp => Timestamp.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    [JsonIgnore]
    public string DisplaySuccess => Success 
        ? CmdDock.Core.Services.I18nService.Instance["Logs.Success"] 
        : CmdDock.Core.Services.I18nService.Instance["Logs.Failed"];

    [JsonIgnore]
    public string DisplayDuration => $"{DurationMs} ms";
}
