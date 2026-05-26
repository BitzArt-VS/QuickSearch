using Cairo;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A panel that paints the vanilla "shaded title bar" look — a slightly lighter solid
/// fill plus an open three-sided dark border (left + top + right). The bottom edge is
/// closed by a sibling <see cref="GuiDialogBackground"/> below
/// (its <see cref="GuiDialogBackground.JoinedTopEdge"/> top stroke meets ours).
/// <para>
/// Mirrors vanilla <c>GuiElementDialogTitleBar.ComposeTextElements</c> except for the
/// inset highlight + <c>BlurPartial</c> bevel pass. That pass relies on surface-level
/// blur — without blur, a literal reproduction shows as a sharp inset rectangle that
/// reads worse than no bevel at all, so it's intentionally omitted.
/// </para>
/// <para>
/// The title text is painted directly by <see cref="DrawBackground"/> rather than as a
/// child component, because vanilla centres the text vertically inside the bar — a
/// capability the layout pass does not yet provide.
/// </para>
/// </summary>
public class GuiDialogTitleBar : GuiContainer
{
    /// <summary>The title text drawn inside the bar.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Font used to draw <see cref="Title"/>. Defaults to <see cref="GuiFontStyle.Default"/>.</summary>
    public GuiFontStyle TitleFont { get; set; } = GuiFontStyle.Default;

    /// <summary>Horizontal inset of the title text from the left edge in logical pixels. Default: <see cref="GuiVanillaStyle.ElementToDialogPadding"/> (20, vanilla).</summary>
    public double TitleLeftPadding { get; set; } = GuiVanillaStyle.ElementToDialogPadding;

    /// <summary>The base fill colour of the bar. Default: <see cref="GuiVanillaStyle.DialogTitleBarBgColor"/> (vanilla strong bg × 1.2).</summary>
    public GuiColor FillColor { get; set; } = GuiVanillaStyle.DialogTitleBarBgColor;

    /// <summary>
    /// Outer dark border stroke width in <b>physical</b> pixels. Default 2 — matches
    /// <see cref="GuiDialogBackground.StrokeWidth"/> so the title bar and body
    /// share a uniform border weight.
    /// </summary>
    public double StrokeWidth { get; set; } = 2;

    /// <summary>Outer dark border colour. Default: <see cref="GuiVanillaStyle.DialogShadedStrokeColor"/>.</summary>
    public GuiColor BorderColor { get; set; } = GuiVanillaStyle.DialogShadedStrokeColor;

    /// <summary>
    /// Drag callback. When set, the title bar acts as a drag handle: holding the left mouse
    /// button on the bar and moving the cursor invokes this callback once per move event with
    /// the cursor delta in <b>logical (unscaled) pixels</b> since the previous event. Pass
    /// <c>this.Move</c> from a <see cref="GuiDialog"/> subclass to make the title bar drag
    /// the parent dialog around. Default <c>null</c> — drag interaction is disabled.
    /// </summary>
    public Action<double, double>? OnDrag { get; set; }

    /// <summary>
    /// Close callback. When set, the bar paints a vanilla-style "X" icon in its top-right
    /// corner that invokes this callback on click. Default <c>null</c> — no close icon
    /// is drawn.
    /// </summary>
    public GuiCallback OnClose { get; set; }

    /// <summary>
    /// Right-edge inset of the close icon in logical pixels. Matches vanilla's
    /// <c>scaled(12)</c> spacing between the icon and the bar's right edge.
    /// </summary>
    public double CloseIconRightPadding { get; set; } = 12;

    /// <summary>
    /// Top inset of the close icon in logical pixels. Matches vanilla's <c>scaled(7)</c>.
    /// </summary>
    public double CloseIconTopPadding { get; set; } = 7;

    // Screen-absolute logical-coordinate anchor for drag delta computation. Absolute coords
    // are stable across moves — the dialog-local frame shifts with each position update, but
    // the screen-absolute frame does not, so deltas taken from it are always correct.
    private double _dragLastX;
    private double _dragLastY;
    private bool _dragging;

    // Captured at BuildRenderTree time (via Configure on the close-icon slot) so DrawBackground
    // can update its absolute Margin.Left to anchor it to the bar's right edge once the bar's
    // actual width is known. The reference is reset on every blueprint pass — Configure runs
    // every rebuild and re-assigns it.
    private GuiDialogCloseIcon? _closeIcon;

