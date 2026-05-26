namespace BitzArt.UI.Tweaks.Gui;

[Flags]
public enum GuiScrollDirection
{
    None = 0,

    Vertical = 1 << 0,

    Horizontal = 1 << 1,

    Both = Vertical | Horizontal,
}
