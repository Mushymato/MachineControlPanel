using Microsoft.Xna.Framework;
using StardewUI;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Overlays;
using StardewUI.Widgets;
using StardewValley;

namespace MachineControlPanel.Framework.UI.Modal;

internal sealed class ScrollableGridModal(IList<IView> outputPanels) : FullScreenOverlay
{
    private const int GRID_COUNT = 6;
    internal string? Heading = null;

    protected override Frame CreateView()
    {
        xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
        outputPanels.First().Measure(new(viewportSize.Width, viewportSize.Height));
        Vector2 gridSize = outputPanels.First().ActualBounds.Size;
        float menuHeight = MathF.Min(
            MathF.Max(400, viewportSize.Height - gridSize.X),
            // min needed height
            MathF.Ceiling((float)outputPanels.Count / GRID_COUNT) * gridSize.Y
        );

        IView content = new ScrollableView()
        {
            Layout = LayoutParameters.FixedSize(GRID_COUNT * gridSize.X, menuHeight),
            Content = new Grid()
            {
                Layout = LayoutParameters.FitContent(),
                Children = outputPanels,
                ItemLayout = new GridItemLayout.Count(GRID_COUNT),
                HorizontalItemAlignment = Alignment.Middle,
            },
        };
        if (Heading != null)
        {
            content = new Lane()
            {
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = Alignment.Middle,
                Children =
                [
                    new Banner() { Text = Heading },
                    new Image()
                    {
                        Layout = new() { Width = Length.Stretch(), Height = Length.Px(RuleListView.ThinHDivider.Size.Y) },
                        Fit = ImageFit.Stretch,
                        Sprite = RuleListView.ThinHDivider,
                    },
                    content,
                ],
            };
        }

        return new Frame()
        {
            Background = UiSprites.MenuBackground,
            Border = UiSprites.MenuBorder,
            BorderThickness = UiSprites.MenuBorderThickness,
            Content = content,
        };
    }
}
