using CmdDock.Core.Models;

namespace CmdDock.Core.Services;

public interface IPresetService
{
    IReadOnlyList<CommandItem> GetBuiltinPresets();
    IReadOnlyList<string> GetDefaultGroups();
}
