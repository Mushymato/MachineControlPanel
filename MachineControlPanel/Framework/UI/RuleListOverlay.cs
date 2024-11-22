using MachineControlPanel.Framework.UI.Integration;
using StardewUI.Overlays;
using StardewUI.Widgets;

namespace MachineControlPanel.Framework.UI;

internal sealed class RuleListOverlay(
    RuleHelper ruleHelper,
    Action<string, IEnumerable<RuleIdent>, IEnumerable<string>, bool[]> saveMachineRules,
    Action<HoveredItemPanel>? setHoverEvents = null,
    Action? updateEdited = null
) : FullScreenOverlay
{
    protected override RuleListView CreateView()
    {
        if (ModEntry.Config.SaveOnChange)
            Close += OnClose;
        return new(ruleHelper, saveMachineRules, setHoverEvents: setHoverEvents, updateEdited: updateEdited);
    }

    /// <summary>Save on close handler</summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    internal void OnClose(object? sender, EventArgs e)
    {
        if (View is Frame frame && frame.Content is RuleListView ruleListView)
        {
            ruleListView.SaveAllRules();
            updateEdited?.Invoke();
        }
    }
}
