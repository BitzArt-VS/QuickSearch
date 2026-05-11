using System;

namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiComponentBuilder<T> : IGuiRenderTreeBuilder
    where T : IGuiNode
{
    internal IGuiComponentBuilder<T> AddConfigurationAction(Action<T> action);

    /// <summary>
    /// Re-applied each blueprint pass — a new callback overwrites the previous one;
    /// passing <c>default</c> clears it.
    /// </summary>
    internal IGuiComponentBuilder<T> SetMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback);

    /// <summary>
    /// Re-applied each blueprint pass — a new callback overwrites the previous one;
    /// passing <c>default</c> clears it. Fires only while this slot's component is focused.
    /// </summary>
    internal IGuiComponentBuilder<T> SetKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback);
}
