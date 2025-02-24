using MachineControlPanel.GUI.Includes;
using PropertyChanged.SourceGenerator;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.GUI;

public sealed partial class ControlPanelContext(Item machine, GlobalToggleContext? globalToggleContext = null)
{
    public readonly Item Machine = machine;
    public readonly string MachineName = machine.DisplayName;
    public readonly ParsedItemData MachineData = ItemRegistry.GetData(machine.QualifiedItemId);

    public readonly GlobalToggleContext GlobalToggle = globalToggleContext ?? new GlobalToggleContext();

    [Notify]
    public int pageIndex = (int)ModEntry.Config.DefaultPage;

    /// <summary>Event binding, change current page</summary>
    /// <param name="page"></param>
    public void ChangePage(int page) => PageIndex = page;

    public ValueTuple<int, int, int, int> TabMarginRules => PageIndex == 1 ? new(0, 0, 0, 0) : new(0, 0, 0, 8);
    public ValueTuple<int, int, int, int> TabMarginInputs => PageIndex == 2 ? new(0, 0, 0, 0) : new(0, 0, 0, 8);
}
