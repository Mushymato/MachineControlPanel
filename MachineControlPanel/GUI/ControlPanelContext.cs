using MachineControlPanel.Data;
using MachineControlPanel.GUI.Includes;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PropertyChanged.SourceGenerator;
using StardewValley;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Menus;

namespace MachineControlPanel.GUI;

public sealed partial record InputIcon(Item InputItem)
{
    public readonly ParsedItemData ItemData = ItemRegistry.GetData(InputItem.QualifiedItemId);
    public readonly SDUITooltipData Tooltip = new(InputItem.getDescription(), InputItem.DisplayName, InputItem);

    [Notify]
    private bool state = true;

    public void ToggleState() => State = !State;

    [Notify]
    private bool active = true;
    public Color Tint
    {
        get
        {
            Color result = State ? Color.White : ControlPanelContext.DisabledColor;
            if (!Active)
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

public sealed record RuleIcon(IconDef IconDef)
{
    public static SDUISprite QuestionIcon =>
        ModEntry.Config.AltQuestionMark
            ? new(Game1.mouseCursors, new Rectangle(240, 192, 16, 16))
            : new(Game1.mouseCursors, new Rectangle(175, 425, 12, 12));
    public readonly Item? ReprItem = IconDef.Items?.FirstOrDefault();
    public SDUISprite Sprite => ReprItem == null ? QuestionIcon : SDUISprite.FromItem(ReprItem);
    public SDUITooltipData? Tooltip
    {
        get
        {
            string? desc = IconDef.Desc;
            if (desc != null)
                return new(desc);
            if (ReprItem != null)
                return new(ReprItem.getDescription(), ReprItem.DisplayName.Trim(), ReprItem);
            return null;
        }
    }

    public int Count => IconDef.Count;
    public bool ShowCount => Count > 1;

    public bool IsMulti => IconDef.Items?.Count > 1;
    public float IsMultiOpacity => IsMulti ? 0.75f : 1f;

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
}

public sealed partial record RuleEntry(RuleDef Def)
{
    private const int SPINNING_CARET_FRAMES = 6;
    private static readonly List<Tuple<Texture2D, Rectangle>> SpinningCaretFrames = Enumerable
        .Range(0, SPINNING_CARET_FRAMES)
        .Select<int, Tuple<Texture2D, Rectangle>>(frame => new(Game1.mouseCursors, new(232 + 9 * frame, 346, 9, 9)))
        .ToList();

    public IEnumerable<RuleIcon> Input = [new(Def.Input)];
    public IEnumerable<RuleIcon> Fuel = Def.SharedFuel?.Select<IconDef, RuleIcon>(iconD => new(iconD)) ?? [];
    public IEnumerable<RuleIcon> Outputs = Def.Outputs.Select<IconDef, RuleIcon>(iconD => new(iconD));

    [Notify]
    private bool state = true;
    public float StateOpacity => State ? 1f : 0.75f;
    public Color StateTint => State ? Color.White : ControlPanelContext.DisabledColor;

    [Notify]
    private int currCaretFrame = 0;

    public Tuple<Texture2D, Rectangle> SpinningCaret =>
        State ? SpinningCaretFrames[CurrCaretFrame] : SpinningCaretFrames[0];

    internal TimeSpan animTimer = TimeSpan.Zero;
    private readonly TimeSpan animInterval = TimeSpan.FromMilliseconds(90);

    internal void Update(TimeSpan elapsed)
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

public sealed partial record ControlPanelContext(Item Machine, IReadOnlyList<RuleIdentDefPair> RuleDefs)
{
    internal static Color DisabledColor = Color.Black * 0.8f;
    public GlobalToggleContext GlobalToggle => MenuHandler.GlobalToggle;

    internal static ControlPanelContext? TryCreate(Item machine)
    {
        if (MachineRuleCache.TryGetRuleDefList(machine.QualifiedItemId) is IReadOnlyList<RuleIdentDefPair> ruleDefs)
            return new ControlPanelContext(machine, ruleDefs);
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
                if (ruleEntries.TryGetValue(ident, out RuleEntry? ruleEntry))
                {
                    input.OriginRules[ident] = ruleEntry.State;
                }
            }
            input.Active = input.OriginRules.Values.Any(val => val);
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

    private readonly Dictionary<RuleIdent, RuleEntry> ruleEntries = RuleDefs.ToDictionary(
        kv => kv.Ident,
        kv => new RuleEntry(kv.Def) { State = ModEntry.SaveData.RuleState(Machine.QualifiedItemId, kv.Ident) }
    );

    public IEnumerable<RuleEntry> RuleEntriesFiltered => ruleEntries.Values;

    public RuleEntry? HoverRuleEntry = null;

    public void HandleHoverRuleEntry(RuleEntry? newHover = null)
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
                inputIcon.State = ModEntry.SaveData.InputState(Machine.QualifiedItemId, item.QualifiedItemId);
                seenItem[item.QualifiedItemId] = inputIcon;
                inputItems.Add(inputIcon);
            }
        }
        CheckInputIconActiveState(inputItems);
        return inputItems;
    }

    private List<InputIcon>? inputItems = null;
    private List<InputIcon> InputItems => inputItems ??= GetInputItems();
    public IEnumerable<InputIcon> InputItemsFiltered => InputItems;
    public QualityStar[] QualityStars =>
        [new QualityStar(0), new QualityStar(1), new QualityStar(2), new QualityStar(4)];

    public void Update(TimeSpan elapsed) => HoverRuleEntry?.Update(elapsed);

    internal void SaveChanges() =>
        ModEntry.SaveMachineRules(
            Machine.QualifiedItemId,
            GlobalToggle.LocationKey,
            ruleEntries.Where(kv => !kv.Value.State).Select(kv => kv.Key),
            InputItems.Where(v => !v.State).Select(v => v.InputItem.QualifiedItemId),
            [!QualityStars[0].State, !QualityStars[1].State, !QualityStars[2].State, !QualityStars[3].State]
        );
}
