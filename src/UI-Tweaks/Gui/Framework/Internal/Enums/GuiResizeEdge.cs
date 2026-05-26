namespace BitzArt.UI.Tweaks.Gui;

[Flags]
internal enum GuiResizeEdge
{
    None = 0,
    Top = 1 << 0,
    Bottom = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
}
