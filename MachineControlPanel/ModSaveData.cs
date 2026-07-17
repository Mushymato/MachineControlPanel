using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using MachineControlPanel.Data;
using Newtonsoft.Json;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel;

public sealed record RuleIdent(string OutputId, string TriggerId);

public sealed record ModSaveDataEntry(
    ImmutableHashSet<RuleIdent> Rules,
    ImmutableHashSet<string> Inputs,
    bool[] Quality
)
{
    public bool IsEmpty()
    {
        if (Rules.Any() || Inputs.Any())
            return false;
        for (int i = 0; i < Quality.Length; i++)
            if (Quality[i])
                return false;
        return true;
    }
}

public enum PanelLocality
{
    Global,
    PerLocation,
    PerMachine,
}

public readonly struct ModSaveDataKey
{
    // sum type: Global { string QId } | PerLocation { string QId, string Location } | PerMachine { Item Machine }

    public static ModSaveDataKey Global(Item machine) =>
        new() { Type = PanelLocality.Global, QId = machine.QualifiedItemId };

    public static ModSaveDataKey PerLocation(SObject machine) =>
        new()
        {
            Type = PanelLocality.PerLocation,
            QId = machine.QualifiedItemId,
            Location = machine.Location.NameOrUniqueName,
        };

    public static ModSaveDataKey PerLocation(Item machine, GameLocation location) =>
        new()
        {
            Type = PanelLocality.PerLocation,
            QId = machine.QualifiedItemId,
            Location = location.NameOrUniqueName,
        };

    public static ModSaveDataKey PerMachine(SObject machine) =>
        new()
        {
            Type = PanelLocality.PerMachine,
            QId = machine.QualifiedItemId,
            Location = machine.Location.NameOrUniqueName,
            Machine = machine,
        };

    // |    Type     |  QId   | Location | Machine |
    // |-------------|--------|----------|---------|
    // |    null     |  null  |   null   |  null   |
    // |   Global    | string |   null   |  null   |
    // | PerLocation | string |  string  |  null   |
    // | PerMachine  | string |  string  |  Item   |

    public PanelLocality? Type { get; private init; }
    public string? QId { get; private init; }
    public string? Location { get; private init; }
    public SObject? Machine { get; private init; }
}

public sealed record ModSaveDataEntryMessage(string QId, string? Location, ModSaveDataEntry? Entry);

public sealed class ModSaveData
{
    private const string PER_MACHINE = "per-machine-data";

    /// <summary>Version, for future compat things</summary>
    public ISemanticVersion? Version { get; set; } = null;

    /// <summary>Global disabled rules</summary>
    public Dictionary<string, ModSaveDataEntry> Disabled { get; set; } = [];

    /// <summary>Per location disabled rules</summary>
    public Dictionary<string, Dictionary<string, ModSaveDataEntry>> DisabledPerLocation { get; set; } = [];

