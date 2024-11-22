using System.Collections.Immutable;
using MachineControlPanel.Framework.UI.Integration;
using MachineControlPanel.Framework.UI.Modal;
using Microsoft.Xna.Framework;
using StardewUI;
using StardewUI.Events;
using StardewUI.Graphics;
using StardewUI.Layout;
using StardewUI.Widgets;
using StardewValley;

namespace MachineControlPanel.Framework.UI;

internal sealed record ContentChangeArgs(IView? Nextview);

internal sealed class RuleListView(
    RuleHelper ruleHelper,
    Action<string, IEnumerable<RuleIdent>, IEnumerable<string>, bool[]> saveMachineRules,
    Action<bool>? exitThisMenu = null,
    Action<HoveredItemPanel>? setHoverEvents = null,
    Action? updateEdited = null
) : ComponentView<Lane>, IPageable
{
    /// Geometry
    private const int ROW_MARGIN = 4;
    private const int COL_MARGIN = 6;
    private const int ROW_W = 64 + ROW_MARGIN * 2;
    private const int BOX_W = ROW_MARGIN * 3;
    private const int MIN_HEIGHT = 400;
    private const int GUTTER_HEIGHT = 400;
    private const int OUTPUT_MAX = 6;
    private static Sprite RightCaret => new(Game1.mouseCursors, new(448, 96, 24, 32));
    internal static Sprite ThinHDivider => new(Game1.menuTexture, SourceRect: new(64, 412, 64, 8));
    internal static Sprite ThinVDivider => new(Game1.menuTexture, SourceRect: new(156, 384, 8, 64));
    private static Sprite TabButton => new(Game1.menuTexture, new(0, 256, 44, 60), new(16, 16, 0, 16));
    internal static readonly Sprite OutputGroup = new(Game1.mouseCursors, new(403, 373, 9, 9), new(2), new(Scale: 4));
    private static readonly IReadOnlyList<Sprite> Digits = Enumerable
        .Range(0, 10)
        .Select((digit) => new Sprite(Game1.mouseCursors, new Rectangle(368 + digit * 5, 56, 5, 7)))
        .ToImmutableList();
    private readonly Edges tabButtonPassive = new(Bottom: ROW_MARGIN * 2);
    private readonly Edges tabButtonActive = new(Bottom: ROW_MARGIN * 2, Right: -6);

    private static LayoutParameters IconLayout => LayoutParameters.FixedSize(64, 64);
    internal readonly Dictionary<RuleIdent, CheckBox> ruleCheckBoxes = [];
    internal readonly List<InputCheckable> inputChecks = [];
    internal readonly List<QualityCheckable> qualityChecks = [];
    private ScrollableView? container;
    private Lane? rulesList = null;
    private Lane? inputsGrid = null;
    private bool implicitOffDirty = false;
    private Button? rulesBtn = null;
    private Button? inputsBtn = null;
    private Button? toggleAllBtn = null;

    /// <summary>
    /// Create rule list view, with tabs and footer buttons
    /// </summary>
    /// <returns></returns>
    protected override Lane CreateView()
    {
        xTile.Dimensions.Size viewportSize = Game1.uiViewport.Size;
        float menuHeight = MathF.Max(MIN_HEIGHT, viewportSize.Height - GUTTER_HEIGHT);
        rulesList = CreateRulesList(viewportSize, ref menuHeight);

        List<IView> vItems = [];
        List<IView> hItems = [];
        container = new()
        {
            Name = "RuleList.Scroll",
            Layout = new() { Width = Length.Content(), Height = Length.Px(menuHeight) },
            Content = rulesList,
        };

        Frame scrollBox =
            new()
            {
                Name = "RuleList.Frame",
                Layout = LayoutParameters.FitContent(),
                Background = UiSprites.MenuBackground,
                Border = UiSprites.MenuBorder,
                BorderThickness = UiSprites.MenuBorderThickness,
                Content = container,
            };

        if (ruleHelper.ValidInputs.Any())
        {
            inputsGrid = CreateInputsGrid();
            container.Measure(new(viewportSize.Width, viewportSize.Height));
            inputsGrid.Layout = new() { Width = Length.Px(rulesList!.ContentSize.X), Height = Length.Content() };
            scrollBox.FloatingElements.Add(new(CreateSidebar(), FloatingPosition.BeforeParent));

            if (ModEntry.Config.DefaultPage == DefaultPageOption.Inputs)
                container.Content = inputsGrid;
            UpdateTabButtons();
        }

        Banner banner =
            new()
            {
                Layout = LayoutParameters.FitContent(),
                Margin = new(Top: -85),
                Padding = new(12),
                Background = UiSprites.BannerBackground,
                BackgroundBorderThickness = (UiSprites.BannerBackground.FixedEdges ?? Edges.NONE) * (UiSprites.BannerBackground.SliceSettings?.Scale ?? 1),
                Text = ruleHelper.Name,
            };

        vItems.Add(banner);
        vItems.Add(scrollBox);
        vItems.Add(CreateFooter());

        Lane center =
            new()
            {
                Name = "RuleList.Body",
                Layout = LayoutParameters.FitContent(),
                Orientation = Orientation.Vertical,
                HorizontalContentAlignment = Alignment.Middle,
                VerticalContentAlignment = Alignment.Middle,
                Children = vItems,
            };
        if (exitThisMenu != null)
        {
            Button closeBtn =
                new()
                {
                    DefaultBackground = MachineSelect.CloseButton,
                    Margin = new Edges(Left: 48),
                    Layout = LayoutParameters.FixedSize(48, 48),
                };
            closeBtn.LeftClick += ExitMenu;
            center.FloatingElements.Add(new(closeBtn, FloatingPosition.AfterParent));
        }

        return center;
    }

    /// <summary>
    /// Create the sidebar for changing pages
    /// </summary>
    /// <returns></returns>
    private Lane CreateSidebar()
    {
        rulesBtn = new()
        {
            DefaultBackground = TabButton,
            Name = "RulesBtn",
            Content = new Label() { Text = I18n.RuleList_Rules(), Margin = new(Left: 12) },
            Layout = LayoutParameters.FixedSize(108, 64),
            Margin = tabButtonActive,
        };
        rulesBtn.LeftClick += ShowRules;
        inputsBtn = new()
        {
            DefaultBackground = TabButton,
            Name = "InputsBtn",
            Content = new Label() { Text = I18n.RuleList_Inputs(), Margin = new(Left: 12) },
            Layout = LayoutParameters.FixedSize(108, 64),
            Margin = tabButtonPassive,
        };
        inputsBtn.LeftClick += ShowInputs;

        return new Lane()
        {
            Layout = new() { Width = Length.Px(108), Height = Length.Content() },
            Padding = new(Top: 32),
            Margin = new(Right: -20),
            HorizontalContentAlignment = Alignment.End,
            Orientation = Orientation.Vertical,
            Children = [rulesBtn, inputsBtn],
            ZIndex = 2,
        };
    }

    private string GetToggleButtonText()
    {
        bool prevCheck = false;
        if (container!.Content == inputsGrid)
        {
            var firstNotImplicitOff = inputChecks.FirstOrDefault((ic) => !ic.IsImplicitOff);
            if (firstNotImplicitOff != null)
            {
                prevCheck = firstNotImplicitOff.IsChecked;
            }
        }
        else
            prevCheck = ruleCheckBoxes.First().Value.IsChecked;
        return prevCheck ? I18n.RuleList_DisableAll() : I18n.RuleList_EnableAll();
    }

    /// <summary>
    /// Move tab position depending on current page
    /// </summary>
    private void UpdateTabButtons()
    {
        if (rulesBtn != null && inputsBtn != null)
        {
            if (container!.Content == inputsGrid)
            {
                rulesBtn.Margin = tabButtonPassive;
                inputsBtn.Margin = tabButtonActive;
            }
            else
            {
                rulesBtn.Margin = tabButtonActive;
                inputsBtn.Margin = tabButtonPassive;
            }
            rulesBtn.Text = ruleHelper.HasDisabledRules ? I18n.RuleList_Rules() + I18n.RuleList_Edited() : I18n.RuleList_Rules();
            inputsBtn.Text = ruleHelper.HasDisabledInputs ? I18n.RuleList_Inputs() + I18n.RuleList_Edited() : I18n.RuleList_Inputs();
            if (toggleAllBtn != null)
                toggleAllBtn.Text = GetToggleButtonText();
        }
    }

    /// <summary>
    /// Make footer buttons for save/reset of rules
    /// </summary>
    /// <returns></returns>
    private Lane CreateFooter()
    {
        List<IView> children;
        if (Game1.IsMasterGame)
        {
            toggleAllBtn = new()
            {
                HoverBackground = UiSprites.ButtonLight,
                Name = "ToggleAllBtn",
                Text = GetToggleButtonText(),
                Margin = new(ROW_MARGIN),
            };
            toggleAllBtn.LeftClick += ToggleAllChecks;
            Button resetBtn =
                new()
                {
                    HoverBackground = UiSprites.ButtonLight,
                    Name = "ResetBtn",
                    Text = I18n.RuleList_Reset(),
                    Margin = new(ROW_MARGIN),
                };
            resetBtn.LeftClick += ResetRules;
            if (ModEntry.Config.SaveOnChange)
                children = [toggleAllBtn, resetBtn];
            else
            {
                Button saveBtn =
                    new()
                    {
                        HoverBackground = UiSprites.ButtonLight,
                        Name = "SaveBtn",
                        Text = I18n.RuleList_Save(),
                        Margin = new(ROW_MARGIN),
                    };
                saveBtn.LeftClick += SaveRules;
                children = [toggleAllBtn, saveBtn, resetBtn];
            }
        }
        else
        {
            children =
            [
                new Frame()
                {
                    Padding = new(12),
                    Background = UiSprites.ButtonDark,
                    BorderThickness = UiSprites.ButtonLight.FixedEdges!,
                    Content = new Label() { Text = I18n.RuleList_FooterNote() },
                },
            ];
        }

        return new Lane()
        {
            Layout = LayoutParameters.FitContent(),
            Orientation = Orientation.Horizontal,
            Children = children,
        };
    }

    /// <summary>
    /// Change to rules list on click
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowRules(object? sender, ClickEventArgs e)
    {
        if (sender is Button)
        {
            container!.Content = rulesList;
            Game1.playSound("smallSelect");
            if (ModEntry.Config.SaveOnChange)
                SaveRulesUI();
            UpdateTabButtons();
        }
    }

    /// <summary>
    /// Change to inputs on click
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ShowInputs(object? sender, ClickEventArgs e)
    {
        if (sender is Button)
        {
            container!.Content = inputsGrid;
            Game1.playSound("smallSelect");
            if (ModEntry.Config.SaveOnChange)
                SaveRulesUI();
            if (implicitOffDirty)
            {
                foreach (InputCheckable checkable in inputChecks)
                    checkable.IsImplicitOff = ruleHelper.IsImplicitDisabled(checkable.Idents);
                implicitOffDirty = false;
            }
            UpdateTabButtons();
        }
    }

    /// <summary>Save rules</summary>
    internal void SaveAllRules()
    {
        bool[] quality = new bool[5];
        foreach (QualityCheckable checkable in qualityChecks)
        {
            quality[checkable.Quality] = !checkable.IsChecked;
        }
        saveMachineRules(
            ruleHelper.QId,
            ruleCheckBoxes.Where((kv) => !kv.Value.IsChecked).Select((kv) => kv.Key),
            inputChecks.Where((ic) => !ic.IsChecked).Select((ic) => ic.QId),
            quality
        );
        implicitOffDirty = true;
    }

    /// <summary>Save rules and update parts of UI</summary>
    internal void SaveRulesUI()
    {
        SaveAllRules();
        updateEdited?.Invoke();
        UpdateTabButtons();
    }

    /// <summary>
    /// Save rules on button press
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SaveRules(object? sender, ClickEventArgs e)
    {
        if (sender is not Button)
            return;
        Game1.playSound("bigSelect");
        SaveRulesUI();
    }

    /// <summary>
    /// Reset saved rules
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ResetRules(object? sender, ClickEventArgs e)
    {
        if (sender is not Button)
            return;
        Game1.playSound("bigDeSelect");

        foreach (var kv in ruleCheckBoxes)
            kv.Value.IsChecked = true;
        foreach (var ic in inputChecks)
        {
            ic.IsImplicitOff = false;
            ic.IsChecked = true;
        }
        foreach (var qc in qualityChecks)
            qc.IsChecked = true;

        saveMachineRules(ruleHelper.QId, [], [], new bool[5]);
        updateEdited?.Invoke();

        UpdateTabButtons();
    }

    private void ToggleAllChecks(object? sender, ClickEventArgs e)
    {
        if (sender is not Button toggleBtn)
            return;
        Game1.playSound("bigDeSelect");
        bool prevCheck = false;
        if (container!.Content == inputsGrid)
        {
            var firstNotImplicitOff = inputChecks.FirstOrDefault((ic) => !ic.IsImplicitOff);
            if (firstNotImplicitOff != null)
            {
                prevCheck = firstNotImplicitOff.IsChecked;
                foreach (var ic in inputChecks)
                {
                    if (!ic.IsImplicitOff)
                        ic.IsChecked = !prevCheck;
                }
            }
            else
            {
                return;
            }
        }
        else
        {
            prevCheck = ruleCheckBoxes.First().Value.IsChecked;
            foreach (var kv in ruleCheckBoxes)
                kv.Value.IsChecked = !prevCheck;
        }
        toggleBtn.Text = prevCheck ? I18n.RuleList_EnableAll() : I18n.RuleList_DisableAll();
    }

    /// <summary>
    /// Exit menu on click
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ExitMenu(object? sender, ClickEventArgs e)
    {
        exitThisMenu!(true);
    }

    /// <summary>
    /// Make inputs page
    /// </summary>
    /// <returns></returns>
    private Lane CreateInputsGrid()
    {
        List<IView> qualityChecksUI = [];
        foreach (int quality in new int[] { SObject.lowQuality, SObject.medQuality, SObject.highQuality, SObject.bestQuality })
        {
            QualityCheckable qualityCheckable =
                new(quality, Game1.IsMasterGame) { IsChecked = !ruleHelper.HasDisabledQuality(quality), Margin = new(ROW_MARGIN) };
            qualityChecksUI.Add(qualityCheckable);
            if (Game1.IsMasterGame)
                qualityChecks.Add(qualityCheckable);
        }
        Lane qualities =
            new()
            {
                Name = "InputsGrid.Quality",
                Orientation = Orientation.Horizontal,
                Children = qualityChecksUI,
            };
        Image divider =
            new()
            {
                Layout = new() { Width = Length.Stretch(), Height = Length.Px(ThinHDivider.Size.Y) },
                Fit = ImageFit.Stretch,
                Sprite = ThinHDivider,
            };
        List<IView> inputChecksUI = [];
        foreach ((string key, ValidInput input) in ruleHelper.ValidInputs)
        {
            Panel itemPanel = FormRuleItemPanel(input.Rule, showDigits: false);
            itemPanel.Margin = new(ROW_MARGIN);
            InputCheckable inputCheck =
                new(input, itemPanel, Game1.IsMasterGame)
                {
                    IsChecked = !ruleHelper.HasDisabledInput(key),
                    IsImplicitOff = ruleHelper.IsImplicitDisabled(input.Idents),
                };
            inputChecksUI.Add(inputCheck.Content);
            if (Game1.IsMasterGame)
                inputChecks.Add(inputCheck);
        }
        Grid inputs =
            new()
            {
                Name = "InputsGrid.Inputs",
                ItemLayout = new GridItemLayout.Length(ROW_W),
                Children = inputChecksUI,
            };

        return new Lane()
        {
            Name = "InputsGrid",
            Orientation = Orientation.Vertical,
            Children = [qualities, divider, inputs],
        };
    }

    /// <summary>
    /// Make rules page
    /// </summary>
    /// <param name="viewportSize"></param>
    /// <param name="menuHeight"></param>
    /// <returns></returns>
    private Lane CreateRulesList(xTile.Dimensions.Size viewportSize, ref float menuHeight)
    {
        ruleCheckBoxes.Clear();
        List<RuleEntry> rules = ruleHelper.RuleEntries;
        List<List<RuleEntry>> rulesColumns;
        int colSize = (int)((menuHeight - BOX_W) / ROW_W) - 2;
        if (rules.Count <= colSize)
        {
            menuHeight = MathF.Max(MIN_HEIGHT, ROW_W * rules.Count + BOX_W);
            rulesColumns = [rules];
        }
        else
        {
            float colByWidth = MathF.Ceiling(viewportSize.Width / 640);
            float colByCount = MathF.Ceiling(rules.Count / colSize) + 1;
            if (colByCount > colByWidth)
            {
                colSize = (int)MathF.Ceiling(rules.Count / colByWidth);
            }
            else
            {
                colSize = (int)MathF.Ceiling(rules.Count / colByCount);
                menuHeight = MathF.Max(MIN_HEIGHT, ROW_W * colSize + BOX_W);
            }
            rulesColumns = [];
            for (int i = 0; i < rules.Count; i += colSize)
            {
                rulesColumns.Add(rules.GetRange(i, Math.Min(colSize, rules.Count - i)));
            }
        }

        List<IView> columns = [];
        int seq = 0;
        foreach (var rulesC in rulesColumns)
        {
            int inputSize = rulesC.Max((rule) => rule.Inputs.Count);
            int outputSize = rulesC.Max((rule) => rule.Outputs.Count > OUTPUT_MAX ? 1 : rule.Outputs.Count);
            LayoutParameters inputLayout = new() { Width = Length.Px(ROW_W * inputSize + ROW_MARGIN * 2), Height = Length.Content() };
            LayoutParameters outputLayout = new() { Width = Length.Px(ROW_W * outputSize + ROW_MARGIN * 2), Height = Length.Content() };

            if (columns.Any())
            {
                columns.Add(
                    new Image()
                    {
                        Layout = new() { Width = Length.Px(ThinVDivider.Size.X), Height = Length.Stretch() },
                        Fit = ImageFit.Stretch,
                        Sprite = ThinVDivider,
                    }
                );
            }
            columns.Add(
                new Lane()
                {
                    Name = $"RuleListColumn_{++seq}",
                    Orientation = Orientation.Vertical,
                    Children = rulesC.Select((rule) => CreateRuleListEntry(rule, inputLayout, outputLayout)).ToList(),
                    Margin = new(COL_MARGIN),
                }
            );
        }

        return new Lane()
        {
            Name = "RuleList",
            // Layout = new() { Width = Length.Content(), Height = Length.Stretch() },
            Layout = LayoutParameters.FitContent(),
            Orientation = Orientation.Horizontal,
            Children = columns,
        };
    }

    /// <summary>
    /// Make a single entry in rules
    /// </summary>
    /// <param name="rule"></param>
    /// <param name="inputLayout"></param>
    /// <param name="outputLayout"></param>
    /// <returns></returns>
    private IView CreateRuleListEntry(RuleEntry rule, LayoutParameters inputLayout, LayoutParameters outputLayout)
    {
        List<IView> children = [];
        if (Game1.IsMasterGame)
        {
            if (ruleCheckBoxes.ContainsKey(rule.Ident))
            {
                children.Add(ruleCheckBoxes[rule.Ident]);
            }
            else
            {
                CheckBox checkBox =
                    new()
                    {
                        IsChecked = !ruleHelper.HasDisabledRule(rule.Ident),
#if DEBUG
                        Tooltip = $"O: {rule.Ident.OutputId}\nT: {rule.Ident.TriggerId}",
#endif
                    };
                ruleCheckBoxes[rule.Ident] = checkBox;
                children.Add(checkBox);
            }
        }
        else
        {
            children.Add(
                new Image()
                {
                    Sprite = ruleHelper.HasDisabledRule(rule.Ident) ? UiSprites.CheckboxUnchecked : UiSprites.CheckboxChecked,
                    Tint = Color.White * 0.5f,
                    Layout = LayoutParameters.FitContent(),
                    Focusable = false,
                }
            );
        }

        children.Add(
            new Lane()
            {
                Name = $"{rule.Repr}.Inputs",
                Layout = inputLayout,
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = Alignment.End,
                VerticalContentAlignment = Alignment.Middle,
                Children = FormRuleItemPanels(rule.Inputs),
            }
        );

        children.Add(
            new Image()
            {
                Name = $"{rule.Repr}.Arrow",
                Layout = LayoutParameters.FitContent(),
                Padding = new(20, 16),
                Sprite = RightCaret,
            }
        );

        List<IView> outputPanels = FormRuleItemPanels(rule.Outputs);
        if (outputPanels.Count > OUTPUT_MAX)
            outputPanels = [new OverflowOutputModalButton(outputPanels)];
        children.Add(
            new Lane()
            {
                Name = $"{rule.Repr}.Outputs",
                Layout = outputLayout,
                Orientation = Orientation.Horizontal,
                HorizontalContentAlignment = Alignment.Start,
                VerticalContentAlignment = Alignment.Middle,
                Children = outputPanels,
                Margin = new(Left: ROW_MARGIN * 1),
            }
        );

        return new Lane()
        {
            Name = $"{rule.Repr}.Lane",
            Layout = LayoutParameters.FitContent(),
            Orientation = Orientation.Horizontal,
            Children = children,
            Margin = new(Left: ROW_MARGIN * 3),
            HorizontalContentAlignment = Alignment.Start,
            VerticalContentAlignment = Alignment.Middle,
        };
    }

    /// <summary>
    /// Make a horizontal lane of RuleItems
    /// </summary>
    /// <param name="ruleItems"></param>
    /// <param name="prefix"></param>
    /// <returns></returns>
    private List<IView> FormRuleItemPanels(List<RuleItem> ruleItems)
    {
        List<IView> content = [];
        foreach (var ruleItem in ruleItems)
        {
            HoveredItemPanel itemPanel = FormRuleItemPanel(ruleItem);
            itemPanel.Margin = new(ROW_MARGIN);
            content.Add(itemPanel);

            if (ruleItem.Extra != null)
                itemPanel.ExtraItems = ruleItem.Extra.Select<RuleItem, IView>((rule) => FormRuleItemPanel(rule)).ToList();
        }
        return content;
    }

    /// <summary>
    /// Make a Panel for a rule item, can have several images on top of each other
    /// </summary>
    /// <param name="ruleItem"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private HoveredItemPanel FormRuleItemPanel(RuleItem ruleItem, bool showDigits = true)
    {
        List<IView> iconImgs = [];
        foreach (var icon in ruleItem.Icons)
        {
            iconImgs.Add(
                new Image()
                {
                    Layout = LayoutParameters.FixedSize(icon.Img.Size.X * icon.Scale, icon.Img.Size.X * icon.Scale),
                    Padding = icon.Edge,
                    Sprite = icon.Img,
                    Tint = icon.Tint ?? Color.White,
                }
            );
        }
        HoveredItemPanel itemPanel =
            new()
            {
                Layout = IconLayout,
                Children = iconImgs,
                Tooltip = ruleItem.GetTooltipData(),
                Focusable = true,
                HoveredItem = ruleItem.Item,
            };
        if (showDigits && ruleItem.Count > 1)
        {
            int num = ruleItem.Count;
            int offset = 44;
            while (num > 0)
            {
                // final digit
                int digit = num % 10;
                itemPanel.Children.Add(
                    new Image()
                    {
                        Layout = LayoutParameters.FixedSize(15, 21),
                        Padding = new(Left: offset, Top: 48),
                        Sprite = Digits[digit],
                    }
                );
                // unclear why this looks the best, shouldnt it be scale * 5?
                offset -= 12;
                num /= 10;
            }
        }
        setHoverEvents?.Invoke(itemPanel);
        return itemPanel;
    }

    /// <summary>
    /// Implement gamepad R to next page
    /// </summary>
    /// <returns></returns>
    public bool NextPage()
    {
        if (container!.Content == rulesList)
        {
            container!.Content = inputsGrid;
            UpdateTabButtons();
            Game1.playSound("smallSelect");
            return true;
        }
        return false;
    }

    /// <summary>
    /// Implement gamepad L to prev page
    /// </summary>
    /// <returns></returns>
    public bool PreviousPage()
    {
        if (container!.Content == inputsGrid)
        {
            container!.Content = rulesList;
            UpdateTabButtons();
            Game1.playSound("smallSelect");
            return true;
        }
        return false;
    }
}
