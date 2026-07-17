namespace MachineControlPanel.GUI.Includes;

public sealed class OverlayToggleContext
{
    public bool CanEnable => ModEntry.Overlay.Value.CanEnable;

    public void ShowOverlay()
    {
        MenuHandler.ShowOverlayInfo();
    }
}
