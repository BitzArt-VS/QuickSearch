namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiDialogRuntime(DialogRenderer renderer, Action requestClose)
{
    public IGuiNode? FocusedNode => renderer.FocusedNode;

    public void RequestClose() => requestClose.Invoke();

    public void RequestFocus() => renderer.RequestFocus();

    public void SetFocusedNode(IGuiNode? node) => renderer.SetFocusedNode(node);

    public void SetMouseOverCursor(string? cursor) => renderer.SetMouseOverCursor(cursor);
}
