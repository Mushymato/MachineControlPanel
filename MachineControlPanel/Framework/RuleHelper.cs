using Microsoft.Xna.Framework;
using StardewUI.Data;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace MachineControlPanel.Framework;

/// <summary>
/// Holds info about how to draw an icon, mostly a wrapper around <seealso cref="Sprite"/>
/// </summary>
/// <param name="Img"></param>
/// <param name="Edg"></param>
/// <param name="Scale"></param>
/// <param name="Tint"></param>
internal sealed record IconEdge(Sprite Img, Edges? Edg = null, float Scale = 4f, Color? Tint = null)
{
    internal Edges Edge => Edg ?? Edges.NONE;
};

/// <summary>
/// Represents an item involved in machine rule.
/// </summary>
/// <param name="Icons">Base icon, plus any decorations indicating their role (context tag, fuel)</param>
/// <param name="Tooltip">Hoverover text</param>
/// <param name="Count">Number required</param>
/// <param name="QId">Qualified item id, if this is a specific item</param>
internal sealed record RuleItem(
    List<IconEdge> Icons,
    List<string> Tooltip,
    int Count = 0,
    Item? Item = null,
    List<RuleItem>? Extra = null,
    string? ExtraItemsHeading = null
)
{
    /// <summary>Make shallow copy</summary>
    /// <returns></returns>
    internal RuleItem Copy()
    {
        return new RuleItem(
            new(Icons),
            new(Tooltip),
            Count: Count,
            Item: Item,
            Extra: Extra,
            ExtraItemsHeading: ExtraItemsHeading
        );
    }

    /// <summary>Get tooltip data for a vanilla tooltip</summary>
    /// <returns></returns>
    internal TooltipData GetTooltipData()
    {
        string tooltipStr = string.Join('\n', Tooltip.Select((tip) => tip.Trim()));
        if (Item != null)
            return new TooltipData(Item.getDescription(), Title: Item.DisplayName, Item: Item);
        return new TooltipData(tooltipStr);
    }

    internal void AddMiniExtraIcon()
    {
        if (Extra == null)
            return;
        if (Extra.Count == 1 && Extra.First().Icons.Count == 1)
        {
            IconEdge firstIcon = Extra.First().Icons.First();
            Icons.Add(new IconEdge(firstIcon.Img, RuleHelper.EmojiPlus.Edg, 2f, firstIcon.Tint));
        }
        else
        {
            Icons.Add(RuleHelper.EmojiPlus);
        }
    }
};

/// <summary>
/// A single machine rule with inputs and outputs.
/// </summary>
/// <param name="Ident"></param>
/// <param name="CanCheck"></param>
/// <param name="Inputs"></param>
/// <param name="Outputs"></param>
internal sealed record RuleEntry(RuleIdent Ident, List<RuleItem> Inputs, List<RuleItem> Outputs)
{
    internal string Repr => $"{Ident.OutputId}.{Ident.TriggerId}";
};

/// <summary>
/// Valid inputs
/// </summary>
/// <param name="Item"></param>
/// <param name="Idents"></param>
internal sealed record ValidInput(RuleItem Rule, HashSet<RuleIdent> Idents);

internal sealed class RuleHelper
{
    internal const string PLACEHOLDER_TRIGGER = "PLACEHOLDER_TRIGGER";
    internal static Integration.IExtraMachineConfigApi? EMC { get; set; } = null;
    internal static IconEdge QuestionIcon =>
        ModEntry.Config.AltQuestionMark
            ? new(new(Game1.mouseCursors, new Rectangle(240, 192, 16, 16)))
            : new(new(Game1.mouseCursors, new Rectangle(175, 425, 12, 12)), new(8));
    internal static IconEdge EmojiExclaim =>
        new(new(ChatBox.emojiTexture, new Rectangle(54, 81, 9, 9)), new(Top: 37), 3f);
    internal static IconEdge EmojiBolt =>
        new(new(ChatBox.emojiTexture, new Rectangle(36, 63, 9, 9)), new(Left: 37), 3f);
    internal static IconEdge EmojiPlus =>
        new(new(ChatBox.emojiTexture, new Rectangle(108, 81, 9, 9)), new(Left: 37), 3f);

