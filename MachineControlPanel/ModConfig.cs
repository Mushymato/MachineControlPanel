using MachineControlPanel.Data;
using MachineControlPanel.GUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;

namespace MachineControlPanel;

// Adds a button in GMCM, credit to ichortower
// https://github.com/ichortower/Nightshade/blob/dev/src/GMCM.cs#L79
internal static class OpenMenuButton
{
    private static bool mouseLastFrame = false;

    public static void Draw(SpriteBatch b, Vector2 origin)
    {
        if (Game1.gameMode == Game1.playingGameMode)
        {
            origin.Y -= 4;
            bool mouseThisFrame =
                Game1.input.GetMouseState().LeftButton == ButtonState.Pressed
                || Game1.input.GetGamePadState().IsButtonDown(Buttons.A);
            bool justClicked = mouseThisFrame && !mouseLastFrame;
            mouseLastFrame = mouseThisFrame;
            int mouseX = Game1.getMouseX();
            int mouseY = Game1.getMouseY();
            Rectangle bounds = new((int)origin.X, (int)origin.Y, 80, 80);
            bool hovering = bounds.Contains(mouseX, mouseY);
            if (hovering && justClicked)
            {
                Game1.playSound("bigSelect");
                // Game1.activeClickableMenu.SetChildMenu(getMachineSelectMenu());
                MenuHandler.ShowMachineSelect();
            }
            b.Draw(
                Game1.mouseCursors2,
                new((int)origin.X, (int)origin.Y, 80, 80),
                new Rectangle(154, 154, 20, 20),
                Color.White,
                0f,
                Vector2.Zero,
                SpriteEffects.None,
                1f
            );
        }
        else
        {
            b.DrawString(
                Game1.dialogueFont,
                I18n.Config_OpenMachineSelectMenu_Description(),
                new Vector2(origin.X + 12, origin.Y + 4),
                Game1.textColor
            );
        }
    }
}

/// <summary>
/// Options for default opened page
/// Rules: machine rules page first
/// Inputs: input page first
/// </summary>
internal enum DefaultPageOption
{
    Rules = 1,
    Inputs = 2,
}

/// <summary>
/// Mod config class + GMCM
/// </summary>
internal sealed class ModConfig
{
    /// <summary>Key for opening control panel when next to a machine</summary>
    public KeybindList ControlPanelKey { get; set; } = KeybindList.Parse($"{SButton.Q}");

    /// <summary>Key for opening machine selection page</summary>
    public KeybindList MachineSelectKey { get; set; } = KeybindList.Parse($"{SButton.LeftControl}+{SButton.Q}");

    /// <summary>Default page to use</summary>
    public DefaultPageOption DefaultPage { get; set; } = DefaultPageOption.Rules;

    /// <summary>Whether menu starts in </summary>
    public bool DefaultIsGlobal { get; set; } = true;

    /// <summary>On the machine selection page, hide machines the player don't not have yet.</summary>
    public bool ProgressionMode { get; set; } = true;

    /// <summary>Use the more visible question mark icon that recolors rarely seem to touch :(</summary>
    public bool AltQuestionMark { get; set; } = false;

    /// <summary>Maximum number of rows to display on rule entries page, lower this if you have performance issues</summary>
    public int RuleEntriesPageSize { get; set; } = 15;

    /// <summary>Maximum number of items to display on inputs page, lower this if you have performance issues</summary>
    public int GridItemsPageSize { get; set; } = 2048;

    private void Reset()
    {
        ControlPanelKey = KeybindList.Parse($"{SButton.MouseLeft}, {SButton.ControllerB}");
        MachineSelectKey = KeybindList.Parse($"{SButton.LeftControl}+{SButton.Q}");
        DefaultPage = DefaultPageOption.Rules;
        DefaultIsGlobal = true;
        ProgressionMode = true;
        AltQuestionMark = false;
        RuleEntriesPageSize = 10;
        GridItemsPageSize = 1024;
    }

