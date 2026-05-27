namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiMeasurable
{
    /// <summary>
    /// Returns the object's intrinsic size given the available space.
    /// </summary>
    public GuiMeasuredSize Measure(double availableWidth, double availableHeight);
}
