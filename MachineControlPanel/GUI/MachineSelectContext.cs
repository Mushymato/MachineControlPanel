using System.Collections.ObjectModel;
using System.Diagnostics;
using MachineControlPanel.Data;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using PropertyChanged.SourceGenerator;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.GUI;

public sealed partial record MachineSelectCell(string QId, MachineData Data, Item Machine)
{
    public override int GetHashCode() => QId.GetHashCode();

    public readonly ParsedItemData MachineData = ItemRegistry.GetData(QId);
    public readonly SDUITooltipData Tooltip = new(Machine.getDescription(), Machine.DisplayName, Machine);

    [Notify]
    private Color backgroundTint = Color.White * 0.5f;

    public void UpdateBackgroundTint()
    {
        if (
            ModEntry.SaveData.TryGetModSaveDataEntry(ModSaveDataKey.Global(Machine), out _)
            || ModEntry.SaveData.TryGetModSaveDataEntry(
                ModSaveDataKey.PerLocation(Machine, Game1.currentLocation),
                out _
            )
        )
        {
            BackgroundTint = Color.White;
        }
        else
        {
            BackgroundTint = Color.White * 0.5f;
        }
    }

    public void ShowControlPanel() => MenuHandler.ShowControlPanel(Machine, realMachine: false, asChildMenu: true);
}

public sealed partial record SearchByItemCell(string QId, ParsedItemData Datum, Item ReprItem)
{
    [Notify]
    private Color backgroundTint = Color.White * 0.5f;

    public override int GetHashCode() => QId.GetHashCode();
}

/// <summary>Context for machine select</summary>
public sealed partial class MachineSelectContext
{
    private readonly Stopwatch stopwatch = new();
    private Queue<string>? prefetchQueue;

    public MachineSelectContext()
    {
        UpdateMachineCellsFiltered();
        Prefetch_Init();
    }

    internal void Prefetch_Init()
    {
        if (prefetchQueue != null)
            return;
        prefetchQueue = [];
        foreach (string key in MachineRuleCache.Machines.Keys)
        {
            prefetchQueue.Enqueue(key);
        }
    }

    public void Update(TimeSpan elapsed)
    {
        if (prefetchQueue == null || prefetchQueue.Count == 0)
            return;
        if (prefetchQueue.TryDequeue(out string? key))
        {
            stopwatch.Restart();
            MachineRuleCache.TryGetRuleDefList(key);
            stopwatch.Stop();
            ModEntry.Log(
                $"{stopwatch.Elapsed} to prefetch '{key}' rule defs",
                stopwatch.ElapsedMilliseconds > 1000f / 60 ? LogLevel.Warn : LogLevel.Debug
            );
        }
    }

    #region machine cells
    public string SearchText
    {
        get => field;
        set
        {
            if (field != value)
            {
                field = value;
                OnPropertyChanged(new(nameof(SearchText)));
                UpdateMachineCellsFiltered();
            }
        }
    } = string.Empty;

    public static Dictionary<string, MachineSelectCell> GetMachineCells()
    {
        Dictionary<string, MachineSelectCell> machineCells = [];
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach ((string key, MachineData value) in MachineRuleCache.Machines)
        {
            if (MachineRuleCache.NoRules(key))
                continue;
            if (ItemQueryCache.GetItem(key) is not Item machine)
                continue;
            MachineSelectCell cell = new(key, value, machine);
            cell.UpdateBackgroundTint();
            machineCells[cell.QId] = cell;
        }
        ModEntry.Log($"Build MachineSelectCells in {stopwatch.Elapsed}");
        return machineCells;
    }

    private readonly IReadOnlyDictionary<string, MachineSelectCell> machineCells = GetMachineCells();
    public readonly ObservableCollection<MachineSelectCell> MachineCellsFiltered = [];

