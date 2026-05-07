using System;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Builder interface returned from <see cref="IGuiRenderTreeBuilder.AddComponent{T}(int)"/>
/// for configuring a newly-declared node. Constrained to <see cref="IGuiNode"/> so that
/// the builder can be used for both layout-participating components and pure layout-
/// transparent decorators (tooltips, focus trackers, etc.). Layout-only configuration
/// (<c>ConfigureLayout</c>) imposes the tighter <see cref="IGuiComponent"/> constraint
/// at the extension level.
/// </summary>
public interface IGuiComponentBuilder<T> : IGuiRenderTreeBuilder
    where T : IGuiNode
{
    internal IGuiComponentBuilder<T> AddConfigurationAction(Action<T> action);

    /// <summary>
    /// Registers a mouse-event callback against this slot for the given <paramref name="kind"/>.
    /// Re-applied each blueprint pass — passing a fresh callback overwrites the previous one,
    /// passing <c>default</c> clears it. Internal: public registration goes through the
    /// <c>OnMouseDown</c> / <c>OnMouseUp</c> / <c>OnMouseClick</c> extension methods.
    /// </summary>
    internal IGuiComponentBuilder<T> SetMouseHandler(GuiMouseEventKind kind, GuiCallback<GuiMouseEventArgs> callback);

    /// <summary>
    /// Registers a keyboard-event callback against this slot for the given
    /// <paramref name="kind"/>. Re-applied each blueprint pass — passing a fresh callback
    /// overwrites the previous one, passing <c>default</c> clears it. The handler fires
    /// only while this slot's component is focused (see <see cref="FocusManager"/>).
    /// Internal: public registration goes through the <c>OnKeyDown</c> / <c>OnKeyUp</c> /
    /// <c>OnKeyPress</c> extension methods.
    /// </summary>
    internal IGuiComponentBuilder<T> SetKeyHandler(GuiKeyEventKind kind, GuiCallback<GuiKeyEventArgs> callback);
}
