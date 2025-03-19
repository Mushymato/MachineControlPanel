using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Data;

/// <summary>Cache info about items matching a condition</summary>
internal static class ItemQueryCache
{
    private const string ALL_ITEMS = "ALL_ITEMS";
    private static readonly Regex ItemContextTagGSQ = new(@"(!?)ITEM_CONTEXT_TAG (?:Target|Input) (.+)");
    private static readonly Regex ItemGSQ =
        new(
            "\\!?(ITEM_CATEGORY|ITEM_HAS_EXPLICIT_OBJECT_CATEGORY|ITEM_ID|ITEM_ID_PREFIX|ITEM_NUMERIC_ID|ITEM_OBJECT_TYPE|ITEM_TYPE|ITEM_EDIBILITY) .+"
        );
    private static readonly Regex ExcludeTags = new("(quality_|preserve_sheet_index_).+");
    private static readonly Dictionary<string, IReadOnlyList<Item>?> conditionItemCache = [];
    private static readonly Dictionary<ValueTuple<string, string>, IReadOnlyList<Item>?> outputMethodCache = [];
    private static readonly Dictionary<string, HashSet<string>> contextTagLookupCache = [];
    private static readonly Dictionary<string, IReadOnlyList<Item>> itemQueryCache = [];

    private static IReadOnlyList<Item>? allItems = null;
    private static IReadOnlyList<Item> AllItems =>
        allItems ??= ItemQueryResolver
            .TryResolve(ALL_ITEMS, Context, filter: ItemQuerySearchMode.AllOfTypeItem)
            .Select(res => (Item)res.Item)
            .ToList();
    internal static ItemQueryContext Context => new();

    /// <summary>Clear cache, usually because Data/Objects was invalidated.</summary>
    internal static void Invalidate()
    {
        conditionItemCache.Clear();
        contextTagLookupCache.Clear();
        allItems = null;
        PopulateContextTagLookupCache();
    }

    /// <summary>Export context tag lookup cache</summary>
    /// <param name="helper"></param>
    internal static void Export(IModHelper helper)
    {
        var stopwatch = Stopwatch.StartNew();
        contextTagLookupCache.Clear();
        PopulateContextTagLookupCache();
        helper.Data.WriteJsonFile(
            "export/context_tag_lookup.json",
            contextTagLookupCache.ToDictionary((kv) => kv.Key, (kv) => kv.Value?.Select((kv1) => kv1).ToList())
        );
        ModEntry.Log($"Wrote export/context_tag_lookup.json in {stopwatch.Elapsed}");
    }

    internal static void PopulateContextTagLookupCache()
    {
        foreach (ItemQueryResult result in ItemQueryResolver.TryResolve(ALL_ITEMS, Context))
        {
            if (result.Item is not Item item)
                continue;
            foreach (string tag in item.GetContextTags())
            {
                if (!contextTagLookupCache.TryGetValue(tag, out HashSet<string>? cached))
                {
                    cached = [];
                    contextTagLookupCache[tag] = cached;
                }
                cached.Add(item.QualifiedItemId);
            }
        }
    }

