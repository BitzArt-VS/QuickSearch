namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiMeasurable
{
    /// <summary>
    /// Returns the object's desired layout size given the available space.
    /// </summary>
    public GuiLayoutSize Measure(GuiLayoutSize available);
}
