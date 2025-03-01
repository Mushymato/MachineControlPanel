using MachineControlPanel.Data;
using MachineControlPanel.GUI.Includes;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PropertyChanged.SourceGenerator;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

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
            Color result = State ? Color.White * 1f : Color.Black * 0.8f;
            if (!active)
                result *= 0.5f;
            return result;
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
    public Color IsMultiTint => IsMulti ? Color.White * 0.5f : Color.White;

    public Tuple<Texture2D, Rectangle>? QualityStar =>
        IconDef.Quality switch
        {
            1 => new(Game1.mouseCursors, new Rectangle(338, 400, 8, 8)),
            2 => new(Game1.mouseCursors, new Rectangle(346, 400, 8, 8)),
            4 => new(Game1.mouseCursors, new Rectangle(346, 392, 8, 8)),
            _ => null,
        };
}

public sealed partial record RuleEntry(RuleIdent Ident, RuleDef Def)
{
    public const int SPINNING_CARET_FRAMES = 6;
    public RuleIcon Input = new(Def.Input);
    public IEnumerable<RuleIcon> Outputs = Def.Outputs.Select<IconDef, RuleIcon>(iconD => new(iconD));

    [Notify]
    private int spinningCaretFrame = 0;

    public Tuple<Texture2D, Rectangle> SpinningCaret =>
        new(Game1.mouseCursors, new(232 + 9 * SpinningCaretFrame, 346, 9, 9));

    internal TimeSpan animTimer = TimeSpan.Zero;
    private readonly TimeSpan animInterval = TimeSpan.FromMilliseconds(90);

    internal void Update(TimeSpan elapsed)
    {
        animTimer += elapsed;
        if (animTimer >= animInterval)
        {
            SpinningCaretFrame = (spinningCaretFrame + 1) % SPINNING_CARET_FRAMES;
            animTimer = TimeSpan.Zero;
        }
    }

    internal void ResetCaret()
    {
        SpinningCaretFrame = 0;
        animTimer = TimeSpan.Zero;
    }
}

public sealed partial record ControlPanelContext(
    Item Machine,
    GlobalToggleContext GlobalToggle,
    IReadOnlyList<RuleIdentDefPair> RuleDefs
)
{
    internal static ControlPanelContext? TryCreate(Item machine, GlobalToggleContext? globalToggle)
    {
        if (MachineRuleCache.TryGetRuleDefList(machine.QualifiedItemId) is IReadOnlyList<RuleIdentDefPair> ruleDefs)
            return new ControlPanelContext(machine, globalToggle ?? new GlobalToggleContext(), ruleDefs);
        return null;
    }

    public readonly string MachineName = Machine.DisplayName;
    public readonly ParsedItemData MachineData = ItemRegistry.GetData(Machine.QualifiedItemId);
    public readonly SDUITooltipData MachineTooltip = new(Machine.getDescription(), Machine.DisplayName, Machine);

    [Notify]
    public int pageIndex = (int)ModEntry.Config.DefaultPage;

    /// <summary>Event binding, change current page</summary>
    /// <param name="page"></param>
    public void ChangePage(int page) => PageIndex = page;

    public ValueTuple<int, int, int, int> TabMarginRules => PageIndex == 1 ? new(0, 0, 0, 0) : new(0, 0, 0, 8);
    public ValueTuple<int, int, int, int> TabMarginInputs => PageIndex == 2 ? new(0, 0, 0, 0) : new(0, 0, 0, 8);

    [Notify]
    private string searchText = "";

    public IEnumerable<InputIcon> InputItems
    {
        get
        {
            HashSet<string> seenItem = [];
            foreach (var pair in RuleDefs)
            {
                if (pair.Def.Input.Items == null)
                    continue;
                foreach (Item item in pair.Def.Input.Items)
                {
                    if (seenItem.Contains(item.QualifiedItemId))
                        continue;
                    seenItem.Add(item.QualifiedItemId);
                    yield return new InputIcon(item);
                }
            }
        }
    }

    public IEnumerable<RuleEntry> RuleEntries => RuleDefs.Select((kv) => new RuleEntry(kv.Ident, kv.Def));

    public RuleEntry? HoverRuleEntry = null;

    public void HandleHoverRuleEntry(RuleEntry? newHover = null)
    {
        HoverRuleEntry?.ResetCaret();
        HoverRuleEntry = newHover;
    }

    public void Update(TimeSpan elapsed)
    {
        HoverRuleEntry?.Update(elapsed);
    }
}
