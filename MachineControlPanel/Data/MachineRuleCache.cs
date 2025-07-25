using System.Diagnostics;
using System.Text;
using MachineControlPanel.Integration.IExtraMachineConfigApi;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;
using StardewValley.Objects;

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
public record IconDef(
    IReadOnlyList<Item>? Items = null,
    int Count = 0,
    IReadOnlyList<string>? ContextTags = null,
    string? Condition = null,
    IReadOnlyList<string>? Notes = null,
    bool IsFuel = false
)
{
    protected readonly StringBuilder sb = new();

    public int Quality => Math.Max(Items?.FirstOrDefault()?.Quality ?? 0, GetContextTagQuality(ContextTags));

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

    internal static SObject? CreateFlavoredItem(Item reqItem, SObject preserveItem)
    {
        SObject.PreserveType? preserveType = reqItem.QualifiedItemId switch
        {
            "(O)447" => SObject.PreserveType.AgedRoe,
            "(O)340" => SObject.PreserveType.Honey,
            "(O)344" => SObject.PreserveType.Jelly,
            "(O)350" => SObject.PreserveType.Juice,
            "(O)342" => SObject.PreserveType.Pickle,
            "(O)812" => SObject.PreserveType.Roe,
            "(O)348" => SObject.PreserveType.Wine,
            "(O)SpecificBait" => SObject.PreserveType.Bait,
            "(O)DriedFruit" => SObject.PreserveType.DriedFruit,
            "(O)DriedMushrooms" => SObject.PreserveType.DriedMushroom,
            "(O)SmokedFish" => SObject.PreserveType.SmokedFish,
            _ => null,
        };
        if (preserveType != null)
        {
            ObjectDataDefinition objectTypeDefinition = ItemRegistry.GetObjectTypeDefinition();
            return objectTypeDefinition.CreateFlavoredItem((SObject.PreserveType)preserveType, preserveItem);
        }
        else if (TailoringMenu.GetDyeColor(preserveItem) is Color preserveColor)
        {
            return new ColoredObject(reqItem.ItemId, 1, preserveColor);
        }
        return null;
    }

    /// <summary>Form icon for the input</summary>
    /// <param name="qId"></param>
    /// <param name="rule"></param>
    /// <param name="motr"></param>
    /// <returns></returns>
    internal static IconDef? FromInput(string qId, MachineOutputRule rule, MachineOutputTriggerRule motr)
    {
        if (!motr.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
            return new IconDef(Condition: motr.Condition, Notes: [motr.Trigger.ToString()]);

        IReadOnlyList<Item>? items = null;
        IReadOnlyList<string>? contextTags = motr.RequiredTags;
        string? condition = motr.Condition;
        List<string>? notes = null;
        if (motr.RequiredItemId != null)
        {
            if (ItemQueryCache.GetItem(motr.RequiredItemId) is Item reqItem)
            {
                // if (motr.RequiredTags != null)
                //     reqItem.GetContextTags().AddRange(motr.RequiredTags);
                if (motr.RequiredTags != null)
                {
                    foreach (string tag in motr.RequiredTags)
                    {
                        if (tag.StartsWith("preserve_sheet_index_"))
                        {
                            if (
                                ItemQueryCache.TryContextTagLookupCache(
                                    [$"id_o_{tag[21..]}"],
                                    out IEnumerable<Item>? resolved
                                ) && resolved.FirstOrDefault() is SObject preserveItem
                            )
                            {
                                reqItem = CreateFlavoredItem(reqItem, preserveItem) ?? reqItem;
                            }
                            break;
                        }
                    }
                }
                items = [reqItem];
            }
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
                    && outputMethodItems.Any()
                )
                {
                    methodNames.Add(itemOut.OutputMethod);
                    allowedItems.AddRange(outputMethodItems);
                }
            }
            if (methodNames.Any())
            {
                if (items == null)
                {
                    items = allowedItems;
                }
                else if (allowedItems.Count != 0)
                {
                    HashSet<string> allowed = allowedItems.Select(item => item.QualifiedItemId).ToHashSet();
                    items = items?.Where((item) => allowed.Contains(item.QualifiedItemId)).ToList();
                }
                notes = methodNames.ToList();
                notes.Sort();
            }
        }

        // context tag intentionally excluded here, since many mods put context tag for other mod's compat only
        if (items?.Count > 0 || !string.IsNullOrEmpty(condition) || notes?.Count > 0)
        {
            return new IconDef(items, motr.RequiredCount, contextTags, condition, Notes: notes);
        }
        return null;
    }

    /// <summary>
    /// Form a fuel icon def (vanilla)
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    internal static IconDef? FromFuel(string itemId, int count)
    {
        if (ItemQueryCache.GetItem(itemId) is Item item)
            return new IconDef([item], Count: count, IsFuel: true);
        return null;
    }

    internal static IconDef? FromEMCTagsFuelItems(IReadOnlyList<string> itemIds, int count)
    {
        IReadOnlyList<Item>? items = itemIds
            .Select(itemId => ItemQueryCache.GetItem(itemId) ?? ItemRegistry.Create(itemId))
            .ToList();
        if (items?.Count > 0)
            return new IconDef(items, Count: count, IsFuel: true);
        return null;
    }

    internal static IconDef? FromEMCTagsFuelTags(IReadOnlyList<string> tags, int count)
    {
        IReadOnlyList<Item>? items = ItemQueryCache.ResolveCondTagItems(
            tags,
            null,
            out IReadOnlyList<string>? contextTags,
            out string? condition
        );
        if (items?.Count > 0)
            return new IconDef(items, Count: count, ContextTags: contextTags, Condition: condition, IsFuel: true);
        return null;
    }

    public string? Desc
    {
        get
        {
            sb.Clear();
            if (
                ContextTags != null
                && ContextTags.Where(tag => !tag.StartsWith("id_")) is IEnumerable<string> filteredTags
                && filteredTags.Any()
            )
                sb.AppendJoin('\n', filteredTags);
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
            {
                string desc = sb.ToString().Trim();
                if (string.Empty != desc)
                    return desc;
            }
            return null;
        }
    }

    internal virtual bool Match(string searchText)
    {
        if (Desc.ContainsIgnoreCase(searchText))
            return true;
        if (Items?.Any(item => item.DisplayName.ContainsIgnoreCase(searchText)) ?? false)
            return true;
        return false;
    }

