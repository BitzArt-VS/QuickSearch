namespace BitzArt.UI.Tweaks.Gui;

internal readonly struct ResizeRegion
{
    public readonly GuiComponentBounds Bounds;
    public readonly object Token;
    public readonly IGuiResizable Target;

    public ResizeRegion(GuiComponentBounds bounds, object token, IGuiResizable target)
    {
        Bounds = bounds;
        Token = token;
        Target = target;
    }

    public bool Contains(double x, double y) =>
        x >= Bounds.X && x < Bounds.Right &&
        y >= Bounds.Y && y < Bounds.Bottom;
}
