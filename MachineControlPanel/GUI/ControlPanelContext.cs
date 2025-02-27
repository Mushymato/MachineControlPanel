using System.Text;
using MachineControlPanel.Data;
using MachineControlPanel.GUI.Includes;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PropertyChanged.SourceGenerator;
using StardewValley;
using StardewValley.GameData.HomeRenovations;
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
            StringBuilder sb = new();
            if (IconDef.ContextTags != null)
                sb.AppendJoin('\n', IconDef.ContextTags);
            if (IconDef.Condition != null)
                sb.AppendLine(IconDef.Condition);
            if (IconDef.Notes != null)
            {
                if (sb.Length > 0)
                    sb.Append('\n');
                sb.AppendJoin('\n', IconDef.Notes);
            }
            if (ReprItem != null)
                return new(sb.Length > 0 ? sb.ToString() : ReprItem.getDescription(), ReprItem.DisplayName, ReprItem);
            else
                return sb.Length > 0 ? new(sb.ToString()) : null;
        }
    }

    public int Count => IconDef.Count;
    public bool ShowCount => Count > 1;
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

    private TimeSpan animTimer = TimeSpan.Zero;
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
