using System;
using System.Threading.Tasks;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Slot-level keyboard-event registration extensions. Mirrors
/// <see cref="MouseEventBuilderExtensions"/> for the keyboard counterpart.
/// <para>
/// Unlike mouse events, keyboard events are not spatially routed — they fire only on the
/// slot whose component currently holds focus (<see cref="FocusManager"/>). A click on a
/// focusable component requests focus; clicks elsewhere clear it.
/// </para>
/// <para>
/// Each event has separate sync (<see cref="Action{T}"/>) and async
/// (<see cref="Func{T,TResult}"/>) overloads — the framework dispatches synchronously
/// regardless, but the async overload exists so callers can plug in <c>async Task</c>
/// methods directly without wrapping. Handlers may mark
/// <see cref="GuiKeyEventArgs.Handled"/> to suppress framework defaults (e.g. Escape
/// closing the dialog).
/// </para>
/// </summary>
public static class KeyEventBuilderExtensions
{
    public static IGuiComponentBuilder<T> OnKeyDown<T>(this IGuiComponentBuilder<T> builder, Action<GuiKeyEventArgs> handler)
        where T : IGuiNode
        => builder.SetKeyHandler(GuiKeyEventKind.Down, handler);

    public static IGuiComponentBuilder<T> OnKeyDown<T>(this IGuiComponentBuilder<T> builder, Func<GuiKeyEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetKeyHandler(GuiKeyEventKind.Down, handler);

    public static IGuiComponentBuilder<T> OnKeyUp<T>(this IGuiComponentBuilder<T> builder, Action<GuiKeyEventArgs> handler)
        where T : IGuiNode
        => builder.SetKeyHandler(GuiKeyEventKind.Up, handler);

    public static IGuiComponentBuilder<T> OnKeyUp<T>(this IGuiComponentBuilder<T> builder, Func<GuiKeyEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetKeyHandler(GuiKeyEventKind.Up, handler);

    public static IGuiComponentBuilder<T> OnKeyPress<T>(this IGuiComponentBuilder<T> builder, Action<GuiKeyEventArgs> handler)
        where T : IGuiNode
        => builder.SetKeyHandler(GuiKeyEventKind.Press, handler);

    public static IGuiComponentBuilder<T> OnKeyPress<T>(this IGuiComponentBuilder<T> builder, Func<GuiKeyEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetKeyHandler(GuiKeyEventKind.Press, handler);
}
