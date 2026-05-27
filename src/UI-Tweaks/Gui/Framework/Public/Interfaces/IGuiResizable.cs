namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiResizable : IGuiMeasurable
{
    /// <summary>
    /// The resize edges this object currently accepts. Return <see cref="GuiResizeEdge.None"/>
    /// while resizing should be disabled.
    /// </summary>
    public GuiResizeEdge SupportedResizeEdges { get; }

    /// <summary>
    /// Applies framework-suggested outer bounds for this resizable.
    /// </summary>
    public void Resize(GuiComponentBounds bounds);
}
