using MachineControlPanel.Framework.UI.Integration;
using Microsoft.Xna.Framework;
using StardewUI;
using StardewUI.Events;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Overlays;
using StardewUI.Widgets;
using StardewValley;
using StardewValley.GameData.Machines;
using StardewValley.ItemTypeDefinitions;

namespace MachineControlPanel.Framework.UI;

internal sealed class MachineSelect(
    Action<string, IEnumerable<RuleIdent>, IEnumerable<string>, bool[]> saveMachineRules,
    Action<bool> exitThisMenu,
    Action<HoveredItemPanel>? setHoverEvents = null
) : ComponentView<Panel>
{
    private const int GUTTER = 400;
    private int gridCount = 12;
    internal static readonly Sprite CloseButton = new(Game1.mouseCursors, new(337, 494, 12, 12));

    /// <summary>
    /// Make machine select grid view
    /// </summary>
    /// <returns></returns>
    protected override Panel CreateView()
    {
        List<IView> cells = CreateMachineSelectCells();
        // derive the desired width and height
        xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
        cells.First().Measure(new(viewportSize.Width, viewportSize.Height));
        Vector2 gridSize = cells.First().ActualBounds.Size;
        gridCount = (int)MathF.Min(gridCount, MathF.Floor((viewportSize.Width - GUTTER) / gridSize.X));
        float menuWidth = gridCount * gridSize.X;
        float menuHeight = MathF.Min(
            MathF.Max(400, viewportSize.Height - gridSize.X),
            // min needed height
            MathF.Ceiling((float)cells.Count / gridCount) * gridSize.Y
        );

        ScrollableView scrollableView =
            new()
            {
                Name = "MachineSelect.View",
                Layout = LayoutParameters.FixedSize(menuWidth, menuHeight),
                Content = new Grid()
                {
                    Name = "MachineSelect.Grid",
                    ItemLayout = new GridItemLayout.Count(gridCount),
                    Children = cells,
                },
            };
        Panel wrapper =
            new()
            {
                Layout = LayoutParameters.FitContent(),
                VerticalContentAlignment = Alignment.Middle,
                Children = [scrollableView],
            };
        Button closeBtn =
            new()
            {
                Margin = new Edges(Left: 96),
                Layout = LayoutParameters.FixedSize(48, 48),
                DefaultBackground = CloseButton,
            };
        closeBtn.LeftClick += ExitMenu;
        wrapper.FloatingElements.Add(new(closeBtn, FloatingPosition.AfterParent));

        return wrapper;
    }

    private List<IView> CreateMachineSelectCells()
    {
        var machinesData = DataLoader.Machines(Game1.content);
        List<IView> cells = [];
        int hidden = 0;
        foreach ((string qId, MachineData machine) in machinesData)
        {
            if (ItemRegistry.GetData(qId) is not ParsedItemData itemData)
                continue;
            if (ModEntry.Config.ProgressionMode && !PlayerHasItemCache.HasItem(qId))
            {
                hidden++;
                continue;
            }

            if (
                RuleHelperCache.TryGetRuleHelper(
                    itemData.QualifiedItemId,
                    itemData.DisplayName,
                    machine,
                    out RuleHelper? ruleHelper
                )
            )
            {
                MachineCell cell = new(ruleHelper, itemData) { Name = $"MachineSelect.{qId}" };
                cell.LeftClick += ShowPanel;
                setHoverEvents?.Invoke(cell);
                cells.Add(cell);
            }
        }
        if (hidden > 0)
        {
            Frame hiddenCnt =
                new()
                {
                    Padding = new(16),
                    Background = MachineCell.bgSprite,
                    BorderThickness = MachineCell.bgSprite.FixedEdges!,
                    Focusable = true,
                    Tooltip = I18n.MachineSelect_HiddenByProgressionMode(),
                    Content = new Label() { Text = $"+{hidden}" },
                    Layout = LayoutParameters.FixedSize(64, 128),
                    BackgroundTint = Color.White * 0.5f,
                    HorizontalContentAlignment = Alignment.Middle,
                    VerticalContentAlignment = Alignment.Middle,
                };
            cells.Add(hiddenCnt);
        }
        return cells;
    }

    /// <summary>
    /// Show a rule list panel for the machine
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowPanel(object? sender, ClickEventArgs e)
    {
        if (sender is MachineCell machineCell)
        {
            if (machineCell.ruleHelper.GetRuleEntries())
            {
                var overlay = new RuleListOverlay(
                    machineCell.ruleHelper,
                    saveMachineRules,
                    setHoverEvents,
                    machineCell.UpdateEdited
                );
                Overlay.Push(overlay);
            }
        }
    }

    /// <summary>
    /// Exit this menu
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ExitMenu(object? sender, ClickEventArgs e)
    {
        exitThisMenu(true);
    }
}
