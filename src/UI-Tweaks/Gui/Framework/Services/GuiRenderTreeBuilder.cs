using Cairo;
using System;
using System.Collections.Generic;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Executes <see cref="GuiRenderFragment"/>s, reconciles the resulting
/// <see cref="RenderTreeFrame"/> instructions against previously-known state,
/// and manages the lifetimes of child <see cref="IGuiNode"/> instances.
/// </summary>
internal sealed class GuiRenderTreeBuilder : IGuiRenderTreeBuilder, IDisposable
{
    private readonly IGuiComponentTreeRenderer _renderer;

    // Frame buffer: filled during the blueprint phase, cleared at the start of each Run().
    private readonly List<RenderTreeFrame> _frames = [];

    // Persistent storage for keyed slots. Key is a value-type struct — no frame allocation
    // needed just for identity.
    private readonly Dictionary<ComponentSlotKey, ComponentSlot> _keyedSlots = [];

    // Ordered list of active slots, rebuilt each Run() to match the current frame order.
    // Used by the render walk to iterate children in declaration order.
    private readonly List<ComponentSlot> _renderOrder = [];

    // Reused scratch buffers — avoid allocating inside the hot path.
    private readonly HashSet<ComponentSlotKey> _seenKeys = [];
    private readonly List<ComponentSlotKey> _staleKeys = [];

    // Cascading-value chain visible to slots declared **inside** this builder. Updated by
    // the parent builder during reconcile (see Run()): set to the parent's chain when the
    // owning component is not a provider, or to a new chain link when it is. The root
    // dialog builder leaves this null. Read live by descendant RenderHandles via their
    // parent-builder reference, so cascade updates from any ancestor reconcile propagate
    // without touching descendant handles.
    internal CascadingValueChain? CascadeChain;

    internal GuiRenderTreeBuilder(IGuiComponentTreeRenderer renderer)
    {
        _renderer = renderer;
    }

    public IGuiComponentBuilder<T> AddComponent<T>(int key)
        where T : IGuiNode, new()
    {
        var slotKey = new ComponentSlotKey(typeof(T), key);

        // Detect duplicate (Type, key) declarations within the current blueprint phase.
        // _seenKeys is cleared at the start of every Run() and populated here, so a key
        // that's already present means the same slot was declared twice among siblings.
        if (!_seenKeys.Add(slotKey))
            throw new InvalidOperationException(
                $"Duplicate component key {key} for {typeof(T).Name} within the same render tree level. Each (Type, key) pair must be unique among siblings.");

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
            else if (slot!.Instance is GuiComponent comp)
            {
                // Reset layout parameters to canonical defaults before applying the new
                // pass's config actions — blueprints are declarative (full state), not
                // deltas. Without this, stale LP from a previous view (e.g. a list column
                // that becomes a setting row at the same key) would persist across reuses.
                comp.ResetLayoutParameters();
            }

            frame.ApplyConfiguration(slot!.Instance);
            if (isNew) slot.Instance.OnInitialized();
            slot.Instance.OnParametersSet();
            // Propagate the cascade chain from the declaration site to the child builder.
            // frame.CascadeChain was snapshotted in AddComponent during the blueprint phase
            // at the point where this slot was declared — it captures any PushCascadeScope
            // wrappers that were lexically active around the AddComponent call. The child
            // builder inherits this chain so its own slots (and their descendants) can
            // look up those values.
            slot.ChildBuilder.CascadeChain = frame.CascadeChain;
            // Cancel any separately scheduled rebuild for this child's fragment — we are
            // about to rebuild its subtree right now, making the pending entry redundant.
            _renderer.Cancel(slot.Instance.RenderFragment);
            slot.ChildBuilder.Run(slot.Instance.RenderFragment);
            _renderOrder.Add(slot);
        }

