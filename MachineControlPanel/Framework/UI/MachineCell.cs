using MachineControlPanel.Framework.UI.Integration;
using Microsoft.Xna.Framework;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI
{
    /// <summary>
    /// Holds some info for launching the corresponding machine panel
    /// </summary>
    internal class MachineCell : HoveredItemPanel
    {
        internal static readonly Sprite bgSprite =
            new(Game1.mouseCursors, new(384, 396, 15, 15), new(5), new(Scale: 4));
        internal RuleHelper ruleHelper;
        private readonly Frame innerFrame;

        internal MachineCell(RuleHelper ruleHelper, ParsedItemData itemData)
            : base()
        {
            this.ruleHelper = ruleHelper;
            innerFrame = new Frame()
            {
                Padding = new(16),
                Background = bgSprite,
                BorderThickness = bgSprite.FixedEdges!,
                Tooltip = ruleHelper.Name,
                Focusable = true,
                Content = new Image()
                {
                    Sprite = new(itemData.GetTexture(), itemData.GetSourceRect()),
                    Layout = LayoutParameters.FixedSize(64, 128),
                    ShadowAlpha = 1,
                    Focusable = false,
                },
            };
            Children = [innerFrame];
            UpdateEdited();
            HoveredItem = ItemRegistry.Create(itemData.QualifiedItemId);
        }

        /// <summary>
        /// Draw BG transparent if no rules were set for this machine
        /// </summary>
        internal void UpdateEdited()
        {
            innerFrame.BackgroundTint = Color.White * (ruleHelper.HasDisabled ? 1 : 0.5f);
        }
    }
}
