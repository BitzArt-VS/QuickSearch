using Cairo;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A leaf component that fills its bounds with a solid <see cref="Color"/>.
/// Both axes default to <see cref="GuiSizeMode.FitContent"/> — override via fluent
/// extensions or set explicit <c>width</c>/<c>height</c> at the call site to produce a
/// fixed-size filled rectangle.
/// </summary>
public class GuiRectangle : GuiComponent
{
    /// <summary>Fill colour. Defaults to <see cref="GuiColor.Transparent"/> — a no-op draw.</summary>
    public GuiColor Color { get; set; }

    public override void Render(Context context, GuiComponentBounds bounds)
    {
        if (Color.A <= 0)
        {
            return;
        }

        context.SetSourceRGBA(Color.R, Color.G, Color.B, Color.A);
        context.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
        context.Fill();
    }
}