    public void Register(IModHelper helper, IManifest mod)
    {
        var GMCM = helper.ModRegistry.GetApi<Integration.IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (GMCM == null)
        {
            helper.WriteConfig(this);
            return;
        }
        GMCM.Register(
            mod: mod,
            reset: () =>
            {
                Reset();
                helper.WriteConfig(this);
            },
            save: () =>
            {
                helper.WriteConfig(this);
            },
            titleScreenOnly: false
        );

        GMCM.AddSectionTitle(mod, I18n.Config_Heading_MenuAccess);
        GMCM.AddKeybindList(
            mod,
            getValue: () => ControlPanelKey,
            setValue: (value) => ControlPanelKey = value,
            name: I18n.Config_ControlPanelKey_Name,
            tooltip: I18n.Config_ControlPanelKey_Description
        );
        GMCM.AddKeybindList(
            mod,
            getValue: () => MachineSelectKey,
            setValue: (value) => MachineSelectKey = value,
            name: I18n.Config_MachineSelectKey_Name,
            tooltip: I18n.Config_MachineSelectKey_Description
        );
        GMCM.AddComplexOption(
            mod,
            name: I18n.Config_OpenMachineSelectMenu_Name,
            draw: OpenMenuButton.Draw,
            height: () => 80
        );

        GMCM.AddSectionTitle(mod, I18n.Config_Heading_MenuAccess);
        GMCM.AddBoolOption(
            mod,
            getValue: () => ProgressionMode,
            setValue: (value) =>
            {
                ProgressionMode = value;
                if (value)
                    PlayerProgressionCache.Populate();
                else
                    PlayerProgressionCache.Clear();
            },
            name: I18n.Config_ProgressionMode_Name,
            tooltip: I18n.Config_ProgressionMode_Description
        );
        GMCM.AddBoolOption(
            mod,
            getValue: () => DefaultIsGlobal,
            setValue: (value) =>
            {
                DefaultIsGlobal = value;
                MenuHandler.GlobalToggle.IsGlobal = value;
            },
            name: I18n.Config_DefaultIsGlobal_Name,
            tooltip: I18n.Config_DefaultIsGlobal_Description
        );
        GMCM.AddTextOption(
            mod,
            getValue: () => DefaultPage.ToString(),
            setValue: (value) => DefaultPage = Enum.Parse<DefaultPageOption>(value),
            allowedValues: Enum.GetNames<DefaultPageOption>(),
            formatAllowedValue: value =>
                value switch
                {
                    nameof(DefaultPageOption.Rules) => I18n.Config_DefaultPage_MachineRules(),
                    nameof(DefaultPageOption.Inputs) => I18n.Config_DefaultPage_ItemInputs(),
                    _ =>
                        "???" // should never happen
                    ,
                },
            name: I18n.Config_DefaultPage_Name,
            tooltip: I18n.Config_DefaultPage_Description
        );
        GMCM.AddNumberOption(
            mod,
            getValue: () => RuleEntriesPageSize,
            setValue: (value) => RuleEntriesPageSize = value,
            name: I18n.Config_RuleEntriesPageSize_Name,
            tooltip: I18n.Config_RuleEntriesPageSize_Description,
            min: 1,
            max: 25
        );
        GMCM.AddNumberOption(
            mod,
            getValue: () => GridItemsPageSize,
            setValue: (value) => GridItemsPageSize = value,
            name: I18n.Config_GridItemsPageSize_Name,
            tooltip: I18n.Config_GridItemsPageSize_Description,
            min: 16,
            max: 4096,
            interval: 16
        );

        GMCM.AddSectionTitle(mod, I18n.Config_Heading_Visual);
        GMCM.AddBoolOption(
            mod,
            getValue: () => AltQuestionMark,
            setValue: (value) => AltQuestionMark = value,
            name: I18n.Config_AltQuestionMark_Name,
            tooltip: I18n.Config_AltQuestionMark_Description
        );
    }
}
