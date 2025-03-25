using Microsoft.Xna.Framework;

namespace MachineControlPanel.Integration;

/// <inheritdoc />
public interface IIconicFrameworkApi
{
    /// <summary>Adds an icon.</summary>
    /// <param name="id">A unique identifier for the icon.</param>
    /// <param name="texturePath">The path to the texture icon.</param>
    /// <param name="sourceRect">The source rectangle of the icon.</param>
    /// <param name="getTitle">Text to appear as the title in the Radial Menu.</param>
    /// <param name="getDescription">Text to appear when hovering over the icon.</param>
    /// <param name="onClick">An action to perform when the icon is pressed.</param>
    /// <param name="onRightClick">An optional secondary action to perform when the icon is pressed.</param>
    public void AddToolbarIcon(
        string id,
        string texturePath,
        Rectangle? sourceRect,
        Func<string>? getTitle,
        Func<string>? getDescription,
        Action onClick,
        Action? onRightClick = null
    );
}
