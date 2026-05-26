using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiElementAdapter(ICoreClientAPI clientApi, DialogRenderer renderer)
    : Vintagestory.API.Client.GuiDialog(clientApi)
{
    private readonly DialogRenderer _renderer = renderer;
    private bool _isDisposed;

    public override string? ToggleKeyCombinationCode => null;

    public override void OnGuiOpened() { }

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
        _renderer.OnFocus();
    }

    public override void UnFocus()
    {
        base.UnFocus();
        _renderer.OnUnFocus();
    }

    public override void OnMouseDown(MouseEvent args) => _renderer.OnMouseDown(args);
    public override void OnMouseUp(MouseEvent args) => _renderer.OnMouseUp(args);
    public override void OnMouseMove(MouseEvent args) => _renderer.OnMouseMove(args);
    public override void OnMouseWheel(MouseWheelEventArgs args) => _renderer.OnMouseWheel(args);

    public override void OnKeyDown(KeyEvent args) => _renderer.OnKeyDown(args);
    public override void OnKeyPress(KeyEvent args) => _renderer.OnKeyPress(args);
    public override void OnKeyUp(KeyEvent args) => _renderer.OnKeyUp(args);
    public override bool OnEscapePressed() => _renderer.OnEscapePressed();
}
