using Cairo;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Executes <see cref="GuiRenderFragment"/>s, reconciles the resulting
/// <see cref="RenderTreeFrame"/> instructions against previously-known state,
/// and manages the lifetimes of child <see cref="IGuiNode"/> instances.
/// </summary>
internal sealed class GuiRenderTreeBuilder : IGuiRenderTreeBuilder, IDisposable
{
    private readonly GuiSurfaceRenderer _renderer;

    // Frame buffer: filled during the blueprint phase, cleared at the start of each Run().
    private readonly List<RenderTreeFrame> _frames = [];

    // Persistent storage for keyed slots. Key is a value-type struct — no frame allocation
    // needed just for identity.
    private readonly Dictionary<ComponentSlotKey, ComponentSlot> _keyedSlots = [];

    // Ordered list of active slots, rebuilt each Run() to match the current frame order.
    // Used by arrange and paint walks to iterate children in declaration order.
    private readonly List<ComponentSlot> _renderOrder = [];
    private readonly IReadOnlyList<IGuiComponentSlot> _componentSlots;

    // Reused scratch buffers — avoid allocating inside the hot path.
    private readonly HashSet<ComponentSlotKey> _seenKeys = [];
    private readonly List<ComponentSlotKey> _staleKeys = [];

    // Cascading-value chain visible to the component that owns this builder. Parent
    // reconciliation sets this to the chain snapshotted at the owning slot's declaration site.
    internal CascadingValueChain? InheritedCascadeChain;

    // Cascading-value chain visible to slots declared inside this builder. Parent
    // reconciliation initializes it from InheritedCascadeChain; PushCascadeScope mutates it
    // temporarily during this builder's blueprint phase for descendants declared inside the scope.
    internal CascadingValueChain? CascadeChain;

    internal IReadOnlyList<IGuiComponentSlot> ComponentSlots => _componentSlots;

    internal GuiRenderTreeBuilder(GuiSurfaceRenderer renderer)
    {
        _renderer = renderer;
        _componentSlots = _renderOrder.AsReadOnly();
    }

    public IGuiComponentBuilder<T> AddComponent<T>(int key)
        where T : IGuiNode, new()
    {
        var slotKey = new ComponentSlotKey(typeof(T), key);

        // Detect duplicate (Type, key) declarations within the current blueprint phase.
        // _seenKeys is cleared at the start of every Run() and populated here, so a key
        // that's already present means the same slot was declared twice among siblings.
        if (!_seenKeys.Add(slotKey))
        {
            throw new InvalidOperationException(
                $"Duplicate component key {key} for {typeof(T).Name} within the same render tree level. Each (Type, key) pair must be unique among siblings.");
        }

        RenderTreeFrame<T> frame;

        if (_keyedSlots.TryGetValue(slotKey, out var existingSlot))
        {
            // Reuse the frame that lives inside the persistent slot — zero allocation in steady state.
            frame = (RenderTreeFrame<T>)existingSlot.Frame;
            // Discard any actions accumulated on the previous pass. Actions are re-registered
            // each pass by the user's BuildRenderTree, so per-pass values (e.g. inline
            // `width: x` arguments) take effect immediately on the next pass.
            frame.Reset();
        }
        else
        {
            frame = new RenderTreeFrame<T>(this, key);
        }

        _frames.Add(frame);
        // Snapshot the chain that is active right now in the blueprint phase. A surrounding
        // PushCascadeScope call may have already pushed a link onto CascadeChain, so this
        // records the exact scope visible at this slot's declaration site. The patch phase
        // reads frame.CascadeChain to set the child builder's chain, ensuring descendants
        // see the correct set of cascading values regardless of push/pop interleaving.
        frame.CascadeChain = CascadeChain;
        return frame;
    }

    /// <summary>
    /// Pushes a cascading value scope for the duration of <paramref name="content"/>.
    /// All slots declared inside <paramref name="content"/> at any depth will see
    /// <paramref name="value"/> (matched by <c>typeof(<typeparamref name="T"/>)</c> and
    /// <paramref name="name"/>) when they query the cascade chain. The scope is stack-based
    /// and is restored after <paramref name="content"/> returns — no component is created,
    /// no slot is allocated, and the layout tree is completely unaffected.
    /// </summary>
    public void PushCascadeScope<T>(T value, string? name, GuiRenderFragment content)
    {
        var saved = CascadeChain;
        CascadeChain = new CascadingValueChain(saved, typeof(T), name, value);
        content.Invoke(this);
        CascadeChain = saved;
    }