        _staleKeys.Clear();
        foreach (var key in _keyedSlots.Keys)
        {
            if (!_seenKeys.Contains(key))
                _staleKeys.Add(key);
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
            DisposeSlot(slot);
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
        // The handle's `parentBuilder` is `this` — what this slot **consumes** as a
        // cascade scope is whatever was visible to its parent (i.e. this builder's chain).
        // Storing the builder reference (not the chain itself) means lookups always see
        // the live chain as ancestors update it on subsequent reconciles.
        instance.Attach(new RenderHandle(_renderer, childBuilder, this), _renderer.ClientApi);
        return new ComponentSlot(instance, childBuilder, frame);
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

    /// <summary>
    /// Render core that operates on an externally-managed cursor. Used directly by
    /// <see cref="Render"/> and recursively for layout-transparent wrappers (slots whose
    /// instance does not implement <see cref="IGuiComponent"/>): the wrapper's child
    /// builder calls back into this method with the <i>parent</i>'s
    /// <paramref name="contentBounds"/> / <paramref name="direction"/> / cursor refs, so
    /// the wrapper's slots flow at the parent's level without the wrapper itself
    /// consuming any space.
    /// </summary>
    private void RenderInto(
        Context context,
        GuiComponentBounds contentBounds,
        GuiDirection direction,
        ref double cursorX,
        ref double cursorY)
    {
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

                slot.ChildBuilder.RenderInto(context, contentBounds, direction, ref cursorX, ref cursorY);

                GuiComponentBounds wrapperBounds = direction == GuiDirection.Vertical
                    ? new GuiComponentBounds(contentBounds.X, startY, contentBounds.Width, cursorY - startY)
                    : new GuiComponentBounds(startX, contentBounds.Y, cursorX - startX, contentBounds.Height);

                slot.Instance.Render(context, wrapperBounds);

                if (slot.Frame.HasMouseHandlers)
                {
                    _renderer.AddInteractiveRegion(new InteractiveRegion(
                        wrapperBounds,
                        slot.Instance,
                        slot.Frame.OnMouseDown,
                        slot.Frame.OnMouseUp,
                        slot.Frame.OnMouseClick,
                        slot.Frame.OnMouseMove,
                        slot.Frame.OnMouseEnter,
                        slot.Frame.OnMouseLeave));
                }

                if (slot.Frame.HasKeyHandlers)
                {
                    _renderer.AddKeyboardRegion(new KeyboardRegion(
                        slot.Instance,
                        slot.Frame.OnKeyDown,
                        slot.Frame.OnKeyUp,
                        slot.Frame.OnKeyPress));
                }

                slot.Instance.RenderOverlay(context, wrapperBounds);
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

            var (slotW, slotH) = ResolveSize(slot, availW, availH);

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
            layoutComponent.Render(context, allocated);

            // Record interactive region after bounds are known. We hand this to the renderer
            // (rather than maintaining a per-builder list) so all regions across the dialog's
            // entire tree end up in a single ordered list — hit-testing walks it in reverse.
            if (slot.Frame.HasMouseHandlers)
            {
                _renderer.AddInteractiveRegion(new InteractiveRegion(
                    allocated,
                    slot.Instance,
                    slot.Frame.OnMouseDown,
                    slot.Frame.OnMouseUp,
                    slot.Frame.OnMouseClick,
                    slot.Frame.OnMouseMove,
                    slot.Frame.OnMouseEnter,
                    slot.Frame.OnMouseLeave));
            }

            if (slot.Frame.HasKeyHandlers)
            {
                _renderer.AddKeyboardRegion(new KeyboardRegion(
                    slot.Instance,
                    slot.Frame.OnKeyDown,
                    slot.Frame.OnKeyUp,
                    slot.Frame.OnKeyPress));
            }

            // Inset by this component's own padding to produce the content area for its children.
            // Clamp width/height at zero — padding can exceed the slot's allocated size when
            // an explicit width/height is smaller than horizontal/vertical padding.
            var childContent = new GuiComponentBounds(
                allocated.X + lp.Padding.Left,
                allocated.Y + lp.Padding.Top,
                Math.Max(0, allocated.Width - lp.Padding.Horizontal),
                Math.Max(0, allocated.Height - lp.Padding.Vertical)
            );

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
    }

    /// <summary>
    /// Scrollable-container child render path. Returns false when no scroll axis is
    /// effectively active so the caller can fall back to the default child render.
    /// On success: measures inner content, decides scrollbar visibility, clips/translates
    /// the child render walk, draws scrollbars and registers their interactive regions
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
        GuiScroll eff = container.Scroll;
        if (lp.WidthMode == GuiSizeMode.FitContent) eff &= ~GuiScroll.Horizontal;
        if (lp.HeightMode == GuiSizeMode.FitContent) eff &= ~GuiScroll.Vertical;
        container.EffectiveScroll = eff;
        if (eff == GuiScroll.None) return false;

        // Measure children at unbounded space on scroll-enabled axes so that Fill children
        // report their true content size rather than collapsing to the viewport. FitContent
        // children return their natural sizes as before. The PositiveInfinity sentinel
        // propagates through AccumulateMeasure and triggers a FitContent fallback in
        // ResolveSize for any Fill-mode component on an unbounded axis.
        double measureAvailW = (eff & GuiScroll.Horizontal) != 0 ? double.PositiveInfinity : childContent.Width;
        double measureAvailH = (eff & GuiScroll.Vertical) != 0 ? double.PositiveInfinity : childContent.Height;
        var measured = slot.ChildBuilder.MeasureChildren(measureAvailW, measureAvailH, lp.Direction);

        // Determine scrollbar visibility. An axis-scrollbar shows when:
        //   (axis ∈ Scrollbar) AND (axis ∈ effective Scroll) AND
        //   (content overflows  OR  axis ∈ AlwaysShowScrollbar).
        const double sbThickness = GuiContainer.ScrollbarThickness;
        const double sbGap = GuiContainer.ScrollbarGap;
        bool wantV = (eff & GuiScroll.Vertical) != 0 && (container.Scrollbar & GuiScroll.Vertical) != 0;
        bool wantH = (eff & GuiScroll.Horizontal) != 0 && (container.Scrollbar & GuiScroll.Horizontal) != 0;

        bool overflowV = measured.Height > childContent.Height + 0.5;
        bool overflowH = measured.Width > childContent.Width + 0.5;
        bool forceV = (container.AlwaysShowScrollbar & GuiScroll.Vertical) != 0;
        bool forceH = (container.AlwaysShowScrollbar & GuiScroll.Horizontal) != 0;

        bool showV = wantV && (overflowV || forceV);
        bool showH = wantH && (overflowH || forceH);

        // Reserve gutter space + gap along the cross axis when a scrollbar is visible.
        // Doing so can shrink the cross-axis viewport enough to make the other axis
        // overflow — handle that by re-evaluating each axis's overflow once.
        double vReserve = sbThickness + sbGap;
        double hReserve = sbThickness + sbGap;
        double vpW = childContent.Width - (showV ? vReserve : 0);
        double vpH = childContent.Height - (showH ? hReserve : 0);
        if (vpW < 0) vpW = 0;
        if (vpH < 0) vpH = 0;

        if (wantV && !showV && measured.Height > vpH + 0.5) showV = true;
        if (wantH && !showH && measured.Width > vpW + 0.5) showH = true;
        // Recompute viewport dimensions if visibility flipped.
        vpW = childContent.Width - (showV ? vReserve : 0);
        vpH = childContent.Height - (showH ? hReserve : 0);
        if (vpW < 0) vpW = 0;
        if (vpH < 0) vpH = 0;

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
        double childW = (eff & GuiScroll.Horizontal) != 0 ? Math.Max(vpW, measured.Width) : vpW;
        double childH = (eff & GuiScroll.Vertical) != 0 ? Math.Max(vpH, measured.Height) : vpH;
        var scrolledChildBounds = new GuiComponentBounds(
            childContent.X - container.ScrollX,
            childContent.Y - container.ScrollY,
            childW, childH);

        // Clip drawing to the viewport so overflowing children do not bleed into adjacent
        // siblings or scrollbar gutters. Cairo Save/Restore brackets the entire child walk
        // (including any nested clips children may set up). Interactive regions are still
        // registered at their translated positions — clipping affects pixels, not hit testing.
        context.Save();
        context.Rectangle(childContent.X, childContent.Y, vpW, vpH);
        context.Clip();

        slot.ChildBuilder.Render(context, scrolledChildBounds, lp.Direction);

        context.Restore();

        // Wheel target: the viewport. Registered first so any nested scrollable child
        // pushes its own region on top and wins the reverse hit-test.
        _renderer.AddScrollRegion(new GuiComponentBounds(childContent.X, childContent.Y, vpW, vpH), container);

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

    /// <summary>
    /// Measures all relative children within the given available space and returns their combined
    /// extent: sum along the flow axis, max on the cross axis. Used by <see cref="ResolveSize"/>
    /// when a slot's dimension mode is <see cref="GuiSizeMode.FitContent"/>.
    /// </summary>
    internal GuiSize MeasureChildren(double availableWidth, double availableHeight, GuiDirection direction)
    {
        double totalW = 0, totalH = 0;
        AccumulateMeasure(availableWidth, availableHeight, direction, ref totalW, ref totalH);
        return new GuiSize(totalW, totalH);
    }

    /// <summary>
    /// Measure core that operates on externally-managed accumulators. Used directly by
    /// <see cref="MeasureChildren"/> and recursively for layout-transparent wrappers
    /// (slots whose instance does not implement <see cref="IGuiComponent"/>): the
    /// wrapper's child builder calls back into this method with the parent's
    /// accumulators, contributing to the parent's measured size as if the inner children
    /// were declared at the parent's level.
    /// </summary>
    private void AccumulateMeasure(
        double availableWidth,
        double availableHeight,
        GuiDirection direction,
        ref double totalW,
        ref double totalH)
    {
        foreach (var slot in _renderOrder)
        {
            // Transparent wrappers contribute their inner children directly to this
            // measurement, mirroring how RenderInto inlines them at the parent's flow.
            // The wrapper's own LayoutParameters are not consulted (they don't have any).
            if (slot.Instance is not IGuiComponent layoutComponent)
            {
                slot.ChildBuilder.AccumulateMeasure(availableWidth, availableHeight, direction, ref totalW, ref totalH);
                continue;
            }

            var lp = layoutComponent.LayoutParameters;
            if (lp.Positioning == GuiComponentPositioning.Absolute) continue;

            double childAvailW = Math.Max(0, availableWidth - lp.Margin.Horizontal);
            double childAvailH = Math.Max(0, availableHeight - lp.Margin.Vertical);

            var (slotW, slotH) = ResolveSize(slot, childAvailW, childAvailH);

            if (direction == GuiDirection.Vertical)
            {
                totalW = Math.Max(totalW, lp.Margin.Horizontal + slotW);
                totalH += lp.Margin.Vertical + slotH;
            }
            else
            {
                totalW += lp.Margin.Horizontal + slotW;
                totalH = Math.Max(totalH, lp.Margin.Vertical + slotH);
            }
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
        if (extra <= 0) return 0;
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
        if (extra <= 0) return 0;
        return alignment switch
        {
            GuiVerticalAlignment.Center => extra * 0.5,
            GuiVerticalAlignment.Bottom => extra,
            _ => 0,
        };
    }

    /// <summary>
    /// Resolves the final width and height for a slot given the available space.
    /// Only called for layout-participating slots (where the instance implements
    /// <see cref="IGuiComponent"/>); transparent wrappers never reach this path.
    /// Priority per dimension:
    /// <list type="number">
    ///   <item>Explicit <see cref="GuiComponentLayoutParameters.Width"/>/<see cref="GuiComponentLayoutParameters.Height"/> → use it.</item>
    ///   <item><see cref="GuiSizeMode.FitContent"/> → <c>max(MeasureChildren(), Measure())</c> + padding.
    ///     <see cref="IGuiComponent.Measure"/> acts as the leaf intrinsic-size hook; its default
    ///     returns <c>(0, 0)</c> so pure containers are unaffected.</item>
    ///   <item><see cref="GuiSizeMode.Fill"/> → <c>(availW, availH)</c> directly. <see cref="IGuiComponent.Measure"/> is not called.</item>
    /// </list>
    /// </summary>
    private GuiSize ResolveSize(ComponentSlot slot, double availW, double availH)
    {
        // Only invoked from the layout branch in RenderInto, which has already verified
        // the slot's instance implements IGuiComponent — this cast is therefore safe.
        var layoutComponent = (IGuiComponent)slot.Instance;
        var layoutParameters = layoutComponent.LayoutParameters;

        // Lazy on-demand so we never compute the same path twice.
        // Inner-available values are clamped at zero — padding larger than available space
        // would otherwise feed negative dimensions into Measure / MeasureChildren.
        GuiSize? instanceMeasured = null;
        GuiSize GetInstanceMeasured() =>
            instanceMeasured ??= layoutComponent.Measure(
                Math.Max(0, availW - layoutParameters.Padding.Horizontal),
                Math.Max(0, availH - layoutParameters.Padding.Vertical));

        GuiSize? childrenMeasured = null;
        GuiSize GetChildrenMeasured() =>
            childrenMeasured ??= slot.ChildBuilder.MeasureChildren(
                Math.Max(0, availW - layoutParameters.Padding.Horizontal),
                Math.Max(0, availH - layoutParameters.Padding.Vertical),
                layoutParameters.Direction);

        // When available space on an axis is PositiveInfinity the component is inside a
        // scrollable measure pass on that axis. Fill mode cannot fill an unbounded extent,
        // so it falls back to FitContent — measuring the component's own children to find
        // the true content size. Bounded axes (finite availW / availH) are unaffected.
        double w = layoutParameters.Width is not null ? layoutParameters.Width.Value
            : layoutParameters.WidthMode == GuiSizeMode.FitContent || double.IsPositiveInfinity(availW)
                ? Math.Max(GetChildrenMeasured().Width, GetInstanceMeasured().Width) + layoutParameters.Padding.Horizontal
            : availW; // Fill

        double h = layoutParameters.Height is not null ? layoutParameters.Height.Value
            : layoutParameters.HeightMode == GuiSizeMode.FitContent || double.IsPositiveInfinity(availH)
                ? Math.Max(GetChildrenMeasured().Height, GetInstanceMeasured().Height) + layoutParameters.Padding.Vertical
            : availH; // Fill

        return new GuiSize(w, h);
    }

    private sealed class ComponentSlot(IGuiNode instance, GuiRenderTreeBuilder childBuilder, RenderTreeFrame frame)
    {
        public readonly IGuiNode Instance = instance;
        public readonly GuiRenderTreeBuilder ChildBuilder = childBuilder;

        // The frame is stored here so AddComponent<T> can retrieve and reset it on subsequent
        // rebuilds rather than allocating a new instance. Safe to cast back to RenderTreeFrame<T>
        // since the slot key includes the type — the frame type always matches.
        public readonly RenderTreeFrame Frame = frame;
    }

    private sealed class RenderTreeFrame<T> : RenderTreeFrame, IGuiComponentBuilder<T>
        where T : IGuiNode, new()
    {
        private readonly GuiRenderTreeBuilder _renderTreeBuilder;

        public override Type ComponentType => typeof(T);

        private Action<T>? _configure;

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

        IGuiComponentBuilder<T> IGuiComponentBuilder<T>.SetMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback)
        {
            switch (kind)
            {
                case GuiMouseEventKind.Down: OnMouseDown = callback; break;
                case GuiMouseEventKind.Up: OnMouseUp = callback; break;
                case GuiMouseEventKind.Click: OnMouseClick = callback; break;
                case GuiMouseEventKind.Move: OnMouseMove = callback; break;
                case GuiMouseEventKind.Enter: OnMouseEnter = callback; break;
                case GuiMouseEventKind.Leave: OnMouseLeave = callback; break;
            }
            return this;
        }

        IGuiComponentBuilder<T> IGuiComponentBuilder<T>.SetKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback)
        {
            switch (kind)
            {
                case GuiKeyEventKind.Down: OnKeyDown = callback; break;
                case GuiKeyEventKind.Up: OnKeyUp = callback; break;
                case GuiKeyEventKind.Press: OnKeyPress = callback; break;
            }
            return this;
        }

        public override void Reset()
        {
            _configure = null;
            // Mouse + keyboard handlers are also per-pass: each blueprint pass re-registers
            // them via the On* extensions, mirroring how Configure actions are re-registered.
            OnMouseDown = default;
            OnMouseUp = default;
            OnMouseClick = default;
            OnMouseMove = default;
            OnMouseEnter = default;
            OnMouseLeave = default;
            OnKeyDown = default;
            OnKeyUp = default;
            OnKeyPress = default;
        }

        public override IGuiNode CreateInstance() => new T();

        public override void ApplyConfiguration(IGuiNode instance)
        {
            if (instance is T typed) _configure?.Invoke(typed);
        }

        IGuiComponentBuilder<TNewComponent> IGuiRenderTreeBuilder.AddComponent<TNewComponent>(int key)
            => _renderTreeBuilder.AddComponent<TNewComponent>(key);

        void IGuiRenderTreeBuilder.PushCascadeScope<TValue>(TValue value, string? name, GuiRenderFragment content)
            => _renderTreeBuilder.PushCascadeScope(value, name, content);
    }

