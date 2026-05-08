using Cairo;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A container that paints the vanilla "hover text" tooltip body — solid fill, rounded corners,
/// and a thin outer stroke. Defaults match vanilla
/// <c>GuiElementHoverText.DefaultBackground</c>: <see cref="GuiVanillaStyle.DialogStrongBgColor"/>
/// fill, <see cref="GuiVanillaStyle.DialogBorderColor"/> stroke at width 3, radius 1, with
/// 5px content padding.
/// <para>
/// Used as the automatic wrapper around a <see cref="GuiTooltip"/>'s tooltip content;
/// users can customise it via the <c>configureBackground</c> parameter on
/// <c>AddTooltip</c>, or replace it entirely by composing their own container inside the
/// tooltip fragment.
/// </para>
/// </summary>
public class GuiTooltipBackground : GuiContainer
{
    /// <summary>Vanilla tooltip default content padding (5 logical pixels).</summary>
    public const double DefaultPadding = 5;

    /// <summary>The base fill colour. Default: vanilla dialog strong bg.</summary>
    public GuiColor FillColor { get; set; } = GuiVanillaStyle.DialogStrongBgColor;

    /// <summary>Outer border colour. Default: vanilla dialog border (black 30% alpha).</summary>
    public GuiColor BorderColor { get; set; } = GuiVanillaStyle.DialogBorderColor;

    /// <summary>
    /// Outer border stroke width in <b>physical</b> pixels. Default 3 — matches vanilla's
    /// <c>TextBackground.BorderWidth</c>. Divided by <see cref="Vintagestory.API.Common.RuntimeEnv.GUIScale"/>
    /// at draw time because the framework's CTM is pre-scaled by GUIScale.
    /// </summary>
    public double BorderWidth { get; set; } = 3;

    /// <summary>Corner radius in logical pixels. Default 1 — vanilla tooltip radius.</summary>
    public double Radius { get; set; } = GuiVanillaStyle.ElementBgRadius;

    protected override void DrawBackground(Context ctx, GuiComponentBounds bounds)
    {
        double sw = BorderWidth / RuntimeEnv.GUIScale;

        ctx.RoundRect(bounds, Radius);
        ctx.FillSolid(FillColor, preserve: true);
        ctx.StrokeSolid(BorderColor, sw);
    }
}
