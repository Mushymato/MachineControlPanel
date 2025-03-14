using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewUI.Graphics;
using StardewValley;
using StardewValley.Delegates;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace MachineControlPanel.Framework;

internal static class DataExtensions
{
    /// <summary>
    /// Attempt to get value from key in dictionary being used as a cache.
    /// If the key is not set, create and set it using provided delegate.
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    /// <param name="dict"></param>
    /// <param name="key"></param>
    /// <param name="createValue"></param>
    /// <returns></returns>
    public static TValue GetOrCreateValue<TValue>(
        this Dictionary<string, TValue> dict,
        string key,
        Func<string, TValue> createValue
    )
    {
        if (dict.TryGetValue(key, out TValue? result))
            return result;
        result = createValue(key);
        dict[key] = result;
        return result;
    }

    /// <summary>
    /// Get a sprite from an <see cref="Item"/>
    /// </summary>
    /// <param name="item"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Sprite GetItemSprite(this Item item, int offset = 0)
    {
        ParsedItemData data =
            ItemRegistry.GetData(item.QualifiedItemId)
            ?? throw new ArgumentException($"Error item '{item.QualifiedItemId}'");
        return new(data.GetTexture(), data.GetSourceRect(offset));
    }

    /// <summary>
    /// I am use bool array bc json doesn't like BitArray
    /// </summary>
    /// <param name="intArray"></param>
    /// <returns></returns>
    public static bool HasAnySet(this bool[] bitArr)
    {
        for (int i = 0; i < bitArr.Length; i++)
        {
            if (bitArr[i])
                return true;
        }
        return false;
    }
}

internal static class RuleHelperCache
{
    // private static readonly ConditionalWeakTable<string, RuleHelper?> ruleHelperCache = [];
    private static readonly Dictionary<string, RuleHelper?> ruleHelperCache = [];

    /// <summary>Clear cache, usually because Data/Machines was invalidated.</summary>
    internal static void Invalidate()
    {
        ruleHelperCache.Clear();
        if (ModEntry.Config?.PrefetchCaches ?? false)
            Prefetch();
    }

    internal static void Prefetch()
    {
        var machinesData = DataLoader.Machines(Game1.content);
        float count = 0;
        foreach ((string qId, MachineData machine) in machinesData)
        {
            if (ItemRegistry.GetData(qId) is not ParsedItemData itemData)
                continue;
            if (TryGetRuleHelper(itemData.QualifiedItemId, itemData.DisplayName, machine, out RuleHelper? ruleHelper))
            {
                ruleHelper.GetRuleEntries();
            }
            count++;
        }
    }

    internal static bool TryGetRuleHelper(
        string bigCraftableId,
        string displayName,
        MachineData machine,
        [NotNullWhen(true)] out RuleHelper? ruleHelper
    )
    {
        ruleHelper = ruleHelperCache.GetOrCreateValue(
            bigCraftableId,
            (bcId) => CreateRuleHelper(bcId, displayName, machine)
        );
        return ruleHelper != null;
    }

    internal static RuleHelper? CreateRuleHelper(string qId, string displayName, MachineData machine)
    {
        if (
            machine.IsIncubator
            || machine.OutputRules == null
            || machine.OutputRules.Count == 0
            || !machine.AllowFairyDust
        )
            return null;
        return new(qId, displayName, machine);
        // return ruleHelper.GetRuleEntries() ? ruleHelper : null;
    }
}

/// <summary>Cache info about items matching a condition</summary>
internal static class ItemQueryCache
{
    internal static IconEdge EmojiNote => new(new(ChatBox.emojiTexture, new Rectangle(81, 81, 9, 9)), Scale: 3f);

    // internal static IconEdge EmojiX => new(new(ChatBox.emojiTexture, new Rectangle(45, 81, 9, 9)), new(14), 4f);
    private static readonly List<string> ItemGSQ =
    [
        "ITEM_CONTEXT_TAG ",
        "ITEM_CATEGORY ",
        "ITEM_HAS_EXPLICIT_OBJECT_CATEGORY ",
        "ITEM_ID ",
        "ITEM_ID_PREFIX ",
        "ITEM_NUMERIC_ID ",
        "ITEM_OBJECT_TYPE ",
        // "ITEM_PRICE ",
        // "ITEM_QUALITY ",
        // "ITEM_STACK ",
        "ITEM_TYPE ",
        "ITEM_EDIBILITY ",
    ];
    private static readonly Regex ExcludeTags = new("(quality_|preserve_sheet_index_).+");
    private static readonly Dictionary<string, List<Item>?> conditionItemDataCache = [];
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

