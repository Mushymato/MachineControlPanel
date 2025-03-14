﻿global using SObject = StardewValley.Object;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using MachineControlPanel.Framework;
using MachineControlPanel.Framework.Integration;
using MachineControlPanel.Framework.UI;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Extensions;
using StardewValley.GameData.Machines;
using StardewValley.GameData.Objects;

namespace MachineControlPanel;

public sealed record RuleIdent(string OutputId, string TriggerId);

internal sealed class ModEntry : Mod
{
    /// <summary>
    /// Key for save data of this mod.
    /// </summary>
    private const string SAVEDATA = "save-machine-rules";

    /// <summary>
    /// Key for a partial message, e.g. only 1 machine's rules/inputs were changed.
    /// </summary>
    private const string SAVEDATA_ENTRY = "save-machine-rules-entry";
    private static IMonitor? mon = null;
    private static ModSaveData? saveData = null;
    internal static ModConfig Config = null!;
    internal static bool HasLookupAnying = false;
    internal static string DefaultThingId = "0";
    internal static Item? DefaultThing = null;
    IIconicFrameworkApi? iconicFrameworkApi;

    /// <summary>
    /// Attempt to get a save data entry for a machine
    /// </summary>
    /// <param name="QId"></param>
    /// <param name="msdEntry"></param>
    /// <returns></returns>
    internal static bool TryGetSavedEntry(string QId, [NotNullWhen(true)] out ModSaveDataEntry? msdEntry)
    {
        msdEntry = null;
        if (saveData != null)
            return saveData.Disabled.TryGetValue(QId, out msdEntry);
        return false;
    }

    /// <summary>
    /// Check if machine has any saved data
    /// </summary>
    /// <param name="QId"></param>
    /// <returns></returns>
    internal static bool HasSavedEntry(string QId)
    {
        if (saveData != null)
            return saveData.Disabled.ContainsKey(QId);
        return false;
    }

