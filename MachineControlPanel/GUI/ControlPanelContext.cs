using System.ComponentModel;
using MachineControlPanel.Data;
using MachineControlPanel.GUI.Includes;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PropertyChanged.SourceGenerator;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;

namespace MachineControlPanel.GUI;

public record SpriteLayer(SDUISprite Sprite, Color Tint, string Layout, SDUIEdges Padding)
{
    public static SpriteLayer QuestionIcon =>
        ModEntry.Config.AltQuestionMark
            ? FromSprite(new(Game1.mouseCursors, new Rectangle(240, 192, 16, 16)))
            : new(new(Game1.mouseCursors, new Rectangle(175, 425, 12, 12)), Color.White, "48px 48px", new(8));

    public static SpriteLayer FromSprite(SDUISprite sprite, int width = 64) =>
        new(sprite, Color.White, $"{width}px {width}px", SDUIEdges.NONE);

    public static IEnumerable<SpriteLayer> FromItem(Item? reprItem, float scale = 4)
    {
        if (reprItem == null)
        {
            yield return QuestionIcon;
            yield break;
        }
        int equiv64 = (int)(16 * scale);
        int equiv32 = (int)(8 * scale);
        ParsedItemData data = ItemRegistry.GetData(reprItem.QualifiedItemId);
        // shirts are 8x8
        if (reprItem is Clothing clothes && clothes.clothesType.Value == Clothing.ClothesType.SHIRT)
        {
            Texture2D texture = data.GetTexture();
            Rectangle sourceRect = data.GetSourceRect();
            yield return new(new(texture, sourceRect), Color.White, $"{equiv32}px {equiv32}px", new((int)(4 * scale)));
            Rectangle layerRect =
                new(sourceRect.X + texture.Width / 2, sourceRect.Y, sourceRect.Width, sourceRect.Height);
            yield return new(new(texture, layerRect), Color.White, $"{equiv32}px {equiv32}px", new((int)(4 * scale)));
            yield break;
        }
        yield return FromSprite(new(data.GetTexture(), data.GetSourceRect()), equiv64);
        // colored items
        if (reprItem is ColoredObject coloredObject && coloredObject.GetPreservedItemId() != null)
        {
            yield return new(
                new(data.GetTexture(), data.GetSourceRect(coloredObject.ColorSameIndexAsParentSheetIndex ? 0 : 1)),
                coloredObject.color.Value,
                $"{equiv64}px {equiv64}px",
                SDUIEdges.NONE
            );
        }
    }
}

public sealed record SubItemIcon(Item Item)
{
    public IEnumerable<SpriteLayer> SpriteLayers => SpriteLayer.FromItem(Item);
    public readonly SDUITooltipData Tooltip = new(Item.getDescription(), Item.DisplayName, Item);
}

public sealed partial record InputIcon(Item InputItem)
{
    public IEnumerable<SpriteLayer> SpriteLayers => SpriteLayer.FromItem(InputItem);
    public readonly SDUITooltipData Tooltip = new(InputItem.getDescription(), InputItem.DisplayName, InputItem);

    [Notify]
    private bool state = true;

    public void ToggleState() => State = !State;

    [Notify]
    private bool activeByRule = true;

    [Notify]
    private bool activeByGlobal = true;

    public Color Tint
    {
        get
        {
            Color result = State ? Color.White : ControlPanelContext.DisabledColor;
            if (!ActiveByRule || !ActiveByGlobal)
                result *= 0.5f;
            return result;
        }
    }
    internal readonly Dictionary<RuleIdent, bool> OriginRules = [];
}

public sealed partial record QualityStar(int Quality)
{
    public Tuple<Texture2D, Rectangle>? Sprite =>
        Quality switch
        {
            0 or 1 => new(Game1.mouseCursors, new Rectangle(338, 400, 8, 8)),
            2 => new(Game1.mouseCursors, new Rectangle(346, 400, 8, 8)),
            4 => new(Game1.mouseCursors, new Rectangle(346, 392, 8, 8)),
            _ => null,
        };

