using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Framework-side mirror of vanilla <see cref="GuiStyle"/>. Exposes the most useful
/// vanilla colours and dimensions as <see cref="GuiColor"/> / numeric constants so
/// components can match the look of vanilla composer-based dialogs without depending
/// on raw <c>double[]</c> arrays.
/// <para>
/// All values are pulled directly from vanilla's <see cref="GuiStyle"/> at type-init
/// time; there is no copy that can drift out of sync.
/// </para>
/// </summary>
public static class GuiVanillaStyle
{
    // ── Backgrounds ─────────────────────────────────────────────────────────

    /// <summary>The default ("light") background colour for dialogs (#403529, alpha 0.75).</summary>
    public static readonly GuiColor DialogLightBgColor    = FromVanilla(GuiStyle.DialogLightBgColor);
    /// <summary>The default background colour for dialogs (#403529, alpha 0.8).</summary>
    public static readonly GuiColor DialogDefaultBgColor  = FromVanilla(GuiStyle.DialogDefaultBgColor);
    /// <summary>The fully opaque ("strong") background colour for dialogs (#403529).</summary>
    public static readonly GuiColor DialogStrongBgColor   = FromVanilla(GuiStyle.DialogStrongBgColor);
    /// <summary>The default dialog border colour — black at 30% alpha.</summary>
    public static readonly GuiColor DialogBorderColor     = FromVanilla(GuiStyle.DialogBorderColor);
    /// <summary>The dark inner stroke colour used by vanilla shaded dialog backgrounds (~#2D2321).</summary>
    public static readonly GuiColor DialogShadedStrokeColor = GuiColor.FromRgba(45, 35, 33, 255);

    /// <summary>
    /// The slightly lighter background colour vanilla uses for title bars
    /// (<see cref="GuiStyle.DialogStrongBgColor"/> with each RGB channel × 1.35, original alpha kept).
    /// </summary>
    public static readonly GuiColor DialogTitleBarBgColor = ScaleRgb(FromVanilla(GuiStyle.DialogStrongBgColor), 1.35);

    /// <summary>
    /// The bright highlight stroke colour vanilla uses on the inside of title bars to
    /// produce the bevel effect (<see cref="GuiStyle.DialogLightBgColor"/> RGB × 1.6, alpha = 1).
    /// </summary>
    public static readonly GuiColor DialogTitleBarHighlightColor = ScaleRgb(FromVanilla(GuiStyle.DialogLightBgColor), 1.6, alpha: 1.0);

    // ── Foregrounds ─────────────────────────────────────────────────────────

    /// <summary>The default text colour used by dialog labels (#e9ddce).</summary>
    public static readonly GuiColor DialogDefaultTextColor = FromVanilla(GuiStyle.DialogDefaultTextColor);
    /// <summary>A darker brown text colour (#5a4530).</summary>
    public static readonly GuiColor DarkBrownColor         = FromVanilla(GuiStyle.DarkBrownColor);
    /// <summary>Highlight (hover) colour used on dialog elements (#a88b6c, alpha 0.9).</summary>
    public static readonly GuiColor DialogHighlightColor   = FromVanilla(GuiStyle.DialogHighlightColor);
    /// <summary>An alternate, lighter background colour (#b5aea6, alpha 0.93).</summary>
    public static readonly GuiColor DialogAlternateBgColor = FromVanilla(GuiStyle.DialogAlternateBgColor);

    // ── Buttons ─────────────────────────────────────────────────────────────

    /// <summary>The dark-brown button background fill (#453524, alpha 0.8) — vanilla <c>GuiStyle.ButtonBackColor</c>.</summary>
    public static readonly GuiColor ButtonBackColor       = FromVanilla(GuiStyle.ButtonBackColor);
    /// <summary>The default button text colour (#e0cfbb) — vanilla <c>GuiStyle.ButtonTextColor</c>.</summary>
    public static readonly GuiColor ButtonTextColor       = FromVanilla(GuiStyle.ButtonTextColor);
    /// <summary>The active/hover button text colour (#c58948) — vanilla <c>GuiStyle.ActiveButtonTextColor</c>.</summary>
    public static readonly GuiColor ActiveButtonTextColor = FromVanilla(GuiStyle.ActiveButtonTextColor);

    // ── Status / accent ────────────────────────────────────────────────────

    public static readonly GuiColor SuccessTextColor = FromVanilla(GuiStyle.SuccessTextColor);
    public static readonly GuiColor ErrorTextColor   = FromVanilla(GuiStyle.ErrorTextColor);
    public static readonly GuiColor WarningTextColor = FromVanilla(GuiStyle.WarningTextColor);
    public static readonly GuiColor LinkTextColor    = FromVanilla(GuiStyle.LinkTextColor);

    /// <summary>
    /// The orange/copper colour vanilla rich-text hyperlinks are drawn in. Backed by
    /// <c>GuiStyle.ActiveButtonTextColor</c>, which is what
    /// <c>LinkTextComponent</c> applies to its font in vanilla — vanilla's
    /// <see cref="GuiStyle.LinkTextColor"/> field holds an unrelated blue that
    /// nothing in the shipped UI actually uses for links.
    /// </summary>
    public static readonly GuiColor HyperlinkColor      = FromVanilla(GuiStyle.ActiveButtonTextColor);

