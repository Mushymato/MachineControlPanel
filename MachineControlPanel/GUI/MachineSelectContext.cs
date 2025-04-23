using System.Diagnostics;
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

    public void UpdateBackgroundTint()
    {
        if (
            ModEntry.SaveData.TryGetModSaveDataEntry(QId, null, out _)
            || ModEntry.SaveData.TryGetModSaveDataEntry(QId, Game1.currentLocation.NameOrUniqueName, out _)
        )
        {
            BackgroundTint = Color.White;
        }
        else
        {
            BackgroundTint = Color.White * 0.5f;
        }
    }

    public void ShowControlPanel() => MenuHandler.ShowControlPanel(Machine, asChildMenu: true);
}

/// <summary>Context for machine select</summary>
public sealed partial class MachineSelectContext
{
    [Notify]
    private string searchText = "";

    public static IEnumerable<MachineSelectCell> GetMachineCells()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach ((string key, MachineData value) in MachineRuleCache.Machines)
        {
            if (MachineRuleCache.NoRules(key))
                continue;
            if (ItemRegistry.Create(key) is not Item machine)
                continue;
            MachineSelectCell cell = new(key, value, machine);
            cell.UpdateBackgroundTint();
            yield return cell;
        }
        ModEntry.Log($"Build MachineSelectCells in {stopwatch.Elapsed}");
    }

    private readonly Dictionary<string, MachineSelectCell> machineCells = GetMachineCells()
        .ToDictionary(cell => cell.QId, cell => cell);

    public IEnumerable<MachineSelectCell> MachineCellsFiltered
    {
        get
        {
            int hidden = 0;
            string searchText = SearchText;
            foreach (MachineSelectCell cell in machineCells.Values)
            {
                if (
                    !string.IsNullOrEmpty(searchText)
                    && !cell.Machine.DisplayName.ContainsIgnoreCase(searchText)
                    && !cell.Machine.QualifiedItemId.ContainsIgnoreCase(searchText)
                )
                    continue;
                if (ModEntry.Config.ProgressionMode && !PlayerProgressionCache.HasItem(cell.QId))
                {
                    hidden++;
                    continue;
                }
                yield return cell;
            }
            HiddenByProgressionCount = hidden;
        }
    }

    public void SetHover(MachineSelectCell? cell = null) => MenuHandler.HoveredItem = cell?.Machine;

    internal void UpdateBackgroundTint(object? sender, string e)
    {
        if (machineCells.TryGetValue(e, out MachineSelectCell? cell))
        {
            cell.UpdateBackgroundTint();
        }
    }

    internal void Closing()
    {
        MenuHandler.HoveredItem = null;
    }

    [Notify]
    public int hiddenByProgressionCount = 0;

    public string HiddenByProgressionCountLabel => $"+{HiddenByProgressionCount}";

    public bool ShowHiddenCount => ModEntry.Config.ProgressionMode && HiddenByProgressionCount > 0;
}