    [Notify]
    private bool state = true;

    public void ToggleState() => State = !State;

    public Color? Tint
    {
        get
        {
            if (!State)
                return ControlPanelContext.DisabledColor;
            return Quality switch
            {
                0 => Color.Black * 0.5f,
                _ => Color.White,
            };
        }
    }
}

public record RuleIcon(IconDef IconDef)
{
    private const string EMC_HOLDER = "selph.ExtraMachineConfig.Holder";
    public readonly Item? ReprItem = GetReprItem(IconDef);

    private static Item? GetReprItem(IconDef IconDef)
    {
        if (IconDef.Items?.FirstOrDefault() is not Item reprItem)
            return null;
        if (reprItem.ItemId == EMC_HOLDER && IconDef is IconOutputDef iod && iod.EMCByproduct?.Count > 0)
        {
            foreach (IconDef iconD in iod.EMCByproduct)
            {
                if (iconD.Items?.FirstOrDefault() is Item byproductItem)
                    return byproductItem;
            }
        }
        return reprItem;
    }

    public IEnumerable<SpriteLayer> SpriteLayers => SpriteLayer.FromItem(ReprItem);

    public IEnumerable<SpriteLayer> EMCByProductOneItem
    {
        get
        {
            if (
                IconDef is IconOutputDef iod
                && iod.EMCByproduct?.Count == 1
                && iod.EMCByproduct?.FirstOrDefault()?.Items?.Count == 1
                && iod.EMCByproduct?.FirstOrDefault()?.Items?.FirstOrDefault() is Item byproductItem
            )
            {
                return SpriteLayer.FromItem(byproductItem, 2);
            }
            return [];
        }
    }

    public SDUITooltipData? Tooltip
    {
        get
        {
            string? desc = IconDef.Desc;
            if (IconDef.Items?.Count != 1)
            {
                if (desc != null)
                    return new(desc);
                return null;
            }
            else if (ReprItem != null)
            {
                if (desc != null)
                    return new(desc, ReprItem.DisplayName.Trim(), ReprItem);
                return new(ReprItem.getDescription(), ReprItem.DisplayName, ReprItem);
            }
            return null;
        }
    }

    public int Count => IconDef.Count;
    public bool ShowCount => Count > 1;

    public bool IsMulti => SubItems?.Count() > 1;
    public float Opacity => IsMulti ? 0.6f : 1f;

    public bool HasQualityStar => QualityStar != null;
    public Tuple<Texture2D, Rectangle>? QualityStar =>
        IconDef.Quality switch
        {
            1 => new(Game1.mouseCursors, new Rectangle(338, 400, 8, 8)),
            2 => new(Game1.mouseCursors, new Rectangle(346, 400, 8, 8)),
            4 => new(Game1.mouseCursors, new Rectangle(346, 392, 8, 8)),
            _ => null,
        };

    public bool IsFuel => IconDef.IsFuel;
    private IList<SubItemIcon>? SubItems
    {
        get
        {
            if (IconDef.Items != null && IconDef.Items.Count > 1)
                return IconDef.Items.Select(item => new SubItemIcon(item)).ToList();
            else if (IconDef is IconOutputDef iod && iod.EMCByproduct != null && iod.EMCByproduct.Count > 1)
                return iod
                    .EMCByproduct.SelectMany(iconD => (iconD.Items ?? []).Select(item => new SubItemIcon(item)))
                    .ToList();
            return null;
        }
    }

    public void ShowSubItemGrid()
    {
        if (EMCByProductOneItem.Any())
            return;
        MenuHandler.ShowSubItemGrid(SubItems);
    }
}

