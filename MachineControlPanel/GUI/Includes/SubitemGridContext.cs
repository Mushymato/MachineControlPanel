using MachineControlPanel.Integration;
using PropertyChanged.SourceGenerator;
using StardewValley;

namespace MachineControlPanel.GUI.Includes;

public sealed record SubItemIcon(Item Item)
{
    public IEnumerable<SpriteLayer> SpriteLayers => SpriteLayer.FromItem(Item);
    public readonly SDUITooltipData Tooltip = new(Item.getDescription(), Item.DisplayName, Item);
}

/// <summary>Context for subitem grid</summary>
public sealed partial record SubitemGridContext(string Header, List<SubItemIcon> SubItems)
{
    private const int ICON_SIZE = 84;
    private const int HEADING = 34;
    private const int COL_CNT = 8;

    public void SetHover(SubItemIcon? subItem = null) => MenuHandler.HoveredItem = subItem?.Item;

    public string SubitemLayout
    {
        get
        {
            if (SubItems.Count <= COL_CNT)
                return $"{ICON_SIZE * Math.Max(SubItems.Count, 2)}px {ICON_SIZE + HEADING}px";
            int neededHeight = (int)
                Math.Min(Game1.viewport.Height * 0.6, ICON_SIZE * Math.Ceiling((float)SubItems.Count / COL_CNT));
            return $"{ICON_SIZE * COL_CNT}px {neededHeight}px";
        }
    }

    [Notify]
    private int paged = 1;

    public List<SubItemIcon> SubItemsPaginated
    {
        get
        {
            int actualPage = Paged - 1;
            int nextPageSize = Math.Min(
                ModEntry.Config.GridItemsPageSize,
                SubItems.Count - actualPage * ModEntry.Config.GridItemsPageSize
            );
            if (nextPageSize == 0)
                nextPageSize = ModEntry.Config.GridItemsPageSize;
            return SubItems.GetRange(actualPage * ModEntry.Config.GridItemsPageSize, nextPageSize);
        }
    }
    public bool HasNextPage => Paged * ModEntry.Config.GridItemsPageSize < SubItems.Count;
    public bool HasPrevPage => (Paged - 1) > 0;
    public bool HasPagination => HasPrevPage || HasNextPage;

    public void PrevPaginatedPage()
    {
        if (!HasPrevPage)
            return;
        Paged--;
    }

    public float PrevPaginateButtonOpacity => HasPrevPage ? 1f : 0.6f;

    public void NextPaginatedPage()
    {
        if (!HasNextPage)
            return;
        Paged++;
    }

    public float NextPaginateButtonOpacity => HasNextPage ? 1f : 0.6f;
}
