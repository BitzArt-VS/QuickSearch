using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Floating-layer renderer for the per-dialog overlay layer (dropdown popups, menus,
/// etc.). Owns its own Cairo surface — like <see cref="TooltipRenderer"/> — so overlay
/// content can extend beyond the dialog's bounds without being clipped, and forwards
/// every interactive / scroll / keyboard region the overlay subtree registers to the
/// owning <see cref="DialogRenderer"/> (translated from overlay-local to dialog-local
/// coordinates) so clicks, hover transitions and keyboard focus route through the
/// dialog's normal input dispatch path.
/// <para>
/// Surface size and on-screen position are dictated by the trigger: <see cref="Show"/>
/// supplies the popup's bounds in dialog-local logical pixels, and the overlay surface
/// is allocated to match. The blit position is the dialog's screen origin plus the
/// popup's logical offset (scaled to physical pixels).
/// </para>
/// <para>
/// Per-frame discipline mirrors the tooltip layer's: the requesting component re-issues
/// <see cref="Show"/> from its <c>Render</c> hook each dirty frame; if a frame passes
/// without that refresh the overlay is pruned. This keeps the overlay's lifetime tied
/// to the trigger's rendered presence with no manual teardown.
/// </para>
/// <para>
/// Lifecycle hooks driven by <see cref="DialogRenderer"/>:
/// <list type="bullet">
///   <item><see cref="OnFrameStart"/> arms the per-frame "refreshed" flag (cleared at
///   the start of every dirty render walk; set by each <see cref="Show"/> call).</item>
///   <item><see cref="RunWalk"/> runs after the main builder but before the main blit:
///   prunes any registration not refreshed this frame, then reconciles + redraws the
///   overlay surface so any forwarded hit-test / scroll / keyboard regions are appended
///   last to the dialog's region tables (winning the topmost-wins reverse hit-test).</item>
///   <item><see cref="Render"/> is the post-blit phase: blits the cached overlay texture
///   on top of the main dialog texture. <see cref="Update"/> is intentionally <i>not</i>
///   re-run here — it already happened in <see cref="RunWalk"/>.</item>
/// </list>
/// </para>
/// </summary>
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

    /// <summary>True when an overlay is currently registered (or pending its first redraw).</summary>
    internal bool HasActiveOverlay => IsActive;

    /// <summary>The active overlay's bounds in dialog-local logical pixels. Valid only
    /// when <see cref="HasActiveOverlay"/> is true.</summary>
    internal GuiComponentBounds ActiveBounds => _activeBounds;

    /// <summary>
    /// Resets the per-frame "refreshed" flag. The requesting component must call
    /// <see cref="Show"/> again from its <c>Render</c> hook this frame to keep the
    /// overlay alive, otherwise <see cref="RunWalk"/> below prunes it.
    /// </summary>
    internal override void OnFrameStart() => _refreshedThisFrame = false;

    internal void Show(object token, GuiComponentBounds dialogLocalBounds, GuiRenderFragment content)
    {
        // Detect identity / layout / content changes so we can flag the surface dirty
        // (and force a re-walk) only when something actually changed. A plain re-Show
        // with the same args during steady-state hover is a single bool flip.
        bool tokenChanged = !ReferenceEquals(_activeToken, token);
        bool boundsChanged = _activeBounds.X != dialogLocalBounds.X
                          || _activeBounds.Y != dialogLocalBounds.Y
                          || _activeBounds.Width != dialogLocalBounds.Width
                          || _activeBounds.Height != dialogLocalBounds.Height;
        bool fragmentChanged = !ReferenceEquals(ActiveFragment, content);

        if (tokenChanged || boundsChanged || fragmentChanged)
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

    /// <summary>
    /// In-walk phase: prune any stale-active overlay (registered last frame but not
    /// refreshed this one), then reconcile + redraw the overlay surface so any forwarded
    /// interactive / scroll / keyboard regions are appended last to the dialog's region
    /// tables.
    /// </summary>
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

    /// <summary>Post-blit phase: blit the cached overlay texture on top of the main
    /// dialog texture. Skip <see cref="Update"/> here — it already ran in <see cref="RunWalk"/>.</summary>
    internal override void Render() => Blit();

    /// <summary>Overlay size is fixed by the trigger — no measurement needed.</summary>
    protected override GuiSize ResolveLogicalSize() =>
        new GuiSize(_activeBounds.Width, _activeBounds.Height);

    protected override (double posX, double posY) GetScreenPosition(int physW, int physH, float scale)
    {
        // Anchor to the dialog's on-screen origin so the overlay tracks dialog drag /
        // resize without invalidating its own surface.
        var (dx, dy) = _dialogRenderer.GetScreenOrigin();
        double posX = dx + _activeBounds.X * scale;
        double posY = dy + _activeBounds.Y * scale;
        return (posX, posY);
    }

    /// <summary>
    /// True when the screen-space point lies inside the active overlay's blit rectangle.
    /// Consumed by <see cref="GuiDialog"/> to allow input through to overlay regions even
    /// when the click landed outside the dialog's own rect (e.g. a dropdown popup hanging
    /// below the dialog's bottom edge).
    /// </summary>
    internal bool ContainsScreenPoint(int x, int y)
    {
        if (!HasActiveOverlay) return false;
        if (_measuredSize.Width <= 0 || _measuredSize.Height <= 0) return false;

        float scale = Vintagestory.API.Config.RuntimeEnv.GUIScale;
        var (dx, dy) = _dialogRenderer.GetScreenOrigin();
        double posX = dx + _activeBounds.X * scale;
        double posY = dy + _activeBounds.Y * scale;
        double w = _activeBounds.Width * scale;
        double h = _activeBounds.Height * scale;
        return x >= posX && x < posX + w && y >= posY && y < posY + h;
    }

    // ── Region forwarding ──────────────────────────────────────────────────────────

    public override void AddInteractiveRegion(in InteractiveRegion region)
    {
        // Overlay-local logical (origin at popup top-left) → dialog-local logical so the
        // dialog's hit-test (which works in dialog-local coordinates) matches correctly.
        var translated = new InteractiveRegion(
            new GuiComponentBounds(
                region.Bounds.X + _activeBounds.X,
                region.Bounds.Y + _activeBounds.Y,
                region.Bounds.Width, region.Bounds.Height),
            region.Token,
            region.OnMouseDown, region.OnMouseUp, region.OnMouseClick,
            region.OnMouseMove, region.OnMouseEnter, region.OnMouseLeave);
        _dialogRenderer.AddInteractiveRegion(translated);
    }

    public override void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container)
    {
        var translated = new GuiComponentBounds(
            bounds.X + _activeBounds.X,
            bounds.Y + _activeBounds.Y,
            bounds.Width, bounds.Height);
        _dialogRenderer.AddScrollRegion(translated, container);
    }

    public override void AddKeyboardRegion(in KeyboardRegion region)
    {
        // Keyboard regions are matched by token identity (not bounds), so no translation
        // is needed — forward verbatim into the dialog's keyboard region table.
        _dialogRenderer.AddKeyboardRegion(region);
    }
}
