namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// The final arranged bounds of a component — its position and size within the rendered surface.
/// All values are in logical (Cairo) pixels.
/// </summary>
public readonly record struct GuiComponentBounds(double X, double Y, double Width, double Height)
{
    public static readonly GuiComponentBounds Empty = new(0, 0, 0, 0);

    public double Right => X + Width;
    public double Bottom => Y + Height;
}
