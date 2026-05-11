using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class OverlayRenderer : FloatingLayerRenderer
{
    private readonly DialogRenderer _dialogRenderer;

    private object? _activeToken;
    private GuiComponentBounds _activeBounds;
    private bool _refreshedThisFrame;

    public OverlayRenderer(DialogRenderer dialogRenderer, ICoreClientAPI clientApi) : base(clientApi)
    {
        _dialogRenderer = dialogRenderer;
    }

    internal bool HasActiveOverlay => IsActive;

    internal GuiComponentBounds ActiveBounds => _activeBounds;

    internal override void OnFrameStart() => _refreshedThisFrame = false;

    internal void Show(object token, GuiComponentBounds dialogLocalBounds, GuiRenderFragment content)
    {
        bool changed = !ReferenceEquals(_activeToken, token)
                    || _activeBounds != dialogLocalBounds
                    || !ReferenceEquals(ActiveFragment, content);

        if (changed)
        {
            _activeToken = token;
            _activeBounds = dialogLocalBounds;
            ActiveFragment = content;
            MarkDirty();
        }

        _refreshedThisFrame = true;
    }

    internal void Hide(object token)
    {
        if (!ReferenceEquals(_activeToken, token)) return;
        ClearActive();
    }

    private void ClearActive()
    {
        if (ActiveFragment is null) return;
        _activeToken = null;
        ActiveFragment = null;
        _activeBounds = default;
        MarkDirty();
    }

    internal override void RunWalk()
    {
        if (ActiveFragment is not null && !_refreshedThisFrame)
            ClearActive();

        // Force a re-walk on every dialog dirty frame: the dialog's region tables were
        // just cleared, so even an unchanged overlay must re-register its regions to
        // keep input dispatch intact. Cheap — typical popup is a few dozen rows.
        if (ActiveFragment is not null) MarkDirty();

        Update();
    }

    internal override void Render() => Blit();

    protected override GuiSize ResolveLogicalSize() =>
        new GuiSize(_activeBounds.Width, _activeBounds.Height);

    protected override (double posX, double posY) GetScreenPosition(int physW, int physH, float scale)
    {
        var (dx, dy) = _dialogRenderer.GetScreenOrigin();
        double posX = dx + _activeBounds.X * scale;
        double posY = dy + _activeBounds.Y * scale;
        return (posX, posY);
    }

    internal bool ContainsScreenPoint(int x, int y)
    {
        if (!HasActiveOverlay || _measuredSize.Width <= 0 || _measuredSize.Height <= 0) return false;

        float scale = RuntimeEnv.GUIScale;
        var (posX, posY) = GetScreenPosition(PhysicalWidth, PhysicalHeight, scale);
        return x >= posX && x < posX + PhysicalWidth && y >= posY && y < posY + PhysicalHeight;
    }

    public override void AddInteractiveRegion(in InteractiveRegion region) =>
        _dialogRenderer.AddInteractiveRegion(region.Translated(_activeBounds.X, _activeBounds.Y));

    public override void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container) =>
        _dialogRenderer.AddScrollRegion(bounds.Translated(_activeBounds.X, _activeBounds.Y), container);

    public override void AddKeyboardRegion(in KeyboardRegion region)
    {
        // Keyboard regions are matched by token identity (not bounds), so no translation
        // is needed — forward verbatim into the dialog's keyboard region table.
        _dialogRenderer.AddKeyboardRegion(region);
    }
}
