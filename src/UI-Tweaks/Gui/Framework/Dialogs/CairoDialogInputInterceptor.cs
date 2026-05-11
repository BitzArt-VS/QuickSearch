using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A minimal vanilla GuiDialog registered with the game's dialog system. Its job is twofold:
/// 1. Keep <c>DialogsOpened &gt; 0</c> while a Cairo <see cref="GuiDialog"/> is open so the
///    cursor is released by <c>UpdateFreeMouse()</c>.
/// 2. Plug the Cairo dialog into vanilla input routing — mouse/keyboard events, focus
///    requests, escape handling — so the Cairo dialog behaves like a normal vanilla dialog
///    relative to other dialogs and HUD elements.
///
/// All event methods forward directly to <see cref="DialogRenderer"/>, which owns both the
/// dispatcher infrastructure and the full event-routing logic. This dialog itself renders nothing.
/// </summary>
internal sealed class CairoDialogInputInterceptor(ICoreClientAPI clientApi, DialogRenderer renderer)
    : VanillaGuiDialog(clientApi)
{
    private readonly DialogRenderer _renderer = renderer;

    // Forward the Cairo renderer's render order as the vanilla DrawOrder so this dialog
    // stacks correctly within game.OpenedGuis. GuiManager.OnRenderFrameGUI iterates that
    // list in reverse, calling OnRenderGUI on each, and RequestFocus() shuffles the
    // focused dialog to the front of its DrawOrder rank — which means it is rendered last
    // (on top) within the rank without us having to re-register a renderer.
    public override double DrawOrder => _renderer.RenderOrder;

    public override void OnGuiOpened() { }

    // Drive the Cairo render from inside vanilla's per-dialog Ortho pass so this dialog
    // shares the z-stack with vanilla dialogs (instead of all of them painting on top of
    // us via GuiManager's single Ortho-1.0 renderer slot).
    public override void OnRenderGUI(float deltaTime) => _renderer.OnRenderFrame(deltaTime, EnumRenderStage.Ortho);

    // Use vanilla defaults: mouse events while opened, keyboard events while focused.
    // Do not override ShouldReceiveMouseEvents / ShouldReceiveKeyboardEvents.

    public override void Focus()
    {
        base.Focus();
        _renderer.OnFocus();
    }

    public override void UnFocus()
    {
        base.UnFocus();
        _renderer.OnUnFocus();
    }

    // Route all mouse and keyboard events directly into the renderer, which owns the
    // dispatcher infrastructure and applies hit-testing, focus management, and dispatch.
    public override void OnMouseDown(MouseEvent args) => _renderer.OnMouseDown(args);
    public override void OnMouseUp(MouseEvent args) => _renderer.OnMouseUp(args);
    public override void OnMouseMove(MouseEvent args) => _renderer.OnMouseMove(args);
    public override void OnMouseWheel(MouseWheelEventArgs args) => _renderer.OnMouseWheel(args);

    public override void OnKeyDown(KeyEvent args) => _renderer.OnKeyDown(args);
    public override void OnKeyPress(KeyEvent args) => _renderer.OnKeyPress(args);
    public override void OnKeyUp(KeyEvent args) => _renderer.OnKeyUp(args);
    public override bool OnEscapePressed() => _renderer.OnEscapePressed();
}
