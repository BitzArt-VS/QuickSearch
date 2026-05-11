using System;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// String-typed breadcrumb with a predefined hyperlink-styled item template. Each
/// previous crumb renders as a large label in <see cref="GuiVanillaStyle.HyperlinkColor"/>;
/// hovering brightens to <see cref="GuiVanillaStyle.HyperlinkHoverColor"/> and switches
/// the platform cursor to the pointer. The current crumb renders bold at the same size.
/// <para>
/// Wire <see cref="OnItemClicked"/> to a navigation callback (e.g.
/// <c>_ => BackToList()</c>) to respond to previous-crumb clicks. The predefined
/// <see cref="GuiBreadcrumbs{T}.ItemTemplate"/> subscribes to <c>OnMouseClick</c>
/// internally and forwards to this action.
/// </para>
/// </summary>
public class GuiBreadcrumbs : GuiBreadcrumbs<string>
{
    private const string LinkCursor = "linkselect";

    private static readonly GuiColor LinkIdleColor = GuiVanillaStyle.HyperlinkColor;
    private static readonly GuiColor LinkHoverColor = GuiVanillaStyle.HyperlinkHoverColor;

    private GuiCursorHost? _cursorHost;
    private int _hoveredItemIndex = -1;
    private int _lastPreviousItemsCount = -1;

    /// <summary>Invoked with the crumb text when a previous item is clicked.</summary>
    public Action<string>? OnItemClicked { get; set; }

    public GuiBreadcrumbs()
    {
        ItemTemplate = BuildStringItem;
        CurrentTemplate = BuildCurrentStringItem;
    }

    /// <inheritdoc/>
    public override void OnParametersSet()
    {
        _cursorHost = GetCascadingValue<GuiCursorHost>();

        int currentCount = PreviousItems?.Length ?? 0;
        if (_lastPreviousItemsCount == currentCount)
        {
            return;
        }

        _lastPreviousItemsCount = currentCount;

        if (_hoveredItemIndex >= 0)
        {
            _hoveredItemIndex = -1;
            _cursorHost?.SetHoverCursor(null);
        }
    }

    private void BuildStringItem(IGuiRenderTreeBuilder builder, string text)
    {
        int capturedIndex = RenderingItemIndex;
        bool isHovered = _hoveredItemIndex == capturedIndex;

        builder.AddContainer(0, content: b =>
            b.AddLabel(0, text, font: GuiFontStyle.Large with { Color = isHovered ? LinkHoverColor : LinkIdleColor }))
            .OnMouseEnter(_ => EnterItemHover(capturedIndex))
            .OnMouseLeave(_ => LeaveItemHover(capturedIndex))
            .OnMouseClick(_ => OnItemClicked?.Invoke(text));
    }

    private void EnterItemHover(int index)
    {
        _hoveredItemIndex = index;
        _cursorHost?.SetHoverCursor(LinkCursor);
        StateHasChanged();
    }

    private void LeaveItemHover(int index)
    {
        if (_hoveredItemIndex != index)
        {
            return;
        }

        _hoveredItemIndex = -1;
        _cursorHost?.SetHoverCursor(null);
        StateHasChanged();
    }

    private void BuildCurrentStringItem(IGuiRenderTreeBuilder builder, string text)
    {
        builder.AddLabel(0, text, font: GuiFontStyle.LargeBold);
    }
}

/// <summary>
/// Horizontal breadcrumb trail: renders a sequence of <see cref="PreviousItems"/> via
/// <see cref="ItemTemplate"/>, separated by " > " labels, followed by
/// <see cref="CurrentItem"/> via <see cref="CurrentTemplate"/>. When
/// <see cref="PreviousItems"/> is null or empty the component collapses to a single
/// heading slot (only <see cref="CurrentTemplate"/> is rendered, with a bottom margin
/// matching the section-spacing rhythm used by page headings).
/// <para>
/// <b>Click and hover subscriptions.</b> Place them inside <see cref="ItemTemplate"/> —
/// chain <c>.OnMouseClick</c>, <c>.OnMouseEnter</c>, and <c>.OnMouseLeave</c> on any
/// slot the template declares. <see cref="RenderingItemIndex"/> is set to the item's
/// index before each call so a template that closes over <c>this</c> (as in
/// <see cref="GuiBreadcrumbs"/>) can produce per-item hover-sensitive rendering without
/// extra plumbing.
/// </para>
/// </summary>
public class GuiBreadcrumbs<T> : GuiComponent
{
    private const double SectionSpacing = 16;
    private const string SeparatorText = "  >  ";

    /// <summary>
    /// Set to the current item's index immediately before each <see cref="ItemTemplate"/>
    /// invocation. Templates that are instance methods closing over <c>this</c> can read
    /// this value to know which item is currently being rendered.
    /// </summary>
    protected int RenderingItemIndex { get; private set; } = -1;

    /// <summary>The previous (non-current) crumbs; each is rendered via
    /// <see cref="ItemTemplate"/> with a " > " separator before the next. May be
    /// null or empty for a single-crumb heading.</summary>
    public T[]? PreviousItems { get; set; }

    /// <summary>The active, rightmost crumb rendered via
    /// <see cref="CurrentTemplate"/>.</summary>
    public T? CurrentItem { get; set; }

    /// <summary>Renders a previous (non-current) crumb. Attach click and hover handlers
    /// inside this template — chain <c>.OnMouseClick</c>, <c>.OnMouseEnter</c>, and
    /// <c>.OnMouseLeave</c> on any declared slot. <see cref="RenderingItemIndex"/> is set
    /// to the item's index before each call.</summary>
    public GuiRenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>Renders the current (rightmost) crumb.</summary>
    public GuiRenderFragment<T>? CurrentTemplate { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        var bottomMargin = new GuiThickness(0, 0, SectionSpacing, 0);

        if (PreviousItems is not { Length: > 0 })
        {
            BuildCurrentItemSlot(builder, key: 0, margin: bottomMargin);
            return;
        }

        builder.AddContainer(0,
            direction: GuiDirection.Horizontal,
            widthMode: GuiSizeMode.Fill,
            margin: bottomMargin,
            content: BuildRow);
    }

    private void BuildRow(IGuiRenderTreeBuilder row)
    {
        int slotKey = 0;

        for (int i = 0; i < PreviousItems!.Length; i++)
        {
            T item = PreviousItems[i];
            int capturedIndex = i;

            row.AddContainer(slotKey++,
                content: b =>
                {
                    RenderingItemIndex = capturedIndex;
                    ItemTemplate?.Invoke(b, item);
                });

            row.AddLabel(slotKey++, SeparatorText, font: GuiFontStyle.Large, margin: new GuiThickness(0));
        }

        BuildCurrentItemSlot(row, slotKey, margin: null);
    }

    private void BuildCurrentItemSlot(IGuiRenderTreeBuilder builder, int key, GuiThickness? margin)
    {
        if (CurrentItem is null || CurrentTemplate is null)
        {
            return;
        }

        T capturedItem = CurrentItem;
        builder.AddContainer(key, margin: margin, content: b => CurrentTemplate(b, capturedItem));
    }
}