    /// <summary>
    /// Runs one reconciliation pass.
    /// <list type="number">
    ///   <item><description><b>Blueprint phase</b> — executes <paramref name="fragment"/>, filling the frame buffer.</description></item>
    ///   <item><description><b>Diff phase</b> — compares frames against existing keyed slots.</description></item>
    ///   <item><description><b>Patch phase</b> — pushes configure into reused instances; creates new ones where needed; prunes stale keyed slots; recurses into each component's own children.</description></item>
    /// </list>
    /// </summary>
    internal void Run(GuiRenderFragment fragment)
    {
        _frames.Clear();
        _renderOrder.Clear();
        // Cleared up-front so AddComponent can populate it during the blueprint phase
        // for both duplicate-key detection and stale-pruning below.
        _seenKeys.Clear();
        fragment.Invoke(this);

        foreach (var frame in _frames)
        {
            var slotKey = new ComponentSlotKey(frame.ComponentType, frame.Key);

            bool isNew = !_keyedSlots.TryGetValue(slotKey, out var slot);
            if (isNew)
            {
                slot = CreateSlot(frame);
                _keyedSlots[slotKey] = slot;
            }
            ReconcileSlot(slot!, frame, isNew);
            _renderOrder.Add(slot!);
        }

        _staleKeys.Clear();
        foreach (var key in _keyedSlots.Keys)
        {
            if (!_seenKeys.Contains(key))
            {
                _staleKeys.Add(key);
            }
        }
        foreach (var key in _staleKeys)
        {
            DisposeSlot(_keyedSlots[key]);
            _keyedSlots.Remove(key);
        }
    }

    /// <summary>
    /// Disposes every slot owned by this builder, asking each slot's child builder to do
    /// the same for its own subtree first. Called when a parent slot is pruned (so its
    /// child builder must release everything it owns) and when the dialog is being torn down.
    /// </summary>
    public void Dispose()
    {
        foreach (var slot in _keyedSlots.Values)
        {
            DisposeSlot(slot);
        }

        _keyedSlots.Clear();
        _renderOrder.Clear();
        _frames.Clear();
    }

    private static void DisposeSlot(ComponentSlot slot)
    {
        // Children first — the parent instance may rely on its subtree still existing
        // during its own Dispose (e.g. unsubscribing from child events).
        slot.ChildBuilder.Dispose();
        (slot.Instance as IDisposable)?.Dispose();
    }


    private ComponentSlot CreateSlot(RenderTreeFrame frame)
    {
        var childBuilder = new GuiRenderTreeBuilder(_renderer);
        var instance = frame.CreateInstance();
        var slot = new ComponentSlot(_renderer, instance, childBuilder, frame);
        instance.Attach(slot, _renderer.ClientApi);
        return slot;
    }

    private void ReconcileSlot(ComponentSlot slot, RenderTreeFrame frame, bool isNew)
    {
        // Propagate the declaration-site cascade chain before any component callbacks run.
        // Configure/OnInitialized/OnParametersSet may all read cascading values from the
        // render handle, and descendants declared by this slot consume the same chain as
        // their inherited parent scope during their own blueprint phase.
        slot.ChildBuilder.InheritedCascadeChain = frame.CascadeChain;
        slot.ChildBuilder.CascadeChain = frame.CascadeChain;

        if (slot.Instance is IGuiComponent component)
        {
            // Reset layout parameters to canonical defaults before applying the new
            // pass's config actions — blueprints are declarative (full state), not
            // deltas. Without this, stale LP from a previous view (e.g. a list column
            // that becomes a setting row at the same key) would persist across reuses.
            component.LayoutParameters.Reset();
        }

        frame.ApplySlotConfiguration(slot.Instance);
        frame.ApplyConfiguration(slot.Instance);
        frame.ComposeSlotConfiguration(slot);
        if (isNew)
        {
            slot.Instance.OnInitialized();
        }
        slot.Instance.OnParametersSet();
        // Cancel any separately scheduled rebuild for this child's fragment — we are
        // about to rebuild its subtree right now, making the pending entry redundant.
        _renderer.Cancel(slot.Instance.RenderFragment);
        slot.ChildBuilder.Run(slot.Instance.RenderFragment);
    }

    /// <summary>
    /// Walks the current render order, running the layout pass for each child and then
    /// calling <see cref="IGuiNode.Render"/> with its computed bounds, before recursing
    /// into the child's own subtree.
    /// </summary>
    /// <param name="context">The Cairo context shared across the whole frame.</param>
    /// <param name="contentBounds">
    /// The parent's content area (already inset by the parent's padding).
    /// Relative children are stacked inside this area; absolute children are pinned to it.
    /// </param>
    /// <param name="direction">
    /// The stacking direction declared by the parent (<see cref="GuiComponentLayoutParameters.Direction"/>).
    /// </param>
    internal void Render(Context context, GuiComponentBounds contentBounds, GuiDirection direction = GuiDirection.Vertical)
    {
        double cursorX = contentBounds.X;
        double cursorY = contentBounds.Y;
        RenderInto(context, contentBounds, direction, ref cursorX, ref cursorY);
    }

    internal void Paint(Context context)
    {
        PaintInto(context);
    }

