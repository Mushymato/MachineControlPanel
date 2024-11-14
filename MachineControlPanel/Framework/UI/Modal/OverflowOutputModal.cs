using Microsoft.Xna.Framework;
using StardewUI;
using StardewUI.Events;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Overlays;
using StardewUI.Widgets;
using StardewValley;

namespace MachineControlPanel.Framework.UI.Modal;

internal sealed class OverflowOutputModalButton : Button
{
    private readonly List<IView> outputPanels;
    private static readonly Sprite bgSprite =
        new(Game1.mouseCursors, new Rectangle(392, 361, 10, 11));
    private static readonly Sprite bgHoverSprite =
        new(Game1.mouseCursors, new Rectangle(402, 361, 10, 11));

    public OverflowOutputModalButton(List<IView> outputPanels)
        : base()
    {
        this.outputPanels = outputPanels;
        DefaultBackground = bgSprite;
        HoverBackground = bgHoverSprite;
        LeftClick += OpenOutputModal;
        Layout = LayoutParameters.FixedSize(40, 44);
        Margin = new(16, 14);
        Tooltip = I18n.RuleList_MoreOutputs(outputPanels.Count);
    }

    private void OpenOutputModal(object? sender, ClickEventArgs e)
    {
        var overlay = new ScrollableGridModal(outputPanels);
        Overlay.Push(overlay);
    }
}
