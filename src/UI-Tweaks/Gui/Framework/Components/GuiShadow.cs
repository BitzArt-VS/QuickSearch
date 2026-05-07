using Cairo;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Draws a soft drop shadow behind its bounds — a stack of offset, decreasing-alpha
/// rounded rectangles that fan out toward the bottom-right. Used both as a standalone
/// component (wraps <see cref="Content"/> so the children render on top of the shadow)
/// and as a static helper via <see cref="Draw"/> for call sites that want the shadow
/// without participating in the render tree (e.g. <see cref="GuiSlider"/>'s handle).
/// <para>
/// The shadow is a sibling-of-content effect, not a CSS-style outer glow that grows the
/// element: it's painted within the component's own bounds, simply offset toward the
/// bottom-right. To leave room for the spread, give the parent enough padding —
/// <see cref="GuiSlider"/>, for example, sizes its handle so the shadow fits inside the
/// track row.
/// </para>
/// </summary>
public sealed class GuiShadow : GuiComponent
{
    /// <summary>Number of stacked shadow layers. More steps = softer falloff at the
    /// cost of fill rate. Default <c>3</c>.</summary>
    public int Steps { get; set; } = 3;

    /// <summary>Per-step offset toward the bottom-right, in logical pixels. The total
    /// visible offset is <c>Steps * Offset</c>. Default <c>1.0</c>.</summary>
    public double Offset { get; set; } = 1.0;

    /// <summary>Per-step spread (outward growth on every side), in logical pixels. The
    /// outermost layer is <c>Steps * Spread</c> larger than the bounds on each side.
    /// Default <c>0.5</c>.</summary>
    public double Spread { get; set; } = 0.5;

    /// <summary>Maximum alpha — used by the innermost (sharpest) layer. Successive
    /// outer layers fade linearly toward zero. Default <c>0.18</c>.</summary>
    public double Alpha { get; set; } = 0.18;

    /// <summary>Corner radius of the shadow rectangles, in logical pixels. Default
    /// <see cref="GuiVanillaStyle.ElementBgRadius"/> (1).</summary>
    public double Radius { get; set; } = GuiVanillaStyle.ElementBgRadius;

    /// <summary>Optional nested render fragment. When set, its declared subtree becomes
    /// the shadow's children — rendered after the shadow so they sit on top of it.</summary>
    public GuiRenderFragment? Content { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        Content?.Invoke(builder);
    }

    /// <inheritdoc/>
    public override void Render(Context ctx, GuiComponentBounds bounds)
    {
        Draw(ctx, bounds.X, bounds.Y, bounds.Width, bounds.Height,
             Steps, Offset, Spread, Alpha, Radius);
    }

    /// <summary>
    /// Draws a drop shadow into <paramref name="ctx"/> behind the rectangle
    /// <c>(x, y, w, h)</c>. Layers are painted from outermost (faintest) to innermost
    /// (sharpest) so the result composites correctly into a single soft shape.
    /// </summary>
    internal static void Draw(
        Context ctx,
        double x, double y, double w, double h,
        int steps, double offset, double spread, double alpha, double radius)
    {
        if (steps <= 0 || alpha <= 0) return;

        for (int i = steps; i >= 1; i--)
        {
            double off = i * offset;
            double sp  = i * spread;
            // Innermost layer (i==1) is the sharpest and gets the full alpha; outer
            // layers fade linearly. Using (steps - i + 1) / steps keeps the sum bounded
            // while letting the silhouette stay soft.
            double a = alpha * (steps - i + 1) / steps;
            ctx.RoundRect(x - sp + off, y - sp + off, w + 2 * sp, h + 2 * sp, radius);
            ctx.SetSourceRGBA(0, 0, 0, a);
            ctx.Fill();
        }
    }
}