    /// <summary>
    /// Render core that operates on an externally-managed cursor. Used directly by
    /// <see cref="Render"/> and recursively for layout-transparent wrappers (slots whose
    /// instance does not implement <see cref="IGuiComponent"/>): the wrapper's child
    /// builder calls back into this method with the <i>parent</i>'s
    /// <paramref name="contentBounds"/> / <paramref name="direction"/> / cursor refs, so
    /// the wrapper's slots flow at the parent's level without the wrapper itself
    /// consuming any space.
    /// </summary>
    private GuiComponentBounds? RenderInto(
        Context context,
        GuiComponentBounds contentBounds,
        GuiDirection direction,
        ref double cursorX,
        ref double cursorY)
    {
        GuiComponentBounds? extent = null;

        foreach (var slot in _renderOrder)
        {
            // Layout-transparent wrappers — slots whose instance is only IGuiNode and
            // does not also implement IGuiComponent — inline their child builder at this
            // level. They contribute no LayoutParameters; cursor advancement is driven
            // entirely by the inner children. After children are placed, the wrapper's
            // Render/Overlay are still called once with bounds spanning the cursor delta
            // along the flow axis — so e.g. GuiTooltip can register its hover region
            // against the union extent.
            if (slot.Instance is not IGuiComponent layoutComponent)
            {
                double startX = cursorX;
                double startY = cursorY;

                GuiComponentBounds? childExtent = slot.ChildBuilder.RenderInto(context, contentBounds, direction, ref cursorX, ref cursorY);

                GuiComponentBounds wrapperBounds = childExtent ?? (direction == GuiDirection.Vertical
                    ? new GuiComponentBounds(contentBounds.X, startY, contentBounds.Width, cursorY - startY)
                    : new GuiComponentBounds(startX, contentBounds.Y, cursorX - startX, contentBounds.Height));

                slot.SetLayoutTransparentBounds(wrapperBounds);
                slot.Instance.Render(context, wrapperBounds);

                RegisterRegions(slot, wrapperBounds);

                slot.Instance.RenderOverlay(context, wrapperBounds);
                extent = Union(extent, wrapperBounds);
                continue;
            }

            var lp = layoutComponent.LayoutParameters;

            // Available space for measuring, after subtracting the slot's own margins AND
            // the space already consumed by previous siblings in the flow direction.
            // The cross-axis still sees the full content extent; only the flow axis shrinks.
            // Without this, a Fill-mode child in a vertical/horizontal stack would claim the
            // full container size and overflow the surface (visible as clipped strokes /
            // missing bottom borders).
            double consumedFlow = direction == GuiDirection.Vertical
                ? cursorY - contentBounds.Y
                : cursorX - contentBounds.X;
            double availW = direction == GuiDirection.Horizontal
                ? Math.Max(0, contentBounds.Width - consumedFlow - lp.Margin.Horizontal)
                : Math.Max(0, contentBounds.Width - lp.Margin.Horizontal);
            double availH = direction == GuiDirection.Vertical
                ? Math.Max(0, contentBounds.Height - consumedFlow - lp.Margin.Vertical)
                : Math.Max(0, contentBounds.Height - lp.Margin.Vertical);

            var (slotW, slotH) = GuiComponentLayout.ResolveAllocatedSize(layoutComponent, new GuiLayoutSize(availW, availH));

            // Determine origin. Absolute components are always pinned to the content-area origin;
            // relative components are placed at the current cursor and advance it.
            double slotX, slotY;
            if (lp.Positioning == GuiComponentPositioning.Absolute)
            {
                slotX = contentBounds.X + lp.Margin.Left;
                slotY = contentBounds.Y + lp.Margin.Top;
                // Absolute components honour both alignment axes — they sit anywhere inside
                // the parent's content area.
                slotX += AlignOffsetH(lp.HorizontalAlignment, availW - slotW);
                slotY += AlignOffsetV(lp.VerticalAlignment, availH - slotH);
                // Absolute components do not participate in flow — cursor unchanged.
            }
            else
            {
                slotX = cursorX + lp.Margin.Left;
                slotY = cursorY + lp.Margin.Top;

                if (direction == GuiDirection.Vertical)
                {
                    // Cross axis is X — apply horizontal alignment within the parent's
                    // cross-axis extent (content width minus this slot's horizontal margin).
                    double crossAvail = Math.Max(0, contentBounds.Width - lp.Margin.Horizontal);
                    slotX += AlignOffsetH(lp.HorizontalAlignment, crossAvail - slotW);
                    cursorY += lp.Margin.Top + slotH + lp.Margin.Bottom;
                }
                else
                {
                    // Cross axis is Y — apply vertical alignment within the parent's
                    // cross-axis extent (content height minus this slot's vertical margin).
                    double crossAvail = Math.Max(0, contentBounds.Height - lp.Margin.Vertical);
                    slotY += AlignOffsetV(lp.VerticalAlignment, crossAvail - slotH);
                    cursorX += lp.Margin.Left + slotW + lp.Margin.Right;
                }
            }

            var allocated = new GuiComponentBounds(slotX, slotY, slotW, slotH);
            extent = Union(extent, allocated);

            // Inset by this component's own padding to produce the content area for its children.
            // Clamp width/height at zero — padding can exceed the slot's allocated size when
            // an explicit width/height is smaller than horizontal/vertical padding.
            var childContent = new GuiComponentBounds(
                allocated.X + lp.Padding.Left,
                allocated.Y + lp.Padding.Top,
                Math.Max(0, allocated.Width - lp.Padding.Horizontal),
                Math.Max(0, allocated.Height - lp.Padding.Vertical)
            );

            slot.SetComponentBounds(allocated);
            layoutComponent.Render(context, allocated);
            RegisterRegions(slot, allocated);

            // Branch for scrollable containers: clip rendering to the viewport, translate
            // children by the current scroll offset, and emit scrollbar visuals + interactive
            // regions. Falls through to the regular path when no axis is effectively active
            // (e.g. user enabled Scroll but both dimensions are FitContent).
            if (slot.Instance is GuiContainer scrollContainer
                && TryRenderScrollableChildren(context, scrollContainer, slot, allocated, childContent, lp))
            {
                layoutComponent.RenderOverlay(context, allocated);
                continue;
            }

            // Non-scrollable path: inset (when enabled) covers the full allocated bounds.
            // Drawn after Render (background colour) and before children so it sits behind
            // them like any other background.
            (slot.Instance as GuiContainer)?.DrawInsetBackground(context, allocated);

            slot.ChildBuilder.Render(context, childContent, lp.Direction);
            layoutComponent.RenderOverlay(context, allocated);
        }

        return extent;
    }

