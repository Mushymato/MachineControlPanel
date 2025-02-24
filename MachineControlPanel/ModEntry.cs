global using SObject = StardewValley.Object;
using MachineControlPanel.Data;
using MachineControlPanel.GUI;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

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
    private static ModSaveData saveData = null!;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        mon = Monitor;
        man = ModManifest;
        Config = Helper.ReadConfig<ModConfig>();

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
#endif
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
                        saveData = e.ReadAs<ModSaveData>();
                    }
                    catch (InvalidOperationException)
                    {
                        Log($"Failed to read save data sent by host.", LogLevel.Warn);
                        saveData = new();
                    }
                    break;
                // 1 entry in saveData
                case SAVEDATA_ENTRY:
                    if (saveData == null)
                    {
                        Log("Received unexpected partial save data.", LogLevel.Error);
                        break;
                    }
                    ModSaveDataEntryMessage msdEntryMsg = e.ReadAs<ModSaveDataEntryMessage>();
                    if (msdEntryMsg.Entry == null)
                        saveData.Disabled.Remove(msdEntryMsg.QId);
                    else
                        saveData.Disabled[msdEntryMsg.QId] = msdEntryMsg.Entry;
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
        if (!Context.IsWorldReady)
            return;
        if (Config.ControlPanelKey.JustPressed()) { }
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
            saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA)!;
            if (saveData == null)
                saveData = new();
            else
                saveData.ClearInvalidData();
            saveData.Version = ModManifest.Version;
            Helper.Data.WriteSaveData(SAVEDATA, saveData);
        }
        catch (InvalidOperationException)
        {
            Log($"Failed to read existing save data, previous settings lost.", LogLevel.Warn);
            saveData = new() { Version = ModManifest.Version };
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
            saveData,
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
    internal void SaveMachineRules(
        string bigCraftableId,
        string? locationName,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        if (!Game1.IsMasterGame)
            return;
        if (saveData?.Version == null)
        {
            Log("Attempted to save machine rules without save loaded", LogLevel.Error);
            return;
        }
        saveData.Version = man.Version;
        Helper.Multiplayer.SendMessage(
            saveData.SetMachineRules(bigCraftableId, locationName, disabledRules, disabledInputs, disabledQuality),
            SAVEDATA_ENTRY,
            modIDs: [ModManifest.UniqueID]
        );
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
        saveData = new() { Version = ModManifest.Version };
    }

    private void ConsoleExportItemQueryCache(string command, string[] args)
    {
        MachineRuleCache.Export(Helper);
        ItemQueryCache.Export(Helper);
    }

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