    internal static Sprite Quality(int quality)
    {
        return quality switch
        {
            0 or 1 => new(Game1.mouseCursors, new Rectangle(338, 400, 8, 8)),
            2 => new(Game1.mouseCursors, new Rectangle(346, 400, 8, 8)),
            4 => new(Game1.mouseCursors, new Rectangle(346, 392, 8, 8)),
            _ => new(Game1.mouseCursors, new Rectangle(354, 400, 8, 8)),
        };
    }

    internal static IconEdge QualityIconEdge(int quality)
    {
        return new(Quality(quality), new(Top: 37), 3);
    }

    private bool populated = false;
    internal readonly List<RuleEntry> RuleEntries = [];
    internal readonly Dictionary<string, ValidInput> ValidInputs = [];

    internal readonly string Name;
    internal readonly string QId;
    private readonly MachineData machine;

    internal RuleHelper(string qId, string displayName, MachineData machine)
    {
        this.QId = qId;
        this.Name = displayName;
        this.machine = machine;
    }

    // Helper for checking state of saved entries
    internal bool HasDisabled => ModEntry.HasSavedEntry(QId);
    internal bool HasDisabledRules =>
        ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) && msdEntry.Rules.Any();
    internal bool HasDisabledInputs =>
        ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) && msdEntry.Inputs.Any();

    internal bool HasDisabledRule(RuleIdent ident) =>
        ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) && msdEntry.Rules.Contains(ident);

    internal bool HasDisabledInput(string inputQId) =>
        ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) && msdEntry.Inputs.Contains(inputQId);

    internal bool IsImplicitDisabled(IEnumerable<RuleIdent> idents) =>
        ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) && !idents.Except(msdEntry.Rules).Any();

    internal bool HasDisabledQuality(int quality) =>
        ModEntry.TryGetSavedEntry(QId, out ModSaveDataEntry? msdEntry) && msdEntry.Quality[quality];

    /// <summary>Add item data valid inputs</summary>
    /// <param name="itemData"></param>
    /// <param name="ident"></param>
    internal void AddValidInput(Item item, RuleIdent ident)
    {
        if (item == null || item.QualifiedItemId == null)
            return;
        if (ValidInputs.TryGetValue(item.QualifiedItemId, out ValidInput? valid))
            valid.Idents.Add(ident);
        else
            ValidInputs[item.QualifiedItemId] = new(
                new RuleItem([new(item.GetItemSprite())], [item.DisplayName], Item: item),
                [ident]
            );
    }

    /// <summary>Add rule item to valid inputs</summary>
    /// <param name="ruleItem"></param>
    /// <param name="ident"></param>
    internal void AddValidInput(RuleItem ruleItem, RuleIdent ident)
    {
        if (ruleItem.Item == null)
            return;
        if (ValidInputs.TryGetValue(ruleItem.Item.QualifiedItemId, out ValidInput? valid))
            valid.Idents.Add(ident);
        else
            ValidInputs[ruleItem.Item.QualifiedItemId] = new(ruleItem, [ident]);
    }

    internal bool GetRuleEntries(bool force = false)
    {
        if (!force && populated)
            return RuleEntries.Any();

        populated = false;
        RuleEntries.Clear();
        ValidInputs.Clear();

        // Fuel
        List<RuleItem> sharedFuel = [];
        if (machine.AdditionalConsumedItems != null)
        {
            foreach (MachineItemAdditionalConsumedItems fuel in machine.AdditionalConsumedItems)
            {
                if (ItemRegistry.Create(fuel.ItemId, allowNull: true) is Item item && item != null)
                {
                    sharedFuel.Add(
                        new RuleItem(
                            [new(item.GetItemSprite()), EmojiBolt],
                            [item.DisplayName],
                            Count: fuel.RequiredCount,
                            Item: item
                        )
                    );
                }
            }
        }

        foreach (MachineOutputRule rule in machine.OutputRules)
        {
            List<MachineItemOutput> complexOutputs = [];

            // rule outputs
            List<Tuple<List<RuleItem>, List<RuleItem>>> withEmcFuel = [];
            List<RuleItem> outputLine = [];
            foreach (MachineItemOutput output in rule.OutputItem)
            {
                List<RuleItem> optLine = GetOutputRuleItemLine(output, ref complexOutputs);
                if (optLine.Count == 0)
                    continue;

                if (EMC != null)
                {
                    // EMC Fuels
                    List<RuleItem> emcFuel = [];
                    var extraReq = EMC.GetExtraRequirements(output);
                    if (extraReq.Any())
                    {
                        foreach ((string tag, int count) in extraReq)
                        {
                            // TODO: deal with category when a mod actually use it
                            if (ItemRegistry.Create(tag, allowNull: true) is Item item && item != null)
                            {
                                emcFuel.Add(
                                    new RuleItem(
                                        [new(item.GetItemSprite()), EmojiBolt],
                                        [item.DisplayName],
                                        Count: count,
                                        Item: item
                                    )
                                );
                            }
                        }
                    }
                    var extraTagReq = EMC.GetExtraTagsRequirements(output);
                    foreach ((string tagExpr, int count) in extraTagReq)
                    {
                        var tags = tagExpr.Split(',');
                        string? normalized = ItemQueryCache.NormalizeCondition(
                            null,
                            tags,
                            out List<string> _,
                            out List<string>? _,
                            out List<string>? filteredTags
                        );
                        if (
                            normalized != null
                            && ItemQueryCache.TryGetConditionItemDatas(
                                normalized,
                                filteredTags,
                                out List<Item>? matchingItemDatas
                            )
                        )
                        {
                            RuleItem condRule = ItemQueryCache.GetReprRuleItem(matchingItemDatas, normalized, count);
                            if (GetContextTagQuality(tags) is IconEdge qualityIcon)
                                condRule.Icons.Add(qualityIcon);
                            condRule.Icons.Add(EmojiBolt);
                            emcFuel.Add(condRule);
                        }
                    }
                    if (emcFuel.Any())
                    {
                        withEmcFuel.Add(new(optLine, emcFuel));
                        continue;
                    }
                }
                outputLine.AddRange(optLine);
            }
            if (outputLine.Count == 0 && withEmcFuel.Count == 0)
                continue;

            // rule inputs (triggers)
            List<Tuple<RuleIdent, List<RuleItem>>> inputs = [];
            foreach (MachineOutputTriggerRule trigger in rule.Triggers)
            {
                RuleIdent ident = new(rule.Id, trigger.Id);
                List<RuleItem> inputLine = [];
                IconEdge? qualityIcon = null;
                List<string> nonItemConditions = [];
                // no item input
                if (!trigger.Trigger.HasFlag(MachineOutputTrigger.ItemPlacedInMachine))
                {
                    List<string> tooltip = [trigger.Trigger.ToString()];
                    inputLine.Add(new RuleItem([QuestionIcon], tooltip));
                    if (trigger.Condition != null)
                        nonItemConditions = new(trigger.Condition.Split(','));
                }
                else
                {
                    // item input based rules
                    if (trigger.RequiredItemId != null)
                    {
                        // specific item
                        if (ItemRegistry.Create(trigger.RequiredItemId, allowNull: true) is Item item && item != null)
                        {
                            if (
                                trigger.RequiredTags != null
                                && ItemQueryCache.GetPreserveRuleItem(trigger.RequiredTags, trigger.RequiredCount, item)
                                    is RuleItem preserve
                            )
                            {
                                inputLine.Add(preserve);
                                // Don't bother showing specific preserve items in inputs, should just use rules for that
                            }
                            else
                            {
                                inputLine.Add(
                                    new RuleItem(
                                        [new(item.GetItemSprite())],
                                        [item.DisplayName],
                                        Count: trigger.RequiredCount,
                                        Item: item
                                    )
                                );
                                AddValidInput(inputLine.Last().Copy(), ident);
                            }
                        }
                        if (trigger.RequiredTags != null)
                        {
                            qualityIcon = GetContextTagQuality(trigger.RequiredTags);
                        }
                    }
                    else
                    {
                        // conditional item
                        string? normalized = ItemQueryCache.NormalizeCondition(
                            trigger.Condition,
                            trigger.RequiredTags,
                            out nonItemConditions,
                            out List<string>? skippedTags,
                            out List<string>? filteredTags
                        );
                        if (
                            ItemQueryCache.TryGetConditionItemDatas(
                                normalized,
                                filteredTags,
                                QId,
                                complexOutputs,
                                out List<Item>? matchingItemDatas
                            )
                        )
                        {
                            foreach (Item item in matchingItemDatas)
                                AddValidInput(item, ident);
                            if (matchingItemDatas.Any())
                                inputLine.Add(
                                    ItemQueryCache.GetReprRuleItem(
                                        matchingItemDatas,
                                        normalized ?? I18n.RuleList_SpecialInput(),
                                        trigger.RequiredCount,
                                        skippedTags
                                    )
                                );
                        }
                    }
                }

                if (inputLine.Any())
                {
                    if (qualityIcon != null)
                    {
                        inputLine.Last().Icons.Add(qualityIcon);
                    }
                    if (nonItemConditions.Any())
                    {
                        inputLine.Last().Tooltip.InsertRange(0, nonItemConditions);
                        inputLine.Last().Icons.Add(EmojiExclaim);
                    }

                    if (sharedFuel.Any())
                        inputLine.AddRange(sharedFuel);

                    inputs.Add(new(ident, inputLine));
                }
            }
            if (complexOutputs.Any())
            {
                if (inputs.Count == 0)
                {
                    inputs.Add(
                        new(
                            new(rule.Id, PLACEHOLDER_TRIGGER),
                            [new RuleItem([QuestionIcon], [I18n.RuleList_SpecialInput()])]
                        )
                    );
                }
            }

            if (withEmcFuel.Any())
            {
                foreach ((RuleIdent ident, List<RuleItem> inputLine) in inputs)
                {
                    foreach ((List<RuleItem> optLine, List<RuleItem> emcFuel) in withEmcFuel)
                    {
                        List<RuleItem> ipt = new(inputLine);
                        foreach (RuleItem emcF in emcFuel)
                        {
                            if (
                                ipt.FindIndex(
                                    (inL) =>
                                        (
                                            inL.Item != null
                                            && emcF.Item != null
                                            && inL.Item.QualifiedItemId == emcF.Item.QualifiedItemId
                                            && inL.Icons.Count == emcF.Icons.Count
                                            &&
                                            // inL.Tooltip == emcF.Tooltip &&
                                            inL.Icons.Contains(EmojiBolt)
                                        )
                                )
                                    is int found
                                && found > -1
                            )
                            {
                                ipt[found] = new RuleItem(
                                    emcF.Icons,
                                    emcF.Tooltip,
                                    Count: ipt[found].Count + emcF.Count,
                                    Item: emcF.Item
                                );
                            }
                            else
                            {
                                ipt.Add(emcF);
                            }
                        }
                        RuleEntries.Add(new RuleEntry(ident, ipt, optLine));
                    }
                }
            }

            if (outputLine.Any())
            {
                foreach ((RuleIdent ident, List<RuleItem> inputLine) in inputs)
                {
                    RuleEntries.Add(new RuleEntry(ident, inputLine, outputLine));
                }
            }
        }

        populated = true;
        return RuleEntries.Any();
    }

    internal List<RuleItem> GetOutputRuleItemLine(
        MachineItemOutput output,
        ref List<MachineItemOutput> complexOutputs,
        bool isEMCExtra = false
    )
    {
        // EMC Extra Output
        List<RuleItem>? extraOptLine = null;
        if (
            !isEMCExtra
            && EMC?.GetExtraOutputs(output, machine) is IList<MachineItemOutput> extraOutput
            && extraOutput.Any()
        )
        {
            extraOptLine = [];
            foreach (var extraOut in extraOutput)
            {
                extraOptLine.AddRange(GetOutputRuleItemLine(extraOut, ref complexOutputs, isEMCExtra: true));
            }
        }

        List<RuleItem> optLine = [];
        if (output.OutputMethod != null)
        {
            complexOutputs.Add(output);
            string methodName = output.OutputMethod.Split(':').Last().Trim();
            optLine.Add(new RuleItem([QuestionIcon], [I18n.RuleList_SpecialOutput(method: methodName)]));
        }
        if (output.ItemId == "DROP_IN")
        {
            optLine.Add(
                new RuleItem(
                    [QuestionIcon],
                    [I18n.RuleList_SameAsInput()],
                    Extra: extraOptLine,
                    ExtraItemsHeading: I18n.RuleList_Byproducts()
                )
            );
        }
        else if (output.ItemId != null)
        {
            IList<ItemQueryResult> itemQueryResults;
            try
            {
                itemQueryResults = ItemQueryResolver.TryResolve(
                    output,
                    ItemQueryCache.Context,
                    formatItemId: id =>
                        id != null
                            ? id.Replace("DROP_IN_ID", ModEntry.DefaultThingId)
                                .Replace("DROP_IN_PRESERVE", ModEntry.DefaultThingId)
                                .Replace("DROP_IN_QUALITY", SObject.lowQuality.ToString())
                                .Replace("NEARBY_FLOWER_ID", SObject.WildHoneyPreservedId)
                            : id
                // inputItem: ModEntry.DefaultThing
                );
                List<Tuple<Item, ParsedItemData>> filteredItems = [];
                foreach (ItemQueryResult res in itemQueryResults.ToList())
                {
                    if (
                        res.Item is Item item
                        && ItemRegistry.GetData(res.Item.QualifiedItemId) is ParsedItemData itemData
                    )
                    {
                        if (res.Item is SObject sobj)
                            sobj.preservedParentSheetIndex.Value = ModEntry.DefaultThingId;
                        filteredItems.Add(new(item, itemData));
                    }
                }
                foreach (
                    (Item item, ParsedItemData itemData) in filteredItems.OrderBy((val) => val.Item1.QualifiedItemId)
                )
                {
                    List<IconEdge> icons = [new(new(itemData.GetTexture(), itemData.GetSourceRect()))];
                    List<string> tooltip = [];
                    if (output.Condition != null)
                    {
                        icons.Add(EmojiExclaim);
                        tooltip.AddRange(output.Condition.Split(','));
                    }
                    if (item.Quality > 0)
                    {
                        icons.Add(QualityIconEdge(item.Quality));
                    }
                    tooltip.Add(itemData.DisplayName);
                    optLine.Add(
                        new RuleItem(
                            icons,
                            tooltip,
                            Count: item.Stack,
                            Item: item,
                            Extra: extraOptLine,
                            ExtraItemsHeading: I18n.RuleList_Byproducts()
                        )
                    );
                }
            }
            catch (NullReferenceException)
            {
                optLine.Add(
                    new RuleItem(
                        [QuestionIcon],
                        [output.ItemId],
                        Extra: extraOptLine,
                        ExtraItemsHeading: I18n.RuleList_Byproducts()
                    )
                );
            }
        }
        foreach (var ruleItem in optLine)
        {
            if (ruleItem.Extra != null)
                ruleItem.AddMiniExtraIcon();
        }
        return optLine;
    }

    internal static IconEdge? GetContextTagQuality(IEnumerable<string> tags)
    {
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
                return QualityIconEdge(quality);
        }
        return null;
    }
}