    protected override void DrawBackground(Context ctx, GuiComponentBounds bounds)
    {
        double sw = StrokeWidth / RuntimeEnv.GUIScale;

        // 1. Solid lighter fill — establishes the title bar's brighter tone vs. the body.
        ctx.RoundRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, 0);
        ctx.FillSolid(FillColor);

        // 2. Open 3-sided dark border (left + top + right; bottom open). Path is flush
        //    with bounds — Cairo strokes straddle the path, so the outer half of the top
        //    edge is clipped by the dialog surface boundary, leaving the visible border
        //    at half the stroke width. This matches vanilla, where the equivalent clip
        //    happens against the dialog's own surface edge.
        ctx.OpenRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, GuiSide.Bottom);
        ctx.StrokeSolid(BorderColor, sw);

        // 3. Title text — vertically centred inside the bar, left-aligned with vanilla padding.
        if (!string.IsNullOrEmpty(Title))
        {
            double textH = TitleFont.MeasureHeight();
            double textY = bounds.Y + (bounds.Height - textH) / 2.0;
            ctx.DrawText(Title, TitleFont, bounds.X + TitleLeftPadding, textY);
        }

        // 4. Anchor the close-icon child to the bar's right edge. We don't know the bar's
        //    final width until the layout pass allocates our bounds, but DrawBackground runs
        //    before this slot's children are laid out — so mutating the close icon's
        //    LayoutParameters here is picked up immediately when the framework iterates our
        //    child slots. Avoids needing a "right anchor" feature in the layout pass.
        if (_closeIcon is not null)
        {
            double iconBox = _closeIcon.CrossLineWidth * 2 + _closeIcon.CrossSize;
            _closeIcon.LayoutParameters.Margin = new GuiThickness(
                Top: CloseIconTopPadding,
                Right: 0,
                Bottom: 0,
                Left: bounds.Width - iconBox - CloseIconRightPadding);
        }
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        // Render any user-supplied content first (matches the GuiContainer contract).
        base.BuildRenderTree(builder);

        // Drag click-target — only emitted when an OnDrag handler is attached. An absolute,
        // fill-mode container covers the title bar's entire content area without participating
        // in flow layout (so it doesn't push Content children around) and registers the
        // mouse handlers that drive drag. Pattern mirrors GuiButton's inner click target.
        //
        // Emitted *before* the close icon below so the icon's interactive region is added to
        // the renderer's region table last; hit-testing walks the table in reverse, so the
        // smaller close-icon region wins over the full-bar drag region when both contain the
        // cursor.
        if (OnDrag is not null)
        {
            builder.Add<GuiMouseTarget>(int.MaxValue - 1)
                .Configure(target => target.Content = BuildDragTargetContent)
                .OnMouseDown(HandleMouseDown)
                .OnMouseMove(HandleMouseMove)
                .OnMouseUp(HandleMouseUp);
        }

        // Close icon — absolute-positioned (Margin.Left is set to anchor it to the right edge
        // by DrawBackground once the bar's allocated width is known; see the comment there).
        // Configure runs every blueprint pass and captures the live instance so DrawBackground
        // can mutate its layout each frame without needing a separate framework anchor mode.
        if (OnClose.HasHandler)
        {
            builder.Add<GuiDialogCloseIcon>(int.MaxValue, positioning: GuiComponentPositioning.Absolute)
                .Configure(icon =>
                {
                    icon.OnClick = OnClose;
                    _closeIcon = icon;
                });
        }
        else
        {
            _closeIcon = null;
        }
    }

    private static void BuildDragTargetContent(IGuiRenderTreeBuilder builder)
    {
        builder.Add<GuiRectangle>(0, fill: true, positioning: GuiComponentPositioning.Absolute);
    }

    private void HandleMouseDown(GuiMouseEventArgs e)
    {
        // Only the left mouse button initiates a drag — matches vanilla title-bar behaviour.
        if (e.Button != Vintagestory.API.Common.EnumMouseButton.Left)
        {
            return;
        }

        _dragging = true;
        _dragLastX = e.AbsolutePosition.X;
        _dragLastY = e.AbsolutePosition.Y;
    }

    private void HandleMouseMove(GuiMouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        double dx = e.AbsolutePosition.X - _dragLastX;
        double dy = e.AbsolutePosition.Y - _dragLastY;
        _dragLastX = e.AbsolutePosition.X;
        _dragLastY = e.AbsolutePosition.Y;

        if (dx == 0 && dy == 0)
        {
            return;
        }

        OnDrag?.Invoke(dx, dy);
    }

    private void HandleMouseUp(GuiMouseEventArgs e)
    {
        _dragging = false;
    }
}
