using MachineControlPanel.GUI.Includes;
using MachineControlPanel.Integration;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Menus;

namespace MachineControlPanel.GUI;

internal static class MenuHandler
{
    private static IViewEngine viewEngine = null!;
    internal static string VIEW_ASSET_PREFIX = null!;
    internal static string VIEW_ASSET_MACHINE_SELECT = null!;
    internal static string VIEW_ASSET_CONTROL_PANEL = null!;

    internal static GlobalToggleContext GlobalToggle = new();
    internal static WeakReference<IClickableMenu?> MachineSelectView = new(null);
    internal static WeakReference<IMenuController?> ControlPanelCtrl = new(null);

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
        ModEntry.Log($"ShowMachineSelect Game1.displayHUD: {Game1.displayHUD}");
        var view = viewEngine.CreateMenuFromAsset(VIEW_ASSET_MACHINE_SELECT, new MachineSelectContext());
        MachineSelectView.SetTarget(view);
        Game1.activeClickableMenu = view;
    }

    internal static bool ShowControlPanel(Item machine, bool asChildMenu = false)
    {
        ModEntry.Log($"ShowControlPanel Game1.displayHUD: {Game1.displayHUD}");
        if (ControlPanelContext.TryCreate(machine) is not ControlPanelContext context)
            return false;
        var menuCtrl = viewEngine.CreateMenuControllerFromAsset(VIEW_ASSET_CONTROL_PANEL, context);
        menuCtrl.Closing += context.SaveChanges;
        ControlPanelCtrl.SetTarget(menuCtrl);
        if (asChildMenu && Game1.activeClickableMenu != null)
            Game1.activeClickableMenu.SetChildMenu(menuCtrl.Menu);
        else
            Game1.activeClickableMenu = menuCtrl.Menu;
        return true;
    }
}
