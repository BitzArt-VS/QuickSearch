using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal interface IGuiDialog : IGuiComponent
{
    double RenderOrder { get; }

    /// <summary>Horizontal offset from screen-centre in logical pixels. Drives dialog dragging.</summary>
    double OffsetX { get; }

    /// <summary>Vertical offset from screen-centre in logical pixels. Drives dialog dragging.</summary>
    double OffsetY { get; }

    internal void OnKeyDown(KeyEvent args);
    internal void OnKeyPress(KeyEvent args);
    internal void OnKeyUp(KeyEvent args);
    internal bool OnEscapePressed();

    /// <summary>Called by the renderer when vanilla focus state changes.</summary>
    internal void OnFocus();
    internal void OnUnFocus();
}
