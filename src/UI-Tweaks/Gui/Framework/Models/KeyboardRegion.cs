namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// One entry in the per-frame keyboard-region table. Records a slot's identity (its
/// <see cref="IGuiNode"/> instance) and the slot-level keyboard callbacks attached to it.
/// Unlike <see cref="InteractiveRegion"/> there are no bounds — keyboard input is not
/// spatially routed; it always goes to the focused node, looked up by token.
/// </summary>
internal readonly struct KeyboardRegion
{
    public readonly object Token;
    public readonly GuiCallback<GuiKeyEventArgs> OnKeyDown;
    public readonly GuiCallback<GuiKeyEventArgs> OnKeyUp;
    public readonly GuiCallback<GuiKeyEventArgs> OnKeyPress;

    public KeyboardRegion(
        object token,
        GuiCallback<GuiKeyEventArgs> onKeyDown,
        GuiCallback<GuiKeyEventArgs> onKeyUp,
        GuiCallback<GuiKeyEventArgs> onKeyPress)
    {
        Token = token;
        OnKeyDown = onKeyDown;
        OnKeyUp = onKeyUp;
        OnKeyPress = onKeyPress;
    }
}
