using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System;
using System.Globalization;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// Hosts the entire <c>HUD ▸ Tooltips</c> branch of the config dialog. Acts as a
/// router-of-one — owns the sub-navigation between the tooltip list and individual
/// tooltip detail views — so the outer dialog stays oblivious to per-tooltip routing.
/// </summary>
/// <remarks>
/// The skeleton phase only wires up the navigation chrome (clickable labels in the
/// list view, breadcrumb in the detail view); the actual per-tooltip settings form
/// lives in a follow-up change.
/// </remarks>
internal sealed class TooltipsModConfigPage : GuiComponent, IModConfigPage
{
    public static string PageName => Lang.Get($"{Constants.ModId}:config-page-tooltips");

    // Vertical rhythm. SectionSpacing matches the bottom margin used by
    // GeneralModConfigPage's heading so a single-crumb "heading-style" breadcrumb here
    // vertically aligns with a normal heading rendered there.
    private const double ListItemSpacing = 6;
    private const double SectionSpacing = 16;

    // Centred list-of-tooltips column width. Wide enough that long predefined names fit
    // on one line without truncation, narrow enough that the buttons read as a list
    // rather than a row of full-width banners. Centring + fixed width keeps the visual
    // centre of the page predictable across dialog widths.
    private const double ListColumnWidth = 320;

    // Settings-form row geometry — mirrors QuickSearchModConfigPage so the two pages
    // share the same visual rhythm.
    private const double LabelColumnWidth = 220;
    private const double RowHeight = 28;
    private const double RowSpacing = 8;

    // Hover-key sentinels. Any non-negative value is an index into PredefinedTooltips.
    private const int HoverKeyNone = -1;
    private const int HoverKeyBreadcrumbBack = -2;

    // Cursor code applied while hovering a hyperlink-styled label. Matches vanilla
    // ScreenManager / LinkTextComponent — those load and apply the same code so the
    // pointer feels consistent with rich-text links elsewhere in the game.
    private const string LinkCursor = "linkselect";

    // Predefined tooltips, in the same order the legacy TooltipsConfigPage listed them
    // so existing user mental-models carry over. The Resolve delegate is invoked once a
    // tooltip is opened, so we don't allocate TooltipOptions references up front for
    // entries the user never visits. Custom tooltips are intentionally omitted from the
    // skeleton — they'll be appended dynamically when the detail form is implemented.
    private readonly record struct TooltipEntry(string LangKey, Func<TooltipsConfig, TooltipOptions> Resolve);

    private static readonly TooltipEntry[] PredefinedTooltips =
    [
        new($"{Constants.ModId}:config-page-env-widget",            c => c.EnvironmentWidget),
        new($"{Constants.ModId}:config-page-healthbar",             c => c.HealthbarTooltip),
        new($"{Constants.ModId}:config-page-satiety",               c => c.SatietyTooltip),
        new($"{Constants.ModId}:config-page-hunger-rate",           c => c.HungerTooltip),
        new($"{Constants.ModId}:config-page-temporal-stability",    c => c.TemporalStabilityTooltip),
    ];

    // Hyperlink colours match vanilla rich-text links: the orange/copper
    // ActiveButtonTextColor at idle, RGB × 1.2 (clamped) on hover. Re-resolved into the
    // chosen-size font on every BuildRenderTree pass — cheap.
    private static readonly GuiColor LinkIdleColor = GuiVanillaStyle.HyperlinkColor;
    private static readonly GuiColor LinkHoverColor = GuiVanillaStyle.HyperlinkHoverColor;

    // Invariant culture used for all double ↔ string conversions in text inputs so the
    // decimal point is always '.' regardless of the host locale — matching the
    // GuiTextInputMode.Decimal constraint which hard-codes '.' as the separator.
    private static readonly CultureInfo InvCulture = CultureInfo.InvariantCulture;

    // DialogArea dropdown items — same nine values the legacy TooltipConfigPage exposed,
    // preserving user mental models. Raw enum names are used as both value and display
    // label, matching the old behaviour.
    private static readonly string[] DialogAreaItems =
    [
        EnumDialogArea.LeftTop.ToString(),
        EnumDialogArea.CenterTop.ToString(),
        EnumDialogArea.RightTop.ToString(),
        EnumDialogArea.LeftMiddle.ToString(),
        EnumDialogArea.CenterMiddle.ToString(),
        EnumDialogArea.RightMiddle.ToString(),
        EnumDialogArea.LeftBottom.ToString(),
        EnumDialogArea.CenterBottom.ToString(),
        EnumDialogArea.RightBottom.ToString(),
    ];

    private ModConfigContext? _context;
    private GuiCursorHost? _cursorHost;

    private int _selectedTooltipIndex = -1;
    private int _hoveredKey = HoverKeyNone;

    public TooltipsModConfigPage()
    {
        LayoutParameters.Padding = new(8);
    }

    internal override void ResetLayoutParameters()
    {
        base.ResetLayoutParameters();
        LayoutParameters.Padding = new(8);
    }

