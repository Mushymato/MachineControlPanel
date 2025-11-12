using Microsoft.Xna.Framework.Graphics;

namespace MachineControlPanel.Integration;

public interface IMobilePhoneApi
{
    bool AddApp(string id, string name, Action action, Texture2D icon);
}
