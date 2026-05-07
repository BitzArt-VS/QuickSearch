using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A minimal vanilla GuiDialog registered with the game's dialog system. Its job is twofold:
/// 1. Keep <c>DialogsOpened &gt; 0</c> while a Cairo <see cref="GuiDialog"/> is open so the
///    cursor is released by <c>UpdateFreeMouse()</c>.
/// 2. Plug the Cairo dialog into vanilla input routing — mouse/keyboard events, focus
///    requests, escape handling — so the Cairo dialog behaves like a normal vanilla dialog
///    relative to other dialogs and HUD elements.
///
/// Mouse routing follows vanilla semantics: events are only marked <c>Handled</c> when the
/// cursor lies inside the Cairo dialog's screen rectangle, so clicks outside still propagate
/// to other dialogs (hotbar, etc.) instead of being swallowed. Keyboard routing relies on
/// vanilla's default <see cref="ShouldReceiveKeyboardEvents"/> (<c>focused</c>).
/// This dialog itself renders nothing.
/// </summary>
internal sealed class CairoDialogInputInterceptor(ICoreClientAPI clientApi, IGuiDialog dialog)
    : VanillaGuiDialog(clientApi)
{
    private readonly IGuiDialog _dialog = dialog;

    // Forward the Cairo dialog's render order as the vanilla DrawOrder so this dialog
    // stacks correctly within game.OpenedGuis. GuiManager.OnRenderFrameGUI iterates that
    // list in reverse, calling OnRenderGUI on each, and RequestFocus() shuffles the
    // focused dialog to the front of its DrawOrder rank — which means it is rendered last
    // (on top) within the rank without us having to re-register a renderer.
    public override double DrawOrder => _dialog.RenderOrder;

    public override void OnGuiOpened() { }

    // Drive the Cairo render from inside vanilla's per-dialog Ortho pass so this dialog
    // shares the z-stack with vanilla dialogs (instead of all of them painting on top of
    // us via GuiManager's single Ortho-1.0 renderer slot).
    public override void OnRenderGUI(float deltaTime) => _dialog.OnRenderGui(deltaTime);

    // Use vanilla defaults: mouse events while opened, keyboard events while focused.
    // Do not override ShouldReceiveMouseEvents / ShouldReceiveKeyboardEvents.

    public override void Focus()
    {
        base.Focus();
        _dialog.OnFocus();
    }

    public override void UnFocus()
    {
        base.UnFocus();
        _dialog.OnUnFocus();
    }

    // Mouse / keyboard pass-through. The Cairo GuiDialog applies hit-testing and the
    // "only handle inside" rule to mirror vanilla GuiDialog.OnMouse* behaviour.
    public override void OnMouseDown(MouseEvent args) => _dialog.OnMouseDown(args);
    public override void OnMouseUp(MouseEvent args) => _dialog.OnMouseUp(args);
    public override void OnMouseMove(MouseEvent args) => _dialog.OnMouseMove(args);
    public override void OnMouseWheel(MouseWheelEventArgs args) => _dialog.OnMouseWheel(args);

    public override void OnKeyDown(KeyEvent args) => _dialog.OnKeyDown(args);
    public override void OnKeyPress(KeyEvent args) => _dialog.OnKeyPress(args);
    public override void OnKeyUp(KeyEvent args) => _dialog.OnKeyUp(args);
    public override bool OnEscapePressed() => _dialog.OnEscapePressed();
}