#if DEBUG
    public override string ToString()
    {
        if (Items == null)
            return "?";
        return string.Join('|', Items.Select(item => item.QualifiedItemId));
    }
#endif
}

public sealed record IconOutputDef(
    IReadOnlyList<Item>? Items = null,
    int Count = 0,
    IReadOnlyList<string>? ContextTags = null,
    string? Condition = null,
    IReadOnlyList<string>? Notes = null,
    bool CopyQuality = false,
    IReadOnlyList<IconDef>? EMCFuel = null,
    IReadOnlyList<IconDef>? EMCByproduct = null
) : IconDef(Items, Count, ContextTags, Condition, Notes, false)
{
    internal static IExtraMachineConfigApi? emc;

    internal List<IconOutputDef>? SameGroupOutputs = null;

    /// <summary>
    /// Form output icon by resolving the item query
    /// </summary>
    /// <param name="mio"></param>
    /// <returns></returns>
    internal static IconOutputDef? FromOutput(MachineItemOutput mio, MachineData data, bool recursive = false)
    {
        if (mio.OutputMethod != null)
            return new IconOutputDef(Notes: [I18n.RuleList_SpecialOutput(mio.OutputMethod.Split(':').Last().Trim())]);
        if (mio.ItemId == "DROP_IN")
        {
            Item defaultItem = (Item)
                ItemQueryResolver.ApplyItemFields(Quirks.DefaultThing.getOne(), mio, ItemQueryCache.IQContext);
            return new(
                [defaultItem],
                defaultItem.Stack,
                Condition: mio.Condition,
                Notes: [I18n.RuleList_SameAsInput()]
            );
        }
        else if (ItemQueryCache.ResolveMachineItemOutput(mio) is IReadOnlyList<Item> items && items.Count > 0)
        {
            List<IconDef>? emcFuel = null;
            List<IconDef>? emcExtraOutputs = null;
            if (emc != null)
            {
                // extra fuel
                emcFuel = [];
                foreach (var fuel in emc.GetExtraRequirements(mio))
                {
                    if (FromEMCTagsFuelItems([fuel.Item1], fuel.Item2) is IconDef fuelDef)
                        emcFuel.Add(fuelDef);
                }
                foreach (var fuel in emc.GetExtraTagsRequirements(mio))
                {
                    if (FromEMCTagsFuelTags(fuel.Item1.Split(','), fuel.Item2) is IconDef fuelDef)
                        emcFuel.Add(fuelDef);
                }
                if (emcFuel.Count == 0)
                    emcFuel = null;
                // extra outputs
                if (!recursive)
                {
                    emcExtraOutputs = [];
                    foreach (var extraMio in emc.GetExtraOutputs(mio, data))
                    {
                        if (FromOutput(extraMio, data, recursive: true) is IconDef extraOutputDef)
                            emcExtraOutputs.Add(extraOutputDef);
                    }
                    if (emcExtraOutputs.Count == 0)
                        emcExtraOutputs = null;
                }
            }
            return new(
                items,
                items[0].Stack,
                Condition: mio.Condition,
                EMCFuel: emcFuel,
                EMCByproduct: emcExtraOutputs,
                CopyQuality: mio.CopyQuality
            );
        }
        return null;
    }

    internal override bool Match(string searchText)
    {
        if (base.Match(searchText))
            return true;
        if (SameGroupOutputs != null && SameGroupOutputs.Any(sgo => sgo.Match(searchText)))
            return true;
        return false;
    }

#if DEBUG
    public override string ToString()
    {
        if (Items != null)
        {
            sb.AppendJoin('|', Items.Select(item => item.QualifiedItemId));
        }
        if (EMCFuel != null)
        {
            sb.Append('\n');
            sb.AppendJoin(',', EMCFuel.Select(outp => outp.ToString()));
        }
        if (EMCByproduct != null)
        {
            sb.Append('\n');
            sb.AppendJoin(',', EMCByproduct.Select(outp => outp.ToString()));
        }
        return sb.ToString();
    }
#endif
}