public sealed partial record RuleInputEntry(RuleDef Def)
{
    private const int SPINNING_CARET_FRAMES = 6;
    private static readonly List<Tuple<Texture2D, Rectangle>> SpinningCaretFrames = Enumerable
        .Range(0, SPINNING_CARET_FRAMES)
        .Select<int, Tuple<Texture2D, Rectangle>>(frame => new(Game1.mouseCursors, new(232 + 9 * frame, 346, 9, 9)))
        .ToList();

    [Notify]
    private bool state = true;

    [Notify]
    private bool active = true;

    [Notify]
    private int currCaretFrame = 0;

    public Tuple<Texture2D, Rectangle> SpinningCaret =>
        State ? SpinningCaretFrames[CurrCaretFrame] : SpinningCaretFrames[0];

    internal TimeSpan animTimer = TimeSpan.Zero;
    private readonly TimeSpan animInterval = TimeSpan.FromMilliseconds(90);

    internal void UpdateCaret(TimeSpan elapsed)
    {
        animTimer += elapsed;
        if (animTimer >= animInterval)
        {
            CurrCaretFrame = (currCaretFrame + 1) % SPINNING_CARET_FRAMES;
            animTimer = TimeSpan.Zero;
        }
    }

    internal void ResetCaret()
    {
        CurrCaretFrame = 0;
        animTimer = TimeSpan.Zero;
    }
}

public sealed record RuleOutputEntry(RuleInputEntry RIE, IconOutputDef IOD) : INotifyPropertyChanged
{
    private const int ICON_SIZE = 76;

    public bool State
    {
        get => RIE.State;
        set => RIE.State = value;
    }

    public bool Active => RIE.Active;

    public float Opacity => State && Active ? 1f : 0.6f;

    public Tuple<Texture2D, Rectangle> SpinningCaret => RIE.SpinningCaret;

    public IEnumerable<RuleIcon> Inputs
    {
        get
        {
            yield return new(RIE.Def.Input);
            if (RIE.Def.SharedFuel != null)
                foreach (var iconD in RIE.Def.SharedFuel)
                    yield return new(iconD);
            if (IOD.EMCFuel != null)
                foreach (var iconD in IOD.EMCFuel)
                    yield return new(iconD);
        }
    }

    public IEnumerable<RuleIcon> Outputs
    {
        get
        {
            yield return new(IOD);
            if (IOD.SameGroupOutputs != null)
                foreach (var sgoIOD in IOD.SameGroupOutputs)
                    yield return new(sgoIOD);
        }
    }

    public int InputLength => 1 + (RIE.Def.SharedFuel?.Count ?? 0) + (IOD.EMCFuel?.Count ?? 0);
    public int OutputLength => 1 + (IOD.SameGroupOutputs?.Count ?? 0);

    // public int spacerLength = 0;
    public int SpacerLength = 0;
    public string InputSpacerLayout => SpacerLength > 0 ? $"{ICON_SIZE}px {ICON_SIZE * SpacerLength}px" : "0px 0px";

    public event PropertyChangedEventHandler? PropertyChanged;

    public void SetupNotify() => RIE.PropertyChanged += RIEPropertyChanged;

    private void RIEPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(RuleInputEntry.State):
            case nameof(RuleInputEntry.Active):
                PropertyChanged?.Invoke(this, new(e.PropertyName));
                PropertyChanged?.Invoke(this, new(nameof(Opacity)));
                break;
            case nameof(RuleInputEntry.CurrCaretFrame):
                PropertyChanged?.Invoke(this, new(nameof(SpinningCaret)));
                break;
        }
    }
}

public sealed partial record ControlPanelContext(Item Machine, IReadOnlyList<RuleIdentDefPair> RuleDefs)
{
    internal const int RULE_ITEM_ROW_CNT = 14;
    internal static Color DisabledColor = Color.Black * 0.8f;
    public GlobalToggleContext GlobalToggle => MenuHandler.GlobalToggle;

