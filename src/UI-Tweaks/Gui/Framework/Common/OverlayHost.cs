namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Per-dialog overlay controller. Published at the dialog root as a cascading value so
/// any descendant component can register a floating overlay (dropdown popup, menu, etc.)
/// during the main render walk via <see cref="Show"/>.
/// <para>
/// Overlay content is rendered onto a dedicated Cairo surface owned by
/// <see cref="OverlayRenderer"/> — the same separate-surface pattern used by the tooltip
/// layer — so it is not clipped by the dialog's bounds and can extend off the dialog's
/// edges. Unlike tooltips, overlays are interactive: their registered hit-test regions
/// are forwarded to the dialog's input dispatch (translated from overlay-local to
/// dialog-local coordinates) so clicks, hovers and keyboard events route normally.
/// </para>
/// <para>
/// Currently a single overlay is active at a time (last <see cref="Show"/> wins). This
/// matches typical dropdown / menu UX where opening a new picker dismisses any existing
/// one through focus-loss. A token argument identifies the requesting component so a
/// matching <see cref="Hide"/> can dismiss the right overlay.
/// </para>
/// </summary>
public sealed class OverlayHost
{
    private readonly OverlayRenderer _renderer;

    internal OverlayHost(OverlayRenderer renderer) => _renderer = renderer;

    /// <summary>
    /// Registers / refreshes the active overlay for <paramref name="token"/>. Should be
    /// called from the requesting component's <see cref="IGuiNode.Render"/> hook so the
    /// supplied dialog-local <paramref name="dialogLocalBounds"/> reflect the just-resolved
    /// trigger geometry. The host marks the overlay as still-active for the current
    /// frame; if a frame passes without a refreshing <c>Show</c> call (e.g. the dropdown
    /// closed), the overlay is pruned automatically.
    /// </summary>
    /// <param name="token">A stable identity for the requesting component — typically
    /// <c>this</c>. Used by <see cref="Hide"/> to ensure only the original requester can
    /// dismiss the overlay.</param>
    /// <param name="dialogLocalBounds">The on-screen rectangle of the overlay in
    /// dialog-local logical pixels. The overlay's Cairo surface is sized to match.</param>
    /// <param name="content">The render fragment producing the overlay's content. Should
    /// be reference-stable across frames where the overlay's identity is unchanged so the
    /// renderer's reuse path skips per-frame closures.</param>
    public void Show(object token, GuiComponentBounds dialogLocalBounds, GuiRenderFragment content)
        => _renderer.Show(token, dialogLocalBounds, content);

    /// <summary>
    /// Unconditionally hides the active overlay if it was registered for
    /// <paramref name="token"/>. No-op when the active overlay belongs to a different
    /// requester (or none is active). Use to drop the overlay imperatively rather than
    /// waiting for the next frame's prune cycle.
    /// </summary>
    public void Hide(object token) => _renderer.Hide(token);
}
