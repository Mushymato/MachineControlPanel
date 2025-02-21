using MachineControlPanel.Integration;
using StardewModdingAPI;
using StardewValley;

namespace MachineControlPanel.GUI;

internal static class MenuHandler
{
    private static IViewEngine viewEngine = null!;
    internal static string VIEW_ASSET_PREFIX = null!;
    internal static string VIEW_ASSET_MACHINE_SELECT = null!;
    internal static string VIEW_ASSET_CONTROL_PANEL = null!;

    internal static void Register(IModHelper helper)
    {
        viewEngine = helper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI")!;
        viewEngine.RegisterSprites($"{ModEntry.ModId}/sprites", "assets/sprites");
        VIEW_ASSET_PREFIX = $"{ModEntry.ModId}/views";
        VIEW_ASSET_MACHINE_SELECT = $"{VIEW_ASSET_PREFIX}/machine-select";
        VIEW_ASSET_CONTROL_PANEL = $"{VIEW_ASSET_PREFIX}/control-panel";
        viewEngine.RegisterViews(VIEW_ASSET_PREFIX, "assets/views");
#if DEBUG
        viewEngine.EnableHotReloadingWithSourceSync();
#endif
    }

    internal static void ShowMachineSelect()
    {
        Game1.activeClickableMenu = viewEngine.CreateMenuFromAsset(
            VIEW_ASSET_MACHINE_SELECT,
            new MachineSelectContext()
        );
    }

    internal static void ShowControlPanel(Item machine, bool isGlobal, bool asChildMenu = false)
    {
        var controlPanel = viewEngine.CreateMenuFromAsset(
            VIEW_ASSET_CONTROL_PANEL,
            new ControlPanelContext(machine, isGlobal)
        );
        if (asChildMenu && Game1.activeClickableMenu != null)
            Game1.activeClickableMenu.SetChildMenu(controlPanel);
        else
            Game1.activeClickableMenu = controlPanel;
    }
}
