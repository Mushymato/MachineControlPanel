using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Data;

/// <summary>Cache info about items matching a condition</summary>
internal static class ItemQueryCache
{
    // internal static IconEdge EmojiNote => new(new(ChatBox.emojiTexture, new Rectangle(81, 81, 9, 9)), Scale: 3f);

    // internal static IconEdge EmojiX => new(new(ChatBox.emojiTexture, new Rectangle(45, 81, 9, 9)), new(14), 4f);
    internal static readonly HashSet<string> InvariantItemGSQ =
    [
        "ITEM_CONTEXT_TAG ",
        "ITEM_CATEGORY ",
        "ITEM_HAS_EXPLICIT_OBJECT_CATEGORY ",
        "ITEM_ID ",
        "ITEM_ID_PREFIX ",
        "ITEM_NUMERIC_ID ",
        "ITEM_OBJECT_TYPE ",
        "ITEM_TYPE ",
        "ITEM_EDIBILITY ",
    ];
    private static readonly Regex ExcludeTags = new("(quality_|preserve_sheet_index_).+");
    private static readonly Dictionary<string, IReadOnlyList<Item>?> conditionItemDataCache = [];
    private static readonly Dictionary<string, HashSet<string>> contextTagLookupCache = [];
    private static readonly ItemQueryContext context = new();
    internal static ItemQueryContext Context => context;

    /// <summary>Clear cache, usually because Data/Objects was invalidated.</summary>
    internal static void Invalidate()
    {
        conditionItemDataCache.Clear();
        contextTagLookupCache.Clear();
        PopulateContextTagLookupCache();
    }

    /// <summary>Export context tag lookup cache</summary>
    /// <param name="helper"></param>
    internal static void Export(IModHelper helper)
    {
        contextTagLookupCache.Clear();
        PopulateContextTagLookupCache();
        helper.Data.WriteJsonFile(
            "context_tag_lookup.json",
            contextTagLookupCache.ToDictionary((kv) => kv.Key, (kv) => kv.Value?.Select((kv1) => kv1).ToList())
        );
    }

    internal static void PopulateContextTagLookupCache()
    {
        foreach (ItemQueryResult result in ItemQueryResolver.TryResolve("ALL_ITEMS", context))
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
    internal static bool TryContextTagLookupCache(IEnumerable<string> tags, [NotNullWhen(true)] out List<Item>? items)
    {
        List<HashSet<string>> results = [];
        HashSet<string> exclude = [];
        foreach (string tag in tags)
        {
            bool negate = tag[0] == '!';
            string realTag = negate ? tag[1..] : tag;
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
            items = results
                .Aggregate(
                    (acc, next) =>
                    {
                        acc.IntersectWith(next);
                        return acc;
                    }
                )
                .Except(exclude)
                .Select((itemId) => ItemRegistry.Create(itemId))
                .ToList();
            return true;
        }
        // do not bother handling the only !tag case, fall through to regular item query
        items = null;
        return false;
    }

    /// <summary>Probe the complex output delegate to verify that the item data is valid</summary>
    /// <param name="complexOutput"></param>
    internal static IEnumerable<Item> FilterByOutputMethod(
        string qId,
        List<MachineItemOutput> outputs,
        IEnumerable<Item>? itemDatas
    )
    {
        SObject machineObj = ItemRegistry.Create<SObject>(qId, allowNull: true);
        // magic knowledge that anvil takes trinkets
        if (qId == "(BC)Anvil")
            itemDatas ??= ItemRegistry
                .RequireTypeDefinition<TrinketDataDefinition>("(TR)")
                .GetAllData()
                .Select((itemData) => ItemRegistry.Create(itemData.QualifiedItemId, allowNull: true));
        else
            itemDatas ??= ItemRegistry
                .GetObjectTypeDefinition()
                .GetAllData()
                .Select((itemData) => ItemRegistry.Create(itemData.QualifiedItemId, allowNull: true));
        Item firstItem = itemDatas.First();
        List<Tuple<MachineItemOutput, MachineOutputDelegate>> outputDelegates = [];
        foreach (MachineItemOutput output in outputs)
        {
            if (
                !StaticDelegateBuilder.TryCreateDelegate<MachineOutputDelegate>(
                    output.OutputMethod,
                    out var createdDelegate,
                    out var error
                )
            )
            {
                ModEntry.LogOnce($"Error creating '{output.OutputMethod}' (from {qId}): {error}");
                continue;
            }
            try
            {
                createdDelegate(machineObj, firstItem, true, output, Game1.player, out _);
            }
            catch (Exception)
            {
                ModEntry.LogOnce($"Error running '{output.OutputMethod}' (from {qId})");
                continue;
            }
            outputDelegates.Add(new(output, createdDelegate));
        }
        return itemDatas.Where(
            (item) =>
            {
                if (item != null)
                {
                    foreach ((MachineItemOutput output, MachineOutputDelegate createdDelegate) in outputDelegates)
                    {
                        try
                        {
                            if (createdDelegate(machineObj, item, true, output, Game1.player, out _) != null)
                                return true;
                        }
                        catch (Exception)
                        {
                            ModEntry.LogOnce(
                                $"Error testing {item.QualifiedItemId} on '{output.OutputMethod}' (from {qId})"
                            );
                        }
                    }
                }
                return false;
            }
        );
    }

    /// <summary>Get list of <see cref="Item"/> matching a particular condition</summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    internal static IReadOnlyList<Item>? CreateConditionItemList(string condition)
    {
        if (
            ItemQueryResolver.TryResolve("ALL_ITEMS", context, ItemQuerySearchMode.All, condition, avoidRepeat: true)
                is ItemQueryResult[] results
            && results.Any()
        )
        {
            List<Item> resultItems = [];
            foreach (var res in results)
            {
                if (res.Item is Item itm)
                    resultItems.Add(itm);
            }
            return resultItems.ToList();
        }
        return null;
    }
}
