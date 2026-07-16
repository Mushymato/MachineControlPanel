using StardewValley;

namespace MachineControlPanel.GUI.Includes;

public sealed class OverlayToggleContext
{
    public bool OverlayEnabled => ModEntry.Overlay.Value.Enabled;

    public void ShowOverlay()
    {
        if (!ModEntry.Overlay.Value.TryEnable())
        {
            Game1.addHUDMessage(new(I18n.OverlayToggle_NoData(), HUDMessage.error_type));
            Quirks.CloseAllMenus(sound: "bigDeSelect");
        }
        else Quirks.CloseAllMenus(sound: "bigSelect");
    }

    public void HideOverlay()
    {
        ModEntry.Overlay.Value.Enabled = false;
        Quirks.CloseAllMenus(sound: "bigDeSelect");
    }
}
