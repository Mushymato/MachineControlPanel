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
    }

#if DEBUG
    public readonly string CurrentLocationName = Game1.currentLocation.NameOrUniqueName;
#else
    public readonly string CurrentLocationName = Game1.currentLocation.DisplayName;
#endif
}
