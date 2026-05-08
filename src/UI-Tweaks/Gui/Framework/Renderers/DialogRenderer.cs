using Cairo;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogRenderer : IGuiComponentTreeRenderer, IDisposable
{
    private readonly ICoreClientAPI _clientApi;
    private readonly IGuiDialog _dialog;
    private readonly string _name;
    private bool _isDisposed;

    private readonly GuiRenderTreeBuilder _builder;
    private ImageSurface _surface;
    private Context _cairoContext;
    private LoadedTexture _texture;
    private float _currentScale;
    // Logical (unscaled) dialog size the current Cairo surface was sized for.
    // Tracked separately from LayoutParameters because derived dialog ctors may mutate
    // Width/Height after the base ctor created the initial surface; we recreate lazily
    // in OnRenderFrame when either the scale or the logical size changes.
    private double _currentLogicalW;
    private double _currentLogicalH;

    // Pending rebuilds, deduplicated by fragment reference identity.
    // Fragment is the key; builder is the subtree runner for that fragment.
    // Two buffers: Schedule() always writes into _pendingRebuilds; OnRenderFrame swaps them
    // before iterating so that StateHasChanged() calls during Run() write into the now-empty
    // _pendingRebuilds without causing a modification-during-enumeration exception.
    private Dictionary<GuiRenderFragment, GuiRenderTreeBuilder> _pendingRebuilds = [];
    private Dictionary<GuiRenderFragment, GuiRenderTreeBuilder> _activeRebuilds = [];

    // Surface invalidation flag. The Cairo surface contents are independent of where the
    // dialog is blitted on screen, so a position change (e.g. a title-bar drag) must not
    // force a full Cairo re-render + GPU texture upload — it only changes the destination
    // rectangle of the blit. We keep the surface "clean" between frames and re-execute the
    // render walk only when something invalidates it: an initial frame, a reconcile drain,
    // or a surface size change. This matches vanilla's <c>GuiComposer.Render</c> which
    // blits a cached static texture every frame and only recomposes when bounds change.
    private bool _surfaceDirty = true;

    // Interactive region table — populated during the render walk in declaration/render order.
    // Hit-testing walks this in reverse so later-rendered (z-order topmost) regions win.
    // Reused across frames; cleared at the start of each render walk.
    private readonly List<InteractiveRegion> _regions = [];

    // Scroll region table — populated by GuiContainer subtrees that have scrolling enabled.
    // Mouse-wheel events hit-test this list in reverse (topmost-wins) and forward the delta
    // to the matching container's HandleMouseWheel. Kept separate from _regions so wheel
    // routing does not interact with mouse-down capture semantics.
    private readonly List<ScrollRegion> _scrollRegions = [];

    private readonly record struct ScrollRegion(GuiComponentBounds Bounds, GuiContainer Container);

    // Mouse capture: when a region receives MouseDown we hold its OnMouseUp/OnMouseClick
    // callbacks and identity until the next MouseUp, even if the cursor wanders outside.
    // This mirrors typical UI capture semantics (Win32 SetCapture, browsers, vanilla VS):
    // OnMouseUp always fires on the originating component so it can release any "pressed"
    // visual state; OnMouseClick only fires when the up coordinates also lie inside that
    // component's current bounds.
    private object? _capturedToken;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseUp;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseClick;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseMove;
    private EnumMouseButton _capturedButton;

    // Hover tracking: records which region the uncaptured cursor is currently over.
    // _hoveredOnMouseLeave is snapshotted at Enter time so we can fire it at Leave time
    // even if the region table has been repopulated in the interim.
    // Hover state is only updated in the non-captured branch of DispatchMouseMove.
    private object? _hoveredToken;
    private GuiCallback<GuiMouseEventArgs> _hoveredOnMouseLeave;

    // Floating layers (tooltip overlay, dropdown / menu popup overlay, ...). Each owns
    // its own Cairo surface so its content can extend beyond this dialog's bounds, and
    // each is driven through the unified FloatingLayerRenderer lifecycle
    // (OnFrameStart / RunWalk / Render) below — see _floatingLayers. Two typed handles
    // are kept alongside the array because callers need them for layer-specific paths
    // (HideTooltip, ContainsOverlayScreenPoint, host construction, etc.).
    //
    // The companion TooltipHost / OverlayHost are published as cascading values at the
    // dialog root so descendants can register tooltip-trigger regions via GuiTooltip
    // and floating popups via OverlayHost.Show.
    private readonly TooltipRenderer _tooltipRenderer;
    private readonly TooltipHost _tooltipHost;
    private readonly OverlayRenderer _overlayRenderer;
    private readonly OverlayHost _overlayHost;

    // Per-dialog cursor controller — published at the dialog-root cascade chain so any
    // descendant can request a hover cursor (linkselect, textselect, …). Read by GuiDialog
    // after each mouse-move dispatch and forwarded to the input interceptor's
    // MouseOverCursor slot. Owned here so the same lifecycle as the rest of the
    // cascading-value hosts (TooltipHost / OverlayHost / FocusManager).
    private readonly GuiCursorHost _cursorHost = new();

    /// <summary>The dialog's hover-cursor controller. Read by <see cref="GuiDialog"/>
    /// after each mouse-move dispatch and forwarded to the platform cursor.</summary>
    internal GuiCursorHost CursorHost => _cursorHost;

    /// <summary>All floating layers, in z-order (back to front). Iterated for the
    /// per-frame OnFrameStart / RunWalk / Render lifecycle so adding a new floating
    /// layer (e.g. context menus, drag ghost) only requires appending to this array.
    /// Tooltip is last so it always paints on top of every overlay.</summary>
    private readonly FloatingLayerRenderer[] _floatingLayers;

    // Per-dialog focus controller. Published as a cascading value alongside TooltipHost
    // so any descendant component can request focus / read focus state. The renderer is
    // the source of truth: FocusManager just wraps SetFocusedNode / FocusedNode.
    private readonly FocusManager _focusManager;

    // The currently focused node. Mutated by SetFocusedNode (called from RequestFocus /
    // Blur via FocusManager, or from DispatchMouseDown when no handler claims focus).
    // Read by DispatchKey* to look up the focused slot's keyboard handlers in
    // _keyboardRegions, and by Render to paint focus-state visuals.
    internal IGuiNode? FocusedNode { get; private set; }

    // Per-frame keyboard region table. Populated during the render walk by
    // AddKeyboardRegion for any slot whose frame has at least one keyboard handler.
    // Cleared at the start of each render walk — unlike _regions, this list is also
    // searched on key dispatch (linear scan for Token == FocusedNode); the typical size
    // is 0–1 entries (the focused input), so the linear scan is faster than a dictionary.
    private readonly List<KeyboardRegion> _keyboardRegions = [];

    // Tracks whether DispatchMouseDown's hit-tested handler chain called RequestFocus.
    // When false at the end of dispatch, focus is cleared — clicking outside any
    // focusable component blurs (matches typical UI behaviour). Reset to false at the
    // start of every dispatch.
    private bool _focusClaimedThisDispatch;

    // Caret-blink tick. Accumulates render-frame deltaTime while a node is focused; every
    // BlinkPeriodMs the renderer flips the cached blink phase and marks the surface dirty
    // so caret visuals (drawn in components' Render) repaint. Reset on focus change so
    // the caret is always visible immediately after focus is granted.
    private const float BlinkPeriodSeconds = 0.5f;
    private float _blinkAccumulator;
    private bool _caretBlinkOn = true;

    /// <summary>True when the caret should currently be drawn. Components read this from
    /// their Render hook to draw a blinking caret without managing their own timer.</summary>
    internal bool CaretBlinkOn => _caretBlinkOn;

    public IGuiRenderHandle Handle { get; private init; }

    public ICoreClientAPI ClientApi => _clientApi;

    public double RenderOrder => _dialog.RenderOrder;
    public int RenderRange => int.MaxValue;

    internal DialogRenderer(ICoreClientAPI clientApi, IGuiDialog dialog, string name)
    {
        _clientApi = clientApi;
        _dialog = dialog;
        _name = name;

        if (dialog.LayoutParameters.Width is null || dialog.LayoutParameters.Height is null)
            throw new ArgumentException("Dialog must have fixed width and height for rendering.", nameof(dialog));

        _currentScale = RuntimeEnv.GUIScale;
        _currentLogicalW = dialog.LayoutParameters.Width!.Value;
        _currentLogicalH = dialog.LayoutParameters.Height!.Value;
        (_surface, _cairoContext) = CreateSurface(_currentScale, _currentLogicalW, _currentLogicalH);

        _builder = new GuiRenderTreeBuilder(this);
        // Root handle: the dialog component has no parent builder \u2014 nothing in the
        // ancestry can publish cascading values to it. Pass null so cascade lookups from
        // the dialog itself short-circuit to "not found".
        Handle = new RenderHandle(this, _builder, parentBuilder: null);
        _texture = new LoadedTexture(clientApi);

        // Tooltip layer. The host is published at the dialog-root cascade chain so every
        // GuiTooltip in the dialog tree can find it via GetCascadingValue<TooltipHost>().
        // Order matters: the renderer is created first so the host can hold a reference
        // to it; then the chain is built and seeded into both the dialog root builder
        // (for descendant lookups) and the tooltip builder (so a tooltip's content can
        // also consume the same set of ancestor cascading values).
        _tooltipRenderer = new TooltipRenderer(clientApi);
        _tooltipHost = new TooltipHost(_tooltipRenderer);
        _overlayRenderer = new OverlayRenderer(this, clientApi);
        _overlayHost = new OverlayHost(_overlayRenderer);
        // z-order: overlays paint on top of the main dialog texture, tooltips on top
        // of overlays (a tooltip on a dropdown row should sit above the popup chrome).
        _floatingLayers = [_overlayRenderer, _tooltipRenderer];
        _focusManager = new FocusManager(this);
        // Publish TooltipHost, FocusManager and OverlayHost at the dialog-root cascade
        // chain so every descendant can resolve them via GetCascadingValue<T>(). The
        // chain is a linked list — stack one link per published value onto the null root.
        var rootChain = new CascadingValueChain(
            parent: null, valueType: typeof(TooltipHost), name: null, value: _tooltipHost);
        rootChain = new CascadingValueChain(
            parent: rootChain, valueType: typeof(FocusManager), name: null, value: _focusManager);
        rootChain = new CascadingValueChain(
            parent: rootChain, valueType: typeof(OverlayHost), name: null, value: _overlayHost);
        rootChain = new CascadingValueChain(
            parent: rootChain, valueType: typeof(GuiCursorHost), name: null, value: _cursorHost);
        _builder.CascadeChain = rootChain;
        _tooltipRenderer.SetCascadeChain(_builder.CascadeChain);
        _overlayRenderer.SetCascadeChain(_builder.CascadeChain);
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (_isDisposed) return;

        // Caret blink tick. Only advances while a node is focused so unfocused dialogs
        // don't pay redraw cost for nothing. Flips _caretBlinkOn every BlinkPeriodSeconds
        // and marks the surface dirty so caret visuals (drawn in components' Render)
        // repaint. Reset on focus change so the caret is visible immediately after focus
        // is granted \u2014 see SetFocusedNode.
        if (FocusedNode is not null)
        {
            _blinkAccumulator += deltaTime;
            if (_blinkAccumulator >= BlinkPeriodSeconds)
            {
                _blinkAccumulator -= BlinkPeriodSeconds;
                _caretBlinkOn = !_caretBlinkOn;
                _surfaceDirty = true;
            }
        }

        // Phase 1: drain reconcile queue.
        // Swap buffers first: _pendingRebuilds is now free for incoming Schedule() calls
        // while we drain _activeRebuilds — prevents modification-during-enumeration.
        // The drain uses a pop-one-at-a-time loop so a Run() that recursively reconciles
        // a child subtree can call Cancel() to remove that child's queued rebuild from
        // _activeRebuilds without breaking iteration.
        if (_pendingRebuilds.Count > 0)
        {
            (_pendingRebuilds, _activeRebuilds) = (_activeRebuilds, _pendingRebuilds);
            while (_activeRebuilds.Count > 0)
            {
                // Pop an arbitrary entry. The enumerator is discarded after one MoveNext,
                // so removing the key on the next line is safe.
                var enumerator = _activeRebuilds.GetEnumerator();
                enumerator.MoveNext();
                var (fragment, builder) = enumerator.Current;
                _activeRebuilds.Remove(fragment);
                builder.Run(fragment);
            }
            // A drained reconcile may have changed component configuration that affects
            // layout / drawing — surface needs a redraw on this frame.
            _surfaceDirty = true;
        }

        // Recreate surface if the UI scale factor or the logical dialog size changed.
        // Derived dialog constructors may set Width/Height after the base ctor created the
        // initial surface, and dialogs may mutate their size at runtime; either case requires
        // resizing the Cairo surface so the full laid-out tree is drawn (not clipped).
        float scale = RuntimeEnv.GUIScale;
        double logicalW = _dialog.LayoutParameters.Width!.Value;
        double logicalH = _dialog.LayoutParameters.Height!.Value;
        if (scale != _currentScale || logicalW != _currentLogicalW || logicalH != _currentLogicalH)
        {
            _currentScale = scale;
            _currentLogicalW = logicalW;
            _currentLogicalH = logicalH;
            _cairoContext.Dispose();
            _surface.Dispose();
            (_surface, _cairoContext) = CreateSurface(scale, logicalW, logicalH);
            _surfaceDirty = true;
        }

        // Phase 2: render walk — only when the surface is dirty. A pure position change (e.g.
        // a title-bar drag updating the dialog's offset) does not invalidate the texture, so
        // we skip the expensive Cairo redraw + GPU upload and reuse the previous frame's
        // texture. _regions stays valid in that case because layout is a function of size /
        // children only, both of which would have flipped _surfaceDirty if changed.
        if (_surfaceDirty)
        {
            _cairoContext.IdentityMatrix();
            _cairoContext.Operator = Operator.Source;
            _cairoContext.SetSourceRGBA(0, 0, 0, 0);
            _cairoContext.Paint();
            _cairoContext.Operator = Operator.Over;

            // Scale so components can use logical (unscaled) coordinates.
            _cairoContext.Scale(scale, scale);

            // Reset the interactive region table — slots with mouse handlers will repopulate it
            // during the render walk via AddInteractiveRegion. Clearing here (rather than after
            // dispatch) means input that arrives after the first frame has a fresh, layout-accurate
            // table to hit-test against.
            _regions.Clear();
            _scrollRegions.Clear();
            _keyboardRegions.Clear();

            // Tooltip-trigger regions are repopulated by GuiTooltip.Render during the same
            // walk; reset here for the same reason as _regions. Skipped on clean frames so a
            // stationary cursor over a stable layout keeps hovering its current trigger.
            _tooltipHost.ResetFrame();

            // Overlays follow the same per-frame discipline: components re-Show their
            // overlays from their Render hook each dirty frame; tokens not refreshed by
            // the time RunWalk runs are pruned. Reset here so the "refreshed this
            // frame" set begins empty. Tooltip layer's OnFrameStart is a no-op default
            // — the tooltip host owns its own per-frame region table cleared above.
            for (int i = 0; i < _floatingLayers.Length; i++)
                _floatingLayers[i].OnFrameStart();

            var bounds = new GuiComponentBounds(0, 0, logicalW, logicalH);
            _builder.Render(_cairoContext, bounds, _dialog.LayoutParameters.Direction);

            // Upload Cairo surface to GPU. Only happens on dirty frames — significant CPU /
            // bandwidth saving while the user is e.g. dragging the dialog around.
            _surface.Flush();
            _clientApi.Gui.LoadOrUpdateCairoTexture(_surface, true, ref _texture);
            _surfaceDirty = false;

            // Phase 2.5: floating layers' in-walk phase. Runs after the main builder so
            // any interactive / scroll / keyboard regions that overlay-style layers
            // forward into the dialog's region tables are appended last (winning the
            // topmost-wins reverse hit-test). Each layer walks its own Cairo surface
            // — separate from this dialog's main surface so the layer can extend beyond
            // the dialog's bounds without being clipped. Cached layer textures are
            // blitted in Phase 4 below, after the main blit. Tooltip's RunWalk is a
            // no-op default; its reconcile + redraw runs in its Render() phase so a
            // tooltip shown by a hover transition on a clean main frame still draws.
            for (int i = 0; i < _floatingLayers.Length; i++)
                _floatingLayers[i].RunWalk();

            // Region table is now fully populated for this frame. Re-evaluate hover
            // against the current cursor position so layout changes that shifted regions
            // under a stationary cursor (most commonly wheel-scrolling a dropdown popup)
            // fire leave/enter transitions immediately rather than waiting for the next
            // mouse-move event. Skipped while a drag is in progress — capture semantics
            // mean the captured component owns the cursor regardless of geometry.
            if (_capturedToken is null)
            {
                TryToLogical(_clientApi.Input.MouseX, _clientApi.Input.MouseY, out double rlx, out double rly);
                RefreshHover(rlx, rly, _clientApi.Input.MouseX, _clientApi.Input.MouseY,
                    EnumMouseButton.None, 0);
            }
        }

        // Phase 3: blit cached texture to screen. Position is recomputed every frame so that
        // a moved dialog tracks the cursor without requiring a redraw.
        // Use the texture's actual integer pixel dimensions (not logicalW*scale, which can
        // be fractional and cause linear-filter resampling that visibly fattens glyph strokes).
        double physW = _texture.Width;
        double physH = _texture.Height;
        var (posX, posY) = ComputeScreenOrigin(physW, physH, scale);

        _clientApi.Render.Render2DTexturePremultipliedAlpha(
            _texture.TextureId,
            posX, posY, physW, physH);

        // Phase 4: floating layers (overlays, tooltips, ...). Drawn after the main blit
        // in z-order: overlays first (so a dropdown popup paints on top of the dialog
        // chrome), then the tooltip last (so a tooltip on a dropdown row sits above the
        // popup). Each layer owns its own Cairo surface so its content can extend
        // beyond this dialog's bounds without being clipped. Each layer's Render runs
        // every frame even when the main surface was clean — the cached layer texture
        // continues to appear and the tooltip layer also re-runs Update so a hover
        // transition that didn't dirty the main surface still draws the new tooltip.
        for (int i = 0; i < _floatingLayers.Length; i++)
            _floatingLayers[i].Render();
    }

    public void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder)
    {
        // Silently ignore post-dispose schedules. A component's RenderHandle may outlive the
        // dialog (queued events, async continuations) and call StateHasChanged after teardown;
        // there is no work to do but throwing would surface in code that has no good way
        // to defend against the race.
        if (_isDisposed) return;
        _pendingRebuilds[fragment] = builder;
    }

    public void Cancel(GuiRenderFragment fragment)
    {
        // Strip from both buffers: pending (scheduled for next frame) and active
        // (currently being drained — a parent rebuild covers this subtree).
        _pendingRebuilds.Remove(fragment);
        _activeRebuilds.Remove(fragment);
    }

    public void AddInteractiveRegion(in InteractiveRegion region)
    {
        _regions.Add(region);
    }

    public void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container)
    {
        _scrollRegions.Add(new ScrollRegion(bounds, container));
    }

    public void AddKeyboardRegion(in KeyboardRegion region)
    {
        _keyboardRegions.Add(region);
    }

    /// <summary>
    /// Updates <see cref="FocusedNode"/> and marks the surface dirty so focus-state
    /// visuals (caret, highlight) repaint on the next frame. No-op when the node is
    /// already focused. Resets the caret blink phase so the caret is visible immediately
    /// after focus is granted.
    /// </summary>
    internal void SetFocusedNode(IGuiNode? node)
    {
        // Mark this dispatch as having claimed focus so DispatchMouseDown's "blur on miss"
        // path doesn't immediately clear what a handler just set. Always set, even when
        // node is null — an explicit Blur() call still counts as "focus was decided".
        _focusClaimedThisDispatch = true;
        if (ReferenceEquals(FocusedNode, node)) return;
        FocusedNode = node;
        _blinkAccumulator = 0f;
        _caretBlinkOn = true;
        _surfaceDirty = true;
    }

    /// <summary>
    /// Routes a vanilla mouse-wheel event to the topmost scrollable container under the
    /// cursor. Walks <see cref="_scrollRegions"/> in reverse so a nested scrollable area
    /// wins over an outer one. Returns true when the event was consumed by a container.
    /// </summary>
    public bool DispatchMouseWheel(MouseWheelEventArgs args)
    {
        if (_scrollRegions.Count == 0) return false;
        // Compute dialog-local logical coordinates without gating on dialog containment
        // — a scrollable region inside an open overlay popup may sit outside the dialog's
        // own rect, but its bounds are still expressed in dialog-local logical pixels.
        TryToLogical(_clientApi.Input.MouseX, _clientApi.Input.MouseY, out double lx, out double ly);

        for (int i = _scrollRegions.Count - 1; i >= 0; i--)
        {
            var r = _scrollRegions[i];
            if (lx >= r.Bounds.X && lx < r.Bounds.X + r.Bounds.Width
             && ly >= r.Bounds.Y && ly < r.Bounds.Y + r.Bounds.Height)
            {
                r.Container.HandleMouseWheel(args.deltaPrecise);
                _surfaceDirty = true;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Hit-tests the current region table against a logical-pixel point and returns the
    /// topmost matching region (last-rendered wins). Returns -1 when no region contains
    /// the point. Walking in reverse mirrors z-order: deeper / later-rendered slots
    /// overlay earlier ones.
    /// </summary>
    private int HitTest(double lx, double ly)
    {
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            if (_regions[i].Contains(lx, ly)) return i;
        }
        return -1;
    }

    /// <summary>
    /// Routes a vanilla mouse-down event to the framework's region table. Returns true if a
    /// region's OnMouseDown handler claimed the event (so the dialog should mark
    /// <see cref="MouseEvent.Handled"/>) — though the dialog still has its own
    /// "click inside dialog rectangle ⇒ handled" behaviour for non-interactive areas.
    /// Even when no OnMouseDown handler is registered, a region with OnMouseUp/OnMouseClick
    /// captures the press so subsequent up/click logic works.
    /// </summary>
    public bool DispatchMouseDown(MouseEvent args)
    {
        // Compute dialog-local logical coordinates without gating on dialog containment.
        // The click may legitimately land outside the dialog rect but still inside an
        // open overlay popup whose interactive regions are stored in dialog-local space
        // — we need lx/ly populated so HitTest can match those regions.
        bool insideDialog = TryToLogical(args.X, args.Y, out double lx, out double ly);

        // Hide any active tooltip on press — tooltips would otherwise visually trail a
        // drag (cursor moves, tooltip follows, but the underlying trigger may scroll out
        // from under the cursor). Mirrors typical UI behaviour where tooltips are
        // suppressed during interaction.
        _tooltipHost.Hide();

        int idx = HitTest(lx, ly);
        if (idx < 0)
        {
            // Missed every interactive region. Only blur when the click was actually
            // inside the dialog — a click off-dialog has no business clearing focus, and
            // an off-dialog overlay-extent miss is a normal close path handled by the
            // requesting component (e.g. dropdown closes on focus loss).
            if (insideDialog) SetFocusedNode(null);
            return false;
        }

        var region = _regions[idx];
        var local = new GuiMouseEventArgs(lx, ly, args.X, args.Y, args.Button, args.Modifiers);

        // Capture even when only OnMouseUp/OnMouseClick is registered — the OnMouseDown
        // listener may not exist but the up/click chain still needs the press recorded.
        _capturedToken = region.Token;
        _capturedOnMouseUp = region.OnMouseUp;
        _capturedOnMouseClick = region.OnMouseClick;
        _capturedOnMouseMove = region.OnMouseMove;
        _capturedButton = args.Button;

        // A mouse handler may flip visual state (e.g. GuiButton._isPressed) without calling
        // StateHasChanged — pressed-state visuals are read directly in Render rather than
        // baked into the blueprint. Invalidate the surface so the next frame redraws and
        // picks up that state. Cheap: handlers run only on actual click frames, not every
        // frame, so the redraw cost is bounded by user input rate.
        _surfaceDirty = true;

        // Track focus claims: a focusable component's OnMouseDown calls
        // FocusManager.RequestFocus, which flips _focusClaimedThisDispatch via
        // SetFocusedNode. If no handler claims focus, blur after the dispatch chain
        // returns — clicking outside any focusable region clears focus.
        _focusClaimedThisDispatch = false;
        region.OnMouseDown.Invoke(local);
        if (!_focusClaimedThisDispatch)
        {
            // No handler called RequestFocus / Blur during dispatch — the click landed on
            // a non-focusable interactive region (e.g. a button), so clear focus.
            SetFocusedNode(null);
        }
        return true;
    }

    /// <summary>
    /// Routes a vanilla mouse-up event. Always fires the captured component's OnMouseUp
    /// (regardless of cursor position) so it can release any "pressed" visual state, and
    /// fires OnMouseClick only when the cursor is still inside that component's current
    /// bounds — matching the user's spec ("MouseClick only if MouseDown + MouseUp both
    /// happened within the component's boundaries"). Returns true if the captured component
    /// was the click target, regardless of where the up landed.
    /// </summary>
    public bool DispatchMouseUp(MouseEvent args)
    {
        if (_capturedToken is null) return false;
        // Match button: a left-down is not released by a right-up. Vanilla rarely interleaves,
        // but be defensive — Modifiers may also represent multi-button state on some setups.
        if (args.Button != _capturedButton) return false;

        // Always compute logical coordinates and run the captured-region lookup, even when
        // the release lands outside the dialog rect — overlay popups (dropdowns, menus)
        // register their interactive regions at dialog-local coordinates that legitimately
        // extend beyond the dialog's own bounds. Gating the lookup on TryToLogical's return
        // (point inside dialog rect) silently drops clicks on overlay items that hang
        // outside the dialog, breaking selection in dropdowns whose popup overflows below
        // the dialog. The Contains() check on the captured region's own bounds is the only
        // correct constraint — that bounds rect already encodes the legitimate hit area.
        TryToLogical(args.X, args.Y, out double lx, out double ly);
        bool inside = false;
        // Look up the captured token in the current region table. If still present, use
        // its current bounds (layout may have shifted between down and up). If not — slot
        // was disposed — treat as "outside" and skip click but still fire up.
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            var r = _regions[i];
            if (r.Token == _capturedToken)
            {
                inside = r.Contains(lx, ly);
                break;
            }
        }

        var local = new GuiMouseEventArgs(lx, ly, args.X, args.Y, args.Button, args.Modifiers);

        // Snapshot then clear capture before invoking, so a callback that synchronously
        // dispatches another mouse event does not see stale capture state.
        var up = _capturedOnMouseUp;
        var click = _capturedOnMouseClick;
        _capturedToken = null;
        _capturedOnMouseUp = default;
        _capturedOnMouseClick = default;
        _capturedOnMouseMove = default;

        up.Invoke(local);
        if (inside) click.Invoke(local);

        // Same rationale as DispatchMouseDown — release may clear pressed-state visuals.
        _surfaceDirty = true;

        return true;
    }

    /// <summary>
    /// Routes a vanilla mouse-move event. Behaviour differs based on capture state:
    /// <list type="bullet">
    ///   <item><b>Captured (drag)</b> — only the captured component's <c>OnMouseMove</c> fires;
    ///   the cursor may be outside the dialog rect. Does <i>not</i> set <c>_surfaceDirty</c> —
    ///   drag only updates the dialog's blit offset (<c>OffsetX</c>/<c>OffsetY</c>), not the
    ///   Cairo surface contents.</item>
    ///   <item><b>Uncaptured (hover)</b> — hit-tests the cursor against the region table and
    ///   fires <c>OnMouseLeave</c> / <c>OnMouseEnter</c> as the hovered region changes.
    ///   Sets <c>_surfaceDirty</c> only when a handler actually fires, so frames where the
    ///   cursor moves within the same region (or outside with no hover) do not trigger a
    ///   redundant Cairo redraw.</item>
    /// </list>
    /// Returns true when the event should be considered handled by this dialog.
    /// </summary>
    public bool DispatchMouseMove(MouseEvent args)
    {
        if (_capturedToken is not null)
        {
            // Drag path: fire OnMouseMove on the captured component only.
            // Allow cursor outside dialog bounds (fast drags can overshoot the edge).
            if (!_capturedOnMouseMove.HasHandler) return true; // capture exists but no move listener — still consume.
            TryToLogical(args.X, args.Y, out double lx, out double ly);
            var local = new GuiMouseEventArgs(lx, ly, args.X, args.Y, args.Button, args.Modifiers);
            _capturedOnMouseMove.Invoke(local);
            // The handler may have mutated component state that affects rendering (e.g.
            // scrollbar drag updates ScrollY). Mark dirty so the surface redraws this frame.
            _surfaceDirty = true;
            return true;
        }

        // Hover path: uncaptured cursor movement. Delegate to the shared refresh helper
        // (also called post-walk after layout changes) so the leave/enter dispatch logic
        // lives in one place.
        TryToLogical(args.X, args.Y, out double hlx, out double hly);
        int idx = RefreshHover(hlx, hly, args.X, args.Y, args.Button, args.Modifiers);
        return idx >= 0;
    }

    /// <summary>
    /// Refreshes hover state against the current region table for the given logical /
    /// physical cursor position. Fires <c>OnMouseLeave</c> on the previously-hovered
    /// region and <c>OnMouseEnter</c> on the new one when the topmost region under the
    /// cursor changes, and updates the tooltip host. Returns the index of the new
    /// hovered region (or -1).
    /// <para>
    /// Called from <see cref="DispatchMouseMove"/> on real mouse motion, and from
    /// <see cref="OnRenderFrame"/> after a dirty render walk has repopulated the region
    /// table — the latter handles the "regions moved under a stationary cursor" case
    /// (e.g. wheel-scrolling a dropdown popup), where hover would otherwise stick to a
    /// region that is no longer geometrically under the cursor until the user nudges
    /// the mouse.
    /// </para>
    /// </summary>
    private int RefreshHover(double lx, double ly, int physX, int physY, EnumMouseButton button, int modifiers)
    {
        int idx = HitTest(lx, ly);
        object? newToken = idx >= 0 ? _regions[idx].Token : null;
        if (newToken != _hoveredToken)
        {
            var local = new GuiMouseEventArgs(lx, ly, physX, physY, button, modifiers);

            // Leave the previous region. Fire the callback snapshotted at Enter time —
            // the new frame's region table may not contain the previously-hovered slot
            // at all (e.g. a dropdown popup just closed), so we cannot look it up here.
            if (_hoveredToken is not null)
            {
                if (_hoveredOnMouseLeave.HasHandler)
                {
                    _hoveredOnMouseLeave.Invoke(local);
                    _surfaceDirty = true;
                }
                _hoveredToken = null;
                _hoveredOnMouseLeave = default;
            }

            // Enter the new region.
            if (newToken is not null)
            {
                var newRegion = _regions[idx];
                _hoveredToken = newToken;
                _hoveredOnMouseLeave = newRegion.OnMouseLeave;
                if (newRegion.OnMouseEnter.HasHandler)
                {
                    newRegion.OnMouseEnter.Invoke(local);
                    _surfaceDirty = true;
                }
            }
        }

        // Tooltip hover update: walks the host's separate region table (independent of
        // _regions / _hoveredToken so a tooltip wrapper around an interactive child like
        // a button still triggers when the child wins the topmost-region hit test).
        _tooltipHost.UpdateHover(lx, ly);

        return idx;
    }

    /// <summary>
    /// Returns true if the given screen-space point (in physical pixels) lies within this
    /// dialog's drawn rectangle. Mirrors the centering math used in <see cref="OnRenderFrame"/>.
    /// </summary>
    /// Returns true if the given screen-space point (in physical pixels) lies within this
    /// dialog's drawn rectangle. Mirrors the centering math used in <see cref="OnRenderFrame"/>.
    /// </summary>
    public bool ContainsScreenPoint(int x, int y)
    {
        float scale = RuntimeEnv.GUIScale;
        double physW = _dialog.LayoutParameters.Width!.Value * scale;
        double physH = _dialog.LayoutParameters.Height!.Value * scale;
        var (posX, posY) = ComputeScreenOrigin(physW, physH, scale);
        return x >= posX && x < posX + physW && y >= posY && y < posY + physH;
    }

    /// <summary>
    /// Returns true when the given screen-space point lies inside the active overlay's
    /// blit rectangle. Forwarded to <see cref="OverlayRenderer"/>; consumed by
    /// <see cref="GuiDialog"/> so input dispatch can route through to overlay regions
    /// even when the click landed outside the dialog's own rect (e.g. a dropdown popup
    /// hanging below the dialog's bottom edge).
    /// </summary>
    public bool ContainsOverlayScreenPoint(int x, int y) => _overlayRenderer.ContainsScreenPoint(x, y);

    /// <summary>
    /// Returns the dialog's current on-screen origin in physical pixels. Used by the
    /// overlay layer to anchor its blit position to the dialog's centre+offset pose so
    /// overlays follow dialog drags without invalidating their own surface.
    /// </summary>
    internal (int posX, int posY) GetScreenOrigin()
    {
        float scale = RuntimeEnv.GUIScale;
        double physW = _dialog.LayoutParameters.Width!.Value * scale;
        double physH = _dialog.LayoutParameters.Height!.Value * scale;
        return ComputeScreenOrigin(physW, physH, scale);
    }

    /// <summary>
    /// Converts a physical-pixel screen point to logical-pixel dialog-local coordinates,
    /// returning false when the point lies outside the dialog. The logical space matches
    /// the coordinate system used by the layout pass and stored in <see cref="InteractiveRegion.Bounds"/>.
    /// </summary>
    public bool TryToLogical(int x, int y, out double lx, out double ly)
    {
        float scale = RuntimeEnv.GUIScale;
        double physW = _dialog.LayoutParameters.Width!.Value * scale;
        double physH = _dialog.LayoutParameters.Height!.Value * scale;
        var (posX, posY) = ComputeScreenOrigin(physW, physH, scale);
        lx = (x - posX) / scale;
        ly = (y - posY) / scale;
        return x >= posX && x < posX + physW && y >= posY && y < posY + physH;
    }

    /// <summary>
    /// Computes the on-screen origin (top-left, physical pixels) of the dialog. The dialog is
    /// centred on the screen and then offset by <see cref="IGuiDialog.OffsetX"/>/<see cref="IGuiDialog.OffsetY"/>
    /// (logical pixels, scaled to physical here).
    /// <para>
    /// Returns whole pixels via integer truncation, exactly mirroring vanilla
    /// <c>(int)bounds.renderX</c> / <c>(int)bounds.renderY</c>. Truncation (rather than
    /// round-to-nearest) is essential for drag smoothness: round-to-nearest flips between
    /// <c>n</c> and <c>n+1</c> as the floating-point screen position oscillates ±epsilon
    /// across an <c>x.5</c> boundary, which manifests as a one-pixel cross-axis stair-step.
    /// Truncation has a stable floor — the value steps exactly once per integer crossing.
    /// </para>
    /// </summary>
    private (int posX, int posY) ComputeScreenOrigin(double physW, double physH, float scale)
    {
        int posX = (int)((_clientApi.Render.FrameWidth - physW) / 2.0 + _dialog.OffsetX * scale);
        int posY = (int)((_clientApi.Render.FrameHeight - physH) / 2.0 + _dialog.OffsetY * scale);
        return (posX, posY);
    }

    private static (ImageSurface Surface, Context Context) CreateSurface(float scale, double logicalW, double logicalH)
    {
        int physW = (int)Math.Round(logicalW * scale);
        int physH = (int)Math.Round(logicalH * scale);
        var surface = new ImageSurface(Format.Argb32, physW, physH);
        return (surface, new Context(surface));
    }

    /// <summary>
    /// Forces the floating tooltip to hide. Called by <see cref="GuiDialog.Close"/> so a
    /// tooltip that was visible at close time does not linger or flash on the next open.
    /// </summary>
    internal void HideTooltip() => _tooltipHost.Hide();

    /// <summary>
    /// Routes a vanilla key-down event to the focused slot's <c>OnKeyDown</c> handler
    /// and to the focused node's virtual <see cref="IGuiNode.OnKeyDown"/> hook (in that
    /// order). Returns true when an entry was found for the focused node, regardless of
    /// whether either site marks the event handled.
    /// </summary>
    public bool DispatchKeyDown(KeyEvent args) => DispatchKey(GuiKeyEventKind.Down, args);
    public bool DispatchKeyUp(KeyEvent args) => DispatchKey(GuiKeyEventKind.Up, args);
    public bool DispatchKeyPress(KeyEvent args) => DispatchKey(GuiKeyEventKind.Press, args);

    private bool DispatchKey(GuiKeyEventKind kind, KeyEvent args)
    {
        if (FocusedNode is null) return false;
        var local = new GuiKeyEventArgs(args);

        // Slot-level handler: linear scan for the entry whose Token matches the focused
        // node. Typical case is 0\u20131 entries, so the scan beats a Dictionary lookup. The
        // entry only exists when the focused slot has at least one keyboard handler
        // attached \u2014 a focused node with no slot-level handlers is fine, just no-op.
        for (int i = 0; i < _keyboardRegions.Count; i++)
        {
            var r = _keyboardRegions[i];
            if (!ReferenceEquals(r.Token, FocusedNode)) continue;
            switch (kind)
            {
                case GuiKeyEventKind.Down: r.OnKeyDown.Invoke(local); break;
                case GuiKeyEventKind.Up: r.OnKeyUp.Invoke(local); break;
                case GuiKeyEventKind.Press: r.OnKeyPress.Invoke(local); break;
            }
            break;
        }

        // Virtual hook on the focused node \u2014 fires alongside the slot-level handler so
        // components can implement their own keyboard logic without forcing every consumer
        // to re-register the same callbacks at every declaration site.
        switch (kind)
        {
            case GuiKeyEventKind.Down: FocusedNode.OnKeyDown(local); break;
            case GuiKeyEventKind.Up: FocusedNode.OnKeyUp(local); break;
            case GuiKeyEventKind.Press: FocusedNode.OnKeyPress(local); break;
        }

        // Keyboard-driven state changes (typing into a TextInput, toggling a Checkbox)
        // mutate component fields read directly in Render. Mark the surface dirty so the
        // next frame redraws and picks up the new state \u2014 same rationale as mouse dispatch.
        _surfaceDirty = true;
        return true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        for (int i = 0; i < _floatingLayers.Length; i++)
            _floatingLayers[i].Dispose();
        _builder.Dispose();
        _texture.Dispose();
        _cairoContext.Dispose();
        _surface.Dispose();
    }
}
