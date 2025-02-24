using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using MachineControlPanel.Integration.IExtraMachineConfigApi;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;

namespace MachineControlPanel.Data;

internal sealed record IconDef(
    IReadOnlyList<Item>? Items = null,
    int Count = 0,
    IReadOnlyList<string>? ContextTags = null,
    string? Condition = null,
    string? Notes = null,
    bool IsFuel = false
)
{
    internal IconDef CopyWithChanges(
        IReadOnlyList<Item>? items = null,
        int? count = null,
        IReadOnlyList<string>? contextTags = null,
        string? condition = null,
        string? notes = null,
        bool? isFuel = null
    )
    {
        return new IconDef(
            Items ?? Items,
            count ?? Count,
            contextTags ?? ContextTags,
            condition ?? Condition,
            notes ?? Notes,
            isFuel ?? IsFuel
        );
    }

    internal static IconDef? FromMachineOutputTriggerRule(
        string qId,
        MachineOutputRule rule,
        MachineOutputTriggerRule motr
    )
    {
        if (!motr.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
            return new IconDef(Condition: motr.Condition, Notes: motr.Trigger.ToString());

        IReadOnlyList<Item>? items;
        IReadOnlyList<string>? contextTags = motr.RequiredTags;
        string? condition = motr.Condition;
        items =
            motr.RequiredItemId != null
                ? [ItemRegistry.Create(motr.RequiredItemId)]
                : ItemQueryCache.ResolveItems(motr.RequiredTags, motr.Condition, out contextTags, out condition);
        // output method
        HashSet<string> methodNames = [];
        List<Item> allowedItems = [];
        foreach (MachineItemOutput itemOut in rule.OutputItem)
        {
            if (
                itemOut.OutputMethod != null
                && !methodNames.Contains(itemOut.OutputMethod)
                && ItemQueryCache.TryGetOutputMethodItemList(qId, itemOut) is IReadOnlyList<Item> outputMethodItems
            )
            {
                methodNames.Add(itemOut.OutputMethod);
                allowedItems.AddRange(outputMethodItems);
            }
        }
        if (methodNames.Any())
            items = items?.Where(allowedItems.Contains).ToList();
        return new IconDef(items, motr.RequiredCount, contextTags, condition, Notes: string.Join('|', methodNames));
    }

    public override string ToString()
    {
        if (Items == null)
            return "";
        return string.Join('|', Items.Select(item => item.QualifiedItemId));
    }
}

internal sealed record RuleDef(
    IReadOnlyList<IconDef>? Inputs,
    IReadOnlyList<IconDef>? Outputs,
    IReadOnlyList<IconDef>? SharedFuel = null,
    IReadOnlyList<IconDef>? EMC_Fuel = null,
    IReadOnlyList<IconDef>? EMC_ExtraOutputs = null
)
{
    public override string ToString()
    {
        if (Inputs == null)
            return "";
        return string.Join('#', Inputs.Select(item => item.ToString()));
    }
}

internal static class MachineRuleCache
{
    private static IExtraMachineConfigApi? emc;
    private static readonly Dictionary<string, IReadOnlyList<ValueTuple<RuleIdent, RuleDef>>?> machineRuleCache = [];
    private static Dictionary<string, MachineData>? machines = null;
    private static Dictionary<string, MachineData> Machines => machines ??= DataLoader.Machines(Game1.content);

    internal static void Register(IModHelper helper)
    {
        emc = helper.ModRegistry.GetApi<IExtraMachineConfigApi>("selph.ExtraMachineConfig");
    }

    /// <summary>Clear cache, usually because Data/Objects was invalidated.</summary>
    internal static void Invalidate()
    {
        machineRuleCache.Clear();
        machines = null;
    }

    internal static void Export(IModHelper helper)
    {
        var stopwatch = Stopwatch.StartNew();
        machineRuleCache.Clear();

        foreach (var kv in Machines)
        {
            try
            {
                TryGetRuleDefList(kv.Key);
            }
            catch (Exception err)
            {
                ModEntry.Log($"TryGetRuleDefList {kv.Value}: {err}", LogLevel.Error);
            }
        }

        helper.Data.WriteJsonFile(
            "export/machine_rule_cache.json",
            machineRuleCache.ToDictionary(
                (kv) => kv.Key,
                (kv) => kv.Value?.Select(rule => new ValueTuple<RuleIdent, string>(rule.Item1, rule.Item2.ToString()))
            )
        );

        ModEntry.Log($"Wrote export/machine_rule_cache.json in {stopwatch.Elapsed}");
    }

    internal static IReadOnlyList<ValueTuple<RuleIdent, RuleDef>>? CreateRuleDefList(string qId)
    {
        if (
            !Machines.TryGetValue(qId, out MachineData? data)
            || data.OutputRules is not List<MachineOutputRule> outputRules
            || data.IsIncubator
        )
            return null;

        List<ValueTuple<RuleIdent, RuleDef>> ruleDefList = [];
        foreach (MachineOutputRule rule in outputRules)
        {
            foreach (MachineOutputTriggerRule motr in rule.Triggers)
            {
                if (IconDef.FromMachineOutputTriggerRule(qId, rule, motr) is IconDef iconDef)
                {
                    ruleDefList.Add(new(new(rule.Id, motr.Id), new([iconDef], null)));
                }
            }
        }

        return ruleDefList;
    }

    internal static IReadOnlyList<ValueTuple<RuleIdent, RuleDef>>? TryGetRuleDefList(string qId) =>
        machineRuleCache.GetOrCreateValue(qId, CreateRuleDefList);
}
