using Cairo;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Low-level Cairo drawing primitives used by higher-level components. Each method does
/// one thing — append a path, fill, stroke, or set a pattern source — so callers can
/// freely compose them into more elaborate visuals (shaded backgrounds, borders, slot
/// frames, etc.) without re-implementing path math or pattern caching.
/// <para>
/// All coordinates are in <b>logical pixels</b> (the framework's CTM is pre-scaled by
/// <c>RuntimeEnv.GUIScale</c>). Patterns loaded via <see cref="SetPatternSource"/>
/// counter-scale automatically so they appear at the same density as in vanilla.
/// </para>
/// </summary>
public static class CairoContextExtensions
{
    // ── Paths ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a rounded rectangle to the current path. Mirrors vanilla
    /// <c>GuiElement.RoundRectangle</c>: when <paramref name="radius"/> is &lt;= 0 a plain
    /// rectangle is appended.
    /// </summary>
    public static void RoundRect(this Context ctx, double x, double y, double w, double h, double radius)
    {
        if (radius <= 0)
        {
            ctx.Rectangle(x, y, w, h);
            return;
        }

        const double degrees = Math.PI / 180.0;
        ctx.NewSubPath();
        ctx.Arc(x + w - radius, y + radius, radius, -90 * degrees, 0 * degrees);
        ctx.Arc(x + w - radius, y + h - radius, radius, 0 * degrees, 90 * degrees);
        ctx.Arc(x + radius, y + h - radius, radius, 90 * degrees, 180 * degrees);
        ctx.Arc(x + radius, y + radius, radius, 180 * degrees, 270 * degrees);
        ctx.ClosePath();
    }

