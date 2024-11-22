using MachineControlPanel.Framework.UI.Integration;

namespace MachineControlPanel.Framework.UI;

internal sealed class MachineMenu(Action<string, IEnumerable<RuleIdent>, IEnumerable<string>, bool[]> saveMachineRules) : HoveredItemMenu<MachineSelect>
{
    protected override MachineSelect CreateView()
    {
        return new(saveMachineRules, exitThisMenu: exitThisMenu, ModEntry.HasLookupAnying ? SetHoverEvents : null);
    }
}
