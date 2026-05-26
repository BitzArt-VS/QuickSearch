using Cairo;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// The framework's general-purpose layout/surface component — analogous to a
/// <c>&lt;div&gt;</c> in HTML or <c>Container</c> in Flutter. Hosts a nested render tree
/// declared via <see cref="Content"/>, stacks children according to
/// <see cref="GuiComponentLayoutParameters.Direction"/> (default
/// <see cref="GuiDirection.Vertical"/>), and optionally paints a background fill.
/// <para>
/// Both <see cref="GuiComponentLayoutParameters.WidthMode"/> and
/// <see cref="GuiComponentLayoutParameters.HeightMode"/> default to
/// <see cref="GuiSizeMode.FitContent"/> — override via fluent extensions.
/// </para>
/// <para>
/// <b>Drawing.</b> The painting pass is split into two overrideable hooks called by the
/// framework in order:
/// <list type="number">
///   <item><see cref="DrawBackground"/> — before children. Default: fills bounds with
///   <see cref="Background"/>; no-op when <see cref="Background"/> is fully transparent.</item>
///   <item><see cref="DrawOverlay"/> — after all children. Default: no-op.</item>
/// </list>
/// Subclass and override these for chrome (textures, borders, glows, etc.). The base
/// <c>Render</c> / <c>RenderOverlay</c> are sealed to keep the two-hook contract uniform
/// across every container subtype.
/// </para>
/// <para>
/// <b>Scrolling.</b> Set <see cref="Scroll"/> to enable scrolling on one or both axes.
/// When enabled, the framework clips drawing to the container's content area and translates
/// children by the current scroll offset. <see cref="GuiSizeMode.FitContent"/> on a given
/// axis disables scrolling on that axis (a fit-to-content container has no overflow by
/// definition). Scrollbars are visible when content overflows the viewport (filtered by
/// <see cref="Scrollbar"/>) or when forced via <see cref="AlwaysShowScrollbar"/>.
/// </para>
/// </summary>
public class GuiContainer : GuiComponent
{
    /// <summary>
    /// The nested render fragment that populates this container's inner content.
    /// Set via <c>.WithContent(b =&gt; { ... })</c>, the <c>content:</c> argument on
    /// <c>AddContainer</c>, or <c>.Configure(c =&gt; c.Content = ...)</c>.
    /// </summary>
    public GuiRenderFragment? Content { get; set; }

    /// <summary>
    /// Background fill colour. Defaults to fully transparent — the default
    /// <see cref="DrawBackground"/> short-circuits when alpha is zero, so a plain
    /// <c>GuiContainer</c> draws nothing and behaves as an invisible flow box.
    /// </summary>
    public GuiColor Background { get; set; }

    // ── Scrolling ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Axes on which scrolling is enabled. Defaults to <see cref="GuiScrollDirection.None"/>.
    /// An axis whose corresponding size mode is <see cref="GuiSizeMode.FitContent"/>
    /// is silently ignored — a fit-to-content container has no overflow.
    /// </summary>
    public GuiScrollDirection Scroll { get; set; } = GuiScrollDirection.None;

    /// <summary>
    /// Axes for which a scrollbar may be displayed. Defaults to <see cref="GuiScrollDirection.Both"/>.
    /// A scrollbar is only shown for an axis when (a) that axis is included here, AND
    /// (b) the axis is included in <see cref="Scroll"/>, AND (c) either content overflows
    /// along that axis or <see cref="AlwaysShowScrollbar"/> includes that axis.
    /// </summary>
    public GuiScrollDirection Scrollbar { get; set; } = GuiScrollDirection.Both;

    /// <summary>
    /// Axes for which the scrollbar should remain visible even when content does not
    /// overflow. Defaults to <see cref="GuiScrollDirection.None"/>. Has no effect for axes not
    /// included in <see cref="Scroll"/> / <see cref="Scrollbar"/>.
    /// </summary>
    public GuiScrollDirection AlwaysShowScrollbar { get; set; } = GuiScrollDirection.None;

    // ── Inset chrome ──────────────────────────────────────────────────────────

