using MachineControlPanel.Integration;
using StardewValley;

namespace MachineControlPanel.GUI.Includes;

public sealed record SubItemIcon(Item Item)
{
    public IEnumerable<SpriteLayer> SpriteLayers => SpriteLayer.FromItem(Item);
    public readonly SDUITooltipData Tooltip = new(Item.getDescription(), Item.DisplayName, Item);
}

/// <summary>Context for subitem grid</summary>
public sealed record SubitemGridContext(string Header, List<SubItemIcon> SubItems)
{
    private const int ICON_SIZE = 76;
    private const int HEADING = 38;
    private const int COL_CNT = 8;

    public void SetHover(SubItemIcon? subItem = null) => MenuHandler.HoveredItem = subItem?.Item;

    public string SubitemLayout
    {
        get
        {
            if (SubItems.Count <= COL_CNT)
                return $"{ICON_SIZE * Math.Max(SubItems.Count, 2)}px {ICON_SIZE + HEADING}px";
            int neededHeight =
                (int)Math.Min(Game1.viewport.Height * 0.6, ICON_SIZE * (1 + SubItems.Count / 8)) + HEADING;
            return $"{ICON_SIZE * COL_CNT}px {neededHeight}px";
        }
    }

    public List<SubItemIcon> SubItemsFiltered => SubItems.GetRange(0, Math.Min(SubItems.Count, 50));
}