    private abstract class RenderTreeFrame
    {
        public abstract Type ComponentType { get; }
        public int Key { get; protected init; }

        // Snapshotted during the blueprint phase in AddComponent. Records the cascade chain
        // that was active at this slot's declaration site — including any PushCascadeScope
        // wrappers around the AddComponent call. Read during the patch phase to initialise
        // the child builder's CascadeChain before recursing into the slot's subtree.
        public CascadingValueChain? CascadeChain;

        // Mouse handlers live on the base so the renderer can read them via a non-generic
        // reference. Default-valued GuiCallback<T> means "no handler" — checking HasMouseHandlers
        // before allocating a region keeps the hot non-interactive path zero-cost.
        public GuiCallback<GuiMouseEventArgs> OnMouseDown;
        public GuiCallback<GuiMouseEventArgs> OnMouseUp;
        public GuiCallback<GuiMouseEventArgs> OnMouseClick;
        public GuiCallback<GuiMouseEventArgs> OnMouseMove;
        public GuiCallback<GuiMouseEventArgs> OnMouseEnter;
        public GuiCallback<GuiMouseEventArgs> OnMouseLeave;

        // Keyboard handlers — fire only while this slot's component is the focused node
        // (see FocusManager). Stored on the frame so the renderer can read them via a
        // non-generic reference, same pattern as the mouse handlers above.
        public GuiCallback<GuiKeyEventArgs> OnKeyDown;
        public GuiCallback<GuiKeyEventArgs> OnKeyUp;
        public GuiCallback<GuiKeyEventArgs> OnKeyPress;

        public bool HasMouseHandlers =>
            OnMouseDown.HasHandler || OnMouseUp.HasHandler
            || OnMouseClick.HasHandler || OnMouseMove.HasHandler
            || OnMouseEnter.HasHandler || OnMouseLeave.HasHandler;

        public bool HasKeyHandlers =>
            OnKeyDown.HasHandler || OnKeyUp.HasHandler || OnKeyPress.HasHandler;

        public abstract IGuiNode CreateInstance();
        /// <summary>Clears per-pass state (registered configuration actions and mouse handlers).
        /// Called when an existing frame is reused at the start of a new blueprint pass.</summary>
        public abstract void Reset();
        public abstract void ApplyConfiguration(IGuiNode instance);
    }

    private readonly record struct ComponentSlotKey(Type ComponentType, int Key);
}