    /// <summary>
    /// When <c>true</c>, the container's <see cref="Content"/> render tree is wrapped in a
    /// <see cref="GuiInset"/> that fills the container — producing the vanilla recessed-
    /// border look around the content. Defaults to <c>false</c>. Use
    /// <see cref="InsetConfiguration"/> to tweak the wrapping inset's
    /// <see cref="GuiInset.Depth"/> / <see cref="GuiInset.Brightness"/> /
    /// <see cref="GuiInset.Radius"/> without subclassing.
    /// </summary>
    public bool HasInset { get; set; }

    /// <summary>
    /// Optional configure callback forwarded — verbatim, via <c>.Configure(...)</c> — to
    /// the wrapping <see cref="GuiInset"/> declared by <see cref="HasInset"/>. Lets external
    /// callers customise the inset visual without subclassing the container.
    /// </summary>
    public Action<GuiInset>? InsetConfiguration { get; set; }

    /// <summary>Current horizontal scroll offset in logical pixels. Mutate via user input
    /// (mouse wheel / scrollbar drag) or <see cref="ScrollTo"/>.</summary>
    public double ScrollX { get; private set; }

    /// <summary>Current vertical scroll offset in logical pixels. Mutate via user input
    /// (mouse wheel / scrollbar drag) or <see cref="ScrollTo"/>.</summary>
    public double ScrollY { get; private set; }

    /// <summary>
    /// Sets scroll offsets explicitly. Values are clamped to the valid range on the next
    /// layout pass. Pass a negative value to leave an axis unchanged.
    /// </summary>
    public void ScrollTo(double scrollX, double scrollY)
    {
        double previousScrollX = ScrollX;
        double previousScrollY = ScrollY;
        if (scrollX >= 0)
        {
            ScrollX = scrollX;
        }

        if (scrollY >= 0)
        {
            ScrollY = scrollY;
        }

        if (ScrollX != previousScrollX || ScrollY != previousScrollY)
        {
            RequestArrange();
        }
    }

    // ── Drawing hooks ─────────────────────────────────────────────────────────

    /// <summary>
    /// Override to draw the container's background. Called before children are rendered.
    /// Default: fills bounds with a solid <see cref="Background"/> when its alpha is
    /// greater than zero; otherwise no-op.
    /// </summary>
    protected virtual void DrawBackground(Context context, GuiComponentBounds bounds)
    {
        if (Background.A <= 0)
        {
            return;
        }

        context.SetSourceRGBA(Background.R, Background.G, Background.B, Background.A);
        context.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        context.Fill();
    }

    /// <summary>
    /// Override to draw overlays on top of children (borders, highlights, etc.).
    /// Called after all children are rendered.
    /// Default: no-op.
    /// </summary>
    protected virtual void DrawOverlay(Context context, GuiComponentBounds bounds) { }

    // ── Framework wiring ──────────────────────────────────────────────────────

    /// <summary>
    /// Renders the nested <see cref="Content"/> fragment into this container. Subclasses may
    /// override to inject additional children (e.g. an overlay click target); call
    /// <c>base.BuildRenderTree(builder)</c> to keep <see cref="Content"/> support.
    /// </summary>
    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        Content?.Invoke(builder);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Forwards <see cref="InsetConfiguration"/> (when set) to the container's owned
    /// <see cref="GuiInset"/> instance once per reconcile, not per frame — the inset's
    /// own properties stay stable between configuration changes.
    /// </remarks>
    public override void OnParametersSet()
    {
        InsetConfiguration?.Invoke(_inset);
    }

    public sealed override void Render(Context context, GuiComponentBounds bounds)
        => DrawBackground(context, bounds);

    public sealed override void RenderOverlay(Context context, GuiComponentBounds bounds)
        => DrawOverlay(context, bounds);

    /// <summary>
    /// Owned inset instance configured via <see cref="InsetConfiguration"/> and drawn as
    /// the container's background by the layout pass when <see cref="HasInset"/> is set.
    /// Single instance per container — no per-frame allocation.
    /// </summary>
    private readonly GuiInset _inset = new();

    /// <summary>
    /// Draws the inset background into <paramref name="context"/> at <paramref name="bounds"/>
    /// when <see cref="HasInset"/> is set; no-op otherwise. Called by the framework's layout
    /// pass with the correct fixed (non-scrolling) region — the full allocated bounds for
    /// non-scrollable containers, or the inset region (allocated minus scrollbar gutter) for
    /// scrollable ones.
    /// </summary>
    internal void DrawInsetBackground(Context context, GuiComponentBounds bounds)
    {
        if (!HasInset)
        {
            return;
        }

        GuiInset.Draw(context, bounds, _inset.Depth, _inset.Brightness, _inset.Radius);
    }

