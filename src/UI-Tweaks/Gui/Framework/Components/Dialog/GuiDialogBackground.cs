using Cairo;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A container that paints the vanilla "shaded dialog" body — solid fill, dirt-texture
/// overlay, and a thin dark outer stroke.
/// </summary>
public class GuiDialogBackground : GuiContainer
{
    /// <summary>The base fill colour painted under the texture. Default: vanilla dialog strong bg.</summary>
    public GuiColor FillColor { get; set; } = GuiVanillaStyle.DialogStrongBgColor;

    /// <summary>The tiled texture painted on top of the fill. Default: vanilla dirt/soil tile.</summary>
    public AssetLocation Texture { get; set; } = GuiVanillaStyle.DirtTexture;

    /// <summary>Alpha multiplier (0–255) applied to <see cref="Texture"/>. Default: 64 (vanilla).</summary>
    public byte TextureAlpha { get; set; } = 64;

    /// <summary>Pattern scale for <see cref="Texture"/>. Default: 0.125 (vanilla).</summary>
    public float TextureScale { get; set; } = 0.125f;

    /// <summary>Outer border colour. Default: vanilla shaded dark stroke (~#2D2321).</summary>
    public GuiColor BorderColor { get; set; } = GuiVanillaStyle.DialogShadedStrokeColor;

    /// <summary>
    /// Outer border alpha (multiplied with <see cref="BorderColor"/>'s own alpha).
    /// Default <c>0.5625</c> — vanilla's <c>AddShadedDialogBG</c> renders the dark stroke
    /// at <c>alpha * alpha</c> of the caller-supplied alpha (default 0.75), so 0.5625 is
    /// the as-shipped vanilla look.
    /// </summary>
    public double BorderAlpha { get; set; } = 0.5625;

    /// <summary>
    /// Outer border stroke width in <b>physical</b> pixels. Default 2.
    /// <para>
    /// Vanilla's <c>AddShadedDialogBG</c> passes <c>strokeWidth = 5</c>, but it also applies
    /// <c>BlurPartial</c> after the highlight pass and re-paints the texture over the
    /// highlight before stroking — both effects soften the dark border so its visible
    /// thickness reads thinner than the 5px would suggest. We can't reproduce the blur
    /// (the framework's render path doesn't expose the underlying <see cref="ImageSurface"/>
    /// to component draw hooks), so the default is reduced to 2 to match vanilla's
    /// on-screen weight.
    /// </para>
    /// <para>
    /// At draw time this value is divided by <see cref="RuntimeEnv.GUIScale"/> because the
    /// framework's CTM is pre-scaled by <c>GUIScale</c>; the on-screen pixel count is the
    /// same regardless of the user's GUI-scale setting — exactly like vanilla.
    /// </para>
    /// </summary>
    public double StrokeWidth { get; set; } = 2;

    /// <summary>Corner radius for the rounded rectangle, in logical pixels. Default <see cref="GuiVanillaStyle.DialogBgRadius"/>.</summary>
    public double Radius { get; set; } = GuiVanillaStyle.DialogBgRadius;

    protected override void DrawBackground(Context ctx, GuiComponentBounds bounds)
    {
        if (RenderHandle is null) return;
        var capi = RenderHandle.ClientApi;

        double sw = StrokeWidth / RuntimeEnv.GUIScale;

        // Path is flush with bounds — no inset. Cairo strokes straddle the path, so the
        // outer half is clipped by the dialog surface boundary on edges that touch it,
        // leaving the visible border at half the stroke width on those edges. This
        // matches vanilla's behaviour. When a sibling component (typically a
        // <see cref="GuiVanillaDialogTitleBar"/>) sits above us, the top edge stroke
        // straddles the join — both halves visible — forming the separator at the full
        // stroke width.

        // 1. Solid fill — establishes the base colour the texture tints.
        ctx.RoundRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, Radius);
        ctx.FillSolid(FillColor, preserve: true);

        // 2. Texture overlay — same path, source pattern, fill again on top of solid.
        ctx.Operator = Operator.Over;
        ctx.FillPattern(capi, Texture, TextureAlpha, TextureScale, preserve: true);

        // 3. Dark outer stroke — full closed rect.
        var border = GuiColor.FromRgba(BorderColor.R, BorderColor.G, BorderColor.B, BorderColor.A * BorderAlpha);
        ctx.StrokeSolid(border, sw);
    }
}
