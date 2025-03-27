using PropertyChanged.SourceGenerator;
using StardewValley;

namespace MachineControlPanel.GUI.Includes;

/// <summary>Context for the global toggle button</summary>
public sealed partial class GlobalToggleContext()
{
    [Notify]
    public bool isGlobal = ModEntry.Config.DefaultIsGlobal;

    public void ToggleGlobalLocal()
    {
        IsGlobal = !IsGlobal;
    }

    internal string? LocationKey => IsGlobal ? null : Game1.currentLocation.NameOrUniqueName;
    internal string? NotLocationKey => !IsGlobal ? null : Game1.currentLocation.NameOrUniqueName;
}