    internal static void Export(IModHelper helper)
    {
        var machinesData = DataLoader.Machines(Game1.content);
        foreach ((string qId, MachineData machine) in machinesData)
        {
            if (ItemRegistry.GetData(qId) is not ParsedItemData item)
                continue;

            if (
                RuleHelperCache.TryGetRuleHelper(
                    item.QualifiedItemId,
                    item.DisplayName,
                    machine,
                    out RuleHelper? ruleHelper
                )
            )
            {
                ruleHelper.GetRuleEntries();
            }
        }

        helper.Data.WriteJsonFile(
            "item_query_cache.json",
            conditionItemDataCache.ToDictionary(
                (kv) => kv.Key,
                (kv) => kv.Value?.Select((kv1) => kv1.QualifiedItemId).ToList()
            )
        );

        contextTagLookupCache.Clear();
        PopulateContextTagLookupCache();
        helper.Data.WriteJsonFile(
            "context_Tag_lookup.json",
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

    internal static bool TryFindInContextTagLookupCache(List<string> tags, [NotNullWhen(true)] out List<Item>? items)
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

    /// <summary>Convert some conditions and tags into a condition of specific form</summary>
    /// <param name="condition"></param>
    /// <param name="tags"></param>
    /// <param name="nonItemConditions"></param>
    /// <returns></returns>
    internal static string? NormalizeCondition(
        string? condition,
        IEnumerable<string>? tags,
        out List<string> nonItemConditions,
        out List<string>? skippedTags,
        out List<string>? filteredTags
    )
    {
        nonItemConditions = [];
        skippedTags = null;
        filteredTags = null;
        SortedSet<string> mergedConds = [];
        if (condition != null)
        {
            foreach (string rawCond in condition.Split(','))
            {
                string cond = rawCond.Trim();
                if (cond.Length == 0)
                    continue;
                if (ItemGSQ.Any((gsq) => cond.StartsWith(gsq) || cond[1..].StartsWith(gsq)))
                    mergedConds.Add(cond.Replace(" Input ", " Target "));
                else
                    nonItemConditions.Add(cond);
            }
        }
        if (tags != null)
        {
            skippedTags = [];
            filteredTags = [];
            foreach (string rawTag in tags)
            {
                string tag = rawTag.Trim();
                if (ExcludeTags.Match(tag).Success)
                    skippedTags.Add(tag);
                else
                    filteredTags.Add(tag);
            }
            if (filteredTags.Any())
            {
                bool onlyHasTag = !nonItemConditions.Any() && !mergedConds.Any();
                mergedConds.Add($"ITEM_CONTEXT_TAG Target {string.Join(' ', filteredTags)}");
                // determine if tags should be used to determine items
                if (!onlyHasTag)
                    filteredTags = null;
            }
        }
        if (!mergedConds.Any())
            return null;

        return string.Join(',', new SortedSet<string>(mergedConds));
    }

    /// <summary>Get conditional item datas from a condition string that should be normalized</summary>
    /// <param name="tag"></param>
    /// <param name="itemDatas"></param>
    /// <returns></returns>
    internal static bool TryGetConditionItemDatas(
        string condition,
        List<string>? filteredTags,
        [NotNullWhen(true)] out List<Item>? itemDatas
    )
    {
        if (conditionItemDataCache.TryGetValue(condition, out itemDatas))
            return itemDatas != null;
        if (filteredTags != null && TryFindInContextTagLookupCache(filteredTags, out itemDatas))
            conditionItemDataCache[condition] = itemDatas;
        else
            itemDatas = conditionItemDataCache.GetOrCreateValue(condition, CreateConditionItemDatas);
        return itemDatas != null;
    }

    /// <summary>Get conditional item datas from a condition string that should be normalized</summary>
    /// <param name="tag"></param>
    /// <param name="itemDatas"></param>
    /// <returns></returns>
    internal static bool TryGetConditionItemDatas(
        string? condition,
        List<string>? filteredTags,
        string qId,
        List<MachineItemOutput> complexOutputs,
        [NotNullWhen(true)] out List<Item>? itemDatas
    )
    {
        itemDatas = null;
        if (condition != null)
            TryGetConditionItemDatas(condition, filteredTags, out itemDatas);
        if (complexOutputs.Any())
            itemDatas = FilterByOutputMethod(qId, complexOutputs, itemDatas).ToList();
        return itemDatas != null && itemDatas.Any();
    }

    /// <summary>Get list of <see cref="Item"/> matching a particular condition</summary>
    /// <param name="condition"></param>
    /// <returns></returns>
    private static List<Item>? CreateConditionItemDatas(string condition)
    {
        if (
            ItemQueryResolver.TryResolve("ALL_ITEMS", context, ItemQuerySearchMode.All, condition)
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

    /// <summary>Get a representative item for particular condition + context tag</summary>
    /// <param name="matchingItemDatas"></param>
    /// <param name="condition"></param>
    /// <returns></returns>
    internal static RuleItem GetReprRuleItem(
        List<Item> matchingItemDatas,
        string condition,
        int count,
        IEnumerable<string>? skippedTags = null
    )
    {
        // ParsedItemData reprItem = Random.Shared.ChooseFrom(matchingItemDatas);
        Item reprItem = matchingItemDatas.First();
        if (matchingItemDatas.Count == 1)
        {
            if (skippedTags != null)
                if (GetPreserveRuleItem(skippedTags, count, reprItem) is RuleItem preserve)
                    return preserve;
            return new RuleItem([new(reprItem.GetItemSprite())], [reprItem.DisplayName], Count: count, Item: reprItem);
        }
        else
        {
            List<string> tooltips = [];
            foreach (string cond in condition.Split(','))
            {
                if (cond.StartsWith("ITEM_CONTEXT_TAG Target ") || cond.StartsWith("!ITEM_CONTEXT_TAG Target "))
                {
                    bool negate = cond[0] == '!';
                    string[] condTags = cond[
                        (negate ? "!ITEM_CONTEXT_TAG Target ".Length : "ITEM_CONTEXT_TAG Target ".Length)..
                    ]
                        .Split(' ');
                    foreach (string tag in condTags)
                    {
                        if (tag == "")
                            continue;
                        string realTag = tag[0] == '!' ? tag[1..] : tag;
                        if (tag[0] == '!' != negate) // XOR
                            tooltips.Add($"NOT {realTag}");
                        else
                            tooltips.Add(realTag);
                    }
                }
                else
                {
                    tooltips.Add(cond);
                }
            }
            return new RuleItem(
                [new(reprItem.GetItemSprite(), Tint: Color.White * 0.5f), EmojiNote],
                tooltips,
                count,
                Extra: matchingItemDatas
                    .Select((mItem) => new RuleItem([new(mItem.GetItemSprite())], [mItem.DisplayName], Item: mItem))
                    .ToList()
            );
        }
    }

    /// <summary>Get preserve rule item which is colored, this is not cached rn maybe later</summary>
    /// <param name="tags"></param>
    /// <param name="count"></param>
    /// <param name="baseItem"></param>
    /// <param name="preserveTag"></param>
    /// <returns></returns>
    internal static RuleItem? GetPreserveRuleItem(IEnumerable<string> tags, int count, Item baseItem)
    {
        foreach (string tag in tags)
        {
            if (tag == $"preserve_sheet_index_{SObject.WildHoneyPreservedId}" && baseItem.QualifiedItemId == "(O)340")
            {
                // special case wild honey
                return new RuleItem(
                    [new(baseItem.GetItemSprite())],
                    [Game1.content.LoadString("Strings\\Objects:Honey_Wild_Name")],
                    Count: count
                );
            }
            bool negate = tag.StartsWith('!');
            string realTag = negate ? tag[1..] : tag;
            if (realTag.StartsWith("preserve_sheet_index_"))
            {
                // obtain ingredient data & derive color
                // not using FLAVORED_ITEM because we cannot create modded preserves that way
                if (
                    ItemQueryResolver
                        .TryResolve(
                            "ALL_ITEMS",
                            Context,
                            ItemQuerySearchMode.FirstOfTypeItem,
                            // id_o_itemid resolves but not preserved_item_index_itemid since thats on SObject
                            $"ITEM_CONTEXT_TAG Target id_o_{realTag[21..]}"
                        )
                        .FirstOrDefault()
                        ?.Item
                    is Item preserveItem
                )
                {
                    Color? tint;
                    if (baseItem.QualifiedItemId == "(O)812" && preserveItem.QualifiedItemId == "(O)698")
                        tint = new Color(61, 55, 42);
                    else
                        tint = TailoringMenu.GetDyeColor(preserveItem);
                    if (tint == null)
                        return null;
                    List<IconEdge> icons = [];
                    if (Game1.objectData.TryGetValue(baseItem.ItemId, out var value)
#if !SDV_168
                        && value.ColorOverlayFromNextIndex
#endif
                    )
                    {
                        icons.Add(new(baseItem.GetItemSprite()));
                        icons.Add(new(baseItem.GetItemSprite(1), Tint: tint));
                    }
                    else
                    {
                        icons.Add(new(baseItem.GetItemSprite(), Tint: tint));
                    }
                    return new RuleItem(
                        icons,
                        [$"{baseItem.DisplayName} ({preserveItem.DisplayName})"],
                        Count: count,
                        Item: baseItem
                    );
                }
            }
        }
        return null;
    }
}

internal static class PlayerHasItemCache
{
    private static readonly HashSet<string> playerHasItem = [];

    internal static void Populate()
    {
        playerHasItem.Clear();
        Utility.ForEachItem(
            (item) =>
            {
                if (item.HasBeenInInventory)
                    playerHasItem.Add(item.QualifiedItemId);
                return true;
            }
        );
    }

    internal static void AddItem(string qId)
    {
        playerHasItem.Add(qId);
    }

    internal static bool HasItem(string qId)
    {
        return playerHasItem.Contains(qId);
    }
}
