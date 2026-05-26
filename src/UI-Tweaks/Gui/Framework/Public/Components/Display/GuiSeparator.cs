namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A pre-styled horizontal rule — a 1 px tall <see cref="GuiRectangle"/> that fills the
/// available width and uses <see cref="GuiStyle.DialogTitleBarBgColor"/> as its
/// colour. Drop it between content groups to draw a visual dividing line that stays
/// within the vanilla aesthetic.
/// <para>
/// Layout parameters may be overridden via fluent extensions; the defaults
/// (<c>height = 1</c>, <c>widthMode = Fill</c>) are restored each pass through the
/// component's own slot configuration.
/// </para>
/// </summary>
public sealed class GuiSeparator : GuiRectangle
{
    public GuiSeparator()
    {
        Color = GuiVanillaStyle.DialogTitleBarBgColor;
    }

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        builder.ConfigureLayout(layout =>
        {
            layout.Height = 1;
            layout.WidthMode = GuiSizeMode.Fill;
        });
    }
}
