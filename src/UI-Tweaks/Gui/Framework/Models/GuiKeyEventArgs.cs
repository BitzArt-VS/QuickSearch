using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Keyboard event payload delivered to slot-level handlers and to a focused node's virtual
/// <c>OnKeyDown</c> / <c>OnKeyUp</c> / <c>OnKeyPress</c> hooks. Wraps the underlying vanilla
/// <see cref="KeyEvent"/> so handlers can mark <see cref="Handled"/> to suppress framework
/// fallbacks (e.g. Escape closing the dialog).
/// <para>
/// Carried as a value type — copying is cheap, and <see cref="Handled"/> writes are
/// reflected on the wrapped (reference-typed) <see cref="KeyEvent"/>, so multiple handlers
/// see the same flag.
/// </para>
/// </summary>
public readonly struct GuiKeyEventArgs
{
    /// <summary>The underlying vanilla key event. Exposed for advanced cases (e.g. reading
    /// <see cref="KeyEvent.KeyCode2"/>); prefer the typed properties on this struct for
    /// the common fields.</summary>
    public KeyEvent Event { get; }

    public GuiKeyEventArgs(KeyEvent ev) => Event = ev;

    /// <summary>The keycode of the pressed/released key, matching <c>GlKeys</c>.</summary>
    public int KeyCode => Event.KeyCode;

    /// <summary>The character produced by a <c>KeyPress</c> event. Undefined for
    /// <c>KeyDown</c> / <c>KeyUp</c>.</summary>
    public char KeyChar => Event.KeyChar;

    public bool ShiftPressed   => Event.ShiftPressed;
    public bool CtrlPressed    => Event.CtrlPressed;
    public bool AltPressed     => Event.AltPressed;
    public bool CommandPressed => Event.CommandPressed;

    /// <summary>Marks the event as handled — suppresses subsequent framework defaults
    /// (e.g. <see cref="GuiDialog.OnEscapePressed"/>'s "close on Escape" fallback when a
    /// focused component consumes the Escape key).</summary>
    public bool Handled
    {
        get => Event.Handled;
        set => Event.Handled = value;
    }
}
