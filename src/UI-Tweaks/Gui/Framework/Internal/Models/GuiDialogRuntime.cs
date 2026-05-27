namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiDialogRuntime(GuiInputRouter inputRouter, Action requestClose) : IGuiDialogRuntime
{
    public IGuiNode? FocusedNode => inputRouter.FocusedNode;

    public void RequestClose() => requestClose.Invoke();

    public void RequestFocus() => inputRouter.RequestFocus();

    public void SetFocusedNode(IGuiNode? node) => inputRouter.SetFocusedNode(node);

    public void SetMouseOverCursor(string? cursor) => inputRouter.SetMouseOverCursor(cursor);
}