    public override void Entry(IModHelper helper)
    {
        mon = Monitor;
        I18n.Init(helper.Translation);

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
            "mcp_reset_savedata",
            "Reset save data associated with this mod.",
            ConsoleResetSaveData
        );
#if DEBUG
        helper.ConsoleCommands.Add("mcp_export_iqc", "export the item query cache", ConsoleExportItemQueryCache);
#endif
    }

    private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e)
    {
        foreach (var item in e.Added)
            PlayerHasItemCache.AddItem(item.QualifiedItemId);
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
            e.Edit(AddDefaultItemNamedSomethingOtherThanWeedses, AssetEditPriority.Default);
    }

    private void AddDefaultItemNamedSomethingOtherThanWeedses(IAssetData asset)
    {
        DefaultThingId = $"{ModManifest.UniqueID}_DefaultItem";
        IDictionary<string, ObjectData> data = asset.AsDictionary<string, ObjectData>().Data;
        data[DefaultThingId] = new()
        {
            Name = DefaultThingId,
            DisplayName = I18n.Object_Thing_DisplayName(),
            Description = "Where did you get this? Put it back where you found it >:(",
            Type = "Basic",
            Category = -20,
            SpriteIndex = 923,
            Edibility = 0,
        };
    }

    /// <summary>
    /// When Data/Objects changes, reset the context tag item cache.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnAssetInvalidated(object? sender, AssetsInvalidatedEventArgs e)
    {
        if (e.Names.Any((name) => name.IsEquivalentTo("Data/Machines")))
        {
            RuleHelperCache.Invalidate();
        }
        if (e.Names.Any((name) => name.IsEquivalentTo("Data/Objects")))
        {
            ItemQueryCache.Invalidate();
            RuleHelperCache.Invalidate();
        }
    }

    // // Decided against rechecking save when Data/Machine changes.
    // // Checking the save is not a huge performance drain, but being eventually correct is good enough for me.
    // private void RecheckSaveData(AssetsInvalidatedEventArgs e)
    // {
    //     if (!Game1.IsMasterGame)
    //         return;
    //     if (saveData != null && e.Names.Any((name) => name.IsEquivalentTo("Data/Machines")))
    //     {
    //         if (saveData.ClearInvalidData())
    //         {
    //             Helper.Multiplayer.SendMessage(
    //                 saveData, SAVEDATA,
    //                 modIDs: [ModManifest.UniqueID]
    //             );
    //         }
    //     }
    // }

    /// <summary>
    /// Read config, get EMC api, do patches
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
    {
        Config = Helper.ReadConfig<ModConfig>();
        Config.Register(Helper, ModManifest, GetMachineSelectMenu);
        var EMC = Helper.ModRegistry.GetApi<IExtraMachineConfigApi>("selph.ExtraMachineConfig");
        if (EMC != null)
            RuleHelper.EMC = EMC;
        HasLookupAnying = Helper.ModRegistry.IsLoaded("Pathoschild.LookupAnything");

        iconicFrameworkApi = Helper.ModRegistry.GetApi<IIconicFrameworkApi>("furyx639.ToolbarIcons");
        if (iconicFrameworkApi != null)
        {
            iconicFrameworkApi.AddToolbarIcon(
                $"{ModManifest.UniqueID}_MachineSelect",
                "LooseSprites/emojis",
                new Rectangle(72, 54, 9, 9),
                I18n.Iconic_MachineSelect_Title,
                I18n.Iconic_MachineSelect_Description
            );
            iconicFrameworkApi.Subscribe(HandleIconicFrameworkEvent);
        }

        Harmony harmony = new(ModManifest.UniqueID);
        GamePatches.Apply(harmony);
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
                        LogSaveData();
                    }
                    catch (InvalidOperationException)
                    {
                        Log($"Failed to read save data sent by host.", LogLevel.Warn);
                        saveData = null;
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
#if DEBUG
                    LogSaveData(msdEntryMsg.QId);
#endif
                    break;
            }
        }
    }

    /// <summary>
    /// Populate the has item cache
    /// Read save data on the host
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
    {
        ItemQueryCache.PopulateContextTagLookupCache();
        if (Config.ProgressionMode)
            PlayerHasItemCache.Populate();
        if (Config.PrefetchCaches)
            RuleHelperCache.Prefetch();
        DefaultThing = ItemRegistry.Create(DefaultThingId);
        if (!Game1.IsMasterGame)
            return;
        try
        {
            saveData = Helper.Data.ReadSaveData<ModSaveData>(SAVEDATA);
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
        LogSaveData();
    }

    /// <summary>
    /// Save machine rule for given machine.
    /// </summary>
    /// <param name="bigCraftableId"></param>
    /// <param name="disabledRules"></param>
    /// <param name="disabledInputs"></param>
    private void SaveMachineRules(
        string bigCraftableId,
        IEnumerable<RuleIdent> disabledRules,
        IEnumerable<string> disabledInputs,
        bool[] disabledQuality
    )
    {
        if (!Game1.IsMasterGame)
            return;
        if (saveData == null)
        {
            Log("Attempted to save machine rules without save loaded", LogLevel.Error);
            return;
        }

        ModSaveDataEntry? msdEntry = null;
        if (!disabledRules.Any() && !disabledInputs.Any() && !disabledQuality.HasAnySet())
        {
            saveData.Disabled.Remove(bigCraftableId);
        }
        else
        {
            msdEntry = new(disabledRules.ToImmutableHashSet(), disabledInputs.ToImmutableHashSet(), disabledQuality);
            saveData.Disabled[bigCraftableId] = msdEntry;
        }
        saveData.Version = ModManifest.Version;
        Helper.Multiplayer.SendMessage(
            new ModSaveDataEntryMessage(bigCraftableId, msdEntry),
            SAVEDATA_ENTRY,
            modIDs: [ModManifest.UniqueID]
        );
        Helper.Data.WriteSaveData(SAVEDATA, saveData);
#if DEBUG
        LogSaveData(bigCraftableId);
#endif
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
        if (ShowPanel())
            return;
        if (Config.MachineSelectKey.JustPressed())
            ShowMachineSelect();
    }

    /// <summary>
    /// Handle iconic framework event
    /// </summary>
    /// <param name="e"></param>
    private void HandleIconicFrameworkEvent(IIconPressedEventArgs e)
    {
        if (e.Id.EqualsIgnoreCase($"{ModManifest.UniqueID}_MachineSelect"))
        {
            ShowMachineSelect();
        }
    }

    /// <summary>
    /// Show the machine selection grid if corresponding button is pressed
    /// </summary>
    /// <returns></returns>
    private bool ShowMachineSelect()
    {
        if (Game1.activeClickableMenu == null && saveData != null)
        {
            Game1.activeClickableMenu = GetMachineSelectMenu();
        }
        return false;
    }

    /// <summary>
    /// Obtain new machine select menu.
    /// </summary>
    /// <returns></returns>
    private MachineMenu GetMachineSelectMenu()
    {
        return new MachineMenu(SaveMachineRules);
    }

    /// <summary>
    /// Show the machine panel if corresponding button is pressed
    /// </summary>
    /// <returns></returns>
    private bool ShowPanel()
    {
        if (Game1.activeClickableMenu == null && Config.ControlPanelKey.JustPressed() && saveData != null)
        {
            // ICursorPosition.GrabTile is unreliable with gamepad controls. Instead recreate game logic.
            Vector2 cursorTile = Game1.currentCursorTile;
            Point tile = Utility.tileWithinRadiusOfPlayer((int)cursorTile.X, (int)cursorTile.Y, 1, Game1.player)
                ? cursorTile.ToPoint()
                : Game1.player.GetGrabTile().ToPoint();
            SObject? bigCraftable = Game1.player.currentLocation.getObjectAtTile(tile.X, tile.Y, ignorePassables: true);
            if (bigCraftable != null && bigCraftable.bigCraftable.Value)
            {
                return ShowPanelFor(bigCraftable);
            }
        }
        return false;
    }

    /// <summary>
    /// Show machine control panel for a big craftable
    /// </summary>
    /// <param name="bigCraftable"></param>
    /// <returns></returns>
    private bool ShowPanelFor(SObject bigCraftable)
    {
        if (bigCraftable.GetMachineData() is not MachineData machine)
            return false;

        if (
            RuleHelperCache.TryGetRuleHelper(
                bigCraftable.QualifiedItemId,
                bigCraftable.DisplayName,
                machine,
                out RuleHelper? ruleHelper
            ) && ruleHelper.GetRuleEntries()
        )
        {
            Game1.activeClickableMenu = new RuleListMenu(ruleHelper, SaveMachineRules, true);
        }

        return true;
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
        ItemQueryCache.Export(Helper);
    }

    internal static void Log(string msg
#if DEBUG
        , LogLevel level = LogLevel.Debug
#else
        , LogLevel level = LogLevel.Trace
#endif
    )
    {
        mon!.Log(msg, level);
    }

    internal static void LogOnce(string msg
#if DEBUG
        , LogLevel level = LogLevel.Debug
#else
        , LogLevel level = LogLevel.Trace
#endif

    )
    {
        mon!.LogOnce(msg, level);
    }

    /// <summary>Debug log save data</summary>
    internal static void LogSaveData()
    {
        if (saveData == null || !saveData.Disabled.Any())
            return;
        if (Game1.IsMasterGame)
            Log("Disabled machine rules:");
        else
            Log("Disabled machine rules (from host):");
        foreach ((string key, ModSaveDataEntry value) in saveData.Disabled)
        {
            Log(key);
            foreach (RuleIdent ident in value.Rules)
                Log($"* {ident}");
            foreach (string inputQId in value.Inputs)
                Log($"- {inputQId}");
            if (value.Quality.HasAnySet())
            {
                string qualityStr = "";
                for (int i = 0; i < value.Quality.Length; i++)
                    qualityStr += value.Quality[i] ? "1" : "0";
                Log($"Q {qualityStr}");
            }
        }
    }

#if DEBUG
    /// <summary>Debug log partial save data</summary>
    internal static void LogSaveData(string qId)
    {
        if (saveData == null)
            return;
        if (Game1.IsMasterGame)
            Log($"Disabled machine rules for {qId}:");
        else
            Log($"Disabled machine rules for {qId}: (from host):");
        if (!saveData.Disabled.TryGetValue(qId, out ModSaveDataEntry? msdEntry))
        {
            Log("= None");
            return;
        }
        foreach (RuleIdent ident in msdEntry.Rules)
            Log($"* {ident}");
        foreach (string inputQId in msdEntry.Inputs)
            Log($"- {inputQId}");
        if (msdEntry.Quality.HasAnySet())
        {
            string qualityStr = "";
            for (int i = 0; i < msdEntry.Quality.Length; i++)
                qualityStr += msdEntry.Quality[i] ? "1" : "0";
            Log($"Q {qualityStr}");
        }
    }
#endif
}
