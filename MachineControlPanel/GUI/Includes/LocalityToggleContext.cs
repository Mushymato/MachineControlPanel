using PropertyChanged.SourceGenerator;
using StardewValley;

namespace MachineControlPanel.GUI.Includes;

/// <summary>Context for the locality toggle button</summary>
public sealed partial class LocalityToggleContext()
{
    [Notify]
    public PanelLocality locality;

    private PanelLocality previousLocality;

    private bool realMachine;

    // | realMachine | DefaultIsGlobal |     Locality (toggle order)     |
    // |-------------|-----------------|---------------------------------|
    // |    true     |      true       | PerMachine, Global, PerLocation |
    // |    true     |      false      | PerMachine, PerLocation, Global |
    // |    false    |      true       |       Global, PerLocation       |
    // |    false    |      false      |       PerLocation, Global       |

    internal void ControlPanelOpened(bool realMachine = false)
    {
        this.realMachine = realMachine;
        if (ModEntry.Config.DefaultIsGlobal)
        {
            Locality = PanelLocality.Global;
        }
        else
        {
            Locality = realMachine ? PanelLocality.PerMachine : PanelLocality.PerLocation;
        }
        previousLocality = Locality;
    }

    public void ToggleLocality()
    {
        previousLocality = Locality;
        if (realMachine)
        {
            if (ModEntry.Config.DefaultIsGlobal)
                Locality = Locality switch
                {
                    PanelLocality.Global => PanelLocality.PerLocation,
                    PanelLocality.PerLocation => PanelLocality.PerMachine,
                    PanelLocality.PerMachine => PanelLocality.Global,
                    _ => Locality,
                };
            else
                Locality = Locality switch
                {
                    PanelLocality.PerMachine => PanelLocality.PerLocation,
                    PanelLocality.PerLocation => PanelLocality.Global,
                    PanelLocality.Global => PanelLocality.PerMachine,
                    _ => Locality,
                };
        }
        else
            Locality = Locality switch
            {
                PanelLocality.Global => PanelLocality.PerLocation,
                PanelLocality.PerLocation => PanelLocality.Global,
                _ => Locality,
            };
    }

    internal ModSaveDataKey DataKey(Item machine) => makeDataKeyFor(Locality, machine);

    internal ModSaveDataKey PreviousDataKey(Item machine) => makeDataKeyFor(previousLocality, machine);

    private static ModSaveDataKey makeDataKeyFor(PanelLocality locality, Item machine) =>
        locality switch
        {
            PanelLocality.Global => ModSaveDataKey.Global(machine),
            PanelLocality.PerLocation => ModSaveDataKey.PerLocation(machine, Game1.currentLocation),
            PanelLocality.PerMachine => (machine is SObject obj) ? ModSaveDataKey.PerMachine(obj) : default,
            _ => default,
        };
}
