using System;
using Cairo;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Draws the vanilla "inset" visual — an optional dark brightness overlay followed by a
/// multi-pass embossed (recessed) rounded-rectangle border — matching vanilla's
/// <c>GuiElementInset</c> / <c>GuiComposer.AddInset</c>. The visual is always painted as
/// a <em>background</em>: <see cref="Content"/> children render on top of the emboss
/// ring, just like any other component's children render on top of its background fill.
/// </summary>
public sealed class GuiInset : GuiComponent
{
    /// <summary>
    /// Number of emboss passes (ring depth). Each pass contracts the ring by one physical
    /// pixel and decreases its alpha. Default <c>4</c> — matches vanilla's
    /// <c>AddInset</c> default.
    /// </summary>
    public int Depth { get; set; } = 4;

    /// <summary>
    /// Brightness of the fill overlay. When less than 1 the bounds are first filled with
    /// a semi-transparent black layer of alpha <c>(1 − Brightness)</c>. Default
    /// <c>0.85f</c> — matches vanilla's <c>AddInset</c> default.
    /// </summary>
    public float Brightness { get; set; } = 0.85f;

    /// <summary>
    /// Corner radius of the emboss ring in logical pixels. Default
    /// <see cref="GuiVanillaStyle.ElementBgRadius"/> (1).
    /// </summary>
    public double Radius { get; set; } = GuiVanillaStyle.ElementBgRadius;

    /// <summary>
    /// Optional nested render fragment. When set, its declared subtree becomes the inset's
    /// children — rendered after the inset's own background (brightness overlay + emboss
    /// ring) so they appear inside the recessed frame.
    /// </summary>
    public GuiRenderFragment? Content { get; set; }

    /// <inheritdoc/>
    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        Content?.Invoke(builder);
    }

    /// <inheritdoc/>
    public override void Render(Context ctx, GuiComponentBounds bounds)
    {
        Draw(ctx, bounds, Depth, Brightness, Radius);
    }

    /// <summary>
    /// Draws an inset visual (brightness overlay + emboss ring) directly into
    /// <paramref name="ctx"/>. Used both by the component's own <see cref="Render"/> and
    /// by call sites that need the inset look without participating in the render tree
    /// (e.g. the scrollbar track in <see cref="GuiContainer"/>).
    /// <para>
    /// When <paramref name="raised"/> is true the emboss is inverted — highlight on the
    /// top-left, shadow on the bottom-right — producing a button-like raised appearance
    /// instead of the default recessed one. The brightness overlay is skipped in that
    /// mode (a raised surface should not be darkened relative to its parent).
    /// </para>
    /// </summary>
    internal static void Draw(
        Context ctx, GuiComponentBounds bounds,
        int depth, float brightness, double radius,
        bool raised = false)
    {
        if (!raised && brightness < 1f)
        {
            ctx.SetSourceRGBA(0, 0, 0, 1.0 - brightness);
            ctx.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            ctx.Fill();
        }

        DrawEmboss(ctx, bounds.X, bounds.Y, bounds.Width, bounds.Height, radius, depth, raised);
    }

    // ── Emboss ─────────────────────────────────────────────────────────────────
    // Ported from vanilla GuiElement.EmbossRoundRectangle (intensity = 0.7,
    // lightDarkBalance = 0.8, alphaOffset = 0.25). Inverse mode flips the balance
    // to 2−0.8 = 1.2 and swaps highlight/shadow colors.
    //
    // Vanilla uses physical-pixel coords (CTM unscaled); here the CTM is already
    // pre-scaled by GUIScale, so line widths and per-pass contraction steps are
    // expressed as 1/GUIScale logical units per physical pixel.
    private static void DrawEmboss(
        Context ctx,
        double x, double y,
        double w, double h,
        double radius,
        int depth,
        bool raised)
    {
        const double intensity        = 0.7;
        const double alphaOffset      = 0.25;
        const double degrees          = Math.PI / 180.0;

        // Recessed: top-left = shadow (black), bottom-right = highlight (white).
        // Raised:   top-left = highlight (white), bottom-right = shadow (black).
        double lightDarkBalance = raised ? 0.8 : 1.2;
        double tlR = raised ? 1 : 0, tlG = tlR, tlB = tlR;
        double brR = raised ? 0 : 1, brG = brR, brB = brR;

        double px = 1.0 / RuntimeEnv.GUIScale;  // 1 physical pixel in logical units

        ctx.Antialias = Antialias.Best;

        for (int pass = 0; pass < depth; pass++)
        {
            double fac = intensity * (depth - pass) / depth;

            // Top-left arcs.
            ctx.NewPath();
            ctx.Arc(x + radius,     y + h - radius, radius, 135 * degrees, 180 * degrees);
            ctx.Arc(x + radius,     y + radius,     radius, 180 * degrees, 270 * degrees);
            ctx.Arc(x + w - radius, y + radius,     radius, -90 * degrees, -45 * degrees);
            double tlAlpha = Math.Max(0.0, Math.Min(1.0, lightDarkBalance * fac) - alphaOffset);
            ctx.SetSourceRGBA(tlR, tlG, tlB, tlAlpha);
            ctx.LineWidth = px;
            ctx.Stroke();

            // Bottom-right arcs.
            ctx.NewPath();
            ctx.Arc(x + w - radius, y + radius,     radius, -45 * degrees,   0 * degrees);
            ctx.Arc(x + w - radius, y + h - radius, radius,   0 * degrees,  90 * degrees);
            ctx.Arc(x + radius,     y + h - radius, radius,  90 * degrees, 135 * degrees);
            double brAlpha = Math.Max(0.0, Math.Min(1.0, (2.0 - lightDarkBalance) * fac) - alphaOffset);
            ctx.SetSourceRGBA(brR, brG, brB, brAlpha);
            ctx.LineWidth = px;
            ctx.Stroke();

            // Contract the ring by 1 physical pixel on every side for the next pass
            x += px; y += px;
            w -= 2 * px; h -= 2 * px;
        }
    }
}
