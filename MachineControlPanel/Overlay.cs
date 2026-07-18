using MachineControlPanel.GUI;
using MachineControlPanel.Integration;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MachineControlPanel;

public sealed class Overlay
{
    private static readonly Point square = new(Game1.tileSize - 1, Game1.tileSize - 1);
    private readonly int screenId;
    private IViewDrawable? OverlayInfo
    {
        get
        {
            if (field == null)
            {
                field = MenuHandler.MakeOverlayInfoDrawable();
                field.Context = new { CountTotal = GetCountTotalString() };
            }
            return field;
        }
        set => field = value;
    }

    public Overlay(int screenId)
    {
        this.screenId = screenId;
        ModEntry.help.Events.Display.RenderedWorld += OnRenderedWorld;
    }

    public bool Enabled
    {
        get => field;
        set
        {
            field = value;
            if (!field)
                OverlayInfo = null;
        }
    }

    public bool CanEnable
    {
        get
        {
            if (Game1.currentLocation == null)
                return false;
            foreach (SObject obj in Game1.currentLocation.Objects.Values)
            {
                if (ModSaveData.MachineHasData(obj))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public string GetCountTotalString()
    {
        if (Game1.currentLocation == null)
            return string.Empty;
        int count = 0;
        int total = 0;
        foreach (SObject obj in Game1.currentLocation.Objects.Values)
        {
            if (obj.GetMachineData() != null)
            {
                total++;
            }
            if (ModSaveData.MachineHasData(obj))
            {
                count++;
            }
        }
        if (count == 0)
            return I18n.Overlay_NoData();
        return I18n.Overlay_CountTotal(count, total);
    }

    private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
    {
        if (!Enabled)
            return;
        if (Context.ScreenId != screenId)
            return;
        if (Game1.currentLocation == null)
            return;

        var x = Quirks.DivFloor(Game1.viewport.X, Game1.tileSize) - 1;
        var y = Quirks.DivFloor(Game1.viewport.Y, Game1.tileSize) - 1;
        var w = Quirks.DivCeil(Game1.viewport.Width, Game1.tileSize) + 2;
        var h = Quirks.DivCeil(Game1.viewport.Height, Game1.tileSize) + 2;

        for (var i = 0; i < w; i++)
        {
            for (var j = 0; j < h; j++)
            {
                var pos = new Vector2(x + i, y + j);
                var point = Game1.GlobalToLocal(pos * Game1.tileSize + new Vector2(1, 1)).ToPoint();

                if (Game1.currentLocation.Objects.TryGetValue(pos, out var obj) && ModSaveData.MachineHasData(obj))
                {
                    Utility.DrawSquare(
                        e.SpriteBatch,
                        pixelArea: new(point, square),
                        borderWidth: 0,
                        backgroundColor: new(255, 128, 255, 64)
                    );
                }
                else
                {
                    Utility.DrawSquare(
                        e.SpriteBatch,
                        pixelArea: new(point, square),
                        borderWidth: 0,
                        backgroundColor: new(0, 0, 0, 64)
                    );
                }
            }
        }

        if (Game1.activeClickableMenu == null)
        {
            OverlayInfo?.Draw(e.SpriteBatch, Vector2.Zero);
        }
    }
}