    // ── Scrollbar layout / drawing / interaction ─────────────────────────────

    /// <summary>
    /// Default scrollbar thickness in logical pixels. Mirrors vanilla
    /// <c>GuiElementScrollbar.DefaultScrollbarWidth</c> (20).
    /// </summary>
    public const double ScrollbarThickness = 20;

    /// <summary>
    /// Logical-pixel gap reserved between the scrollable viewport and the scrollbar track —
    /// keeps content from butting up against the scrollbar handle. The scrollbar itself
    /// still sits flush against the container's allocated edge.
    /// </summary>
    public const double ScrollbarGap = 2;

    /// <summary>Minimum scrollbar handle length in logical pixels — keeps the handle
    /// grabbable when content vastly exceeds the viewport. Matches vanilla.</summary>
    private const double MinHandleLength = 10;

    /// <summary>Approximate logical-pixel scroll distance per mouse-wheel notch.</summary>
    private const double WheelStep = 30;

    /// <summary>
    /// Per-side inset of the scrollbar handle relative to the track. Renders the handle
    /// 2*<see cref="HandleInset"/> pixels narrower than the track so the recessed track
    /// frame stays visible around the handle — small departure from vanilla, intentionally
    /// so.
    /// </summary>
    private const double HandleInset = 1;

    // Effective scroll axes — set by the framework each layout pass after filtering
    // FitContent dimensions out of the user-declared Scroll mask. Read by HandleMouseWheel.
    internal GuiScrollDirection EffectiveScroll;

    // Cached layout state for the most recent frame, used by RenderScrollbars and the
    // scrollbar mouse handlers. All values in dialog-local logical pixels.
    private double _allocatedX, _allocatedY, _allocatedW, _allocatedH;
    private double _viewportX, _viewportY, _viewportW, _viewportH;
    private double _contentW, _contentH;
    private bool _showVScrollbar, _showHScrollbar;
    private double _sbThickness;

    // Drag state for scrollbar handles. Tracked independently per axis. Only one is active
    // at a time in practice; mouse capture matches by token, not by axis.
    private bool _vDragging;
    private bool _hDragging;
    // Offset from the handle's origin (top for V, left for H) to the click point at
    // drag start. Preserves the grab position as the cursor moves.
    private double _vDragHandleOffset;
    private double _hDragHandleOffset;

    // Stable tokens for scrollbar interactive regions. Allocated once per container —
    // the framework dispatcher compares tokens by reference identity for capture matching,
    // so each axis needs its own object.
    internal readonly object VScrollbarToken = new();
    internal readonly object HScrollbarToken = new();

    // Pre-bound mouse-handler callbacks. Building them in the constructor keeps the
    // per-frame cost of declaring scrollbar interactive regions to zero allocations.
    internal readonly GuiCallback<GuiMouseEventArgs> OnVScrollbarDown;
    internal readonly GuiCallback<GuiMouseEventArgs> OnVScrollbarUp;
    internal readonly GuiCallback<GuiMouseEventArgs> OnVScrollbarMove;
    internal readonly GuiCallback<GuiMouseEventArgs> OnHScrollbarDown;
    internal readonly GuiCallback<GuiMouseEventArgs> OnHScrollbarUp;
    internal readonly GuiCallback<GuiMouseEventArgs> OnHScrollbarMove;
    internal readonly GuiCallback<GuiMouseEventArgs> OnScrollWheel;

    public GuiContainer()
    {
        OnVScrollbarDown = (Action<GuiMouseEventArgs>)HandleVScrollbarDown;
        OnVScrollbarUp = (Action<GuiMouseEventArgs>)HandleVScrollbarUp;
        OnVScrollbarMove = (Action<GuiMouseEventArgs>)HandleVScrollbarMove;
        OnHScrollbarDown = (Action<GuiMouseEventArgs>)HandleHScrollbarDown;
        OnHScrollbarUp = (Action<GuiMouseEventArgs>)HandleHScrollbarUp;
        OnHScrollbarMove = (Action<GuiMouseEventArgs>)HandleHScrollbarMove;
        OnScrollWheel = (Action<GuiMouseEventArgs>)HandleScrollWheel;
    }

