using System.Collections.Immutable;
using MachineControlPanel.Data;
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
);

public sealed record ModSaveDataEntryMessage(string QId, ModSaveDataEntry? Entry);

public sealed class ModSaveData
{
    /// <summary>Version, for future compat things</summary>
    public ISemanticVersion? Version { get; set; } = null;

    /// <summary>Global disabled rules</summary>
    public Dictionary<string, ModSaveDataEntry> Disabled { get; set; } = [];

    /// <summary>Per location disabled rules</summary>
    public Dictionary<ValueTuple<string, string>, ModSaveDataEntry> DisabledPerLocation { get; set; } = [];

    /// <summary>
    /// Validate the rules under a particular ModSaveDataEntry
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <param name="msdDict"></param>
    /// <param name="msdKey"></param>
    /// <param name="msdEntry"></param>
    /// <param name="machine"></param>
    /// <returns></returns>
    private static bool ClearInvalidMSDEntry<TKey>(
        Dictionary<TKey, ModSaveDataEntry> msdDict,
        TKey msdKey,
        ModSaveDataEntry msdEntry,
        MachineData machine
    )
        where TKey : notnull
    {
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
                msdDict.Remove(msdKey);
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
            hasChange = ClearInvalidMSDEntry(Disabled, qId, msdEntry, machine) || hasChange;
        }
        // Per location rules
        foreach (((string qId, string locationName), ModSaveDataEntry msdEntry) in DisabledPerLocation)
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
            hasChange = ClearInvalidMSDEntry(Disabled, qId, msdEntry, machine) || hasChange;
        }
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
    private static ModSaveDataEntry? SetMSDEntry<TKey>(
        Dictionary<TKey, ModSaveDataEntry> msdDict,
        TKey msdKey,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
        where TKey : notnull
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
        string bigCraftableId,
        string? locationName,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        ModSaveDataEntry? msdEntry =
            locationName == null
                ? SetMSDEntry(Disabled, bigCraftableId, disabledRules, disabledInputs, disabledQuality)
                : SetMSDEntry(
                    DisabledPerLocation,
                    new ValueTuple<string, string>(bigCraftableId, locationName),
                    disabledRules,
                    disabledInputs,
                    disabledQuality
                );

        return new ModSaveDataEntryMessage(bigCraftableId, msdEntry);
    }
}
