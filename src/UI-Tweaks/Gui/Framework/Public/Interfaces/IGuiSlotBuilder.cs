namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiSlotBuilder
{
    internal IGuiSlotBuilder AddLayoutConfiguration(Action<GuiComponentLayoutParameters> configure);

    internal IGuiSlotBuilder AddMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback);

    internal IGuiSlotBuilder AddKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback);

    internal IGuiSlotBuilder AddFocusChangedHandler(GuiCallback<bool> callback);
}
