using System.Diagnostics;
using System.Text;
using MachineControlPanel.Integration.IExtraMachineConfigApi;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Internal;

namespace MachineControlPanel.Data;

/// <summary>
/// A definition for a singular icon displayed on a rule.
/// </summary>
/// <param name="Items"></param>
/// <param name="Count"></param>
/// <param name="ContextTags"></param>
/// <param name="Condition"></param>
/// <param name="Notes"></param>
/// <param name="IsFuel"></param>
public sealed record IconDef(
    IReadOnlyList<Item>? Items = null,
    int Count = 0,
    IReadOnlyList<string>? ContextTags = null,
    string? Condition = null,
    IReadOnlyList<string>? Notes = null,
    bool IsFuel = false
)
{
    readonly StringBuilder sb = new();

    // internal IconDef CopyWithChanges(
    //     IReadOnlyList<Item>? items = null,
    //     int? count = null,
    //     IReadOnlyList<string>? contextTags = null,
    //     string? condition = null,
    //     string? notes = null,
    //     bool? isFuel = null
    // )
    // {
    //     return new IconDef(
    //         Items ?? Items,
    //         count ?? Count,
    //         contextTags ?? ContextTags,
    //         condition ?? Condition,
    //         notes ?? Notes,
    //         isFuel ?? IsFuel
    //     );
    // }
    public int Quality => Math.Max(Items?[0].Quality ?? 0, GetContextTagQuality(ContextTags));

    internal static int GetContextTagQuality(IEnumerable<string>? tags)
    {
        if (tags == null)
            return 0;
        foreach (string tag in tags)
        {
            int quality = tag.Trim() switch
            {
                "quality_none" => 0,
                "quality_silver" => 1,
                "quality_gold" => 2,
                "quality_iridium" => 4,
                _ => -1,
            };
            if (quality > -1)
                return quality;
        }
        return 0;
    }

    /// <summary>Form icon for the input</summary>
    /// <param name="qId"></param>
    /// <param name="rule"></param>
    /// <param name="motr"></param>
    /// <returns></returns>
    internal static IconDef? FormInputIconDef(string qId, MachineOutputRule rule, MachineOutputTriggerRule motr)
    {
        if (!motr.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
            return new IconDef(Condition: motr.Condition, Notes: [motr.Trigger.ToString()]);

        IReadOnlyList<Item>? items;
        IReadOnlyList<string>? contextTags = motr.RequiredTags;
        string? condition = motr.Condition;
        List<string>? notes = null;
        if (motr.RequiredItemId != null)
        {
            items = ItemRegistry.Create(motr.RequiredItemId, allowNull: true) is Item item ? [item] : null;
        }
        else
        {
            items = ItemQueryCache.ResolveCondTagItems(
                motr.RequiredTags,
                motr.Condition,
                out contextTags,
                out condition
            );
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
            {
                items = items?.Where(allowedItems.Contains).ToList();
                notes = methodNames.ToList();
                notes.Sort();
            }
        }

        if (items != null || condition != null || notes != null)
            return new IconDef(items, motr.RequiredCount, contextTags, condition, Notes: notes);
        return null;
    }

    internal static IconDef? FormOutputIconDef(MachineItemOutput mio)
    {
        if (mio.OutputMethod != null)
            return new IconDef(Notes: [I18n.RuleList_SpecialOutput(mio.OutputMethod.Split(':').Last().Trim())]);
        if (mio.ItemId == "DROP_IN")
        {
            Item defaultItem = (Item)
                ItemQueryResolver.ApplyItemFields(Quirks.DefaultThing.getOne(), mio, ItemQueryCache.Context);
            return new(
                [defaultItem],
                defaultItem.Stack,
                Condition: mio.Condition,
                Notes: [I18n.RuleList_SameAsInput()]
            );
        }
        else if (ItemQueryCache.ResolveItemQuery(mio) is IReadOnlyList<Item> items && items.Count > 0)
        {
            return new(items, items[0].Stack, Condition: mio.Condition);
        }
        return null;
    }

    public string? Desc
    {
        get
        {
            sb.Clear();
            if (ContextTags != null)
                sb.AppendJoin('\n', ContextTags);
            if (Condition != null)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.AppendLine(Condition);
            }
            if (Notes != null)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.AppendJoin('\n', Notes);
            }
            if (sb.Length > 0)
                return sb.ToString();
            return null;
        }
    }

    public override string ToString()
    {
        if (Items == null)
            return "?";
        return string.Join('|', Items.Select(item => item.QualifiedItemId));
    }
}

public sealed record RuleDef(
    IconDef Input,
    IReadOnlyList<IconDef> Outputs,
    IReadOnlyList<IconDef>? SharedFuel = null,
    IReadOnlyList<IconDef>? EMC_Fuel = null,
    IReadOnlyList<IconDef>? EMC_ExtraOutputs = null
)
{
    public override string ToString()
    {
        if (Input == null)
            return "";
        return string.Join('#', Input.ToString());
    }
}

public sealed record RuleIdentDefPair(RuleIdent Ident, RuleDef Def);

internal static class MachineRuleCache
{
    private static IExtraMachineConfigApi? emc;
    private static readonly Dictionary<string, IReadOnlyList<RuleIdentDefPair>?> machineRuleCache = [];
    private static Dictionary<string, MachineData>? machines = null;
    internal static Dictionary<string, MachineData> Machines => machines ??= DataLoader.Machines(Game1.content);

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
                (kv) => kv.Value?.Select(rule => new ValueTuple<RuleIdent, string>(rule.Ident, rule.Def.ToString()))
            )
        );

        ModEntry.Log($"Wrote export/machine_rule_cache.json in {stopwatch.Elapsed}");
    }

    internal static IReadOnlyList<RuleIdentDefPair>? CreateRuleDefList(string qId)
    {
        if (
            !Machines.TryGetValue(qId, out MachineData? data)
            || data.OutputRules is not List<MachineOutputRule> outputRules
            || data.IsIncubator
        )
            return null;

        List<RuleIdentDefPair> ruleDefList = [];
        foreach (MachineOutputRule rule in outputRules)
        {
            List<IconDef> outputs = [];
            foreach (MachineItemOutput mio in rule.OutputItem)
            {
                if (IconDef.FormOutputIconDef(mio) is IconDef iconDef)
                {
                    outputs.Add(iconDef);
                }
            }
            foreach (MachineOutputTriggerRule motr in rule.Triggers)
            {
                if (IconDef.FormInputIconDef(qId, rule, motr) is IconDef iconDef)
                {
                    ruleDefList.Add(new(new(rule.Id, motr.Id), new(iconDef, outputs)));
                }
            }
        }

        return ruleDefList;
    }

    internal static IReadOnlyList<RuleIdentDefPair>? TryGetRuleDefList(string qId) =>
        machineRuleCache.GetOrCreateValue(qId, CreateRuleDefList);
}
