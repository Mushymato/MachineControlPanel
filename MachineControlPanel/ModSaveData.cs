using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using MachineControlPanel.Data;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel;

public sealed record RuleIdent(string OutputId, string TriggerId);

public sealed record QIdLocation(string QId, string Location);

public sealed record ModSaveDataEntry(
    ImmutableHashSet<RuleIdent> Rules,
    ImmutableHashSet<string> Inputs,
    bool[] Quality
);

public sealed record ModSaveDataEntryMessage(string QId, string? Location, ModSaveDataEntry? Entry);

public sealed class ModSaveData
{
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

    /// <summary>
    /// I am use bool array bc json doesn't like BitArray
    /// </summary>
    /// <param name="intArray"></param>
    /// <returns></returns>
    public static bool HasAnySet(bool[] bitArr)
    {
        for (int i = 0; i < bitArr.Length; i++)
        {
            if (bitArr[i])
                return true;
        }
        return false;
    }

    /// <summary>
    /// Set MSD entry
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="msdDict"></param>
    /// <param name="msdKey"></param>
    /// <param name="disabledRules"></param>
    /// <param name="disabledInputs"></param>
    /// <param name="disabledQuality"></param>
    /// <returns></returns>
    private static ModSaveDataEntry? SetMSDEntry(
        Dictionary<string, ModSaveDataEntry> msdDict,
        string msdKey,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        ModSaveDataEntry? msdEntry = null;
        if (!disabledRules.Any() && !disabledInputs.Any() && !HasAnySet(disabledQuality))
        {
            msdDict.Remove(msdKey);
        }
        else
        {
            msdEntry = new(disabledRules.ToImmutableHashSet(), disabledInputs.ToImmutableHashSet(), disabledQuality);
            msdDict[msdKey] = msdEntry;
        }
        return msdEntry;
    }

    /// <summary>
    /// Save machine rule for given machine.
    /// </summary>
    /// <param name="bigCraftableId"></param>
    /// <param name="disabledRules"></param>
    /// <param name="disabledInputs"></param>
    internal ModSaveDataEntryMessage? SetMachineRules(
        string qId,
        string? location,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        ModSaveDataEntry? msdEntry;
        if (location == null)
        {
            msdEntry = SetMSDEntry(Disabled, qId, disabledRules, disabledInputs, disabledQuality);
        }
        else
        {
            if (!DisabledPerLocation.TryGetValue(location, out var perLocation))
            {
                perLocation = [];
                DisabledPerLocation[location] = perLocation;
            }
            msdEntry = SetMSDEntry(perLocation, qId, disabledRules, disabledInputs, disabledQuality);
        }
        return new ModSaveDataEntryMessage(qId, location, msdEntry);
    }

    internal bool TryGetModSaveDataEntry(string qId, string? location, [NotNullWhen(true)] out ModSaveDataEntry? msd)
    {
        msd = null;
        if (location == null)
        {
            return Disabled.TryGetValue(qId, out msd);
        }
        else
        {
            if (DisabledPerLocation.TryGetValue(location, out var perLocation))
            {
                return perLocation.TryGetValue(qId, out msd);
            }
        }
        return false;
    }

    internal bool RuleState(string qId, string? location, RuleIdent ident)
    {
        if (TryGetModSaveDataEntry(qId, location, out ModSaveDataEntry? msd))
            return !msd.Rules.Contains(ident);
        return true;
    }

    internal bool InputState(string qId, string? location, string inputId)
    {
        if (TryGetModSaveDataEntry(qId, location, out ModSaveDataEntry? msd))
            return !msd.Inputs.Contains(inputId);
        return true;
    }
}
