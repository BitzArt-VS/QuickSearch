using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// Renders the settings form for a single tooltip, pushed onto the
/// <see cref="ModConfigPageNavigator"/> stack by <see cref="TooltipsModConfigPage"/>
/// when the user selects an entry from the list. Receives the specific
/// <see cref="TooltipOptions"/> instance to edit via the cascading-value chain
/// (published by the fragment stored in the navigator at push time).
/// </summary>
internal sealed class TooltipDetailModConfigPage : GuiComponent
{
    private const double LabelColumnWidth = 224;
    private const double RowHeight = 28;
    private const double RowSpacing = 8;
    private const double SectionSpacing = 16;
    private const double SectionRuleGap = 4;

    private static readonly CultureInfo InvCulture = CultureInfo.InvariantCulture;
    private static readonly GuiColor SectionSeparatorColor = GuiColor.FromRgba(0.78, 0.69, 0.58, 0.11);

    private static readonly string[] HorizontalAlignmentItems = ["Left", "Center", "Right"];
    private static readonly string[] VerticalAlignmentItems = ["Top", "Middle", "Bottom"];

    public TooltipOptions? Options { get; set; }

    private ModConfigContext? _context;
    private TooltipOptions? _options;

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        builder.ConfigureLayout(layout => layout.Padding = new(8));
    }

    public override void OnParametersSet()
    {
        _context = GetCascadingValue<ModConfigContext>();
        _options = Options;
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        if (_context is null || _options is null)
        {
            return;
        }

        var options = _options;

        BuildSectionLabel(builder, key: 100, Lang.Get($"{Constants.ModId}:config-tooltip-section-general"), isFirst: true);

        BuildSettingRow(builder, key: 1,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-enable"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-enable-tooltip")),
            control: b => b.AddCheckbox(0,
                checked_: options.Enable,
                onCheckedChanged: value =>
                {
                    options.Enable = value;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Enable));
                    _context!.SaveConfig();
                }));

        BuildSectionLabel(builder, key: 110, Lang.Get($"{Constants.ModId}:config-tooltip-section-layout"));

        BuildSettingRow(builder, key: 2,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area-horizontal"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area-horizontal-tooltip")),
            control: b => b.AddDropdown<string>(0,
                items: HorizontalAlignmentItems,
                selectedIndex: Math.Max(0, Array.IndexOf(HorizontalAlignmentItems, ExtractHorizontalAlignment(options.DialogArea))),
                onSelectionChanged: idx =>
                {
                    options.DialogArea = CombineDialogArea(HorizontalAlignmentItems[idx], ExtractVerticalAlignment(options.DialogArea));
                    options.NotifyPropertyChanged(nameof(TooltipOptions.DialogArea));
                    _context!.SaveConfig();
                },
                widthMode: GuiSizeMode.Fill));

        BuildSettingRow(builder, key: 16,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area-vertical"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area-vertical-tooltip")),
            control: b => b.AddDropdown<string>(0,
                items: VerticalAlignmentItems,
                selectedIndex: Math.Max(0, Array.IndexOf(VerticalAlignmentItems, ExtractVerticalAlignment(options.DialogArea))),
                onSelectionChanged: idx =>
                {
                    options.DialogArea = CombineDialogArea(ExtractHorizontalAlignment(options.DialogArea), VerticalAlignmentItems[idx]);
                    options.NotifyPropertyChanged(nameof(TooltipOptions.DialogArea));
                    _context!.SaveConfig();
                },
                widthMode: GuiSizeMode.Fill));

        BuildSettingRow(builder, key: 3,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-height"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-height-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Height.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v) || v <= 0)
                    {
                        return;
                    }

                    options.Height = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Height));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 4,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-width"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-width-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Width.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v) || v <= 0)
                    {
                        return;
                    }

                    options.Width = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Width));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 6,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-offset-x"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-offset-x-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Offset.X.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.Offset.X = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Offset));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 7,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-offset-y"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-offset-y-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Offset.Y.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.Offset.Y = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Offset));
                    _context!.SaveConfig();
                }));

        BuildSectionLabel(builder, key: 120, Lang.Get($"{Constants.ModId}:config-tooltip-section-content"));

        BuildSettingRow(builder, key: 5,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-center-text"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-center-text-tooltip")),
            control: b => b.AddCheckbox(0,
                checked_: options.CenterText,
                onCheckedChanged: value =>
                {
                    options.CenterText = value;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.CenterText));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 15,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-font-size"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-font-size-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.FontSize.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v) || v <= 0)
                    {
                        return;
                    }

                    options.FontSize = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.FontSize));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 8,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-padding-top"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-padding-top-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Padding.Top.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.Padding.Top = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 9,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-padding-right"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-padding-right-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Padding.Right.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.Padding.Right = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 10,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-padding-bottom"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-padding-bottom-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Padding.Bottom.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.Padding.Bottom = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 11,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-padding-left"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-padding-left-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.Padding.Left.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.Padding.Left = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        BuildSectionLabel(builder, key: 130, Lang.Get($"{Constants.ModId}:config-tooltip-section-background"));

        BuildSettingRow(builder, key: 12,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-has-background"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-has-background-tooltip")),
            control: b => b.AddCheckbox(0,
                checked_: options.HasBackground,
                onCheckedChanged: value =>
                {
                    options.HasBackground = value;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.HasBackground));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 13,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-background-opacity"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-background-opacity-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.BackgroundOpacity.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                spinnerInterval: 0.05,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.BackgroundOpacity = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.BackgroundOpacity));
                    _context!.SaveConfig();
                }));

        BuildSettingRow(builder, key: 14,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-background-corner-radius"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-background-corner-radius-tooltip")),
            control: b => b.AddTextInput(0,
                text: options.BackgroundCornerRadius.ToString(InvCulture),
                mode: GuiTextInputMode.Decimal,
                showSpinnerButtons: true,
                widthMode: GuiSizeMode.Fill,
                onTextChanged: val =>
                {
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v))
                    {
                        return;
                    }

                    options.BackgroundCornerRadius = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.BackgroundCornerRadius));
                    _context!.SaveConfig();
                }));
    }

    private static string ExtractHorizontalAlignment(string dialogArea)
    {
        foreach (var horizontal in HorizontalAlignmentItems)
        {
            if (dialogArea.StartsWith(horizontal, StringComparison.OrdinalIgnoreCase))
            {
                return horizontal;
            }
        }
        return HorizontalAlignmentItems[1];
    }

    private static string ExtractVerticalAlignment(string dialogArea)
    {
        foreach (var horizontal in HorizontalAlignmentItems)
        {
            if (dialogArea.StartsWith(horizontal, StringComparison.OrdinalIgnoreCase))
            {
                return dialogArea[horizontal.Length..];
            }
        }
        return VerticalAlignmentItems[1];
    }

    private static string CombineDialogArea(string horizontal, string vertical) => horizontal + vertical;

    private static void BuildSectionLabel(IGuiRenderTreeBuilder builder, int key, string text, bool isFirst = false)
    {
        builder.AddLabel(key, text,
            font: GuiFontStyle.MediumBold,
            margin: new(isFirst ? 0 : SectionSpacing, 0, SectionRuleGap, 0));
        builder.AddRectangle(key + 1,
            color: SectionSeparatorColor,
            height: 1,
            widthMode: GuiSizeMode.Fill,
            margin: new(0, 0, RowSpacing, 0));
    }

    private static void BuildSettingRow(
        IGuiRenderTreeBuilder builder,
        int key,
        string label,
        GuiRenderFragment tooltip,
        GuiRenderFragment control)
    {
        builder.AddContainer(key,
            widthMode: GuiSizeMode.Fill,
            height: RowHeight,
            direction: GuiDirection.Horizontal,
            margin: new(0, 0, RowSpacing, 0),
            content: builder =>
            {
                builder.AddTooltip(0,
                    tooltip: tooltip,
                    content: builder => builder.AddLabel(0, label,
                        width: LabelColumnWidth,
                        verticalAlignment: GuiVerticalAlignment.Center));

                builder.AddContainer(1, fill: true, content: control);
            });
    }
}
