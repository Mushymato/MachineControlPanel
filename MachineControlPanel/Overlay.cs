using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;

namespace MachineControlPanel;

public sealed class Overlay
{
    public bool Enabled { get; set; } = false;

    public bool TryEnable()
    {
        if (Enabled) return true;
        if (Game1.currentLocation == null) return false;

        foreach (var (pos, obj) in Game1.currentLocation.Objects.Pairs)
        {
            if (ModSaveData.MachineHasData(obj))
            {
                Enabled = true;
                return true;
            }
        }
        return false;
    }

    public void Draw(SpriteBatch b)
    {
        if (!Enabled) return;
        if (Game1.currentLocation == null) return;

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
                    Utility.DrawSquare(b,
                        pixelArea: new(point, square),
                        borderWidth: 0,
                        backgroundColor: new(255, 128, 255, 64)
                    );
                }
                else
                {
                    Utility.DrawSquare(b,
                        pixelArea: new(point, square),
                        borderWidth: 0,
                        backgroundColor: new(0, 0, 0, 64)
                    );
                }
            }
        }
    }
}
