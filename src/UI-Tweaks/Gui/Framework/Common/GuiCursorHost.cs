namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Per-dialog cursor controller. Published at the dialog root as a cascading value so
/// descendant components can request a custom mouse cursor while hovered (vanilla codes
/// like <c>"linkselect"</c>, <c>"textselect"</c>, …, or any custom cursor registered
/// via <c>ICoreClientAPI.LoadMouseCursor</c>).
/// <para>
/// <b>Usage.</b> Resolve the host with <c>GetCascadingValue&lt;GuiCursorHost&gt;()</c> in
/// <see cref="GuiComponent.OnParametersSet"/>, then call <see cref="SetHoverCursor"/>
/// from an <c>OnMouseEnter</c> handler and pass <c>null</c> from <c>OnMouseLeave</c>.
/// The dialog reads <see cref="HoverCursor"/> after every mouse-move dispatch and
/// forwards it to vanilla's per-dialog <c>MouseOverCursor</c> slot, which
/// <c>GuiManager</c> then applies as the platform cursor for that frame.
/// </para>
/// <para>
/// Resize-edge cursors managed by <see cref="GuiDialog"/> take priority over hover
/// cursors — hovering a resize grab zone shows the resize sprite even if the slot
/// underneath has set its own cursor preference.
/// </para>
/// </summary>
public sealed class GuiCursorHost
{
    /// <summary>
    /// The cursor code currently requested by a hovered slot, or <c>null</c> when no slot
    /// has set a preference. Read by <see cref="GuiDialog"/> after each mouse-move
    /// dispatch.
    /// </summary>
    public string? HoverCursor { get; private set; }

    /// <summary>
    /// Sets (or clears, when <paramref name="code"/> is <c>null</c>) the active hover
    /// cursor preference. Pass a vanilla cursor code (e.g. <c>"linkselect"</c>,
    /// <c>"textselect"</c>) or any custom code previously registered with
    /// <c>ICoreClientAPI.LoadMouseCursor</c>.
    /// </summary>
    public void SetHoverCursor(string? code) => HoverCursor = code;
}
