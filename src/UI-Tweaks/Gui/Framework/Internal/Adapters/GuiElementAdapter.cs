using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiElementAdapter(ICoreClientAPI clientApi, DialogRenderer renderer)
    : Vintagestory.API.Client.GuiDialog(clientApi)
{
    private readonly DialogRenderer _renderer = renderer;
    private GuiInputRouter _input = null!;
    private bool _isDisposed;

    public override string? ToggleKeyCombinationCode => null;

    public override void OnGuiOpened() { }

    internal void AttachInput(GuiInputRouter input) => _input = input;

    public override bool TryOpen()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (IsOpened())
        {
            return false;
        }

        return base.TryOpen();
    }

    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        base.Dispose();
        _isDisposed = true;
    }

    // Drive the Cairo render from inside vanilla's per-dialog Ortho pass so this dialog
    // shares the z-stack with vanilla dialogs (instead of all of them painting on top of
    // us via GuiManager's single Ortho-1.0 renderer slot).
    public override void OnRenderGUI(float deltaTime) => _renderer.OnRenderFrame(deltaTime);

    public override void Focus()
    {
        base.Focus();
        _input.OnFocus();
    }

    public override void UnFocus()
    {
        base.UnFocus();
        _input.OnUnFocus();
    }

    public override void OnMouseDown(MouseEvent args) => _input.OnMouseDown(args);
    public override void OnMouseUp(MouseEvent args) => _input.OnMouseUp(args);
    public override void OnMouseMove(MouseEvent args) => _input.OnMouseMove(args);
    public override void OnMouseWheel(MouseWheelEventArgs args) => _input.OnMouseWheel(args);

    public override void OnKeyDown(KeyEvent args) => _input.OnKeyDown(args);
    public override void OnKeyPress(KeyEvent args) => _input.OnKeyPress(args);
    public override void OnKeyUp(KeyEvent args) => _input.OnKeyUp(args);
    public override bool OnEscapePressed() => _input.OnEscapePressed();
}