    /// <summary>
    /// The brighter hover variant of <see cref="HyperlinkColor"/>. Mirrors vanilla
    /// <c>LinkTextComponent</c>'s hover composition: each RGB channel multiplied by 1.2,
    /// clamped to [0, 1]; alpha preserved.
    /// </summary>
    public static readonly GuiColor HyperlinkHoverColor = ScaleRgb(HyperlinkColor, 1.2);

    // ── Texture asset locations ────────────────────────────────────────────

    /// <summary>The dirt/soil tile vanilla shaded dialog backgrounds use.</summary>
    public static readonly AssetLocation DirtTexture       = GuiElement.dirtTextureName;
    /// <summary>The noisy metal tile used for various accents.</summary>
    public static readonly AssetLocation NoisyMetalTexture = GuiElement.noisyMetalTextureName;
    /// <summary>The oak wood tile used for slot frames.</summary>
    public static readonly AssetLocation WoodTexture       = GuiElement.woodTextureName;
    /// <summary>The stone tile used for engraved-style elements.</summary>
    public static readonly AssetLocation StoneTexture      = GuiElement.stoneTextureName;
    /// <summary>The water tile used for liquid containers and similar.</summary>
    public static readonly AssetLocation WaterTexture      = GuiElement.waterTextureName;
    /// <summary>The sign-paper tile used for text overlays.</summary>
    public static readonly AssetLocation PaperTexture      = GuiElement.paperTextureName;

    // ── Dimensions ──────────────────────────────────────────────────────────

    /// <summary>The vanilla title bar height in logical pixels (31).</summary>
    public const double TitleBarHeight        = 31;
    /// <summary>The vanilla dialog background corner radius in logical pixels (1).</summary>
    public const double DialogBgRadius        = 1;
    /// <summary>The vanilla element background corner radius in logical pixels (1).</summary>
    public const double ElementBgRadius       = 1;
    /// <summary>The vanilla padding between an element and its enclosing dialog (20).</summary>
    public const double ElementToDialogPadding = 20;
    /// <summary>The vanilla padding between a dialog and the screen edge (10).</summary>
    public const double DialogToScreenPadding = 10;
    /// <summary>Half of the vanilla element padding (5).</summary>
    public const double HalfPadding           = 5;

    // ── Input mark / fill ────────────────────────────────────────────────────

    /// <summary>
    /// The framework's primary accent — a steel / weathered-iron blue
    /// (sRGB <c>#7AA0C2</c>, H≈210° / S≈32% / L≈60%). Used as a single design token for
    /// "this control is active": checkbox tick fill, slider filled track (composited at
    /// lower alpha — see <see cref="SliderFillColor"/>), and similar inputs.
    /// <para>
    /// <b>Hue rationale.</b> The dialog palette is warm/earthy, so the accent has to
    /// sit on the cool side of the wheel for real chromatic contrast — neutral whites
    /// and analogous warms (copper, brass, sage) all read as "more dialog" rather than
    /// "active mark." We pick a desaturated true blue (210°) rather than vanilla's
    /// near-cyan switch blue: the lower saturation (~32% vs vanilla's ~70%) keeps it
    /// from screaming, and is in line with mature UI accent tokens (GitHub primary,
    /// VS Code accent, macOS system blue), which all live in the 30–45% saturation
    /// band for exactly this reason. Thematically this hue reads as cold steel /
    /// tarnished iron / distant water — all materials the game's setting evokes.
    /// </para>
    /// </summary>
    public static readonly GuiColor SwitchMarkColor =
        GuiColor.FromHex("#7AAEC2");

    /// <summary>
    /// The fill colour the framework uses for the active / filled portion of a slider
    /// track — the same hue as <see cref="SwitchMarkColor"/> but at much lower alpha.
    /// <para>
    /// A slider fill is a wide flat region; at full opacity any saturated accent reads
    /// as a loud stripe. At ~40% alpha the steel blue instead composites with the dark
    /// inset behind it as a muted slate tint, which reads as "this portion of the track
    /// is active" without dominating the dialog. Single-hue, two intensities is a
    /// common pattern in mature design systems (Material's primary vs primary container,
    /// Tailwind's 500 vs 200) for exactly this small-mark-vs-large-fill problem.
    /// </para>
    /// </summary>
    public static readonly GuiColor SliderFillColor =
        GuiColor.FromRgba(SwitchMarkColor.R, SwitchMarkColor.G, SwitchMarkColor.B, 0.5);

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static GuiColor FromVanilla(double[] rgba) =>
        GuiColor.FromRgba(rgba[0], rgba[1], rgba[2], rgba[3]);

    /// <summary>Scales <paramref name="c"/>'s RGB channels by <paramref name="factor"/>, clamped to [0, 1]. Alpha is replaced when <paramref name="alpha"/> is supplied, otherwise preserved.</summary>
    private static GuiColor ScaleRgb(GuiColor c, double factor, double? alpha = null) =>
        GuiColor.FromRgba(
            System.Math.Clamp(c.R * factor, 0.0, 1.0),
            System.Math.Clamp(c.G * factor, 0.0, 1.0),
            System.Math.Clamp(c.B * factor, 0.0, 1.0),
            alpha ?? c.A);
}
