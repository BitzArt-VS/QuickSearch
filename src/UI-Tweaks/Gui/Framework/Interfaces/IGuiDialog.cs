using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal interface IGuiDialog : IGuiComponent
{
    double RenderOrder { get; }

    /// <summary>
    /// Drives a single Cairo render frame. Called by the input interceptor from inside
    /// vanilla's <c>GuiManager.OnRenderFrameGUI</c> pass so this dialog participates in
    /// the same z-stack as vanilla dialogs.
    /// </summary>
    void OnRenderGui(float deltaTime);

    /// <summary>
    /// Horizontal offset in logical (unscaled) pixels from the screen-centred default position.
    /// Drives dialog dragging.
    /// </summary>
    double OffsetX { get; }

    /// <summary>
    /// Vertical offset in logical (unscaled) pixels from the screen-centred default position.
    /// Drives dialog dragging.
    /// </summary>
    double OffsetY { get; }

    /// <summary>
    /// Hit-test against the dialog's screen-space rectangle.
    /// Coordinates are in physical pixels (matching <see cref="MouseEvent.X"/>/<see cref="MouseEvent.Y"/>).
    /// </summary>
    internal bool ContainsScreenPoint(int x, int y);

    internal void OnMouseDown(MouseEvent args);
    internal void OnMouseUp(MouseEvent args);
    internal void OnMouseMove(MouseEvent args);
    internal void OnMouseWheel(MouseWheelEventArgs args);
    internal void OnKeyDown(KeyEvent args);
    internal void OnKeyPress(KeyEvent args);
    internal void OnKeyUp(KeyEvent args);
    internal bool OnEscapePressed();

    /// <summary>Called by the input interceptor when vanilla focus state changes.</summary>
    internal void OnFocus();
    internal void OnUnFocus();
}