    /// <summary>Appends a rounded rectangle covering <paramref name="bounds"/> to the current path.</summary>
    public static void RoundRect(this Context ctx, GuiComponentBounds bounds, double radius)
        => ctx.RoundRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, radius);

    /// <summary>
    /// Appends an "open" rectangular path that omits one of the four sides — useful when
    /// stacking panels whose shared edge is drawn by a sibling. Mirrors vanilla title-bar
    /// stroke geometry (left + top + right, bottom open).
    /// </summary>
    /// <param name="open">Which side of the rectangle to leave open.</param>
    public static void OpenRect(this Context ctx, double x, double y, double w, double h, GuiSide open)
    {
        ctx.NewPath();
        // Walk the perimeter starting opposite the open side, so the open edge is the last
        // (un-drawn) segment.
        switch (open)
        {
            case GuiSide.Bottom:
                ctx.MoveTo(x, y + h);
                ctx.LineTo(x, y);
                ctx.LineTo(x + w, y);
                ctx.LineTo(x + w, y + h);
                break;
            case GuiSide.Top:
                ctx.MoveTo(x, y);
                ctx.LineTo(x, y + h);
                ctx.LineTo(x + w, y + h);
                ctx.LineTo(x + w, y);
                break;
            case GuiSide.Right:
                ctx.MoveTo(x + w, y);
                ctx.LineTo(x, y);
                ctx.LineTo(x, y + h);
                ctx.LineTo(x + w, y + h);
                break;
            case GuiSide.Left:
                ctx.MoveTo(x, y);
                ctx.LineTo(x + w, y);
                ctx.LineTo(x + w, y + h);
                ctx.LineTo(x, y + h);
                break;
        }
    }

    /// <summary>
    /// Appends a single-edge straight-line segment to the current path — the opposite of
    /// <see cref="OpenRect"/>. Useful for one-sided highlights (top-only bevel, bottom-only
    /// shadow, etc.).
    /// </summary>
    public static void EdgeLine(this Context ctx, double x, double y, double w, double h, GuiSide side)
    {
        ctx.NewPath();
        switch (side)
        {
            case GuiSide.Top: ctx.MoveTo(x, y); ctx.LineTo(x + w, y); break;
            case GuiSide.Bottom: ctx.MoveTo(x, y + h); ctx.LineTo(x + w, y + h); break;
            case GuiSide.Left: ctx.MoveTo(x, y); ctx.LineTo(x, y + h); break;
            case GuiSide.Right: ctx.MoveTo(x + w, y); ctx.LineTo(x + w, y + h); break;
        }
    }

    // ── Solid colour fills / strokes ──────────────────────────────────────────

    /// <summary>Fills the current path with <paramref name="color"/>; preserves the path when requested.</summary>
    public static void FillSolid(this Context ctx, GuiColor color, bool preserve = false)
    {
        ctx.SetSourceRGBA(color.R, color.G, color.B, color.A);
        if (preserve)
        {
            ctx.FillPreserve();
        }
        else
        {
            ctx.Fill();
        }
    }

    /// <summary>Strokes the current path with <paramref name="color"/> at <paramref name="width"/> logical pixels.</summary>
    public static void StrokeSolid(this Context ctx, GuiColor color, double width, bool preserve = false)
    {
        ctx.SetSourceRGBA(color.R, color.G, color.B, color.A);
        ctx.LineWidth = width;
        if (preserve)
        {
            ctx.StrokePreserve();
        }
        else
        {
            ctx.Stroke();
        }
    }

    // ── Linear-gradient fills ─────────────────────────────────────────────────

    /// <summary>
    /// Fills the current path with a vertical linear gradient running from
    /// <paramref name="topColor"/> at <paramref name="y0"/> to <paramref name="bottomColor"/>
    /// at <paramref name="y1"/>. Useful for soft fades / approximated halos when surface
    /// blur is unavailable.
    /// </summary>
    public static void FillVerticalGradient(this Context ctx, double y0, double y1, GuiColor topColor, GuiColor bottomColor, bool preserve = false)
    {
        using var grad = new LinearGradient(0, y0, 0, y1);
        grad.AddColorStop(0, new Color(topColor.R, topColor.G, topColor.B, topColor.A));
        grad.AddColorStop(1, new Color(bottomColor.R, bottomColor.G, bottomColor.B, bottomColor.A));
        ctx.SetSource(grad);
        if (preserve)
        {
            ctx.FillPreserve();
        }
        else
        {
            ctx.Fill();
        }
    }

    // ── Surface-pattern source ────────────────────────────────────────────────

    // Framework-private pattern cache, separate from vanilla's GuiElement.cachedPatterns.
    // We keep our own so we can rewrite the pattern matrix to match the framework's
    // logical-pixel CTM without polluting vanilla's shared instances (which would alter
    // the on-screen tile size of any vanilla element drawing the same texture).
    // Keyed by (asset, mulAlpha, scale) — same dimensions vanilla keys by.
    private static readonly System.Collections.Generic.Dictionary<(AssetLocation Loc, byte Alpha, float Scale), SurfacePattern> _patternCache = [];

    // Reflection handle for GuiElement.getImageSurfaceFromAsset(ICoreClientAPI, AssetLocation, int).
    // Resolved via reflection so this assembly does not need a SkiaSharp reference — direct
    // overload resolution against the static method set forces the compiler to load the
    // SKBitmap-typed overloads, which would require a SkiaSharp PackageReference on every
    // consumer. Reflection sidesteps the C# overload-resolution machinery entirely.
    private static readonly MethodInfo _getImageSurfaceFromAsset = typeof(GuiElement)
        .GetMethod(
            nameof(GuiElement.getImageSurfaceFromAsset),
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: [typeof(ICoreClientAPI), typeof(AssetLocation), typeof(int)],
            modifiers: null)
        ?? throw new InvalidOperationException(
            "GuiElement.getImageSurfaceFromAsset(ICoreClientAPI, AssetLocation, int) not found — VSAPI ABI changed?");

    /// <summary>
    /// Sets the source on <paramref name="ctx"/> to a tiled <see cref="SurfacePattern"/>
    /// loaded from <paramref name="textureLoc"/>. Lazily builds the underlying surface
    /// from vanilla's <c>GuiElement.getImageSurfaceFromAsset</c> (so we still benefit from
    /// vanilla's BitmapExternal pipeline), but caches the wrapping <see cref="SurfacePattern"/>
    /// in a framework-private dictionary so the pattern matrix can be tuned to our
    /// logical-pixel CTM without affecting other dialogs that use the same texture.
    /// </summary>
    /// <param name="ctx">Cairo context to configure.</param>
    /// <param name="capi">Client API used to resolve the asset.</param>
    /// <param name="textureLoc">Asset location of the pattern bitmap.</param>
    /// <param name="mulAlpha">Multiplier (0–255) applied to the bitmap's alpha channel.</param>
    /// <param name="scale">Pattern scale in vanilla terms (1.0 = native, 0.125 = 1/8 size).</param>
    public static void SetPatternSource(this Context ctx, ICoreClientAPI capi, AssetLocation textureLoc, byte mulAlpha = 255, float scale = 1f)
    {
        var key = (textureLoc, mulAlpha, scale);
        if (!_patternCache.TryGetValue(key, out var pattern) || !pattern.HandleValid)
        {
            // Build the underlying ImageSurface via vanilla's helper (invoked by reflection
            // so this assembly does not need a SkiaSharp reference; see _getImageSurfaceFromAsset).
            // The surface is *not* registered with vanilla's GuiElement.cachedPatterns
            // dictionary — we wrap it in our own SurfacePattern so mutating Filter/Matrix
            // below cannot leak back into vanilla dialogs.
            //
            // NOTE: do NOT call GuiElement.getPattern(doCache:false) — that overload still
            // returns the existing cached SurfacePattern when one is present for this key,
            // so mutating its Filter (e.g. to Filter.Good) silently changed the appearance
            // of every vanilla dialog drawing the same texture.
            var surface = (ImageSurface)_getImageSurfaceFromAsset.Invoke(null, [capi, textureLoc, (int)mulAlpha])!;
            pattern = new SurfacePattern(surface)
            {
                Extend = Extend.Repeat,
                Filter = Filter.Nearest, // match vanilla — no surprise blurring of shared textures
            };
            _patternCache[key] = pattern;
        }

        // Pattern matrix maps user-space coords to pattern-space (image) coords. Our CTM
        // is already pre-scaled by GUIScale (logical pixels), so the matrix factor is
        // plain `scale` — equivalent to vanilla's `scale / GUIScale` in physical-pixel space.
        var m = new Matrix();
        m.Scale(scale, scale);
        pattern.Matrix = m;
        ctx.SetSource(pattern);
    }

    /// <summary>
    /// Fills the current path with a tiled pattern loaded from <paramref name="textureLoc"/>.
    /// Convenience wrapper over <see cref="SetPatternSource"/> + <see cref="Context.Fill"/>.
    /// </summary>
    public static void FillPattern(this Context ctx, ICoreClientAPI capi, AssetLocation textureLoc, byte mulAlpha = 255, float scale = 1f, bool preserve = false)
    {
        ctx.SetPatternSource(capi, textureLoc, mulAlpha, scale);
        if (preserve)
        {
            ctx.FillPreserve();
        }
        else
        {
            ctx.Fill();
        }
    }

    // ── Text ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws a single line of text at logical-pixel position (<paramref name="x"/>, <paramref name="y"/>),
    /// where <paramref name="y"/> is the <i>top</i> of the line (not the baseline). Handles the
    /// physical-pixel CTM dance required by vanilla-style font hinting + subpixel AA — same
    /// recipe as <see cref="GuiLabel"/>. Save/restore is performed internally so the caller's
    /// CTM is left untouched.
    /// </summary>
    public static void DrawText(this Context ctx, string text, GuiFontStyle font, double x, double y)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        double scale = RuntimeEnv.GUIScale;
        double physX = x * scale;
        double physY = y * scale;

        ctx.Save();
        ctx.IdentityMatrix();
        font.Apply(ctx);
        ctx.MoveTo((int)physX, (int)(physY + ctx.FontExtents.Ascent));
        ctx.ShowText(text);
        if (font.RenderTwice)
        {
            ctx.ShowText(text);
        }

        ctx.Restore();
    }
}
