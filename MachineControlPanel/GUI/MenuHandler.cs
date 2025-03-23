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
    internal static string VIEW_ASSET_SUBITEM_GRID = null!;
    private static readonly PerScreen<Item?> hoveredItem = new();

    // private static readonly PerScreen<WeakReference<MachineSelectContext?>> machineSelectContext = new();
    internal static Item? HoveredItem
    {
        get => hoveredItem.Value;
        set => hoveredItem.Value = value;
    }
    internal static GlobalToggleContext GlobalToggle = new();

    internal static void Register(IModHelper helper)
    {
        viewEngine = helper.ModRegistry.GetApi<IViewEngine>("focustense.StardewUI")!;
        viewEngine.RegisterSprites($"{ModEntry.ModId}/sprites", "assets/sprites");
        VIEW_ASSET_PREFIX = $"{ModEntry.ModId}/views";
        VIEW_ASSET_MACHINE_SELECT = $"{VIEW_ASSET_PREFIX}/machine-select";
        VIEW_ASSET_CONTROL_PANEL = $"{VIEW_ASSET_PREFIX}/control-panel";
        VIEW_ASSET_SUBITEM_GRID = $"{VIEW_ASSET_PREFIX}/subitem-grid";
        viewEngine.RegisterViews(VIEW_ASSET_PREFIX, "assets/views");
#if DEBUG
        viewEngine.EnableHotReloadingWithSourceSync();
#endif
    }

    internal static void ShowMachineSelect()
    {
        hoveredItem.Value = null;
        MachineSelectContext context = new();
        ModEntry.SavedMachineRules += context.UpdateBackgroundTint;
        var menuCtrl = viewEngine.CreateMenuControllerFromAsset(VIEW_ASSET_MACHINE_SELECT, context);
        menuCtrl.Closing += context.Closing;
        Game1.activeClickableMenu = menuCtrl.Menu;
    }

    internal static bool ShowControlPanel(Item machine, bool asChildMenu = false)
    {
        hoveredItem.Value = null;
        if (ControlPanelContext.TryCreate(machine) is not ControlPanelContext context)
        {
            // Game1.addHUDMessage(
            //     new HUDMessage(I18n.RuleList_NoRules(machine.DisplayName))
            // );
            return false;
        }
        if (!context.HasInputs)
            context.PageIndex = (int)DefaultPageOption.Rules;
        var menuCtrl = viewEngine.CreateMenuControllerFromAsset(VIEW_ASSET_CONTROL_PANEL, context);
        menuCtrl.Closing += context.Closing;
        if (asChildMenu && Game1.activeClickableMenu != null)
            Game1.activeClickableMenu.SetChildMenu(menuCtrl.Menu);
        else
            Game1.activeClickableMenu = menuCtrl.Menu;
        return true;
    }

    internal static void ShowSubItemGrid(string title, List<SubItemIcon>? itemDatas)
    {
        if (itemDatas == null)
            return;
        if (Game1.activeClickableMenu == null)
            return;
        if (Game1.activeClickableMenu.GetChildMenu() is IClickableMenu childMenu)
        {
            childMenu.SetChildMenu(
                viewEngine.CreateMenuFromAsset(VIEW_ASSET_SUBITEM_GRID, new SubitemGridContext(title, itemDatas))
            );
        }
        else
        {
            Game1.activeClickableMenu.SetChildMenu(
                viewEngine.CreateMenuFromAsset(VIEW_ASSET_SUBITEM_GRID, new SubitemGridContext(title, itemDatas))
            );
        }
    }
}