    internal static ControlPanelContext? TryCreate(Item machine)
    {
        if (MachineRuleCache.TryGetRuleDefList(machine.QualifiedItemId) is IReadOnlyList<RuleIdentDefPair> ruleDefs)
        {
            ControlPanelContext context = new(machine, ruleDefs);
            MenuHandler.GlobalToggle.PropertyChanged += context.RecheckSavedStates;
            context.RecheckSavedStates();
            return context;
        }
        return null;
    }

    public readonly string MachineName = Machine.DisplayName;
    public readonly ParsedItemData MachineData = ItemRegistry.GetData(Machine.QualifiedItemId);
    public readonly SDUITooltipData MachineTooltip = new(Machine.getDescription(), Machine.DisplayName, Machine);

    [Notify]
    public int pageIndex = (int)ModEntry.Config.DefaultPage;

    private void CheckInputIconActiveState(IEnumerable<InputIcon> inputItems)
    {
        foreach (InputIcon input in inputItems)
        {
            foreach (RuleIdent ident in input.OriginRules.Keys)
            {
                if (ruleEntries.TryGetValue(ident, out RuleInputEntry? ruleEntry))
                {
                    input.OriginRules[ident] = ruleEntry.State;
                }
            }
            input.ActiveByRule = input.OriginRules.Values.Any(val => val);
        }
    }

    /// <summary>Event binding, change current page</summary>
    /// <param name="page"></param>
    public void ChangePage(int page)
    {
        if (page == 2)
        {
            CheckInputIconActiveState(InputItems);
        }
        PageIndex = page;
    }

    public ValueTuple<int, int, int, int> TabMarginRules => PageIndex == 1 ? new(0, 0, 0, 0) : new(0, 0, 0, 8);
    public ValueTuple<int, int, int, int> TabMarginInputs => PageIndex == 2 ? new(0, 0, 0, 0) : new(0, 0, 0, 8);

    [Notify]
    private string searchText = "";

    private readonly Dictionary<RuleIdent, RuleInputEntry> ruleEntries = RuleDefs.ToDictionary(
        kv => kv.Ident,
        kv => new RuleInputEntry(kv.Def)
        {
            State = ModEntry.SaveData.RuleState(
                Machine.QualifiedItemId,
                MenuHandler.GlobalToggle.LocationKey,
                kv.Ident
            ),
        }
    );

    public IEnumerable<RuleOutputEntry> RuleEntriesFiltered
    {
        get
        {
            int rowCnt = RULE_ITEM_ROW_CNT;
            int maxInputLength = 0;
            List<RuleOutputEntry> ruleEntriesFiltered = [];
            foreach (var rie in ruleEntries.Values)
            {
                foreach (var output in rie.Def.Outputs)
                {
                    if (
                        !string.IsNullOrEmpty(SearchText)
                        && !rie.Def.Input.Match(SearchText)
                        && !output.Match(SearchText)
                    )
                        continue;
                    RuleOutputEntry ROE = new(rie, output);
                    ROE.SetupNotify();
                    ruleEntriesFiltered.Add(ROE);
                    maxInputLength = Math.Max(maxInputLength, ROE.InputLength);
                    rowCnt--;
                    if (rowCnt == 0)
                    {
                        for (int idx = RULE_ITEM_ROW_CNT; idx > 0; idx--)
                        {
                            ruleEntriesFiltered[^idx].SpacerLength =
                                maxInputLength - ruleEntriesFiltered[^idx].InputLength;
                        }
                        rowCnt = RULE_ITEM_ROW_CNT;
                        maxInputLength = 0;
                    }
                }
            }
            for (int idx = RULE_ITEM_ROW_CNT - rowCnt; idx > 0; idx--)
            {
                ruleEntriesFiltered[^idx].SpacerLength = maxInputLength - ruleEntriesFiltered[^idx].InputLength;
            }
            return ruleEntriesFiltered;
        }
    }

    public RuleInputEntry? HoverRuleEntry = null;

    public void HandleHoverRuleEntry(RuleInputEntry? newHover = null)
    {
        HoverRuleEntry?.ResetCaret();
        HoverRuleEntry = newHover;
    }