    private void PaintInto(Context context)
    {
        foreach (var slot in _renderOrder)
        {
            if (!slot.HasArrangedBounds)
            {
                continue;
            }

            if (slot.Instance is not IGuiComponent layoutComponent)
            {
                slot.ChildBuilder.PaintInto(context);
                slot.Instance.Render(context, slot.Bounds);
                slot.Instance.RenderOverlay(context, slot.Bounds);
                continue;
            }

            layoutComponent.Render(context, slot.Bounds);

            if (slot.IsScrollable && slot.Instance is GuiContainer scrollContainer)
            {
                scrollContainer.DrawInsetBackground(context, scrollContainer.GetScrollInsetBounds());
                context.Save();
                context.Rectangle(
                    slot.ScrollClipBounds.X,
                    slot.ScrollClipBounds.Y,
                    slot.ScrollClipBounds.Width,
                    slot.ScrollClipBounds.Height);
                context.Clip();
                slot.ChildBuilder.PaintInto(context);
                context.Restore();
                scrollContainer.RenderScrollbars(context);
                layoutComponent.RenderOverlay(context, slot.Bounds);
                continue;
            }

            (slot.Instance as GuiContainer)?.DrawInsetBackground(context, slot.Bounds);
            slot.ChildBuilder.PaintInto(context);
            layoutComponent.RenderOverlay(context, slot.Bounds);
        }
    }

