using MachineControlPanel.Framework.UI.Modal;
using StardewUI;
using StardewUI.Events;
using StardewUI.Layout;
using StardewUI.Overlays;
using StardewUI.Widgets;
using StardewValley;

namespace MachineControlPanel.Framework.UI.Integration;

/// <summary>
/// Integration with lookup anything, provide a panel with an Item to use for LA
/// </summary>
internal class HoveredItemPanel : Panel
{
    internal Item? HoveredItem { get; set; } = null;
    internal string? ExtraItemsHeading { get; set; } = null;
    private IList<IView>? extraItems;
    internal IList<IView>? ExtraItems
    {
        get => extraItems;
        set
        {
            extraItems = value;
            if (extraItems == null)
                LeftClick -= ShowExtraOutputsOverlay;
            else
                LeftClick += ShowExtraOutputsOverlay;
        }
    }

    internal void ShowExtraOutputsOverlay(object? sender, ClickEventArgs e)
    {
        Overlay.Push(new ScrollableGridModal(ExtraItems!) { Heading = ExtraItemsHeading });
    }
}