    /// <summary>
    /// Pushes the latest layout state into this container so subsequent scrollbar drawing
    /// and interaction can reference it. Called by the framework's layout pass for every
    /// scrollable container, in dialog-local logical coordinates. Clamps the current scroll
    /// offsets to the valid range derived from content vs viewport size.
    /// </summary>
    /// <summary>
    /// Pushes the latest layout state into this container so subsequent scrollbar drawing
    /// and interaction can reference it. Called by the framework's layout pass for every
    /// scrollable container, in dialog-local logical coordinates. The <c>allocated*</c>
    /// rectangle is the container's outer bounds (used to anchor the scrollbar track and
    /// inset region against the container edge); the <c>viewport*</c> rectangle is the
    /// inner area where children scroll. Clamps the current scroll offsets to the valid
    /// range derived from content vs viewport size.
    /// </summary>
    internal void UpdateScrollLayout(
        double allocatedX, double allocatedY, double allocatedW, double allocatedH,
        double viewportX, double viewportY, double viewportW, double viewportH,
        double contentW, double contentH,
        bool showV, bool showH, double sbThickness)
    {
        _allocatedX = allocatedX;
        _allocatedY = allocatedY;
        _allocatedW = allocatedW;
        _allocatedH = allocatedH;
        _viewportX = viewportX;
        _viewportY = viewportY;
        _viewportW = viewportW;
        _viewportH = viewportH;
        _contentW = contentW;
        _contentH = contentH;
        _showVScrollbar = showV;
        _showHScrollbar = showH;
        _sbThickness = sbThickness;

        double maxX = Math.Max(0, contentW - viewportW);
        double maxY = Math.Max(0, contentH - viewportH);
        if (ScrollX > maxX)
        {
            ScrollX = maxX;
        }

        if (ScrollY > maxY)
        {
            ScrollY = maxY;
        }

        if (ScrollX < 0)
        {
            ScrollX = 0;
        }

        if (ScrollY < 0)
        {
            ScrollY = 0;
        }
    }

    /// <summary>
    /// Routes a mouse-wheel event hit on this container's viewport. Vertical wheel scrolls
    /// the vertical axis when enabled, falling back to horizontal otherwise.
    /// </summary>
    internal void HandleMouseWheel(float deltaPrecise)
    {
        double previousScrollX = ScrollX;
        double previousScrollY = ScrollY;
        if ((EffectiveScroll & GuiScrollDirection.Vertical) != 0)
        {
            ScrollY = Clamp(ScrollY - deltaPrecise * WheelStep, 0, Math.Max(0, _contentH - _viewportH));
        }
        else if ((EffectiveScroll & GuiScrollDirection.Horizontal) != 0)
        {
            ScrollX = Clamp(ScrollX - deltaPrecise * WheelStep, 0, Math.Max(0, _contentW - _viewportW));
        }

        if (ScrollX != previousScrollX || ScrollY != previousScrollY)
        {
            RequestArrange();
        }
    }

    private void HandleScrollWheel(GuiMouseEventArgs args) => HandleMouseWheel(args.WheelDelta);

    /// <summary>
    /// Draws the visible scrollbars over the container's allocated bounds. Called by the
    /// framework's layout pass after children render, before <see cref="DrawOverlay"/>.
    /// Mirrors the look of vanilla <c>GuiElementScrollbar</c> — dimmed track with a rounded
    /// highlight handle.
    /// </summary>
    internal void RenderScrollbars(Context ctx)
    {
        if (_showVScrollbar)
        {
            var track = GetVScrollbarTrackBounds();
            DrawScrollbarTrack(ctx, track);
            var (handleY, handleH) = GetVHandleSpan();
            DrawScrollbarHandle(ctx, new GuiComponentBounds(
                track.X + HandleInset, handleY, track.Width - 2 * HandleInset, handleH));
        }
        if (_showHScrollbar)
        {
            var track = GetHScrollbarTrackBounds();
            DrawScrollbarTrack(ctx, track);
            var (handleX, handleW) = GetHHandleSpan();
            DrawScrollbarHandle(ctx, new GuiComponentBounds(
                handleX, track.Y + HandleInset, handleW, track.Height - 2 * HandleInset));
        }
    }