    /// <summary>
    /// Scrollable-container child render path. Returns false when no scroll axis is
    /// effectively active so the caller can fall back to the default child render.
    /// On success: measures inner content, decides scrollbar visibility, clips/translates
    /// the child combined arrange/paint walk, draws scrollbars and registers their interactive regions
    /// plus a wheel target for the viewport.
    /// </summary>
    private bool TryRenderScrollableChildren(
        Context context,
        GuiContainer container,
        ComponentSlot slot,
        GuiComponentBounds allocated,
        GuiComponentBounds childContent,
        GuiComponentLayoutParameters lp)
    {
        // Effective axes: user-declared Scroll mask minus axes whose mode is FitContent
        // (per spec — fit-to-content has no overflow). Recomputed each frame so toggling
        // size mode at runtime takes effect immediately.
        GuiScrollDirection eff = container.Scroll;
        if (lp.WidthMode == GuiSizeMode.FitContent)
        {
            eff &= ~GuiScrollDirection.Horizontal;
        }

        if (lp.HeightMode == GuiSizeMode.FitContent)
        {
            eff &= ~GuiScrollDirection.Vertical;
        }

        container.EffectiveScroll = eff;
        if (eff == GuiScrollDirection.None)
        {
            return false;
        }

        // Measure children at unbounded space on scroll-enabled axes so that Fill children
        // report their true content size rather than collapsing to the viewport. FitContent
        // children return their natural sizes as before. The PositiveInfinity sentinel
        // propagates through component-owned measurement and triggers a FitContent fallback
        // in ResolveAllocatedSize for any Fill-mode component on an unbounded axis.
        double measureAvailW = (eff & GuiScrollDirection.Horizontal) != 0 ? double.PositiveInfinity : childContent.Width;
        double measureAvailH = (eff & GuiScrollDirection.Vertical) != 0 ? double.PositiveInfinity : childContent.Height;
        var measured = container.Measure(new GuiLayoutSize(measureAvailW, measureAvailH));

        // Determine scrollbar visibility. An axis-scrollbar shows when:
        //   (axis ∈ Scrollbar) AND (axis ∈ effective Scroll) AND
        //   (content overflows  OR  axis ∈ AlwaysShowScrollbar).
        const double sbThickness = GuiContainer.ScrollbarThickness;
        const double sbGap = GuiContainer.ScrollbarGap;
        bool wantV = (eff & GuiScrollDirection.Vertical) != 0 && (container.Scrollbar & GuiScrollDirection.Vertical) != 0;
        bool wantH = (eff & GuiScrollDirection.Horizontal) != 0 && (container.Scrollbar & GuiScrollDirection.Horizontal) != 0;

        bool overflowV = measured.Height > childContent.Height + 0.5;
        bool overflowH = measured.Width > childContent.Width + 0.5;
        bool forceV = (container.AlwaysShowScrollbar & GuiScrollDirection.Vertical) != 0;
        bool forceH = (container.AlwaysShowScrollbar & GuiScrollDirection.Horizontal) != 0;

        bool showV = wantV && (overflowV || forceV);
        bool showH = wantH && (overflowH || forceH);

        // Reserve gutter space + gap along the cross axis when a scrollbar is visible.
        // Doing so can shrink the cross-axis viewport enough to make the other axis
        // overflow — handle that by re-evaluating each axis's overflow once.
        double vReserve = sbThickness + sbGap;
        double hReserve = sbThickness + sbGap;
        double vpW = childContent.Width - (showV ? vReserve : 0);
        double vpH = childContent.Height - (showH ? hReserve : 0);
        if (vpW < 0)
        {
            vpW = 0;
        }

        if (vpH < 0)
        {
            vpH = 0;
        }

        if (wantV && !showV && measured.Height > vpH + 0.5)
        {
            showV = true;
        }

        if (wantH && !showH && measured.Width > vpW + 0.5)
        {
            showH = true;
        }
        // Recompute viewport dimensions if visibility flipped.
        vpW = childContent.Width - (showV ? vReserve : 0);
        vpH = childContent.Height - (showH ? hReserve : 0);
        if (vpW < 0)
        {
            vpW = 0;
        }

        if (vpH < 0)
        {
            vpH = 0;
        }

        // Push allocated + viewport + content sizes into the container so it can clamp the
        // scroll offset before we read it back to translate children, and so scrollbar
        // tracks anchor against the container's allocated edge (not the viewport).
        container.UpdateScrollLayout(
            allocated.X, allocated.Y, allocated.Width, allocated.Height,
            childContent.X, childContent.Y, vpW, vpH,
            measured.Width, measured.Height,
            showV, showH, sbThickness);

        // Inset background — fixed, not scrolled. Covers allocated bounds minus any
        // scrollbar gutters, so the scrollbar sits flush against the container edge with
        // a small gap between the inset's inner viewport and the scrollbar handle.
        container.DrawInsetBackground(context, container.GetScrollInsetBounds());

        // Translate child content area by (-scrollX, -scrollY); expand size along the
        // active scroll axes to the measured content extent so children flow naturally
        // and Fill children along the cross axis still see the viewport size.
        double childW = (eff & GuiScrollDirection.Horizontal) != 0 ? Math.Max(vpW, measured.Width) : vpW;
        double childH = (eff & GuiScrollDirection.Vertical) != 0 ? Math.Max(vpH, measured.Height) : vpH;
        var scrolledChildBounds = new GuiComponentBounds(
            childContent.X - container.ScrollX,
            childContent.Y - container.ScrollY,
            childW, childH);

        // Clip drawing to the viewport so overflowing children do not bleed into adjacent
        // siblings or scrollbar gutters. Cairo Save/Restore brackets the entire child walk
        // (including any nested clips children may set up). Interactive regions are still
        // registered at their translated positions — clipping affects pixels, not hit testing.
        // When the container has an inset, shrink the clip inward by the emboss depth so
        // scrolled content cannot paint over the emboss ring at the viewport edges.
        context.Save();
        double clipInset = container.ScrollViewportClipInset;
        slot.SetScrollableBounds(
            new GuiComponentBounds(
                childContent.X + clipInset,
                childContent.Y + clipInset,
                Math.Max(0, vpW - 2 * clipInset),
                Math.Max(0, vpH - 2 * clipInset)));
        context.Rectangle(
            slot.ScrollClipBounds.X,
            slot.ScrollClipBounds.Y,
            slot.ScrollClipBounds.Width,
            slot.ScrollClipBounds.Height);
        context.Clip();

        slot.ChildBuilder.Render(context, scrolledChildBounds, lp.Direction);

        context.Restore();

        // Wheel target: the viewport. Registered first so any nested scrollable child
        // pushes its own region on top and wins the reverse hit-test. Wheel-only regions
        // have no click handlers, so HitTest skips them and they never consume click events.
        _renderer.AddInteractiveRegion(new InteractiveRegion(
            new GuiComponentBounds(childContent.X, childContent.Y, vpW, vpH),
            container,
            onMouseDown: default,
            onMouseUp: default,
            onMouseClick: default,
            onMouseMove: default,
            onMouseEnter: default,
            onMouseLeave: default,
            onMouseWheel: container.OnScrollWheel));

        // Scrollbar visuals + interactive regions. Drawn outside the clip so they sit
        // on top of children. Each axis registers its own region with stable per-container
        // tokens so mouse-capture matching survives layout changes during a drag.
        container.RenderScrollbars(context);

        if (showV)
        {
            _renderer.AddInteractiveRegion(new InteractiveRegion(
                container.GetVScrollbarTrackBounds(),
                container.VScrollbarToken,
                container.OnVScrollbarDown,
                container.OnVScrollbarUp,
                default,
                container.OnVScrollbarMove,
                default,
                default));
        }
        if (showH)
        {
            _renderer.AddInteractiveRegion(new InteractiveRegion(
                container.GetHScrollbarTrackBounds(),
                container.HScrollbarToken,
                container.OnHScrollbarDown,
                container.OnHScrollbarUp,
                default,
                container.OnHScrollbarMove,
                default,
                default));
        }
        return true;
    }

