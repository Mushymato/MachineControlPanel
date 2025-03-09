global using SObject = StardewValley.Object;
using MachineControlPanel.Data;
using MachineControlPanel.GUI;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace MachineControlPanel;

public class ModEntry : Mod
{
#if DEBUG
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Debug;
#else
    private const LogLevel DEFAULT_LOG_LEVEL = LogLevel.Trace;
#endif
    private static IMonitor? mon;
    internal static ModConfig Config = null!;
    private static IManifest man = null!;
    internal static string ModId => man.UniqueID;

    /// <summary>
    /// Key for save data of this mod.
    /// </summary>
    private const string SAVEDATA = "save-machine-rules";

    /// <summary>
    /// Key for a partial message, e.g. only 1 machine's rules/inputs were changed.
    /// </summary>
    private const string SAVEDATA_ENTRY = "save-machine-rules-entry";
    internal static ModSaveData SaveData { get; private set; } = null!;
    private static IModHelper help = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        mon = Monitor;
        man = ModManifest;
        help = helper;
        Config = Helper.ReadConfig<ModConfig>();
        Patches.Patch();

        // shared events
        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.Multiplayer.ModMessageReceived += OnModMessageReceived;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.Content.AssetRequested += OnAssetRequested;
        helper.Events.Content.AssetsInvalidated += OnAssetInvalidated;
        helper.Events.Player.InventoryChanged += OnInventoryChanged;

        // host only events
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.Multiplayer.PeerConnected += OnPeerConnected;