    /// <summary>
    /// Validate the rules under a particular ModSaveDataEntry
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="msdDict"></param>
    /// <param name="msdKey"></param>
    /// <param name="msdEntry"></param>
    /// <param name="machine"></param>
    /// <returns></returns>
    private static bool ClearInvalidMSDEntry(
        Dictionary<string, ModSaveDataEntry> msdDict,
        string msdKey,
        ModSaveDataEntry msdEntry,
        MachineData machine,
        out string? removeKey
    )
    {
        removeKey = null;
        HashSet<RuleIdent> allIdents = [];
        foreach (MachineOutputRule rule in machine.OutputRules)
        {
            foreach (MachineOutputTriggerRule trigger in rule.Triggers)
            {
                RuleIdent ident = new(rule.Id, trigger.Id);
                allIdents.Add(ident);
            }
        }

        var newRules = msdEntry.Rules.Where(allIdents.Contains).ToImmutableHashSet();
        var newInputs = msdEntry.Inputs.Where((input) => ItemRegistry.GetData(input) != null).ToImmutableHashSet();
        if (newRules.Count != msdEntry.Rules.Count || newInputs.Count != msdEntry.Inputs.Count)
        {
            ModEntry.Log($"Clear nonexistent rules for '{msdKey}' from save data");
            if (newRules.IsEmpty && newInputs.IsEmpty)
                removeKey = msdKey;
            else
                msdDict[msdKey] = new(newRules, newInputs, msdEntry.Quality);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Clear inputs and rules known to be invalid, usually due to removing mods.
    /// It is possible for mod to add trigger and invalidate certain indexes though, can't easily account for that but clearing it
    /// is for the best.
    /// </summary>
    public bool ClearInvalidData()
    {
        bool hasChange = false;
        var machinesData = MachineRuleCache.Machines;
        HashSet<string> removeKeys = [];
        // Global rules
        foreach ((string qId, ModSaveDataEntry msdEntry) in Disabled)
        {
            if (
                ItemRegistry.GetData(qId) is not ParsedItemData itemData
                || !machinesData.TryGetValue(qId, out MachineData? machine)
            )
            {
                Disabled.Remove(qId);
                ModEntry.Log($"Remove nonexistent machine {qId} from save data");
                hasChange = true;
                continue;
            }
            hasChange = ClearInvalidMSDEntry(Disabled, qId, msdEntry, machine, out string? removeKey) || hasChange;
            if (removeKey != null)
                removeKeys.Add(removeKey);
        }
        foreach (string key in removeKeys)
            Disabled.Remove(key);
        // Per location rules
        removeKeys.Clear();
        foreach ((string location, var perLocation) in DisabledPerLocation)
        {
            HashSet<string> removeSubKeys = [];
            foreach ((string qId, ModSaveDataEntry msdEntry) in perLocation)
            {
                if (
                    ItemRegistry.GetData(qId) is not ParsedItemData itemData
                    || !machinesData.TryGetValue(qId, out MachineData? machine)
                )
                {
                    perLocation.Remove(qId);
                    ModEntry.Log($"Remove nonexistent machine {qId} from save data");
                    hasChange = true;
                    continue;
                }
                hasChange =
                    ClearInvalidMSDEntry(perLocation, qId, msdEntry, machine, out string? removeKey) || hasChange;
                if (removeKey != null)
                    removeSubKeys.Add(removeKey);
            }
            foreach (string key in removeSubKeys)
                perLocation.Remove(key);
            if (!perLocation.Any())
                removeKeys.Add(location);
        }
        foreach (string key in removeKeys)
            DisabledPerLocation.Remove(key);

        return hasChange;
    }

    internal static bool MachineHasData(Item machine) => machine.modData.ContainsKey($"{ModEntry.ModId}_{PER_MACHINE}");

    /// <summary>
    /// Save machine rule for given machine.
    /// </summary>
    internal ModSaveDataEntryMessage? SetMachineRules(
        ModSaveDataKey key,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        var entry = new ModSaveDataEntry(
            disabledRules.ToImmutableHashSet(),
            disabledInputs.ToImmutableHashSet(),
            disabledQuality
        );
        if (entry.IsEmpty())
            entry = null;

        switch (key.Type)
        {
            case PanelLocality.Global:
                if (entry == null)
                    Disabled.Remove(key.QId!);
                else
                    Disabled[key.QId!] = entry;
                return new ModSaveDataEntryMessage(key.QId!, null, entry);

            case PanelLocality.PerLocation:
                if (!DisabledPerLocation.TryGetValue(key.Location!, out var perLocation))
                {
                    perLocation = [];
                    DisabledPerLocation[key.Location!] = perLocation;
                }
                if (entry == null)
                    perLocation.Remove(key.QId!);
                else
                    perLocation[key.QId!] = entry;
                return new ModSaveDataEntryMessage(key.QId!, key.Location!, entry);

            case PanelLocality.PerMachine:
                if (entry == null)
                    key.Machine!.modData.Remove($"{ModEntry.ModId}_{PER_MACHINE}");
                else
                    key.Machine!.modData[$"{ModEntry.ModId}_{PER_MACHINE}"] = JsonConvert.SerializeObject(entry);
                return null;

            default:
                return null;
        }
    }

    internal bool TryGetModSaveDataEntry(ModSaveDataKey key, [NotNullWhen(true)] out ModSaveDataEntry? entry)
    {
        entry = key.Type switch
        {
            PanelLocality.Global => Disabled.TryGetValue(key.QId!, out var modSaveData) ? modSaveData : null,

            PanelLocality.PerLocation => DisabledPerLocation.TryGetValue(key.Location!, out var perLocation)
                ? (perLocation.TryGetValue(key.QId!, out var modSaveData) ? modSaveData : null)
                : null,

            PanelLocality.PerMachine => key.Machine!.modData.TryGetValue(
                $"{ModEntry.ModId}_{PER_MACHINE}",
                out string json
            )
                ? JsonConvert.DeserializeObject<ModSaveDataEntry>(json)
                : null,

            _ => null,
        };

        if (entry != null && entry.IsEmpty())
            entry = null;
        return entry != null;
    }

    internal bool RuleState(ModSaveDataKey key, RuleIdent ident)
    {
        if (TryGetModSaveDataEntry(key, out ModSaveDataEntry? msd))
            return !msd.Rules.Contains(ident);
        return true;
    }

    internal bool InputState(ModSaveDataKey key, string inputId)
    {
        if (TryGetModSaveDataEntry(key, out ModSaveDataEntry? msd))
            return !msd.Inputs.Contains(inputId);
        return true;
    }

    internal bool QualityState(ModSaveDataKey key, int quality)
    {
        if (TryGetModSaveDataEntry(key, out ModSaveDataEntry? msd))
        {
            if (msd.Quality.Length > quality)
                return !msd.Quality[quality];
        }
        return true;
    }
}
