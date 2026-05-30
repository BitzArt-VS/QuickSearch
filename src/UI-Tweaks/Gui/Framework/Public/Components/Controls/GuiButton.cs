using Cairo;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A clickable button component matching vanilla <c>EnumButtonStyle.Normal</c> visuals — solid
/// dark-brown fill with a top/left highlight bevel and bottom/right shadow, dimmed by a 40%
/// black overlay while pressed. Text is centred horizontally and vertically and uses the
/// vanilla button text colour, swapping to the active (hover/press) colour while pressed.
/// <para>
/// Mouse handling follows the framework convention: <see cref="GuiButton"/> subscribes
/// its own slot-level <c>OnMouseDown</c> / <c>OnMouseUp</c> / <c>OnMouseClick</c>
/// handlers. The button itself exposes only a single
/// <see cref="OnClick"/> property typed as <see cref="GuiCallback"/>, which can be assigned
/// from either an <see cref="System.Action"/> or a <see cref="System.Func{T}"/> returning
/// <see cref="System.Threading.Tasks.Task"/>.
/// </para>
/// <para>
/// Visual blur from vanilla's <c>BlurPartial</c> step is intentionally omitted (consistent
/// with other framework components) — without surface-level blur, soft highlights are not
/// reproducible and a literal copy looks worse.
/// </para>
/// </summary>
public sealed class GuiButton : GuiComponent
{
    /// <summary>The text rendered inside the button.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Click callback. Accepts either a synchronous <see cref="System.Action"/> or an
    /// asynchronous <see cref="System.Func{T}"/> returning <see cref="System.Threading.Tasks.Task"/>
    /// via implicit conversion. Default is "no handler".</summary>
    public GuiCallback OnClick { get; set; }

    /// <summary>When false, mouse interactions are ignored and the button never enters the
    /// pressed visual state. Defaults to true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Font used for the button text in its idle (un-pressed) state.</summary>
    public GuiFontStyle NormalFont { get; set; } = GuiFontStyle.Default with { Color = GuiVanillaStyle.ButtonTextColor, Weight = FontWeight.Bold };

    /// <summary>Font used for the button text while the button is pressed (or hovered, if hover
    /// support is added later). Defaults to vanilla <c>ActiveButtonTextColor</c>.</summary>
    public GuiFontStyle PressedFont { get; set; } = GuiFontStyle.Default with { Color = GuiVanillaStyle.ActiveButtonTextColor, Weight = FontWeight.Bold };

    /// <summary>Horizontal text padding when sizing to <c>FitContent</c>. Default 8 logical pixels.</summary>
    public double HorizontalTextPadding { get; set; } = 8;

    /// <summary>Vertical text padding when sizing to <c>FitContent</c>. Default 4 logical pixels.</summary>
    public double VerticalTextPadding { get; set; } = 4;

    /// <summary>Emboss height for the highlight/shadow bevel, in logical pixels. Default 1.5
    /// (matches vanilla <c>EnumButtonStyle.Normal</c>).</summary>
    public double EmbossHeight { get; set; } = 1.5;

    /// <summary>Solid background fill colour. Default <see cref="GuiStyle.ButtonBackColor"/>.</summary>
    public GuiColor BackgroundColor { get; set; } = GuiVanillaStyle.ButtonBackColor;

    // Press / hover state — driven by own-slot mouse handlers, read by Render.
    // These fields are mutated on the render thread alongside Render, so no synchronisation
    // is required.
    private bool _isPressed;
    private bool _isHovered;
    // Last arranged bounds, captured in Render. Used by HandleMouseUp to determine whether
    // the release happened inside or outside — needed because OnMouseUp fires before
    // OnMouseClick, so the position must be checked manually rather than relying on the
    // framework's inside/outside routing.
    private GuiComponentBounds _lastBounds;

    public override GuiLayoutSize Measure(GuiLayoutSize available)
    {
        var ts = NormalFont.Measure(Text);
        return new GuiLayoutSize(ts.Width + HorizontalTextPadding * 2, ts.Height + VerticalTextPadding * 2);
    }

