using PropertyChanged.SourceGenerator;
using StardewValley;

namespace MachineControlPanel.GUI.Includes;

/// <summary>Context for the global toggle button</summary>
public sealed partial class GlobalToggleContext()
{
    [Notify]
    public bool isGlobal = true;

    public void ToggleGlobalLocal()
    {
        IsGlobal = !IsGlobal;
        if (!IsGlobal)
            ModEntry.Log(Game1.currentLocation.NameOrUniqueName);
    }

    public readonly string LocationDisplayName = Game1.currentLocation.DisplayName;
    internal string? LocationKey => isGlobal ? null : Game1.currentLocation.NameOrUniqueName;
}
