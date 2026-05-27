namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Slot-level keyboard-event registration extensions. Mirrors
/// <see cref="MouseEventBuilderExtensions"/> for the keyboard counterpart.
/// <para>
/// Unlike mouse events, keyboard events are not spatially routed. The root dialog slot
/// receives keyboard events first for dialog-wide shortcuts; unhandled events then fire
/// on the slot whose component currently holds focus (<see cref="FocusManager"/>). A
/// click on a focusable component requests focus; clicks elsewhere clear it.
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
    public static TBuilder OnKeyDown<TBuilder>(this TBuilder builder, Action<GuiKeyEventArgs> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddKeyHandler(GuiKeyEventKind.Down, handler);
        return builder;
    }

    public static TBuilder OnKeyDown<TBuilder>(this TBuilder builder, Func<GuiKeyEventArgs, Task> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddKeyHandler(GuiKeyEventKind.Down, handler);
        return builder;
    }

    public static TBuilder OnKeyUp<TBuilder>(this TBuilder builder, Action<GuiKeyEventArgs> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddKeyHandler(GuiKeyEventKind.Up, handler);
        return builder;
    }

    public static TBuilder OnKeyUp<TBuilder>(this TBuilder builder, Func<GuiKeyEventArgs, Task> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddKeyHandler(GuiKeyEventKind.Up, handler);
        return builder;
    }

    public static TBuilder OnKeyPress<TBuilder>(this TBuilder builder, Action<GuiKeyEventArgs> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddKeyHandler(GuiKeyEventKind.Press, handler);
        return builder;
    }

    public static TBuilder OnKeyPress<TBuilder>(this TBuilder builder, Func<GuiKeyEventArgs, Task> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddKeyHandler(GuiKeyEventKind.Press, handler);
        return builder;
    }
}
