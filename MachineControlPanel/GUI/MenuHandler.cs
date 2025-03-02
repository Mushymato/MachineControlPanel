using MachineControlPanel.GUI.Includes;
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
        viewEngine.PreloadAssets();
        viewEngine.PreloadModels(
            typeof(MachineSelectContext),
            typeof(ControlPanelContext),
            typeof(GlobalToggleContext)
        );
    }

    internal static void ShowMachineSelect()
    {
        Game1.activeClickableMenu = viewEngine.CreateMenuFromAsset(
            VIEW_ASSET_MACHINE_SELECT,
            new MachineSelectContext()
        );
    }

    internal static bool ShowControlPanel(
        Item machine,
        GlobalToggleContext? globalToggleContext = null,
        bool asChildMenu = false
    )
    {
        if (ControlPanelContext.TryCreate(machine, globalToggleContext) is not ControlPanelContext context)
            return false;
        var menuCtrl = viewEngine.CreateMenuControllerFromAsset(VIEW_ASSET_CONTROL_PANEL, context);
        menuCtrl.Closing += context.SaveChanges;
        if (asChildMenu && Game1.activeClickableMenu != null)
            Game1.activeClickableMenu.SetChildMenu(menuCtrl.Menu);
        else
            Game1.activeClickableMenu = menuCtrl.Menu;
        return true;
    }
}
