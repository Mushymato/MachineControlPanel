using MachineControlPanel.Data;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using PropertyChanged.SourceGenerator;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.GUI;

public sealed partial record MachineSelectCell(string QId, MachineData Data, Item Machine)
{
    public readonly ParsedItemData MachineData = ItemRegistry.GetData(QId);
    public readonly SDUITooltipData Tooltip = new(Machine.getDescription(), Machine.DisplayName, Machine);

    [Notify]
    private Color backgroundTint = Color.White * 0.5f;

    public void ShowControlPanel(bool isGlobal) => MenuHandler.ShowControlPanel(Machine, isGlobal, asChildMenu: true);
}

/// <summary>Context for machine select</summary>
public sealed partial class MachineSelectContext
{
    /// <summary>All machine data, loaded everytime menu is opened</summary>
    private readonly Dictionary<string, MachineData> allMachines = DataLoader.Machines(Game1.content);

    [Notify]
    private string searchText = "";

    [Notify]
    public bool isGlobal = true;

    public void ToggleGlobalLocal()
    {
        IsGlobal = !IsGlobal;
    }

    public IEnumerable<MachineSelectCell> MachineCells
    {
        get
        {
            // List<MachineSelectCell> machineCells = [];
            int hidden = 0;
            string searchText = SearchText;
            foreach ((string key, MachineData value) in allMachines)
            {
                if (ItemRegistry.Create(key) is not Item machine)
                    continue;
                if (
                    !string.IsNullOrEmpty(searchText)
                    && !machine.DisplayName.ContainsIgnoreCase(searchText)
                    && !machine.QualifiedItemId.ContainsIgnoreCase(searchText)
                )
                    continue;
                if (ModEntry.Config.ProgressionMode && !PlayerProgressionCache.HasItem(key))
                {
                    hidden++;
                    continue;
                }
                // machineCells.Add(new MachineSelectCell(key, value, machine));
                yield return new MachineSelectCell(key, value, machine);
            }
            HiddenByProgressionCount = hidden;
            // ModEntry.Log(HiddenByProgressionCountLabel);
            // return machineCells;
        }
    }

    [Notify]
    public int hiddenByProgressionCount = 0;

    public string HiddenByProgressionCountLabel => $"+{HiddenByProgressionCount}";

    public bool ShowHiddenCount => ModEntry.Config.ProgressionMode && HiddenByProgressionCount > 0;
}
