using Cairo;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A leaf component that renders a single line of text using <see cref="GuiFontStyle"/>.
/// Overrides <see cref="Measure"/> to return the text's intrinsic dimensions, combining
/// with the <see cref="GuiSizeMode.FitContent"/> default to size the component naturally.
/// </summary>
public sealed class GuiLabel : GuiComponent
{
    /// <summary>The text to display.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// The font style used to render <see cref="Text"/>.
    /// Defaults to <see cref="GuiFontStyle.Default"/>.
    /// </summary>
    public GuiFontStyle Font { get; set; } = GuiFontStyle.Default;

    // ── IGuiComponent ─────────────────────────────────────────────────────

    public override GuiMeasuredSize Measure(double availableWidth, double availableHeight)
    {
        if (string.IsNullOrEmpty(Text))
        {
            return default;
        }

        return Font.Measure(Text);
    }

    public override void Render(Context context, GuiComponentBounds bounds)
    {
        // DrawText handles the physical-pixel CTM dance required for vanilla-style hinting.
        context.DrawText(Text, Font, bounds.X, bounds.Y);
    }
}