    /// <summary>Do fast lookup of context tags</summary>
    /// <param name="tags"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    internal static bool TryContextTagLookupCache(
        IEnumerable<string> tags,
        [NotNullWhen(true)] out IEnumerable<Item>? items,
        HashSet<string>? include = null
    )
    {
        List<HashSet<string>> results = [];
        HashSet<string> exclude = [];
        foreach (string tag in tags)
        {
            bool negate = tag[0] == '!';
            string realTag = negate ? tag[1..] : tag;
            if (ExcludeTags.Match(realTag).Success)
                continue;
            if (contextTagLookupCache.TryGetValue(realTag, out HashSet<string>? cached))
            {
                if (negate)
                    exclude.AddRange(cached);
                else
                    results.Add(cached);
            }
        }
        if (results.Any())
        {
            IEnumerable<string> aggregate = results.Aggregate(
                (acc, next) =>
                {
                    acc.IntersectWith(next);
                    return acc;
                }
            );
            if (include != null)
                aggregate = aggregate.Where(include.Contains);
            items = aggregate.Except(exclude).Select(itemId => ItemRegistry.Create(itemId));
            return true;
        }
        // do not bother handling the only !tag case, fall through to regular item query
        items = null;
        return false;
    }

    /// <summary>Do fast lookup of context tags</summary>
    /// <param name="tags"></param>
    /// <param name="items"></param>
    /// <returns></returns>
    internal static IReadOnlyList<Item>? CreateOutputMethodItemList(
        ValueTuple<string, string> machineAndMethod,
        MachineItemOutput output
    )
    {
        SObject machineObj = ItemRegistry.Create<SObject>(machineAndMethod.Item1, allowNull: true);
        string outputMethod = machineAndMethod.Item2;
        // magic knowledge that anvil takes trinkets
        IEnumerable<Item>? itemDatas;
        if (machineObj.QualifiedItemId == "(BC)Anvil")
            itemDatas = ItemRegistry
                .RequireTypeDefinition<TrinketDataDefinition>("(TR)")
                .GetAllData()
                .Select((itemData) => ItemRegistry.Create(itemData.QualifiedItemId, allowNull: true));
        else
            itemDatas = AllItems;
        Item firstItem = itemDatas.First();
        if (
            !StaticDelegateBuilder.TryCreateDelegate<MachineOutputDelegate>(
                outputMethod,
                out var createdDelegate,
                out var error
            )
        )
        {
            ModEntry.LogOnce($"Error creating '{outputMethod}' (from {machineObj.QualifiedItemId}): {error}");
            return null;
        }
        try
        {
            createdDelegate(machineObj, firstItem, true, output, Game1.player, out _);
        }
        catch (Exception)
        {
            ModEntry.LogOnce($"Error running '{outputMethod}' (from {machineObj.QualifiedItemId})");
            return null;
        }
        return itemDatas
            .Where(
                (item) =>
                {
                    if (item != null)
                    {
                        try
                        {
                            if (createdDelegate(machineObj, item, true, output, Game1.player, out _) != null)
                                return true;
                        }
                        catch (Exception)
                        {
                            ModEntry.LogOnce(
                                $"Error testing {item.QualifiedItemId} on '{output.OutputMethod}' (from {machineObj.QualifiedItemId})"
                            );
                        }
                    }
                    return false;
                }
            )
            .ToList();
    }

    internal static IReadOnlyList<Item>? TryGetOutputMethodItemList(string qId, MachineItemOutput output)
    {
        return outputMethodCache.GetOrCreateValue(
            new ValueTuple<string, string>(qId, output.OutputMethod),
            (key) => CreateOutputMethodItemList(key, output)
        );
    }

    /// <summary>Get list of <see cref="Item"/> matching a particular condition</summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    internal static IReadOnlyList<Item>? CreateConditionItemList(string condition)
    {
        if (
            ItemQueryResolver.TryResolve(
                ALL_ITEMS,
                Context,
                ItemQuerySearchMode.AllOfTypeItem,
                condition,
                avoidRepeat: true
            )
                is ItemQueryResult[] results
            && results.Any()
        )
        {
            return results.Select(res => (Item)res.Item).ToList();
        }
        return null;
    }

    internal static IReadOnlyList<Item>? ResolveCondTagItems(
        IReadOnlyList<string>? contextTags,
        string? condition,
        out IReadOnlyList<string>? resolvedContextTags,
        out string? nonItemCondition
    )
    {
        resolvedContextTags = null;
        nonItemCondition = null;
        List<string>? nonItemConditions = null;
        List<Item>? items = null;
        // conditions
        if (condition != null)
        {
            nonItemConditions = [];
            List<string> conditionsToResolve = [];
            List<string> gsqTags = [];
            foreach (string rawCond in condition.Replace(" Input", " Target").Split(','))
            {
                string cond = rawCond.Trim();
                if (cond.Length == 0)
                    continue;
                if (ItemContextTagGSQ.Match(cond) is Match res && res.Success)
                {
                    var tags = res.Groups[2].ValueSpan.ToString().Split(' ');
                    if (res.Groups[1].ValueSpan.Length > 0)
                        gsqTags.AddRange(tags.Select(tag => $"!{tag}"));
                    else
                        gsqTags.AddRange(tags);
                }
                else if (ItemGSQ.Match(cond).Success)
                    conditionsToResolve.Add(cond);
                else
                    nonItemConditions.Add(cond);
            }
            if (contextTags == null)
                resolvedContextTags = gsqTags;
            else
                resolvedContextTags = [.. gsqTags, .. contextTags];
            if (nonItemConditions.Any())
            {
                nonItemConditions.Sort();
                nonItemCondition = string.Join('\n', nonItemConditions);
            }
            conditionsToResolve.Sort();
            string condToResolve = string.Join(',', conditionsToResolve);
            items = conditionItemCache.GetOrCreateValue(condToResolve, CreateConditionItemList)?.ToList();
        }
        else if (contextTags != null)
        {
            resolvedContextTags = contextTags;
        }
        else
        {
            return AllItems;
        }
        if (resolvedContextTags == null || !resolvedContextTags.Any())
            return items ?? AllItems;
        // tag
        if (
            TryContextTagLookupCache(
                resolvedContextTags,
                out IEnumerable<Item>? tagItems,
                items != null ? items.Select(item => item.QualifiedItemId).ToHashSet() : null
            )
        )
        {
            items = tagItems.ToList();
        }

        return items;
    }

    internal static Item ApplyMachineField(MachineItemOutput mio, ISalable salable)
    {
        if (salable is not SObject sobject)
            return (Item)salable;
        if (mio.ObjectInternalName != null)
        {
            sobject.Name = string.Format(mio.ObjectInternalName, Quirks.DefaultThing.Name ?? "");
        }
        if (!string.IsNullOrWhiteSpace(mio.PreserveType))
        {
            sobject.preserve.Value = (SObject.PreserveType)Enum.Parse(typeof(SObject.PreserveType), mio.PreserveType);
        }
        if (!string.IsNullOrWhiteSpace(mio.PreserveId))
        {
            sobject.preservedParentSheetIndex.Value = mio.PreserveId switch
            {
                "DROP_IN" or "DROP_IN_PRESERVE" => Quirks.DefaultThing?.ItemId,
                _ => mio.PreserveId,
            };
        }
        return sobject;
    }

    internal static IReadOnlyList<Item> ResolveMachineItemOutput(MachineItemOutput mio, string? randomItemId = null)
    {
        string originalItemId = mio.ItemId;
        List<string> originalRandomItemId = mio.RandomItemId;
        if (randomItemId != null)
        {
            mio.ItemId = randomItemId;
            mio.RandomItemId = null;
        }
        string mioHash = Quirks.HashMD5(mio, null);
        if (!itemQueryCache.TryGetValue(mioHash, out IReadOnlyList<Item>? itemQRes))
        {
            if (randomItemId == null && mio.RandomItemId != null && mio.RandomItemId.Count > 0)
            {
                itemQRes = mio
                    .RandomItemId.SelectMany(qId => ResolveMachineItemOutput(mio, qId))
                    .Where(item => item is not null)
                    .ToList();
            }
            else
            {
                itemQRes = ItemQueryResolver
                    .TryResolve(
                        mio,
                        Context,
                        formatItemId: id =>
                            id != null
                                ? id.Replace("DROP_IN_ID", Quirks.DefaultThingId)
                                    .Replace("DROP_IN_PRESERVE", Quirks.DefaultThingId)
                                    .Replace("DROP_IN_QUALITY", SObject.lowQuality.ToString())
                                    .Replace("NEARBY_FLOWER_ID", SObject.WildHoneyPreservedId)
                                : id,
                        filter: ItemQuerySearchMode.AllOfTypeItem,
                        inputItem: Quirks.DefaultThing
                    )
                    .Select(res => ApplyMachineField(mio, res.Item))
                    .OrderBy(item => item.QualifiedItemId)
                    .ToList();
            }
            itemQueryCache[mioHash] = itemQRes;
        }
        mio.ItemId = originalItemId;
        mio.RandomItemId = originalRandomItemId;
        return itemQRes;
    }
}
