using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Describes how text should be rendered. Lightweight value type — copy freely.
/// <para>
/// <b>Sizes are specified in logical (unscaled) pixels.</b> <see cref="Apply"/> internally
/// pre-multiplies the font size by <c>RuntimeEnv.GUIScale</c>, so the context the font is
/// applied to must be in <i>physical</i>-pixel user-space when text is drawn — exactly like
/// vanilla <c>CairoFont</c>. <see cref="GuiLabel"/> achieves this by resetting the CTM with
/// <c>ctx.IdentityMatrix()</c> for the duration of the text draw.
/// </para>
/// <para>
/// Does not depend on <c>CairoFont</c> or any other vanilla text infrastructure, but
/// deliberately mirrors its rendering setup so output quality matches the rest of the game.
/// </para>
/// </summary>
public readonly record struct GuiFontStyle
{
    // Shared surface + context used only for text measurement. Never drawn to screen.
    private static readonly ImageSurface _measureSurface = new(Format.Argb32, 1, 1);
    private static readonly Context _measureContext = new(_measureSurface);
    private static readonly FontOptions _fontOptions = CreateFontOptions();

    private static FontOptions CreateFontOptions()
    {
        // Match vanilla CairoFont.SetupContext: only Antialias is set, hinting is left at
        // Cairo's defaults (HintMetrics.On / HintStyle.Default). With the font matrix in
        // physical-pixel space, default hinting snaps stems and advance widths to the
        // pixel grid for crisp metrics. Subpixel AA then softens edges horizontally with
        // RGB-channel coverage — producing the slightly fuller, smoother look that
        // matches the rest of the game's text.
        // Antialias.Best is avoided because it breaks fonts on Linux (per vanilla comment).
        var opts = new FontOptions
        {
            Antialias = Antialias.Subpixel
        };

        return opts;
    }

    // ── Properties ────────────────────────────────────────────────────────

    public string Fontname { get; init; }
    public double Size { get; init; }
    public FontWeight Weight { get; init; }
    public FontSlant Slant { get; init; }
    public GuiColor Color { get; init; }

    /// <summary>
    /// When <c>true</c>, <see cref="GuiLabel"/> calls <c>ShowText</c> twice in succession,
    /// thickening glyphs by overlapping their AA fringe with itself. Mirrors vanilla
    /// <c>CairoFont.RenderTwice</c>. Off by default.
    /// </summary>
    public bool RenderTwice { get; init; }

    // ── Constructors ──────────────────────────────────────────────────────

    /// <summary>
    /// Initialises a <see cref="GuiFontStyle"/> with sensible defaults:
    /// <c>sans-serif</c>, 13 px, normal weight, white.
    /// </summary>
    public GuiFontStyle()
    {
        Fontname = GuiStyle.StandardFontName;
        Size = 16;
        Weight = FontWeight.Normal;
        Slant = FontSlant.Normal;
        Color = GuiVanillaStyle.DialogDefaultTextColor;
    }

    // ── Context application ───────────────────────────────────────────────

    /// <summary>
    /// Configures <paramref name="ctx"/> for text rendering using this style. The font size
    /// is pre-multiplied by <c>RuntimeEnv.GUIScale</c>, so <paramref name="ctx"/> must be in
    /// physical-pixel user-space when text is drawn (matches vanilla <c>CairoFont</c>).
    /// </summary>
    public void Apply(Context ctx)
    {
        ctx.SelectFontFace(Fontname, Slant, Weight);
        ctx.SetFontSize(Size * RuntimeEnv.GUIScale);
        ctx.FontOptions = _fontOptions;
        ctx.SetSourceRGBA(Color.R, Color.G, Color.B, Color.A);
    }

    // ── Measurement ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns the advance width and line height of <paramref name="text"/> in logical pixels.
    /// Thread-unsafe — call from the render thread only.
    /// </summary>
    public GuiLayoutSize Measure(string text)
    {
        Apply(_measureContext);
        var te = _measureContext.TextExtents(text);
        var fe = _measureContext.FontExtents;
        // Apply scales font size by GUIScale, so extents come back in physical pixels.
        // Convert back to logical pixels for the layout system.
        double inv = 1.0 / RuntimeEnv.GUIScale;
        return new GuiLayoutSize(te.XAdvance * inv, fe.Height * inv);
    }

    /// <summary>Returns the line height of this font in logical pixels.</summary>
    public double MeasureHeight()
    {
        Apply(_measureContext);
        return _measureContext.FontExtents.Height / RuntimeEnv.GUIScale;
    }

    // ── Presets ───────────────────────────────────────────────────────────

    /// <summary>16 px sans-serif, white.</summary>
    public static GuiFontStyle Default => new();

    /// <summary>14 px sans-serif, white.</summary>
    public static GuiFontStyle Small => new() { Size = 14 };

    /// <summary>14 px sans-serif, white, bold.</summary>
    public static GuiFontStyle SmallBold => new() { Size = 14, Weight = FontWeight.Bold };

    /// <summary>16 px sans-serif, white.</summary>
    public static GuiFontStyle Medium => Default;

    /// <summary>16 px sans-serif, white, bold.</summary>
    public static GuiFontStyle MediumBold => new() { Weight = FontWeight.Bold };

    /// <summary>18 px sans-serif, white.</summary>
    public static GuiFontStyle Large => new() { Size = 18 };

    /// <summary>18 px sans-serif, white, bold.</summary>
    public static GuiFontStyle LargeBold => new() { Size = 18, Weight = FontWeight.Bold };
}
