using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal abstract class FloatingLayerRenderer : IGuiComponentTreeRenderer, IDisposable
{
    protected readonly ICoreClientAPI _clientApi;
    private readonly GuiContentSurface _contentSurface;
    private float _currentScale;

    private bool _surfaceDirty;

    protected GuiSize _measuredSize;

    public IGuiRenderHandle Handle => _contentSurface.Handle;
    public ICoreClientAPI ClientApi => _clientApi;

    public double RenderOrder => 1.0;
    public int RenderRange => int.MaxValue;

    protected int PhysicalWidth => _contentSurface.PhysicalWidth;
    protected int PhysicalHeight => _contentSurface.PhysicalHeight;

    protected GuiRenderTreeBuilder Builder => _contentSurface.Builder;

    protected FloatingLayerRenderer(ICoreClientAPI clientApi)
    {
        _clientApi = clientApi;
        _contentSurface = new GuiContentSurface(clientApi, this);
        _currentScale = RuntimeEnv.GUIScale;
    }

    internal void SetCascadeChain(CascadingValueChain? chain) => _contentSurface.Builder.CascadeChain = chain;

    protected GuiRenderFragment? ActiveFragment { get; set; }

    protected void MarkDirty() => _surfaceDirty = true;

    internal bool IsActive => ActiveFragment is not null;

    internal virtual void OnFrameStart() { }

    internal virtual void RunWalk() { }

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

    public void Cancel(GuiRenderFragment fragment) { }

    public virtual void AddInteractiveRegion(in InteractiveRegion region) { }
    public virtual void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container) { }
    public virtual void AddKeyboardRegion(in KeyboardRegion region) { }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage) { }

    protected virtual GuiSize ResolveLogicalSize() =>
        Builder.MeasureChildren(double.PositiveInfinity, double.PositiveInfinity, GuiDirection.Vertical);

    protected abstract (double posX, double posY) GetScreenPosition(int physW, int physH, float scale);

    protected void Update()
    {
        if (ActiveFragment is null) return;

        float scale = RuntimeEnv.GUIScale;
        bool needsRedraw = _surfaceDirty || scale != _currentScale;

        if (needsRedraw) ReconcileAndMeasure();

        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0) return;

        if (ReallocateSurfaceIfNeeded(scale)) needsRedraw = true;
        if (needsRedraw) DrawToSurface(scale);
    }

    private void ReconcileAndMeasure()
    {
        Builder.Run(ActiveFragment!);
        _measuredSize = ResolveLogicalSize();
    }

    private bool ReallocateSurfaceIfNeeded(float scale)
    {
        int physW = (int)Math.Ceiling(_measuredSize.Width * scale);
        int physH = (int)Math.Ceiling(_measuredSize.Height * scale);
        bool reallocated = _contentSurface.EnsureSize(physW, physH);
        if (reallocated) _currentScale = scale;
        return reallocated;
    }

    private void DrawToSurface(float scale)
    {
        var bounds = new GuiComponentBounds(0, 0, _measuredSize.Width, _measuredSize.Height);
        _contentSurface.DrawContents(bounds, GuiDirection.Vertical, scale);
        _surfaceDirty = false;
    }

    protected void Blit()
    {
        if (ActiveFragment is null) return;
        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0) return;

        var (posX, posY) = GetScreenPosition(_contentSurface.PhysicalWidth, _contentSurface.PhysicalHeight, _currentScale);
        _contentSurface.Blit(posX, posY);
    }

    public virtual void Dispose()
    {
        _contentSurface.Dispose();
    }
}
