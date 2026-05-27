using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiDialog : IGuiComponent
{
    /// <summary>Horizontal offset from screen-centre in logical pixels. Drives dialog dragging.</summary>
    double OffsetX { get; }

    /// <summary>Vertical offset from screen-centre in logical pixels. Drives dialog dragging.</summary>
    double OffsetY { get; }

    internal void OnKeyDown(KeyEvent args);
    internal void OnKeyPress(KeyEvent args);
    internal void OnKeyUp(KeyEvent args);
    internal void OnMouseDown(GuiMouseEventArgs args);
    internal void OnMouseUp(GuiMouseEventArgs args);
    internal void OnMouseMove(GuiMouseEventArgs args);
    internal void OnMouseLeave(GuiMouseEventArgs args);
    internal bool OnEscapePressed();

    /// <summary>Called by the renderer when vanilla focus state changes.</summary>
    internal void OnFocus();
    internal void OnUnFocus();
}