    /// <summary>Vertical scrollbar track bounds (dialog-local logical px). Anchored
    /// against the container's allocated right edge, with the gap reserved between
    /// viewport and track on the inner side.</summary>
    internal GuiComponentBounds GetVScrollbarTrackBounds()
        => new(_allocatedX + _allocatedW - _sbThickness, _viewportY, _sbThickness, _viewportH);

    /// <summary>Horizontal scrollbar track bounds (dialog-local logical px). Anchored
    /// against the container's allocated bottom edge.</summary>
    internal GuiComponentBounds GetHScrollbarTrackBounds()
        => new(_viewportX, _allocatedY + _allocatedH - _sbThickness, _viewportW, _sbThickness);

    /// <summary>
    /// Bounds the inset background occupies when scrollbars are visible — the container's
    /// allocated area minus the scrollbar gutter <em>and</em> the gap on each visible
    /// scrollbar axis. Yielding the gap as well as the track keeps a visible separation
    /// between the inset's emboss and the scrollbar track, so they don't read as one
    /// continuous dark strip.
    /// </summary>
    internal GuiComponentBounds GetScrollInsetBounds()
        => new(
            _allocatedX, _allocatedY,
            _allocatedW - (_showVScrollbar ? _sbThickness + ScrollbarGap : 0),
            _allocatedH - (_showHScrollbar ? _sbThickness + ScrollbarGap : 0));

    /// <summary>
    /// Per-side logical-pixel inset to apply to the scroll viewport clip region when
    /// <see cref="HasInset"/> is set, so scrollable content is clipped before it reaches
    /// the emboss ring and cannot paint over it.
    /// </summary>
    internal double ScrollViewportClipInset => HasInset ? _inset.Depth / RuntimeEnv.GUIScale : 0;

    private static void DrawScrollbarTrack(Context ctx, GuiComponentBounds b)
    {
        // Vanilla composer paints a recessed inset behind the scrollbar handle. Reuse the
        // shared GuiInset visual rather than approximating it — keeps the scrollbar look
        // consistent with the container's own optional inset chrome.
        GuiInset.Draw(ctx, b,
            depth: 4,
            brightness: 0.85f,
            radius: GuiVanillaStyle.ElementBgRadius);
    }

    private static void DrawScrollbarHandle(Context ctx, GuiComponentBounds b)
    {
        // Two-pass fill mirroring vanilla GuiElementScrollbar.RecomposeHandle:
        // 1) DialogHighlightColor base, 2) 40% black wash for depth.
        var hl = GuiVanillaStyle.DialogHighlightColor;
        ctx.Rectangle(b.X, b.Y, b.Width, b.Height);
        ctx.SetSourceRGBA(hl.R, hl.G, hl.B, hl.A);
        ctx.Fill();

        ctx.Rectangle(b.X, b.Y, b.Width, b.Height);
        ctx.SetSourceRGBA(0, 0, 0, 0.4);
        ctx.Fill();

        // Lightweight emboss: 1px top/left highlight + bottom/right shadow.
        ctx.Rectangle(b.X, b.Y, b.Width - 1, 1);
        ctx.SetSourceRGBA(1, 1, 1, 0.18);
        ctx.Fill();
        ctx.Rectangle(b.X, b.Y + 1, 1, b.Height - 1);
        ctx.SetSourceRGBA(1, 1, 1, 0.18);
        ctx.Fill();
        ctx.Rectangle(b.X + 1, b.Y + b.Height - 1, b.Width - 1, 1);
        ctx.SetSourceRGBA(0, 0, 0, 0.25);
        ctx.Fill();
        ctx.Rectangle(b.X + b.Width - 1, b.Y, 1, b.Height - 1);
        ctx.SetSourceRGBA(0, 0, 0, 0.25);
        ctx.Fill();
    }

    private (double Y, double H) GetVHandleSpan()
    {
        double trackH = _viewportH;
        double ratio = _contentH > 0 ? Math.Min(1, _viewportH / _contentH) : 1;
        double handleH = Math.Max(MinHandleLength, ratio * trackH);
        if (handleH > trackH)
        {
            handleH = trackH;
        }

        double scrollable = Math.Max(0, trackH - handleH);
        double maxScroll = Math.Max(0, _contentH - _viewportH);
        double handleY = _viewportY + (maxScroll > 0 ? ScrollY / maxScroll * scrollable : 0);
        return (handleY, handleH);
    }

