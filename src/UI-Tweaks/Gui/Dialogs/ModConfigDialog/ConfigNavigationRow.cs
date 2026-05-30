using BitzArt.UI.Tweaks.Gui;
using Cairo;

namespace BitzArt.UI.Tweaks;

internal sealed class ConfigNavigationRow : GuiComponent
{
    private const double TextLeftPadding = 14;
    private const double AccentWidth = 3;
    private const double SeparatorHeight = 1;

    private static readonly GuiColor SelectedBackground = GuiColor.FromRgba(0.38, 0.29, 0.20, 0.58);
    private static readonly GuiColor HoverBackground = GuiColor.FromRgba(0.48, 0.38, 0.27, 0.14);
    private static readonly GuiColor PressedBackground = GuiColor.FromRgba(0, 0, 0, 0.22);
    private static readonly GuiColor SeparatorColor = GuiColor.FromRgba(0.78, 0.69, 0.58, 0.16);
    private static readonly GuiColor AccentColor = GuiVanillaStyle.ActiveButtonTextColor;
    private static readonly GuiColor ActiveTextColor = GuiColor.FromRgba(0.96, 0.92, 0.86, 1.0);

    public string Text { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public GuiCallback OnClick { get; set; }

    private bool _isHovered;
    private bool _isPressed;

    public override GuiLayoutSize Measure(GuiLayoutSize available)
    {
        var textSize = GuiFontStyle.MediumBold.Measure(Text);
        return new(textSize.Width + TextLeftPadding * 2, textSize.Height);
    }

    public override void Render(Context context, GuiComponentBounds bounds)
    {
        if (IsSelected)
        {
            context.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            context.FillSolid(SelectedBackground);

            context.Rectangle(bounds.X, bounds.Y, AccentWidth, bounds.Height);
            context.FillSolid(AccentColor);
        }
        else if (_isHovered)
        {
            context.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            context.FillSolid(HoverBackground);
        }

        if (_isPressed)
        {
            context.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            context.FillSolid(PressedBackground);
        }

        var font = GuiFontStyle.MediumBold with
        {
            Color = IsSelected || _isHovered
                ? ActiveTextColor
                : GuiVanillaStyle.ButtonTextColor
        };
        var textSize = font.Measure(Text);
        context.DrawText(Text, font, bounds.X + TextLeftPadding, bounds.Y + (bounds.Height - textSize.Height) / 2.0);

        context.Rectangle(bounds.X, bounds.Bottom - SeparatorHeight, bounds.Width, SeparatorHeight);
        context.FillSolid(SeparatorColor);
    }

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        builder
            .OnMouseDown(HandleMouseDown)
            .OnMouseUp(HandleMouseUp)
            .OnMouseClick(HandleMouseClick)
            .OnMouseEnter(HandleMouseEnter)
            .OnMouseLeave(HandleMouseLeave);
    }

    private void HandleMouseDown(GuiMouseEventArgs args)
    {
        _isPressed = true;
        ClientApi?.Gui.PlaySound("menubutton_down");
        RequestPaint();
    }

    private void HandleMouseUp(GuiMouseEventArgs args)
    {
        _isPressed = false;
        RequestPaint();
    }

    private void HandleMouseClick(GuiMouseEventArgs args)
    {
        OnClick.Invoke();
    }

    private void HandleMouseEnter(GuiMouseEventArgs args)
    {
        _isHovered = true;
        ClientApi?.Gui.PlaySound("menubutton");
        RequestPaint();
    }

    private void HandleMouseLeave(GuiMouseEventArgs args)
    {
        _isHovered = false;
        RequestPaint();
    }
}