        helper.ConsoleCommands.Add(
            "mcp-reset-savedata",
            "Reset save data associated with this mod.",
            ConsoleResetSaveData
        );
#if DEBUG
        helper.ConsoleCommands.Add("mcp-export-cache", "export all the data caches", ConsoleExportItemQueryCache);
        helper.ConsoleCommands.Add("mcp-resolve-ctag", "resolve context tag", ConsoleResolveContextTag);
        helper.ConsoleCommands.Add("mcp-print-active", "resolve context tag", ConsolePrintActive);
#endif
    }

    private static void printMenu(string name, IClickableMenu menu)
    {
        if (menu == null)
        {
            Console.WriteLine($"{name} is NULL");
        }
        else
        {
            Console.WriteLine($"{name} is {menu}");
        }
    }

    private void ConsolePrintActive(string arg1, string[] arg2)
    {
        printMenu("Game1.activeClickableMenu", Game1.activeClickableMenu);
        printMenu("Game1.overlayMenu", Game1.overlayMenu);
        foreach (var menu in Game1.onScreenMenus)
        {
            printMenu("Game1.onScreenMenus", menu);
        }
    }

    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Config = Helper.ReadConfig<ModConfig>();
        Config.Register(Helper, ModManifest);
        MenuHandler.Register(Helper);
        MachineRuleCache.Register(Helper);
    }

    /// <summary>
    /// Receive saved data sent from host
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
    {
        if (e.FromModID == ModManifest.UniqueID)
        {
            switch (e.Type)
            {
                // entire saveData
                case SAVEDATA:
                    try
                    {
                        SaveData = e.ReadAs<ModSaveData>();
                    }
                    catch (InvalidOperationException)
                    {
                        Log($"Failed to read save data sent by host.", LogLevel.Warn);
                        SaveData = new();
                    }
                    break;
                // 1 entry in saveData
                case SAVEDATA_ENTRY:
                    if (SaveData == null)
                    {
                        Log("Received unexpected partial save data.", LogLevel.Error);
                        break;
                    }
                    ModSaveDataEntryMessage msdEntryMsg = e.ReadAs<ModSaveDataEntryMessage>();
                    if (msdEntryMsg.Entry == null)
                        SaveData.Disabled.Remove(msdEntryMsg.QId);
                    else
                        SaveData.Disabled[msdEntryMsg.QId] = msdEntryMsg.Entry;
                    break;
            }
        }
    }

    /// <summary>
    /// Try and show either the machine control panel, or page to select machine from
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady || Game1.activeClickableMenu != null)
            return;
        if (Config.ControlPanelKey.JustPressed())
        {
            // ICursorPosition.GrabTile is unreliable with gamepad controls. Instead recreate game logic.
            Vector2 cursorTile = Game1.currentCursorTile;
            Point tile = Utility.tileWithinRadiusOfPlayer((int)cursorTile.X, (int)cursorTile.Y, 1, Game1.player)
                ? cursorTile.ToPoint()
                : Game1.player.GetGrabTile().ToPoint();
            SObject? machine = Game1.player.currentLocation.getObjectAtTile(tile.X, tile.Y, ignorePassables: true);
            if (machine != null && DataLoader.Machines(Game1.content).ContainsKey(machine.QualifiedItemId))
                MenuHandler.ShowControlPanel(machine);
        }
        if (Config.MachineSelectKey.JustPressed())
            MenuHandler.ShowMachineSelect();
    }

    /// <summary>
    /// Edit machine info to ensure unique rule ids
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (e.Name.IsEquivalentTo("Data/Machines"))
            e.Edit(Quirks.EnsureUniqueMachineOutputRuleId, AssetEditPriority.Late + 100);
        if (e.Name.IsEquivalentTo("Data/Objects"))
            e.Edit(Quirks.AddDefaultItemNamedSomethingOtherThanWeedses, AssetEditPriority.Default);
    }

    /// <summary>
    /// When Data/Objects changes, reset the context tag item cache.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.Names.Any((name) => name.IsEquivalentTo("Data/Objects")))
            ItemQueryCache.Invalidate();
    }

    /// <summary>
    /// Inventory changed water, to update progression mode
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        if (Config.ProgressionMode)
            foreach (var item in e.Added)
                PlayerProgressionCache.AddItem(item.QualifiedItemId);
    }

    /// <summary>
    /// Populate the has item cache
    /// Read save data on the host
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        if (Config.ProgressionMode)
            PlayerProgressionCache.Populate();
        if (!Game1.IsMasterGame)
            return;
        try
        {
            SaveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA)!;
            if (SaveData == null)
                SaveData = new();
            else
                SaveData.ClearInvalidData();
            SaveData.Version = ModManifest.Version;
            Helper.Data.WriteSaveData(SAVEDATA, SaveData);
        }
        catch (InvalidOperationException)
        {
            Log($"Failed to read existing save data, previous settings lost.", LogLevel.Warn);
            SaveData = new() { Version = ModManifest.Version };
        }
    }

    /// <summary>
    /// When someone joins in co-op, send entire saved data over
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
    {
        if (!Game1.IsMasterGame)
            return;

        Helper.Multiplayer.SendMessage(
            SaveData,
            SAVEDATA,
            modIDs: [ModManifest.UniqueID],
            playerIDs: [e.Peer.PlayerID]
        );
    }

    /// <summary>
    /// Save machine rule for given machine, message peers about it too.
    /// </summary>
    /// <param name="bigCraftableId"></param>
    /// <param name="locationName"></param>
    /// <param name="disabledRules"></param>
    /// <param name="disabledInputs"></param>
    /// <param name="disabledQuality"></param>
    internal static void SaveMachineRules(
        string bigCraftableId,
        string? locationName,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        if (!Game1.IsMasterGame)
            return;
        if (SaveData?.Version == null)
        {
            Log("Attempted to save machine rules without save loaded", LogLevel.Error);
            return;
        }
        SaveData.Version = man.Version;
        help.Multiplayer.SendMessage(
            SaveData.SetMachineRules(bigCraftableId, locationName, disabledRules, disabledInputs, disabledQuality),
            SAVEDATA_ENTRY,
            modIDs: [ModId]
        );
        help.Data.WriteSaveData(SAVEDATA, SaveData);
    }

    /// <summary>
    /// Reset save data from this mod, for when things are looking wrong
    /// </summary>
    /// <param name="command"></param>
    /// <param name="args"></param>
    private void ConsoleResetSaveData(string command, string[] args)
    {
        if (!Context.IsWorldReady)
        {
            Log("Must load save first.", LogLevel.Error);
            return;
        }
        Helper.Data.WriteSaveData<ModSaveData>(SAVEDATA, null);
        SaveData = new() { Version = ModManifest.Version };
    }

#if DEBUG
    private void ConsoleExportItemQueryCache(string command, string[] args)
    {
        MachineRuleCache.Export(Helper);
        ItemQueryCache.Export(Helper);
    }

    private void ConsoleResolveContextTag(string arg1, string[] arg2)
    {
        if (ItemQueryCache.TryContextTagLookupCache(arg2, out IEnumerable<Item>? items))
        {
            foreach (var item in items)
            {
                Log($"{item.QualifiedItemId}: {item.DisplayName}");
            }
        }
    }
#endif

    /// <summary>SMAPI static monitor Log wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void Log(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.Log(msg, level);
    }

    /// <summary>SMAPI static monitor LogOnce wrapper</summary>
    /// <param name="msg"></param>
    /// <param name="level"></param>
    internal static void LogOnce(string msg, LogLevel level = DEFAULT_LOG_LEVEL)
    {
        mon!.LogOnce(msg, level);
    }
}