    private void UpdateMachineCellsFiltered()
    {
        MachineCellsFiltered.Clear();
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
            if (
                selectedSearchByItem != null
                && MachineRuleCache.TryGetRuleDefList(cell.QId) is IReadOnlyList<RuleDef> ruleDefs
                && !RuleDefCanUseThisItem(ruleDefs, selectedSearchByItem.QId)
            )
            {
                continue;
            }
            if (ModEntry.Config.ProgressionMode && !PlayerProgressionCache.HasItem(cell.QId))
            {
                hidden++;
                continue;
            }
            MachineCellsFiltered.Add(cell);
        }
        HiddenByProgressionCount = hidden;
    }

    internal void UpdateBackgroundTint(object? sender, string e)
    {
        if (machineCells.TryGetValue(e, out MachineSelectCell? cell))
        {
            cell.UpdateBackgroundTint();
        }
    }
    #endregion

    #region progression count
    [Notify]
    public int hiddenByProgressionCount = 0;

    public string HiddenByProgressionCountLabel => $"+{HiddenByProgressionCount}";

    public bool ShowHiddenCount => ModEntry.Config.ProgressionMode && HiddenByProgressionCount > 0;
    #endregion

    #region show overlay
    public void ShowOverlay()
    {
        MenuHandler.ShowOverlayInfo();
    }
    #endregion

    #region search by item
    [Notify]
    private string searchByItemText = "";
    private SearchByItemCell? selectedSearchByItem = null;

    public readonly ObservableCollection<SearchByItemCell> SearchByItemCells = [];
    public bool HasSearchByItemText
    {
        get
        {
            if (selectedSearchByItem != null)
            {
                selectedSearchByItem = null;
                UpdateMachineCellsFiltered();
            }
            if (string.IsNullOrEmpty(SearchByItemText))
                return false;
            List<SearchByItemCell> foundCells = GetSearchByItemOptions(searchByItemText, 8);
            SearchByItemCells.Clear();
            foreach (SearchByItemCell cell in foundCells)
                SearchByItemCells.Add(cell);
            return true;
        }
    }

    public static List<SearchByItemCell> GetSearchByItemOptions(string searchText, int limit)
    {
        List<SearchByItemCell> foundCells = [];
        foreach (Item item in ItemQueryCache.AllItems)
        {
            if (
                ItemRegistry.GetData(item.QualifiedItemId) is ParsedItemData parsed
                && item.DisplayName.ContainsIgnoreCase(searchText)
            )
            {
                foundCells.Add(new(item.QualifiedItemId, parsed, item));
                if (foundCells.Count >= limit)
                {
                    return foundCells;
                }
            }
        }
        return foundCells;
    }

    public void DoSearchByItem(SearchByItemCell itemCell)
    {
        if (selectedSearchByItem == itemCell)
        {
            selectedSearchByItem?.BackgroundTint = Color.White * 0.5f;
            selectedSearchByItem = null;
            UpdateMachineCellsFiltered();
            return;
        }
        selectedSearchByItem?.BackgroundTint = Color.White * 0.5f;
        selectedSearchByItem = itemCell;
        selectedSearchByItem?.BackgroundTint = Color.White;
        UpdateMachineCellsFiltered();
    }

    private static bool RuleDefCanUseThisItem(IReadOnlyList<RuleDef> ruleDefs, string qId)
    {
        foreach (RuleDef def in ruleDefs)
        {
            if (def.Input.Items == null)
                continue;
            foreach (Item item in def.Input.Items)
            {
                if (item.QualifiedItemId == qId)
                    return true;
            }
            if (FuelCanUseThisItem(qId, def.SharedFuel))
                return true;
            foreach (IconOutputDef iconOutputDef in def.Outputs)
            {
                if (FuelCanUseThisItem(qId, iconOutputDef.EMCFuel))
                    return true;
            }
        }
        return false;

        static bool FuelCanUseThisItem(string qId, IReadOnlyList<IconDef>? iconDefs)
        {
            if (iconDefs == null)
                return false;
            foreach (IconDef iconDef in iconDefs)
            {
                if (iconDef.Items == null)
                {
                    continue;
                }
                foreach (Item item in iconDef.Items)
                {
                    if (item.QualifiedItemId == qId)
                        return true;
                }
            }
            return false;
        }
    }
    #endregion
}
