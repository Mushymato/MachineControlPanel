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

    // | realMachine | DefaultIsGlobal | PerMachineControlPanel |     Locality (toggle order)     |
    // |-------------|-----------------|------------------------|---------------------------------|
    // |    true     |      true       |          true          | PerMachine, Global, PerLocation |
    // |    true     |      true       |          false         | Global, PerLocation, PerMachine |
    // |    true     |      false      |          true          | PerMachine, PerLocation, Global |
    // |    true     |      false      |          false         | PerLocation, Global, PerMachine |
    // |    false    |      true       |           -            |       Global, PerLocation       |
    // |    false    |      false      |           -            |       PerLocation, Global       |

    internal void ControlPanelOpened(bool realMachine = false)
    {
        this.realMachine = realMachine;
        if (realMachine && ModEntry.Config.PerMachineControlPanel)
            Locality = PanelLocality.PerMachine;
        else if (ModEntry.Config.DefaultIsGlobal)
            Locality = PanelLocality.Global;
        else
            Locality = PanelLocality.PerLocation;
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

    internal MsdKey DataKey(Item machine) => makeDataKeyFor(Locality, machine);

    internal MsdKey PreviousDataKey(Item machine) => makeDataKeyFor(previousLocality, machine);

    private static MsdKey makeDataKeyFor(PanelLocality locality, Item machine) =>
        locality switch
        {
            PanelLocality.Global => MsdKey.Global(machine),
            PanelLocality.PerLocation => MsdKey.PerLocation(machine, Game1.currentLocation),
            PanelLocality.PerMachine => (machine is SObject obj) ? MsdKey.PerMachine(obj) : default,
            _ => default,
        };
}
