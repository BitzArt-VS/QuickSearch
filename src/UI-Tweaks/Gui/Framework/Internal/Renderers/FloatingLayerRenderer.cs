using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal class FloatingLayerRenderer : GuiSurfaceRenderer
{
    protected GuiLayoutSize _measuredSize;

    private object? _activeToken;
    private FloatingLayerPlacement _activePlacement;
    private bool _refreshedThisFrame;

    protected GuiRenderFragment? ActiveFragment { get; set; }

    internal bool IsActive => ActiveFragment is not null;

    public FloatingLayerRenderer(ICoreClientAPI clientApi) : base(clientApi) { }

    public void Show(object token, GuiRenderFragment content, in FloatingLayerPlacement placement)
    {
        _activeToken = token;
        ActiveFragment = content;
        _activePlacement = placement;
        _refreshedThisFrame = true;
        RequestArrange();
    }

    public void Hide(object token)
    {
        if (!ReferenceEquals(_activeToken, token))
        {
            return;
        }

        ClearActive();
    }

    public void OnFrameStart() => _refreshedThisFrame = false;

    public void RunWalk()
    {
        if (ActiveFragment is not null
            && _activePlacement.AutoClearWhenNotRefreshed
            && !_refreshedThisFrame)
        {
            ClearActive();
        }

        if (!_activePlacement.RewalkOnDialogWalk)
        {
            return;
        }

        // The host's region tables were just cleared by the dialog's arrange walk —
        // even an unchanged layer must re-walk so its regions get re-registered.
        if (ActiveFragment is not null)
        {
            RequestArrange();
        }
        Update();
    }

    public void Render()
    {
        // RewalkOnDialogWalk layers already updated in RunWalk; others (which are not
        // tied to the dialog's walk cadence — e.g. tooltips driven by mouse events)
        // reconcile here so changes between walks still take effect.
        if (!_activePlacement.RewalkOnDialogWalk)
        {
            Update();
        }

        Blit();
    }

    private void ClearActive()
    {
        if (ActiveFragment is null)
        {
            return;
        }

        _activeToken = null;
        ActiveFragment = null;
        _activePlacement = default;
    }

    protected void Update()
    {
        if (ActiveFragment is null)
        {
            return;
        }

        float scale = RuntimeEnv.GUIScale;
        if (!HasPendingSurfaceUpdate && scale == _currentScale)
        {
            return;
        }

        ReconcileAndMeasure();

        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0)
        {
            return;
        }

        ReallocateSurfaceIfNeeded(scale);
        DrawToSurface(scale);
    }

    private void ReconcileAndMeasure()
    {
        Builder.Run(BuildRootFragment);
        _measuredSize = ResolveLogicalSize();
    }

    private void BuildRootFragment(IGuiRenderTreeBuilder builder)
    {
        GuiSize? width = null;
        GuiSize? height = null;

        if (_activePlacement.FixedLogicalSize is GuiLayoutSize fixedSize)
        {
            width = fixedSize.Width;
            height = fixedSize.Height;
        }

        builder.AddContainer(0, width: width, height: height, content: ActiveFragment);
    }

    private void ReallocateSurfaceIfNeeded(float scale)
    {
        int physW = (int)Math.Ceiling(_measuredSize.Width * scale);
        int physH = (int)Math.Ceiling(_measuredSize.Height * scale);
        EnsureSurfaceSize(physW, physH);
    }

    private void DrawToSurface(float scale)
    {
        var bounds = new GuiComponentBounds(0, 0, _measuredSize.Width, _measuredSize.Height);
        DrawSurfaceContents(bounds, GuiDirection.Vertical, scale, arrange: true);
    }

    private void Blit()
    {
        if (ActiveFragment is null)
        {
            return;
        }

        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0)
        {
            return;
        }

        var (posX, posY) = GetScreenPosition(PhysicalWidth, PhysicalHeight, _currentScale);
        BlitAt(posX, posY);
    }

    public override bool ContainsScreenPoint(int x, int y)
    {
        if (!IsActive || _measuredSize.Width <= 0 || _measuredSize.Height <= 0)
        {
            return false;
        }

        float scale = RuntimeEnv.GUIScale;
        var (posX, posY) = GetScreenPosition(PhysicalWidth, PhysicalHeight, scale);
        return x >= posX && x < posX + PhysicalWidth && y >= posY && y < posY + PhysicalHeight;
    }

    public override void AddInteractiveRegion(in InteractiveRegion region)
    {
        if (_activePlacement.InputHost is null)
        {
            return;
        }

        _activePlacement.InputHost.AddInteractiveRegion(
            region.Translated(_activePlacement.InputRegionOffsetX, _activePlacement.InputRegionOffsetY));
    }

    public override void AddKeyboardRegion(in KeyboardRegion region)
    {
        if (_activePlacement.InputHost is null)
        {
            return;
        }
        // Keyboard regions are matched by token identity, not bounds; no translation.
        _activePlacement.InputHost.AddKeyboardRegion(region);
    }

    protected virtual GuiLayoutSize ResolveLogicalSize()
    {
        if (_activePlacement.FixedLogicalSize is GuiLayoutSize fixedSize)
        {
            return fixedSize;
        }

        double maxWidth = _activePlacement.MaxLogicalWidth > 0
            ? _activePlacement.MaxLogicalWidth
            : double.PositiveInfinity;
        double maxHeight = _activePlacement.MaxLogicalHeight > 0
            ? _activePlacement.MaxLogicalHeight
            : double.PositiveInfinity;

        if (Builder.ComponentSlots.Count == 0 || Builder.ComponentSlots[0].Node is not IGuiComponent rootComponent)
        {
            return default;
        }

        return rootComponent.Measure(new GuiLayoutSize(maxWidth, maxHeight));
    }

    protected virtual (double posX, double posY) GetScreenPosition(double physW, double physH, float scale) =>
        _activePlacement.Anchor.Invoke(physW, physH, scale, _clientApi);
}
