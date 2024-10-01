using MachineControlPanel.Framework.UI.Integration;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class RuleListMenu(
        RuleHelper ruleHelper,
        Action<string, IEnumerable<RuleIdent>, IEnumerable<string>> saveMachineRules,
        bool showExitX = false,
        Action? updateEdited = null
    ) : HoveredItemMenu<RuleListView>
    {
        protected override RuleListView CreateView()
        {
            return new(
                ruleHelper,
                saveMachineRules,
                showExitX ? exitThisMenu : null,
                ModEntry.HasLookupAnying ? SetHoverEvents : null,
                updateEdited: updateEdited
            );
        }

        /// <summary>
        /// Autosave changes when closing the control panel.
        /// </summary>
        protected override void cleanupBeforeExit()
        {
            if (ModEntry.Config.SaveOnChange)
                View.SaveAllRules();
        }

    }
}