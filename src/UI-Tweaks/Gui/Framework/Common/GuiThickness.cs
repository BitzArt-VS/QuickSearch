namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Represents spacing on four sides of a component (margin or padding).
/// All values are in logical (Cairo) pixels.
/// </summary>
public readonly record struct GuiThickness(double Top, double Right, double Bottom, double Left)
{
    public static readonly GuiThickness Zero = new(0, 0, 0, 0);

    /// <summary>Uniform spacing on all four sides.</summary>
    public GuiThickness(double all) : this(all, all, all, all) { }

    /// <summary>Symmetric spacing: <paramref name="vertical"/> on top/bottom, <paramref name="horizontal"/> on left/right.</summary>
    public GuiThickness(double vertical, double horizontal) : this(vertical, horizontal, vertical, horizontal) { }

    /// <summary>Total horizontal spacing (Left + Right).</summary>
    public double Horizontal => Left + Right;

    /// <summary>Total vertical spacing (Top + Bottom).</summary>
    public double Vertical => Top + Bottom;
}