    private (double X, double W) GetHHandleSpan()
    {
        double trackW = _viewportW;
        double ratio = _contentW > 0 ? Math.Min(1, _viewportW / _contentW) : 1;
        double handleW = Math.Max(MinHandleLength, ratio * trackW);
        if (handleW > trackW)
        {
            handleW = trackW;
        }

        double scrollable = Math.Max(0, trackW - handleW);
        double maxScroll = Math.Max(0, _contentW - _viewportW);
        double handleX = _viewportX + (maxScroll > 0 ? ScrollX / maxScroll * scrollable : 0);
        return (handleX, handleW);
    }

    private void HandleVScrollbarDown(GuiMouseEventArgs e)
    {
        if (!_showVScrollbar)
        {
            return;
        }

        double maxScroll = Math.Max(0, _contentH - _viewportH);
        if (maxScroll <= 0)
        {
            return; // forced-visible scrollbar with no overflow — non-interactive.
        }

        var (handleY, handleH) = GetVHandleSpan();
        if (e.Position.Y >= handleY && e.Position.Y < handleY + handleH)
        {
            // Grab inside handle: keep grab offset so the handle doesn't jump under the cursor.
            _vDragHandleOffset = e.Position.Y - handleY;
        }
        else
        {
            // Track click: jump so the handle is centred on the click, then continue dragging.
            _vDragHandleOffset = handleH / 2.0;
            ApplyVHandlePos(e.Position.Y - _vDragHandleOffset);
        }
        _vDragging = true;
    }

    private void HandleVScrollbarMove(GuiMouseEventArgs e)
    {
        if (!_vDragging)
        {
            return;
        }

        ApplyVHandlePos(e.Position.Y - _vDragHandleOffset);
    }

    private void HandleVScrollbarUp(GuiMouseEventArgs e) => _vDragging = false;

    private void HandleHScrollbarDown(GuiMouseEventArgs e)
    {
        if (!_showHScrollbar)
        {
            return;
        }

        double maxScroll = Math.Max(0, _contentW - _viewportW);
        if (maxScroll <= 0)
        {
            return;
        }

        var (handleX, handleW) = GetHHandleSpan();
        if (e.Position.X >= handleX && e.Position.X < handleX + handleW)
        {
            _hDragHandleOffset = e.Position.X - handleX;
        }
        else
        {
            _hDragHandleOffset = handleW / 2.0;
            ApplyHHandlePos(e.Position.X - _hDragHandleOffset);
        }
        _hDragging = true;
    }

    private void HandleHScrollbarMove(GuiMouseEventArgs e)
    {
        if (!_hDragging)
        {
            return;
        }

        ApplyHHandlePos(e.Position.X - _hDragHandleOffset);
    }

    private void HandleHScrollbarUp(GuiMouseEventArgs e) => _hDragging = false;

    private void ApplyVHandlePos(double newHandleY)
    {
        double previousScrollY = ScrollY;
        var (_, handleH) = GetVHandleSpan();
        double scrollable = Math.Max(0, _viewportH - handleH);
        double maxScroll = Math.Max(0, _contentH - _viewportH);
        double rel = scrollable > 0 ? Clamp((newHandleY - _viewportY) / scrollable, 0, 1) : 0;
        ScrollY = rel * maxScroll;
        if (ScrollY != previousScrollY)
        {
            RequestArrange();
        }
    }

    private void ApplyHHandlePos(double newHandleX)
    {
        double previousScrollX = ScrollX;
        var (_, handleW) = GetHHandleSpan();
        double scrollable = Math.Max(0, _viewportW - handleW);
        double maxScroll = Math.Max(0, _contentW - _viewportW);
        double rel = scrollable > 0 ? Clamp((newHandleX - _viewportX) / scrollable, 0, 1) : 0;
        ScrollX = rel * maxScroll;
        if (ScrollX != previousScrollX)
        {
            RequestArrange();
        }
    }

    private static double Clamp(double v, double lo, double hi)
        => v < lo ? lo : (v > hi ? hi : v);
}
