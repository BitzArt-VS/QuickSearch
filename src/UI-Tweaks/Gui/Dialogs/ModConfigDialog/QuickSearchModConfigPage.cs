using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using Cairo;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class QuickSearchModConfigPage : GuiComponent, IModConfigPage
{
    public static string PageName => Lang.Get($"{Constants.ModId}:config-page-quicksearch");

    // Row geometry — the label column is fixed-width so labels align across rows; the
    // control column fills whatever horizontal space is left, so controls stretch /
    // shrink with the dialog. No fixed control width — that was the legacy behaviour
    // we explicitly want to drop.
    private const double LabelColumnWidth = 220;
    private const double RowHeight = 28;
    private const double RowSpacing = 8;

    // Quick-search slider range — matches the legacy QuickSearchConfigPage so existing
    // user values stay valid after the migration.
    private const int QsMinResultListHeight = 50;
    private const int QsMaxResultListHeight = 600;
    private const int QsResultListHeightStep = 10;

    // Italic-leaning font reused for the trailing "* requires a game restart." footnote
    // inside tooltip bodies. We render this as a separate label rather than embedding
    // VTML <i>…</i> tags because the new framework's labels are plain-text only.
    private static readonly GuiFontStyle ItalicNoteFont = GuiFontStyle.Default with
    {
        Slant = FontSlant.Italic,
    };

    private ModConfigContext? _context;

    protected override void SetDefaultLayoutParameters()
    {
        LayoutParameters.Padding = new(8);
    }

    public override void OnParametersSet()
    {
        _context = GetCascadingValue<ModConfigContext>();
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        var quickSearch = _context?.Config.QuickSearch;
        if (quickSearch is null) return;

        // Enable toggle.
        BuildSettingRow(builder, key: 1,
            label: Lang.Get($"{Constants.ModId}:config-quicksearch-enable"),
            tooltip: builder =>
            {
                builder.AddLabel(0, Lang.Get($"{Constants.ModId}:config-quicksearch-enable-tooltip"));
                builder.AddLabel(1, string.Empty);
                builder.AddLabel(2, Lang.Get($"{Constants.ModId}:config-requires-restart"),
                    font: ItalicNoteFont,
                    margin: new(0, 0, GuiVanillaStyle.HalfPadding, 0));
            },
            control: builder => builder.AddCheckbox(0,
                checked_: quickSearch.Enable,
                onCheckedChanged: value =>
                {
                    quickSearch.Enable = value;
                    quickSearch.NotifyPropertyChanged(nameof(QuickSearchConfig.Enable));
                    _context!.SaveConfig();
                }));

        // Result list height slider.
        BuildSettingRow(builder, key: 2,
            label: Lang.Get($"{Constants.ModId}:config-quicksearch-result-list-height"),
            tooltip: builder => builder.AddLabel(0,
                Lang.Get($"{Constants.ModId}:config-quicksearch-result-list-height-tooltip")),
            control: builder => builder.AddSlider(0,
                value: quickSearch.ResultListHeight,
                minValue: QsMinResultListHeight,
                maxValue: QsMaxResultListHeight,
                step: QsResultListHeightStep,
                unit: "px",
                triggerOnMouseUp: true,
                widthMode: GuiSizeMode.Fill,
                onValueChanged: value =>
                {
                    quickSearch.ResultListHeight = value;
                    quickSearch.NotifyPropertyChanged(nameof(QuickSearchConfig.ResultListHeight));
                    _context!.SaveConfig();
                }));
    }

    /// <summary>
    /// Standard "label on the left, control on the right" row used by every setting.
    /// Only the <i>label</i> side gets the tooltip wrapper — hovering the control
    /// itself is reserved for control-specific affordances (the slider's own value
    /// readout, etc.). The control column fills the remaining row width so the inner
    /// control stretches with the dialog.
    /// </summary>
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
                // Label column — wrapped in a tooltip so only the left side triggers
                // the hover help. Width is fixed so labels line up across all rows.
                // Vertically centred inside the 28 px row (cross-axis alignment of a
                // relative child in a horizontal stack).
                builder.AddTooltip(0,
                    tooltip: tooltip,
                    content: builder => builder.AddLabel(0, label,
                        width: LabelColumnWidth,
                        verticalAlignment: GuiVerticalAlignment.Center));

                // Control column — fills the rest of the row; the control inside should
                // also use widthMode: Fill (or fill: true) to stretch with the dialog.
                builder.AddContainer(1, fill: true, content: control);
            });
    }
}
