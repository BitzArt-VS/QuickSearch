namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// One entry in a dialog's per-frame interactive region table. Records a slot's allocated
/// bounds plus the mouse callbacks attached to it, in render order. Hit-testing walks the
/// table in reverse so later-rendered (z-order topmost) regions win.
/// <para>
/// <see cref="Token"/> is an opaque identity used to match a mouse-down to its mouse-up:
/// the slot's <see cref="IGuiComponent"/> instance reference. Stable across rebuilds as long
/// as the slot persists (slots are keyed by <c>(Type, key)</c> — see <c>GuiRenderTreeBuilder</c>).
/// </para>
/// </summary>
internal readonly struct InteractiveRegion
{
    public readonly GuiComponentBounds Bounds;
    public readonly object Token;
    public readonly GuiCallback<GuiMouseEventArgs> OnMouseDown;
    public readonly GuiCallback<GuiMouseEventArgs> OnMouseUp;
    public readonly GuiCallback<GuiMouseEventArgs> OnMouseClick;
    public readonly GuiCallback<GuiMouseEventArgs> OnMouseMove;
    public readonly GuiCallback<GuiMouseEventArgs> OnMouseEnter;
    public readonly GuiCallback<GuiMouseEventArgs> OnMouseLeave;

    public InteractiveRegion(
        GuiComponentBounds bounds,
        object token,
        GuiCallback<GuiMouseEventArgs> onMouseDown,
        GuiCallback<GuiMouseEventArgs> onMouseUp,
        GuiCallback<GuiMouseEventArgs> onMouseClick,
        GuiCallback<GuiMouseEventArgs> onMouseMove,
        GuiCallback<GuiMouseEventArgs> onMouseEnter,
        GuiCallback<GuiMouseEventArgs> onMouseLeave)
    {
        Bounds = bounds;
        Token = token;
        OnMouseDown = onMouseDown;
        OnMouseUp = onMouseUp;
        OnMouseClick = onMouseClick;
        OnMouseMove = onMouseMove;
        OnMouseEnter = onMouseEnter;
        OnMouseLeave = onMouseLeave;
    }

    public bool Contains(double x, double y) =>
        x >= Bounds.X && x < Bounds.X + Bounds.Width &&
        y >= Bounds.Y && y < Bounds.Y + Bounds.Height;
}