    private List<InputIcon> GetInputItems()
    {
        Dictionary<string, InputIcon> seenItem = [];
        List<InputIcon> inputItems = [];
        foreach ((RuleIdent ident, RuleDef def) in RuleDefs)
        {
            if (def.Input.Items == null)
                continue;
            foreach (Item item in def.Input.Items)
            {
                if (seenItem.TryGetValue(item.QualifiedItemId, out InputIcon? inputIcon))
                {
                    inputIcon.OriginRules[ident] = false;
                    continue;
                }
                inputIcon = new(item);
                inputIcon.OriginRules[ident] = false;
                inputIcon.State = ModEntry.SaveData.InputState(
                    Machine.QualifiedItemId,
                    MenuHandler.GlobalToggle.LocationKey,
                    item.QualifiedItemId
                );
                seenItem[item.QualifiedItemId] = inputIcon;
                inputItems.Add(inputIcon);
            }
        }
        CheckInputIconActiveState(inputItems);
        return inputItems;
    }

    private List<InputIcon>? inputItems = null;
    private List<InputIcon> InputItems => inputItems ??= GetInputItems();
    public bool HasInputs => InputItems.Any();
    public IEnumerable<InputIcon> InputItemsFiltered =>
        InputItems.Where((ipt) => string.IsNullOrEmpty(SearchText) || ipt.InputItem.DisplayName.Contains(SearchText));
    public QualityStar[] QualityStars =>
        [new QualityStar(0), new QualityStar(1), new QualityStar(2), new QualityStar(4)];

    public void Update(TimeSpan elapsed) => HoverRuleEntry?.UpdateCaret(elapsed);

    public void RecheckSavedStates(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != "IsGlobal")
            return;
        SaveChanges(MenuHandler.GlobalToggle.NotLocationKey);
        RecheckSavedStates();
    }

    public void RecheckSavedStates()
    {
        foreach (var kv in ruleEntries)
        {
            kv.Value.State = ModEntry.SaveData.RuleState(
                Machine.QualifiedItemId,
                MenuHandler.GlobalToggle.LocationKey,
                kv.Key
            );
        }
        foreach (var inputIcon in InputItems)
        {
            inputIcon.State = ModEntry.SaveData.InputState(
                Machine.QualifiedItemId,
                MenuHandler.GlobalToggle.LocationKey,
                inputIcon.InputItem.QualifiedItemId
            );
        }
        if (MenuHandler.GlobalToggle.LocationKey == null)
        {
            foreach (var kv in ruleEntries)
                kv.Value.Active = true;
            foreach (var inputIcon in InputItems)
                inputIcon.ActiveByGlobal = true;
        }
        else
        {
            foreach (var kv in ruleEntries)
            {
                kv.Value.Active = ModEntry.SaveData.RuleState(Machine.QualifiedItemId, null, kv.Key);
            }
            foreach (var inputIcon in InputItems)
            {
                inputIcon.ActiveByGlobal = ModEntry.SaveData.InputState(
                    Machine.QualifiedItemId,
                    null,
                    inputIcon.InputItem.QualifiedItemId
                );
            }
            if (PageIndex == 2)
            {
                CheckInputIconActiveState(InputItems);
            }
        }
    }

    internal void SaveChanges(string? locationKey) =>
        ModEntry.SaveMachineRules(
            Machine.QualifiedItemId,
            locationKey,
            ruleEntries.Where(kv => !kv.Value.State).Select(kv => kv.Key),
            InputItems.Where(v => !v.State).Select(v => v.InputItem.QualifiedItemId),
            [!QualityStars[0].State, !QualityStars[1].State, !QualityStars[2].State, !QualityStars[3].State]
        );

    internal void Closing()
    {
        SaveChanges(MenuHandler.GlobalToggle.LocationKey);
        MenuHandler.GlobalToggle.PropertyChanged -= RecheckSavedStates;
    }
}