    private void RegisterRegions(ComponentSlot slot, GuiComponentBounds bounds)
    {
        if (slot.Instance is IGuiResizable resizable && resizable.SupportedResizeEdges != GuiResizeEdge.None)
        {
            _renderer.AddResizeRegion(new ResizeRegion(bounds, slot.Instance, resizable));
        }

        if (slot.HasMouseHandlers)
        {
            _renderer.AddInteractiveRegion(new InteractiveRegion(
                bounds,
                slot.Instance,
                slot.OnMouseDown,
                slot.OnMouseUp,
                slot.OnMouseClick,
                slot.OnMouseMove,
                slot.OnMouseEnter,
                slot.OnMouseLeave));
        }

        if (slot.HasKeyboardRegionHandlers)
        {
            _renderer.AddKeyboardRegion(new KeyboardRegion(
                slot.Instance,
                slot.OnKeyDown,
                slot.OnKeyUp,
                slot.OnKeyPress,
                slot.OnFocusChanged));
        }
    }

    /// <summary>
    /// Translates a <see cref="GuiHorizontalAlignment"/> into a pixel offset, given the
    /// slack <paramref name="extra"/> (available cross-axis extent minus slot width).
    /// Negative or zero slack collapses to zero — alignment never pulls a slot outside
    /// its allotted space.
    /// </summary>
    private static double AlignOffsetH(GuiHorizontalAlignment alignment, double extra)
    {
        if (extra <= 0)
        {
            return 0;
        }

        return alignment switch
        {
            GuiHorizontalAlignment.Center => extra * 0.5,
            GuiHorizontalAlignment.Right => extra,
            _ => 0,
        };
    }

    /// <summary>
    /// Translates a <see cref="GuiVerticalAlignment"/> into a pixel offset, given the
    /// slack <paramref name="extra"/> (available cross-axis extent minus slot height).
    /// Negative or zero slack collapses to zero — alignment never pulls a slot outside
    /// its allotted space.
    /// </summary>
    private static double AlignOffsetV(GuiVerticalAlignment alignment, double extra)
    {
        if (extra <= 0)
        {
            return 0;
        }

        return alignment switch
        {
            GuiVerticalAlignment.Center => extra * 0.5,
            GuiVerticalAlignment.Bottom => extra,
            _ => 0,
        };
    }

    private static GuiComponentBounds Union(GuiComponentBounds first, GuiComponentBounds second)
    {
        double left = Math.Min(first.X, second.X);
        double top = Math.Min(first.Y, second.Y);
        double right = Math.Max(first.Right, second.Right);
        double bottom = Math.Max(first.Bottom, second.Bottom);
        return new GuiComponentBounds(left, top, right - left, bottom - top);
    }

    private static GuiComponentBounds? Union(GuiComponentBounds? extent, GuiComponentBounds bounds)
        => extent is null ? bounds : Union(extent.Value, bounds);

    private struct SlotCallbacks
    {
        public GuiCallback<GuiMouseEventArgs> OnMouseDown;
        public GuiCallback<GuiMouseEventArgs> OnMouseUp;
        public GuiCallback<GuiMouseEventArgs> OnMouseClick;
        public GuiCallback<GuiMouseEventArgs> OnMouseMove;
        public GuiCallback<GuiMouseEventArgs> OnMouseEnter;
        public GuiCallback<GuiMouseEventArgs> OnMouseLeave;

