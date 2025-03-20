using System.Diagnostics;
using MachineControlPanel.Data;
using MachineControlPanel.GUI.Includes;
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
            || ModEntry.SaveData.TryGetModSaveDataEntry(QId, MenuHandler.GlobalToggle.LocationKey, out _)
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
    /// <summary>All machine data, loaded everytime menu is opened</summary>
    public GlobalToggleContext GlobalToggle => MenuHandler.GlobalToggle;

    [Notify]
    private string searchText = "";

    public static IEnumerable<MachineSelectCell> GetMachineCells()
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach ((string key, MachineData value) in MachineRuleCache.Machines)
        {
            if (ItemRegistry.Create(key) is not Item machine)
                continue;
            if ((MachineRuleCache.CreateRuleDefList(key)?.Count ?? 0) == 0)
                continue;
            yield return new(key, value, machine);
        }
        ModEntry.Log($"Build MachineCells in in {stopwatch.Elapsed}");
    }

    private readonly IEnumerable<MachineSelectCell> machineCells = GetMachineCells();

    public IEnumerable<MachineSelectCell> MachineCellsFiltered
    {
        get
        {
            int hidden = 0;
            string searchText = SearchText;
            foreach (MachineSelectCell cell in machineCells)
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
                cell.UpdateBackgroundTint();
                yield return cell;
            }
            HiddenByProgressionCount = hidden;
        }
    }

    public void UpdateBackgroundTintOnAllMachineCells(object? sender, EventArgs? e)
    {
        // weird INPC nonsense means just calling UpdateBackgroundTint does nothing, this works though
        OnPropertyChanged(new(nameof(MachineCellsFiltered)));
    }

    [Notify]
    public int hiddenByProgressionCount = 0;

    public string HiddenByProgressionCountLabel => $"+{HiddenByProgressionCount}";

    public bool ShowHiddenCount => ModEntry.Config.ProgressionMode && HiddenByProgressionCount > 0;
}
