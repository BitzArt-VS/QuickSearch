using Cairo;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Shared base for floating layers that draw subtrees onto their own dedicated Cairo
/// surface, blitted on top of the main dialog texture. Each subclass represents one
/// floating layer (tooltip, overlay/dropdown popup, …); the dialog renderer owns one
/// instance per concern and drives them through a uniform per-frame lifecycle.
///
/// <para>What the base captures (so subclasses don't have to re-implement it):</para>
/// <list type="bullet">
///   <item>own <see cref="GuiRenderTreeBuilder"/> + <see cref="IGuiRenderHandle"/> so the
///   floating subtree has independent component lifecycle, scoped reconcile, and
///   cascading-value lookup;</item>
///   <item>own <see cref="ImageSurface"/> / <see cref="Context"/> / <see cref="LoadedTexture"/>
///   so the layer can extend beyond the dialog's main surface without being clipped;</item>
///   <item>active-fragment storage (<see cref="ActiveFragment"/>) and dirty-flag tracking
///   (<see cref="MarkDirty"/>) — set the property and flip the flag, the base does the rest;</item>
///   <item>dirty-flag driven reconcile + measure + Cairo redraw + GPU upload, with a
///   separate blit step so the cached texture can be drawn on every frame regardless
///   of whether the contents changed.</item>
/// </list>
///
/// <para>Per-frame lifecycle (called by the owning <see cref="DialogRenderer"/> in order):</para>
/// <list type="number">
///   <item><see cref="OnFrameStart"/> — invoked at the start of every dirty render walk
///   (before the main builder runs). Default no-op; overlay-style layers override to
///   reset per-frame "refreshed" tracking.</item>
///   <item><see cref="RunWalk"/> — invoked at the end of every dirty render walk (after
///   the main builder runs but before the main blit). Default no-op; overlay-style
///   layers override to prune stale registrations and run the layer's own reconcile +
///   redraw inside the walk so any forwarded hit-test / scroll / keyboard regions are
///   appended last to the dialog's tables (winning the topmost-wins reverse hit test).</item>
///   <item><see cref="Render"/> — invoked every frame after the main blit. Default does
///   <c>Update + Blit</c>; overlay-style layers that already updated in <see cref="RunWalk"/>
///   override to blit only.</item>
/// </list>
///
/// <para>Concrete subclasses must implement <see cref="GetScreenPosition"/>; they may
/// override <see cref="ResolveLogicalSize"/> (default measures via the builder) when
/// the trigger dictates the bounds (e.g. an anchored popup).</para>
///
/// <para>The <see cref="IRenderer"/> hooks are no-ops; floating layers are never
/// registered with the vanilla render pipeline and are driven manually by their owning
/// dialog renderer.</para>
/// </summary>
internal abstract class FloatingLayerRenderer : IGuiComponentTreeRenderer, IDisposable
{
    protected readonly ICoreClientAPI _clientApi;
    protected readonly GuiRenderTreeBuilder _builder;
    private readonly IGuiRenderHandle _handle;

    private ImageSurface? _surface;
    private Context? _ctx;
    private LoadedTexture _texture;
    private float _currentScale;
    private int _currentSurfacePhysW;
    private int _currentSurfacePhysH;

    /// <summary>Set by <see cref="MarkDirty"/> / <see cref="Schedule"/> and consumed by
    /// the next <see cref="Update"/> call. Cleared once the surface has been redrawn.</summary>
    protected bool _surfaceDirty;

    /// <summary>The most recently resolved logical size of the layer's content. Cached so
    /// <see cref="Blit"/> can size the on-screen rectangle without re-measuring.</summary>
    protected GuiSize _measuredSize;

    public IGuiRenderHandle Handle => _handle;
    public ICoreClientAPI ClientApi => _clientApi;

    // IRenderer requirements — never registered, so values don't matter, but supply
    // sensible numbers in case anything reflects on them.
    public double RenderOrder => 1.0;
    public int RenderRange => int.MaxValue;

