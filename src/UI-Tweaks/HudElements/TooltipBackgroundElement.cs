using Cairo;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

public class TooltipBackgroundElement(ICoreClientAPI capi, ElementBounds bounds, double opacity, double cornerRadius)
        : GuiElement(capi, bounds)
{
    public override void ComposeElements(Context ctx, ImageSurface surface)
    {
        Bounds.CalcWorldBounds();

        double x = (int)Bounds.drawX;
        double y = (int)Bounds.drawY;
        double w = (int)Bounds.OuterWidth;
        double h = (int)Bounds.OuterHeight;

        double lineWidth = 2.0;
        double inset = lineWidth / 2.0; // 1.0px inset for a 2.0px line

        x += inset;
        y += inset;
        w -= (inset * 2) + 1.0;
        h -= (inset * 2) + 1.0;

        if (cornerRadius <= 0)
        {
            DrawSquare(ctx, x, y, w, h, opacity, lineWidth);
            return;
        }

        double r = cornerRadius;
        r = Math.Min(r, Math.Min(w / 2, h / 2));

        ctx.NewPath();
        ctx.Arc(x + w - r, y + r, r, -Math.PI / 2, 0);
        ctx.Arc(x + w - r, y + h - r, r, 0, Math.PI / 2);
        ctx.Arc(x + r, y + h - r, r, Math.PI / 2, Math.PI);
        ctx.Arc(x + r, y + r, r, Math.PI, 3 * Math.PI / 2);
        ctx.ClosePath();

        ctx.SetSourceRGBA(0.12, 0.11, 0.10, opacity);
        ctx.FillPreserve();

        double borderOpacity = 1.0 - ((1.0 - opacity) / 2.0);
        ctx.SetSourceRGBA(0.35, 0.33, 0.30, borderOpacity);
        ctx.LineWidth = lineWidth;
        ctx.Stroke();
    }

    private static void DrawSquare(Context ctx, double x, double y, double w, double h, double opacity, double lineWidth)
    {
        ctx.NewPath();
        ctx.Rectangle(x, y, w, h);
        ctx.ClosePath();

        ctx.SetSourceRGBA(0.12, 0.11, 0.10, opacity);
        ctx.FillPreserve();

        double borderOpacity = 1.0 - ((1.0 - opacity) / 2.0);
        ctx.SetSourceRGBA(0.35, 0.33, 0.30, borderOpacity);
        ctx.LineWidth = lineWidth;
        ctx.Stroke();
    }
}

public static class TooltipBackgroundExtensions
{
    public static GuiComposer AddTooltipBackground(this GuiComposer composer, ElementBounds bounds, double backgroundOpacity, double cornerRadius)
        => composer.AddStaticElement(new TooltipBackgroundElement(composer.Api, bounds, backgroundOpacity, cornerRadius));
}
