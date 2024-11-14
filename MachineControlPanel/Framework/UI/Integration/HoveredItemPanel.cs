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
        Overlay.Push(new ScrollableGridModal(ExtraItems!) { Heading = I18n.RuleList_Byproducts() });
    }

    // internal void AddExtraOutputsDisplay(IList<IView> extraItems)
    // {
    //     ExtraItemsFrame = new Frame()
    //     {
    //         Border = RuleListView.OutputGroup,
    //         BorderThickness = RuleListView.OutputGroup.FixedEdges!,
    //         Visibility = Visibility.Hidden,
    //         ZIndex = 2,
    //         Margin = new Edges(0, -80, 0, 0),
    //         Content = new Lane() { Orientation = Orientation.Horizontal, Children = extraItems },
    //     };
    //     // FloatingElements.Add(new(extraItemsFrame, FloatingPosition.AboveParent));
    //     // this.Children.Add(extraItemsFrame);
    // }

    // internal void ShowExtraItems()
    // {
    //     if (extraItemsFrame != null)
    //         extraItemsFrame.Visibility = Visibility.Visible;
    // }

    // internal void HideExtraItems()
    // {
    //     if (extraItemsFrame != null)
    //         extraItemsFrame.Visibility = Visibility.Hidden;
    // }
}
