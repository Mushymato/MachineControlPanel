using StardewValley;

namespace MachineControlPanel.Integration;

/// <summary>Duck types for StardewUI</summary>

public record SDUITooltipData(string Text, string? Title = null, Item? Item = null);
