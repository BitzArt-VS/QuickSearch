namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Controls how a component resolves a dimension that has no explicit value set.
/// </summary>
public enum GuiSizeMode
{
    /// <summary>
    /// Stretch to fill the available space given by the parent's layout pass.
    /// Calls <see cref="IGuiComponent.Measure"/> to obtain the desired size.
    /// This is the default.
    /// </summary>
    Fill = 0,

    /// <summary>
    /// Shrink to exactly contain the component's children.
    /// The layout pass measures all relative children and uses their combined
    /// extent (sum on the flow axis, max on the cross axis) plus this
    /// component's own padding.
    /// </summary>
    FitContent = 1,
}