        public GuiCallback<GuiKeyEventArgs> OnKeyDown;
        public GuiCallback<GuiKeyEventArgs> OnKeyUp;
        public GuiCallback<GuiKeyEventArgs> OnKeyPress;
        public GuiCallback<bool> OnFocusChanged;

        public void AddMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback)
        {
            switch (kind)
            {
                case GuiMouseEventKind.Down:
                    OnMouseDown = GuiCallback<GuiMouseEventArgs>.Combine(OnMouseDown, callback);
                    break;
                case GuiMouseEventKind.Up:
                    OnMouseUp = GuiCallback<GuiMouseEventArgs>.Combine(OnMouseUp, callback);
                    break;
                case GuiMouseEventKind.Click:
                    OnMouseClick = GuiCallback<GuiMouseEventArgs>.Combine(OnMouseClick, callback);
                    break;
                case GuiMouseEventKind.Move:
                    OnMouseMove = GuiCallback<GuiMouseEventArgs>.Combine(OnMouseMove, callback);
                    break;
                case GuiMouseEventKind.Enter:
                    OnMouseEnter = GuiCallback<GuiMouseEventArgs>.Combine(OnMouseEnter, callback);
                    break;
                case GuiMouseEventKind.Leave:
                    OnMouseLeave = GuiCallback<GuiMouseEventArgs>.Combine(OnMouseLeave, callback);
                    break;
            }
        }

