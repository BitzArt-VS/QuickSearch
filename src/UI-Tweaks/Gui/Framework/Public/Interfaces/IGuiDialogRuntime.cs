namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiDialogRuntime
{
    IGuiNode? FocusedNode { get; }

    void RequestClose();

    void RequestFocus();

    void SetFocusedNode(IGuiNode? node);

    void SetMouseOverCursor(string? cursor);
}
