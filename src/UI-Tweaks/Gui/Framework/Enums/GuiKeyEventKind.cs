namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Identifies which keyboard event a slot-level handler reacts to. Internal to the
/// framework — public registration goes through the <c>OnKeyDown</c> / <c>OnKeyUp</c> /
/// <c>OnKeyPress</c> extension methods.
/// </summary>
internal enum GuiKeyEventKind
{
    Down,
    Up,
    Press,
}
