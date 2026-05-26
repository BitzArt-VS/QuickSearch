namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiComponent : IGuiNode
{
    public GuiComponentLayoutParameters LayoutParameters { get; }

    /// <summary>
    /// Returns the component's intrinsic size given the available space. Called by the
    /// layout pass for <see cref="GuiSizeMode.FitContent"/> dimensions; not called for
    /// <see cref="GuiSizeMode.Fill"/>. Override in leaf components (text, icons) to
    /// report their natural size.
    /// </summary>
    public GuiMeasuredSize Measure(double availableWidth, double availableHeight);
}
