using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace MachineControlPanel;

public sealed class Overlay(int screenId)
{
    public bool Enabled
    {
        get => field;
        set
        {
            field = value;
            if (field)
                ModEntry.help.Events.Display.RenderedWorld += OnRenderedWorld;
            else
                ModEntry.help.Events.Display.RenderedWorld -= OnRenderedWorld;
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

        var square = new Point(Game1.tileSize - 2, Game1.tileSize - 2);

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
    }
}
