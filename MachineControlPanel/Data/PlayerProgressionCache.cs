using StardewValley;

namespace MachineControlPanel.Data;

/// <summary>Cache info about what items a player owns at least one copy of</summary>
internal static class PlayerProgressionCache
{
    private static readonly HashSet<string> playerHasItem = [];

    internal static void Populate()
    {
        playerHasItem.Clear();
        Utility.ForEachItem(
            (item) =>
            {
                if (item.HasBeenInInventory)
                    playerHasItem.Add(item.QualifiedItemId);
                return true;
            }
        );
    }

    internal static void AddItem(string qId) => playerHasItem.Add(qId);

    internal static bool HasItem(string qId) => playerHasItem.Contains(qId);

    internal static void Clear() => playerHasItem.Clear();
}
