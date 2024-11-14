using Microsoft.Xna.Framework;
using StardewUI.Events;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;

namespace MachineControlPanel.Framework.UI;

internal sealed class QualityCheckable : Image
{
    private bool isChecked = true;
    internal bool IsChecked
    {
        get => isChecked;
        set
        {
            if (value == isChecked)
                return;
            isChecked = value;
            UpdateTint();
        }
    }
    internal readonly int Quality;
    internal readonly Color BaseColor = Color.White;

    internal QualityCheckable(int quality, bool canCheck)
        : base()
    {
        Quality = quality;
        Sprite = RuleHelper.Quality(quality);
        Layout = LayoutParameters.FixedSize(Sprite.Size.X * 4, Sprite.Size.X * 4);
        Focusable = true;
        if (canCheck)
            LeftClick += OnLeftClick;
        if (Quality == 0)
            BaseColor = Color.Black * 0.5f;
        Tint = BaseColor;
    }

    private void OnLeftClick(object? sender, ClickEventArgs e)
    {
        Game1.playSound("drumkit6");
        IsChecked = !isChecked;
    }

    private void UpdateTint()
    {
        Tint = isChecked ? BaseColor : InputCheckable.COLOR_DISABLED;
    }
}