        public void AddKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback)
        {
            switch (kind)
            {
                case GuiKeyEventKind.Down:
                    OnKeyDown = GuiCallback<GuiKeyEventArgs>.Combine(OnKeyDown, callback);
                    break;
                case GuiKeyEventKind.Up:
                    OnKeyUp = GuiCallback<GuiKeyEventArgs>.Combine(OnKeyUp, callback);
                    break;
                case GuiKeyEventKind.Press:
                    OnKeyPress = GuiCallback<GuiKeyEventArgs>.Combine(OnKeyPress, callback);
                    break;
            }
        }

        public void AddFocusChangedHandler(GuiCallback<bool> callback)
        {
            OnFocusChanged = GuiCallback<bool>.Combine(OnFocusChanged, callback);
        }

        public static SlotCallbacks Combine(SlotCallbacks ownCallbacks, SlotCallbacks externalCallbacks)
        {
            return new SlotCallbacks
            {
                OnMouseDown = GuiCallback<GuiMouseEventArgs>.Combine(ownCallbacks.OnMouseDown, externalCallbacks.OnMouseDown),
                OnMouseUp = GuiCallback<GuiMouseEventArgs>.Combine(ownCallbacks.OnMouseUp, externalCallbacks.OnMouseUp),
                OnMouseClick = GuiCallback<GuiMouseEventArgs>.Combine(ownCallbacks.OnMouseClick, externalCallbacks.OnMouseClick),
                OnMouseMove = GuiCallback<GuiMouseEventArgs>.Combine(ownCallbacks.OnMouseMove, externalCallbacks.OnMouseMove),
                OnMouseEnter = GuiCallback<GuiMouseEventArgs>.Combine(ownCallbacks.OnMouseEnter, externalCallbacks.OnMouseEnter),
                OnMouseLeave = GuiCallback<GuiMouseEventArgs>.Combine(ownCallbacks.OnMouseLeave, externalCallbacks.OnMouseLeave),
                OnKeyDown = GuiCallback<GuiKeyEventArgs>.Combine(ownCallbacks.OnKeyDown, externalCallbacks.OnKeyDown),
                OnKeyUp = GuiCallback<GuiKeyEventArgs>.Combine(ownCallbacks.OnKeyUp, externalCallbacks.OnKeyUp),
                OnKeyPress = GuiCallback<GuiKeyEventArgs>.Combine(ownCallbacks.OnKeyPress, externalCallbacks.OnKeyPress),
                OnFocusChanged = GuiCallback<bool>.Combine(ownCallbacks.OnFocusChanged, externalCallbacks.OnFocusChanged),
            };
        }

        public void ApplyTo(ComponentSlot slot)
        {
            slot.OnMouseDown = OnMouseDown;
            slot.OnMouseUp = OnMouseUp;
            slot.OnMouseClick = OnMouseClick;
            slot.OnMouseMove = OnMouseMove;
            slot.OnMouseEnter = OnMouseEnter;
            slot.OnMouseLeave = OnMouseLeave;
            slot.OnKeyDown = OnKeyDown;
            slot.OnKeyUp = OnKeyUp;
            slot.OnKeyPress = OnKeyPress;
            slot.OnFocusChanged = OnFocusChanged;
        }
    }

    private sealed class RenderTreeFrame<T> : RenderTreeFrame, IGuiComponentBuilder<T>
        where T : IGuiNode, new()
    {
        private readonly GuiRenderTreeBuilder _renderTreeBuilder;

        public override Type ComponentType => typeof(T);

        private Action<T>? _configure;
        private SlotCallbacks _ownCallbacks;
        private SlotCallbacks _externalCallbacks;

        public RenderTreeFrame(GuiRenderTreeBuilder renderTreeBuilder, int key)
        {
            _renderTreeBuilder = renderTreeBuilder;
            Key = key;
        }

        IGuiComponentBuilder<T> IGuiComponentBuilder<T>.AddConfigurationAction(Action<T> action)
        {
            _configure += action;
            return this;
        }

        IGuiSlotBuilder IGuiSlotBuilder.AddLayoutConfiguration(Action<GuiComponentLayoutParameters> configure)
        {
            _configure += node =>
            {
                if (node is not IGuiComponent component)
                {
                    throw new InvalidOperationException(
                        $"Layout parameters cannot be applied to layout-transparent node {typeof(T).Name}.");
                }

                configure(component.LayoutParameters);
            };
            return this;
        }

        IGuiSlotBuilder IGuiSlotBuilder.AddMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback)
        {
            _externalCallbacks.AddMouseHandler(kind, callback);
            return this;
        }

        IGuiSlotBuilder IGuiSlotBuilder.AddKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback)
        {
            _externalCallbacks.AddKeyHandler(kind, callback);
            return this;
        }

        IGuiSlotBuilder IGuiSlotBuilder.AddFocusChangedHandler(GuiCallback<bool> callback)
        {
            _externalCallbacks.AddFocusChangedHandler(callback);
            return this;
        }

        public override void Reset()
        {
            _configure = null;
            // Mouse + keyboard handlers are also per-pass: each blueprint pass re-registers
            // them via own-slot configuration and the On* extensions, mirroring how Configure
            // actions are re-registered.
            _ownCallbacks = default;
            _externalCallbacks = default;
        }

        public override IGuiNode CreateInstance() => new T();

        public override void ApplyConfiguration(IGuiNode instance)
        {
            if (instance is T typed)
            {
                _configure?.Invoke(typed);
            }
        }

        public override void ApplySlotConfiguration(IGuiNode instance)
        {
            _ownCallbacks = default;
            if (instance is GuiNode node)
            {
                node.ApplySlotConfiguration(new SlotBuilder(this, instance));
            }
        }

        public override void ComposeSlotConfiguration(ComponentSlot slot)
        {
            SlotCallbacks.Combine(_ownCallbacks, _externalCallbacks).ApplyTo(slot);
        }

        IGuiComponentBuilder<TNewComponent> IGuiRenderTreeBuilder.AddComponent<TNewComponent>(int key)
            => _renderTreeBuilder.AddComponent<TNewComponent>(key);

        void IGuiRenderTreeBuilder.PushCascadeScope<TValue>(TValue value, string? name, GuiRenderFragment content)
            => _renderTreeBuilder.PushCascadeScope(value, name, content);

        private sealed class SlotBuilder(RenderTreeFrame<T> frame, IGuiNode instance) : IGuiSlotBuilder
        {
            IGuiSlotBuilder IGuiSlotBuilder.AddLayoutConfiguration(Action<GuiComponentLayoutParameters> configure)
            {
                if (instance is not IGuiComponent component)
                {
                    throw new InvalidOperationException(
                        $"Layout parameters cannot be applied to layout-transparent node {typeof(T).Name}.");
                }

                configure(component.LayoutParameters);
                return this;
            }

            IGuiSlotBuilder IGuiSlotBuilder.AddMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback)
            {
                frame._ownCallbacks.AddMouseHandler(kind, callback);
                return this;
            }

            IGuiSlotBuilder IGuiSlotBuilder.AddKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback)
            {
                frame._ownCallbacks.AddKeyHandler(kind, callback);
                return this;
            }

            IGuiSlotBuilder IGuiSlotBuilder.AddFocusChangedHandler(GuiCallback<bool> callback)
            {
                frame._ownCallbacks.AddFocusChangedHandler(callback);
                return this;
            }
        }
    }

    internal abstract class RenderTreeFrame
    {
        public abstract Type ComponentType { get; }
        public int Key { get; protected init; }

        // Snapshotted during the blueprint phase in AddComponent. Records the cascade chain
        // that was active at this slot's declaration site — including any PushCascadeScope
        // wrappers around the AddComponent call. Read during the patch phase to initialise
        // the child builder's CascadeChain before recursing into the slot's subtree.
        public CascadingValueChain? CascadeChain;

        public abstract IGuiNode CreateInstance();
        /// <summary>Clears per-pass state (registered configuration actions and mouse handlers).
        /// Called when an existing frame is reused at the start of a new blueprint pass.</summary>
        public abstract void Reset();
        public abstract void ApplySlotConfiguration(IGuiNode instance);
        public abstract void ApplyConfiguration(IGuiNode instance);
        public abstract void ComposeSlotConfiguration(ComponentSlot slot);
    }

    private readonly record struct ComponentSlotKey(Type ComponentType, int Key);

}