    public override void OnParametersSet()
    {
        // Snapshot the cascading context so BuildRenderTree can read it without paying
        // for a chain walk per slot. Mirrors GeneralModConfigPage's resolution pattern.
        _context = GetCascadingValue<ModConfigContext>();
        // Cursor host is published by DialogRenderer at the dialog root; descendants
        // request a hover cursor by setting GuiCursorHost.HoverCursor. Cached so we
        // don't walk the cascade chain on every Enter/Leave handler invocation.
        _cursorHost = GetCascadingValue<GuiCursorHost>();
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        if (_selectedTooltipIndex < 0)
        {
            BuildListView(builder);
        }
        else
        {
            BuildDetailView(builder, _selectedTooltipIndex);
        }
    }

    // ── List view ─────────────────────────────────────────────────────────

    private void BuildListView(IGuiRenderTreeBuilder builder)
    {
        // Single-crumb breadcrumb renders as a normal page heading (LargeBold + section
        // spacing), matching GeneralModConfigPage's title visually so the two pages share
        // the same vertical rhythm above the content.
        BuildBreadcrumb(builder, key: 0, current: PageName);

        // Centred fixed-width column of tooltip buttons. The outer container is
        // horizontally centred inside its parent; the inner column sizes its buttons
        // to fill, so each row reads as a list item rather than a free-floating button.
        builder.AddContainer(1,
            width: ListColumnWidth,
            horizontalAlignment: GuiHorizontalAlignment.Center,
            content: column =>
            {
                for (int i = 0; i < PredefinedTooltips.Length; i++)
                {
                    int idx = i;
                    column.AddButton(idx, Lang.Get(PredefinedTooltips[idx].LangKey),
                        onClick: () => OpenTooltip(idx),
                        widthMode: GuiSizeMode.Fill,
                        margin: new GuiThickness(
                            Top: idx == 0 ? 0 : ListItemSpacing,
                            Right: 0, Bottom: 0, Left: 0));
                }

                // TODO: append custom tooltips from _context.Config.Hud.Tooltips.CustomTooltips
                // once the detail form is implemented (skeleton phase intentionally skips them).
            });
    }

    // ── Detail view ───────────────────────────────────────────────────────

