using Microsoft.Xna.Framework;
using StardewUI;
using StardewUI.Events;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Overlays;
using StardewUI.Widgets;
using StardewValley;

namespace MachineControlPanel.Framework.UI
{
    internal sealed class OutputModalButton : Button
    {
        private readonly List<IView> outputPanels;
        private static readonly Sprite bgSprite = new(Game1.mouseCursors, new Rectangle(392, 361, 10, 11));
        private static readonly Sprite bgHoverSprite = new(Game1.mouseCursors, new Rectangle(402, 361, 10, 11));
        public OutputModalButton(List<IView> outputPanels) : base()
        {
            this.outputPanels = outputPanels;
            LeftClick += OpenOutputModal;
            Layout = LayoutParameters.FixedSize(40, 44);
            Margin = new(16, 14);
            Tooltip = I18n.RuleList_MoreOutputs(outputPanels.Count);
        }
        private void OpenOutputModal(object? sender, ClickEventArgs e)
        {
            var overlay = new OutputModal(outputPanels);
            Overlay.Push(overlay);
        }
    }

    internal sealed class OutputModal(List<IView> outputPanels) : FullScreenOverlay
    {
        private const int GRID_COUNT = 8;
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

            return new Frame()
            {
                Background = UiSprites.MenuBackground,
                Border = UiSprites.MenuBorder,
                BorderThickness = UiSprites.MenuBorderThickness,
                Content = new ScrollableView()
                {
                    Layout = LayoutParameters.FixedSize(GRID_COUNT * gridSize.X, menuHeight),
                    Content = new Grid()
                    {
                        Children = outputPanels,
                        ItemLayout = new GridItemLayout.Count(GRID_COUNT),
                    }
                }
            };
        }
    }
}