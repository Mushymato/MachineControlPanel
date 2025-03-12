using StardewValley;

namespace MachineControlPanel.GUI.Includes;

/// <summary>Context for subitem grid</summary>
public sealed record SubitemGridContext(IList<SubItemIcon> ItemDatas)
{
    private const int ICON_SIZE = 76;
    private const int COL_CNT = 8;

    public string SubitemLayout
    {
        get
        {
            if (ItemDatas.Count <= COL_CNT)
            {
                return $"{ICON_SIZE * ItemDatas.Count}px {ICON_SIZE}px";
            }
            int neededHeight = (int)Math.Min(Game1.viewport.Height * 0.6, ICON_SIZE * (1 + ItemDatas.Count / 8));
            return $"{ICON_SIZE * COL_CNT}px {neededHeight}px";
        }
    }
}