    protected FloatingLayerRenderer(ICoreClientAPI clientApi)
    {
        _clientApi = clientApi;
        _builder = new GuiRenderTreeBuilder(this);
        // The floating subtree's root has no upstream builder publishing values (it does
        // not lexically nest inside the dialog tree). Pass null as parentBuilder; the
        // initial cascade chain (e.g. the dialog's hosts) is supplied imperatively via
        // SetCascadeChain from the owning dialog renderer's ctor.
        _handle = new RenderHandle(this, _builder, parentBuilder: null);
        _texture = new LoadedTexture(clientApi);
        _currentScale = RuntimeEnv.GUIScale;
    }

    /// <summary>
    /// Seeds the floating subtree's cascading-value chain. Called once by the owning
    /// <see cref="DialogRenderer"/> after it has published the dialog-root values, so
    /// the layer's content can consume any value the dialog's main tree can. The chain
    /// is stored on the builder and read live by descendant render handles.
    /// </summary>
    internal void SetCascadeChain(CascadingValueChain? chain) => _builder.CascadeChain = chain;

    /// <summary>The fragment to reconcile + render this frame, or <c>null</c> when the
    /// layer is dormant. Subclasses assign this directly when their content changes; the
    /// base treats <c>null</c> as a hard short-circuit in <see cref="Update"/> and
    /// <see cref="Blit"/>. Mutating this does <i>not</i> automatically mark the surface
    /// dirty — call <see cref="MarkDirty"/> alongside the assignment when the change
    /// should trigger a redraw on the next frame.</summary>
    protected GuiRenderFragment? ActiveFragment { get; set; }

    /// <summary>Marks the surface dirty — next <see cref="Update"/> will rebuild.</summary>
    protected void MarkDirty() => _surfaceDirty = true;

    /// <summary>True when there is content currently to render.</summary>
    protected bool HasActive => ActiveFragment is not null;

    /// <summary>True when this layer currently has content to render. Exposed for the
    /// dialog renderer's hit-testing / debug paths (e.g. "is any overlay active?").</summary>
    internal bool IsActive => ActiveFragment is not null;

    // ── Per-frame lifecycle hooks (called by DialogRenderer) ───────────────────────
    // All three default to a sensible behaviour so a no-state-tracking layer (the
    // tooltip layer being the canonical example) can rely entirely on the defaults.

    /// <summary>Invoked at the start of each dirty render walk, before the main builder
    /// runs. Default no-op; overlay-style layers override to clear per-frame
    /// "refreshed this frame" tracking so unrefreshed registrations get pruned.</summary>
    internal virtual void OnFrameStart() { }

    /// <summary>Invoked at the end of each dirty render walk, before the main blit.
    /// Default no-op; overlay-style layers override to run their own reconcile + redraw
    /// inside the walk so that any forwarded hit-test / scroll / keyboard regions are
    /// appended last to the dialog's region tables.</summary>
    internal virtual void RunWalk() { }

    /// <summary>Invoked every frame after the main blit. Default reconciles + redraws
    /// the layer surface (<see cref="Update"/>) then blits it (<see cref="Blit"/>).
    /// Overlay-style layers that already ran <see cref="Update"/> from <see cref="RunWalk"/>
    /// override this to call <see cref="Blit"/> only.</summary>
    internal virtual void Render()
    {
        Update();
        Blit();
    }