    public override void Render(Context ctx, GuiComponentBounds b)
    {
        _lastBounds = b;
        double emboss = EmbossHeight;

        // 1. Solid fill.
        ctx.Rectangle(b.X, b.Y, b.Width, b.Height);
        ctx.FillSolid(BackgroundColor);

        // 2. Top highlight (full width minus the right-shadow notch, matches vanilla path math).
        ctx.Rectangle(b.X, b.Y, b.Width - emboss, emboss);
        ctx.SetSourceRGBA(1, 1, 1, 0.15);
        ctx.Fill();

        // 3. Left highlight (offset down by emboss so it doesn't double-paint the top corner).
        ctx.Rectangle(b.X, b.Y + emboss, emboss, b.Height - emboss);
        ctx.SetSourceRGBA(1, 1, 1, 0.15);
        ctx.Fill();

        // 4. Bottom shadow.
        ctx.Rectangle(b.X + emboss, b.Y + b.Height - emboss, b.Width - 2 * emboss, emboss);
        ctx.SetSourceRGBA(0, 0, 0, 0.2);
        ctx.Fill();

        // 5. Right shadow.
        ctx.Rectangle(b.X + b.Width - emboss, b.Y, emboss, b.Height);
        ctx.SetSourceRGBA(0, 0, 0, 0.2);
        ctx.Fill();

        // 6. Hover overlay — vanilla paints a 10% white wash when the cursor is over the button.
        if (_isHovered && Enabled && !_isPressed)
        {
            ctx.Rectangle(b.X, b.Y, b.Width, b.Height);
            ctx.SetSourceRGBA(1, 1, 1, 0.1);
            ctx.Fill();
        }

        // 7. Pressed overlay — vanilla paints a 40% black wash over the entire button while
        //    held down. This sits *on top* of the bevel, slightly muting it as in vanilla.
        if (_isPressed && Enabled)
        {
            ctx.Rectangle(b.X, b.Y, b.Width, b.Height);
            ctx.SetSourceRGBA(0, 0, 0, 0.4);
            ctx.Fill();
        }

        // 8. Text — centred. Use pressed colour when held to mirror vanilla's press-font swap.
        if (!string.IsNullOrEmpty(Text))
        {
            var font = (_isPressed && Enabled) ? PressedFont : NormalFont;
            var ts = font.Measure(Text);
            double textX = b.X + (b.Width - ts.Width) / 2.0;
            double textY = b.Y + (b.Height - ts.Height) / 2.0;
            ctx.DrawText(Text, font, textX, textY);
        }
    }

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        base.ConfigureSlot(builder);
        builder
            .OnMouseDown(HandleMouseDown)
            .OnMouseUp(HandleMouseUp)
            .OnMouseClick(HandleMouseClick)
            .OnMouseEnter(HandleMouseEnter)
            .OnMouseLeave(HandleMouseLeave);
    }

    private void HandleMouseDown(GuiMouseEventArgs e)
    {
        if (!Enabled)
        {
            return;
        }

        _isPressed = true;
        ClientApi?.Gui.PlaySound("menubutton_down");
        RequestPaint();
    }

    private void HandleMouseUp(GuiMouseEventArgs e)
    {
        // Always release, even when disabled — mouse capture means OnMouseUp may fire for a
        // button that was disabled mid-press; we still want to clear the visual.
        _isPressed = false;
        // While captured, OnMouseLeave is suppressed by the renderer (hover tracking only runs
        // on uncaptured moves). Clear _isHovered here based on actual cursor position so the
        // button doesn't stay highlighted after a press-drag-off-release sequence.
        bool inside = e.Position.X >= _lastBounds.X && e.Position.X < _lastBounds.Right
                   && e.Position.Y >= _lastBounds.Y && e.Position.Y < _lastBounds.Bottom;
        _isHovered = inside;
        // Play sound on release only when cursor is outside — this is the "pressed, moved off,
        // then released" case. When released inside, OnMouseClick follows immediately after
        // OnMouseUp, so we would double-play; the down-press sound is sufficient for that path.
        if (!inside && Enabled)
        {
            ClientApi?.Gui.PlaySound("menubutton_up");
        }

        RequestPaint();
    }

    private void HandleMouseClick(GuiMouseEventArgs e)
    {
        if (!Enabled)
        {
            return;
        }

        OnClick.Invoke();
    }

    private void HandleMouseEnter(GuiMouseEventArgs e)
    {
        _isHovered = true;
        if (Enabled)
        {
            ClientApi?.Gui.PlaySound("menubutton");
        }

        RequestPaint();
    }

    private void HandleMouseLeave(GuiMouseEventArgs e)
    {
        _isHovered = false;
        RequestPaint();
    }
}