/// <summary>
/// A machine rule.
/// It is 1 trigger an N outputs even though trigger and output are same level in data, this is because we disable rules per trigger
/// </summary>
/// <param name="Input"></param>
/// <param name="Outputs"></param>
/// <param name="SharedFuel"></param>
/// <param name="EMCFuel"></param>
/// <param name="EMCExtraOutputs"></param>
public sealed record RuleDef(
    IconDef Input,
    IReadOnlyList<IconOutputDef> Outputs,
    IReadOnlyList<IconDef>? SharedFuel = null
)
{
#if DEBUG
    public override string ToString()
    {
        StringBuilder sb = new();
        sb.Append(Input.ToString());
        sb.Append('\n');
        sb.AppendJoin(',', Outputs.Select(outp => outp.ToString()));
        return sb.ToString();
    }
#endif
};

public sealed record RuleIdentDefPair(RuleIdent Ident, RuleDef Def);

internal static class MachineRuleCache
{
    private static readonly Dictionary<string, IReadOnlyList<RuleIdentDefPair>?> machineRuleCache = [];
    private static Dictionary<string, MachineData>? machines = null;
    internal static Dictionary<string, MachineData> Machines => machines ??= DataLoader.Machines(Game1.content);

    internal static void Register(IModHelper helper)
    {
        IconOutputDef.emc = helper.ModRegistry.GetApi<IExtraMachineConfigApi>("selph.ExtraMachineConfig");
    }

    /// <summary>Clear cache, usually because Data/Objects was invalidated.</summary>
    internal static void Invalidate()
    {
        machineRuleCache.Clear();
        machines = null;
    }

#if DEBUG
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
#endif

    internal static bool NoRules(string qId)
    {
        if (
            !Machines.TryGetValue(qId, out MachineData? data)
            || data.OutputRules == null
            || data.OutputRules.Count == 0
            || data.IsIncubator
        )
            return true;
        return false;
    }

    internal static IReadOnlyList<RuleIdentDefPair>? CreateRuleDefList(string qId)
    {
        if (
            !Machines.TryGetValue(qId, out MachineData? data)
            || data.OutputRules is not List<MachineOutputRule> outputRules
            || data.OutputRules.Count == 0
            || data.IsIncubator
        )
            return null;

        List<IconDef>? sharedFuel = null;
        if (data.AdditionalConsumedItems != null)
        {
            sharedFuel = [];
            foreach (MachineItemAdditionalConsumedItems fuel in data.AdditionalConsumedItems)
            {
                if (IconDef.FromFuel(fuel.ItemId, fuel.RequiredCount) is IconDef iconDef)
                    sharedFuel.Add(iconDef);
            }
        }

        List<RuleIdentDefPair> ruleDefList = [];

        foreach (MachineOutputRule rule in outputRules)
        {
            if (rule.OutputItem == null || rule.Triggers == null)
                continue;

            foreach (MachineOutputTriggerRule motr in rule.Triggers)
            {
                if (IconDef.FromInput(qId, rule, motr) is not IconDef inputDef)
                    continue;
                List<IconOutputDef> outputDefs = [];
                IconOutputDef? plainOutputDef = null;
                foreach (MachineItemOutput mio in rule.OutputItem)
                {
                    if (IconOutputDef.FromOutput(mio, data) is not IconOutputDef outputDef)
                        continue;
                    if (outputDef.EMCByproduct == null && outputDef.EMCFuel == null)
                    {
                        if (plainOutputDef == null)
                        {
                            plainOutputDef = outputDef;
                            outputDefs.Add(outputDef);
                        }
                        else
                        {
                            plainOutputDef.SameGroupOutputs ??= [];
                            plainOutputDef.SameGroupOutputs.Add(outputDef);
                        }
                        continue;
                    }
                    outputDefs.Add(outputDef);
                }
                if (outputDefs.Count == 0)
                    continue;
                ruleDefList.Add(new(new(rule.Id, motr.Id), new(inputDef, outputDefs, sharedFuel)));
            }
        }

        return ruleDefList;
    }

    internal static IReadOnlyList<RuleIdentDefPair>? TryGetRuleDefList(string qId) =>
        machineRuleCache.GetOrCreateValue(qId, CreateRuleDefList);
}