    public void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder)
    {
        // Floating subtrees are shallow and rebuild-from-scratch is cheap; collapse all
        // scoped rebuild requests into a single dirty flag. Next Update() runs the active
        // fragment from scratch.
        _surfaceDirty = true;
    }

    public void Cancel(GuiRenderFragment fragment)
    {
        // No queued-rebuild table to mutate — Schedule only flips the dirty flag.
    }

    public virtual void AddInteractiveRegion(in InteractiveRegion region) { }
    public virtual void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container) { }
    public virtual void AddKeyboardRegion(in KeyboardRegion region) { }

    // IRenderer no-op — driven manually by the owning DialogRenderer.
    public void OnRenderFrame(float deltaTime, EnumRenderStage stage) { }

    /// <summary>Called after reconcile to resolve the logical size of the layer's content.
    /// Default: ask the builder to measure the reconciled root (used when the size is
    /// derived from natural content extents, e.g. tooltips). Override to return a fixed
    /// size when the trigger dictates the bounds (e.g. an anchored popup).</summary>
    protected virtual GuiSize ResolveLogicalSize() =>
        _builder.MeasureChildren(double.PositiveInfinity, double.PositiveInfinity, GuiDirection.Vertical);

    /// <summary>Computes the on-screen origin (physical pixels) for the cached texture
    /// blit. Called every frame the layer is active, so position can track moving
    /// anchors (cursor, dialog offset, etc.) without invalidating the surface.</summary>
    protected abstract (double posX, double posY) GetScreenPosition(int physW, int physH, float scale);

    /// <summary>Hook invoked at the start of each Cairo render walk, after the surface
    /// has been cleared and scaled but before <see cref="GuiRenderTreeBuilder.Render"/>
    /// runs. Subclasses may use this to reset per-frame state that depends on the upcoming
    /// walk (e.g. interactive region tables they own).</summary>
    protected virtual void OnBeforeRenderWalk() { }

    /// <summary>
    /// Reconciles the active fragment, redraws the Cairo surface, and uploads to GPU
    /// when dirty. Cheap when clean and idempotent across consecutive calls. Does not
    /// blit — call <see cref="Blit"/> for that.
    /// </summary>
    protected void Update()
    {
        var fragment = ActiveFragment;
        if (fragment is null) return;

        float scale = RuntimeEnv.GUIScale;
        bool scaleChanged = scale != _currentScale;
        bool needsRedraw = _surfaceDirty || scaleChanged;

        if (needsRedraw)
        {
            // 1. Reconcile the floating subtree against the active fragment. The builder
            // creates / reuses / disposes child component instances as content changes.
            _builder.Run(fragment);

            // 2. Resolve the logical size of the reconciled root. Default impl measures
            // via the builder; subclasses with fixed-size content override.
            _measuredSize = ResolveLogicalSize();
        }

        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0) return;

        int physW = (int)Math.Ceiling(_measuredSize.Width * scale);
        int physH = (int)Math.Ceiling(_measuredSize.Height * scale);

        // 3. (Re)allocate the Cairo surface when its size or the GUI scale changes.
        if (_surface is null || physW != _currentSurfacePhysW || physH != _currentSurfacePhysH || scaleChanged)
        {
            _ctx?.Dispose();
            _surface?.Dispose();
            _surface = new ImageSurface(Format.Argb32, physW, physH);
            _ctx = new Context(_surface);
            _currentSurfacePhysW = physW;
            _currentSurfacePhysH = physH;
            _currentScale = scale;
            needsRedraw = true; // surface contents undefined after recreation
        }

        if (needsRedraw)
        {
            // Clear the surface to fully transparent.
            _ctx!.IdentityMatrix();
            _ctx.Operator = Operator.Source;
            _ctx.SetSourceRGBA(0, 0, 0, 0);
            _ctx.Paint();
            _ctx.Operator = Operator.Over;

            // Pre-scale the CTM by GUIScale so component code works in logical pixels —
            // mirrors DialogRenderer's main-surface setup.
            _ctx.Scale(scale, scale);

            OnBeforeRenderWalk();

            var bounds = new GuiComponentBounds(0, 0, _measuredSize.Width, _measuredSize.Height);
            _builder.Render(_ctx, bounds, GuiDirection.Vertical);

            _surface.Flush();
            _clientApi.Gui.LoadOrUpdateCairoTexture(_surface, true, ref _texture);
            _surfaceDirty = false;
        }
    }

    /// <summary>
    /// Blits the cached texture at <see cref="GetScreenPosition"/>. No-op when the
    /// layer is dormant or the surface has never been built.
    /// </summary>
    protected void Blit()
    {
        if (ActiveFragment is null) return;
        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0) return;
        if (_texture.TextureId == 0) return;

        int physW = _currentSurfacePhysW;
        int physH = _currentSurfacePhysH;
        var (posX, posY) = GetScreenPosition(physW, physH, _currentScale);

        _clientApi.Render.Render2DTexturePremultipliedAlpha(
            _texture.TextureId, posX, posY, physW, physH);
    }

    public virtual void Dispose()
    {
        _builder.Dispose();
        _texture.Dispose();
        _ctx?.Dispose();
        _surface?.Dispose();
        _ctx = null;
        _surface = null;
    }
}
