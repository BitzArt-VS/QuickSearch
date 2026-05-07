namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Identifies which mouse event a slot-level handler reacts to. Internal to the framework —
/// public registration goes through the <c>OnMouseDown</c> / <c>OnMouseUp</c> / <c>OnMouseClick</c>
/// extension methods.
/// </summary>
internal enum GuiMouseEventKind
{
    Down,
    Up,
    Click,
    Move,
    Enter,
    Leave,
}