    private void BuildDetailView(IGuiRenderTreeBuilder builder, int selectedIndex)
    {
        var entry = PredefinedTooltips[selectedIndex];
        string tooltipName = Lang.Get(entry.LangKey);

        // Breadcrumb: [Tooltips] > {tooltip name}
        BuildBreadcrumb(builder, key: 0,
            current: tooltipName,
            previous:
            [
                new BreadcrumbCrumb(PageName, HoverKey: HoverKeyBreadcrumbBack, OnClick: BackToList),
            ]);

        if (_context is null) return;
        var options = entry.Resolve(_context.Config.Hud.Tooltips);

        // Enable
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

        // Dialog area
        BuildSettingRow(builder, key: 2,
            label: Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area"),
            tooltip: b => b.AddLabel(0, Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area-tooltip")),
            control: b => b.AddDropdown<string>(0,
                items: DialogAreaItems,
                selectedIndex: Math.Max(0, Array.IndexOf(DialogAreaItems, options.DialogArea)),
                onSelectionChanged: idx =>
                {
                    options.DialogArea = DialogAreaItems[idx];
                    options.NotifyPropertyChanged(nameof(TooltipOptions.DialogArea));
                    _context!.SaveConfig();
                },
                widthMode: GuiSizeMode.Fill));

        // Height
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v) || v <= 0) return;
                    options.Height = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Height));
                    _context!.SaveConfig();
                }));

        // Width
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v) || v <= 0) return;
                    options.Width = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Width));
                    _context!.SaveConfig();
                }));

        // Center text
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

        // Offset X
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.Offset.X = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Offset));
                    _context!.SaveConfig();
                }));

        // Offset Y
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.Offset.Y = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Offset));
                    _context!.SaveConfig();
                }));

        // Padding Top
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.Padding.Top = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        // Padding Right
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.Padding.Right = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        // Padding Bottom
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.Padding.Bottom = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        // Padding Left
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.Padding.Left = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                    _context!.SaveConfig();
                }));

        // Has background
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

        // Background opacity
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.BackgroundOpacity = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.BackgroundOpacity));
                    _context!.SaveConfig();
                }));

        // Background corner radius
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
                    if (!double.TryParse(val, NumberStyles.Any, InvCulture, out double v)) return;
                    options.BackgroundCornerRadius = v;
                    options.NotifyPropertyChanged(nameof(TooltipOptions.BackgroundCornerRadius));
                    _context!.SaveConfig();
                }));
    }

    // ── Settings-row helper ───────────────────────────────────────────────

    /// <summary>
    /// Standard "label on the left, control on the right" row. Only the label side gets
    /// the tooltip wrapper. The control column fills remaining row width so inner
    /// controls stretch with the dialog.
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
                builder.AddTooltip(0,
                    tooltip: tooltip,
                    content: builder => builder.AddLabel(0, label,
                        width: LabelColumnWidth,
                        verticalAlignment: GuiVerticalAlignment.Center));

                builder.AddContainer(1, fill: true, content: control);
            });
    }

    // ── Breadcrumb helper ─────────────────────────────────────────────────

    /// <summary>
    /// A single non-active crumb in a multi-segment breadcrumb. Clicking the crumb's
    /// label invokes <see cref="OnClick"/>; the crumb owns its own hover-state key so
    /// transitions don't bleed into adjacent crumbs.
    /// </summary>
    private readonly record struct BreadcrumbCrumb(string Text, int HoverKey, Action OnClick);

    /// <summary>
    /// Renders a breadcrumb above page content. With no <paramref name="previous"/>
    /// crumbs this collapses into a normal page heading (a single bold label) so a
    /// single-segment trail and a plain heading are visually identical. With one or
    /// more previous crumbs it lays them out horizontally with " > " separators; each
    /// previous crumb is a non-bold hyperlink-styled clickable label, the current
    /// (last) crumb matches the heading exactly.
    /// </summary>
    private void BuildBreadcrumb(
        IGuiRenderTreeBuilder builder,
        int key,
        string current,
        BreadcrumbCrumb[]? previous = null)
    {
        // Section spacing below the heading/breadcrumb matches the bottom margin used by
        // GeneralModConfigPage's heading so cross-page vertical rhythm stays consistent.
        var bottomMargin = new GuiThickness(0, 0, SectionSpacing, 0);

        if (previous is null || previous.Length == 0)
        {
            builder.AddLabel(key, current,
                font: GuiFontStyle.LargeBold,
                margin: bottomMargin);
            return;
        }

        // Multi-segment trail. All segments use the Large size (18 px) so they share a
        // baseline; only the current crumb is bold. Separators inherit the same size so
        // they don't visually shift between segments.
        builder.AddContainer(key,
            direction: GuiDirection.Horizontal,
            widthMode: GuiSizeMode.Fill,
            margin: bottomMargin,
            content: row =>
            {
                int slotKey = 0;
                for (int i = 0; i < previous.Length; i++)
                {
                    var crumb = previous[i];
                    BuildNavLink(row, slotKey++,
                        text: crumb.Text,
                        hoverKey: crumb.HoverKey,
                        onClick: crumb.OnClick,
                        font: GuiFontStyle.Large);

                    row.AddLabel(slotKey++, "  >  ",
                        font: GuiFontStyle.Large,
                        margin: new GuiThickness(0));
                }

                row.AddLabel(slotKey, current, font: GuiFontStyle.LargeBold);
            });
    }

    // ── Nav-link helper ───────────────────────────────────────────────────

    /// <summary>
    /// Adds a label that visually behaves as a clickable hyperlink: hyperlink-coloured
    /// text at idle, brighter on hover, fires <paramref name="onClick"/> when clicked,
    /// and switches the platform cursor to <see cref="LinkCursor"/> while hovered.
    /// Hover state is tracked in <see cref="_hoveredKey"/> using <paramref name="hoverKey"/>
    /// as a stable identifier across renders.
    /// </summary>
    private void BuildNavLink(
        IGuiRenderTreeBuilder builder,
        int key,
        string text,
        int hoverKey,
        Action onClick,
        GuiFontStyle? font = null,
        GuiThickness? margin = null)
    {
        bool isHovered = _hoveredKey == hoverKey;
        var resolvedFont = (font ?? GuiFontStyle.Default) with
        {
            Color = isHovered ? LinkHoverColor : LinkIdleColor,
        };

        builder.AddLabel(key, text, font: resolvedFont, margin: margin)
            .OnMouseEnter(_ => SetHover(hoverKey))
            .OnMouseLeave(_ => ClearHover(hoverKey))
            .OnMouseClick(_ => onClick());
    }

    // ── State transitions ─────────────────────────────────────────────────

    private void OpenTooltip(int index)
    {
        if (_selectedTooltipIndex == index) return;
        _selectedTooltipIndex = index;
        _hoveredKey = HoverKeyNone;
        // The hovered slot is about to be torn down — proactively clear the platform
        // cursor so it doesn't linger on "linkselect" until the next mouse move
        // happens to land on a non-link region.
        _cursorHost?.SetHoverCursor(null);
        StateHasChanged();
    }

    private void BackToList()
    {
        if (_selectedTooltipIndex < 0) return;
        _selectedTooltipIndex = -1;
        _hoveredKey = HoverKeyNone;
        _cursorHost?.SetHoverCursor(null);
        StateHasChanged();
    }

    private void SetHover(int hoverKey)
    {
        if (_hoveredKey == hoverKey) return;
        _hoveredKey = hoverKey;
        _cursorHost?.SetHoverCursor(LinkCursor);
        StateHasChanged();
    }

    private void ClearHover(int hoverKey)
    {
        // Guard against stale Leave events arriving after the hover has already moved
        // on to a different link — we only clear if *we* are the active hover.
        if (_hoveredKey != hoverKey) return;
        _hoveredKey = HoverKeyNone;
        _cursorHost?.SetHoverCursor(null);
        StateHasChanged();
    }
}
