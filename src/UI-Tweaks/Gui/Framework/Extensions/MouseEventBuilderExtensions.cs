using System;
using System.Threading.Tasks;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Slot-level mouse-event registration extensions. Attach a handler to any component slot
/// during <c>BuildRenderTree</c>. Handlers receive a <see cref="GuiMouseEventArgs"/> in
/// dialog-local logical coordinates and are wired to the slot's allocated bounds — there is
/// no need to scan child trees or forward events manually.
/// <para>
/// Each builder offers two overloads per event: one taking a synchronous
/// <see cref="Action{T}"/> and one taking an asynchronous <see cref="Func{T,TResult}"/> over
/// <see cref="Task"/>. This avoids forcing method-group conversions through a struct cast,
/// keeping DX on par with vanilla event hookup while still funnelling everything through the
/// allocation-free <see cref="GuiCallback{T}"/> internally.
/// </para>
/// <para>
/// Semantics:
/// <list type="bullet">
///   <item><c>OnMouseDown</c> fires when the cursor presses inside the slot.</item>
///   <item><c>OnMouseUp</c> fires on the slot that received <c>OnMouseDown</c>, regardless of
///   where the release lands — mouse capture is implicit, mirroring typical UI behaviour
///   (Win32 SetCapture, browsers, vanilla VS buttons).</item>
///   <item><c>OnMouseClick</c> fires only when both press and release land inside the slot's
///   bounds.</item>
///   <item><c>OnMouseMove</c> fires while the mouse is captured by this slot (i.e. between
///   <c>OnMouseDown</c> and <c>OnMouseUp</c>) — including while the cursor is outside the
///   slot's bounds. It is the basis for drag interactions.</item>
///   <item><c>OnMouseEnter</c> fires once when the uncaptured cursor first moves over the
///   slot's bounds. Basis for hover overlays and tooltips.</item>
///   <item><c>OnMouseLeave</c> fires once when the cursor leaves the slot's bounds (or the
///   dialog entirely) after a previous <c>OnMouseEnter</c>. Always paired with Enter.</item>
/// </list>
/// </para>
/// </summary>
public static class MouseEventBuilderExtensions
{
    public static IGuiComponentBuilder<T> OnMouseDown<T>(this IGuiComponentBuilder<T> builder, Action<GuiMouseEventArgs> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Down, handler);

    public static IGuiComponentBuilder<T> OnMouseDown<T>(this IGuiComponentBuilder<T> builder, Func<GuiMouseEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Down, handler);

    public static IGuiComponentBuilder<T> OnMouseUp<T>(this IGuiComponentBuilder<T> builder, Action<GuiMouseEventArgs> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Up, handler);

    public static IGuiComponentBuilder<T> OnMouseUp<T>(this IGuiComponentBuilder<T> builder, Func<GuiMouseEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Up, handler);

    public static IGuiComponentBuilder<T> OnMouseClick<T>(this IGuiComponentBuilder<T> builder, Action<GuiMouseEventArgs> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Click, handler);

    public static IGuiComponentBuilder<T> OnMouseClick<T>(this IGuiComponentBuilder<T> builder, Func<GuiMouseEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Click, handler);

    public static IGuiComponentBuilder<T> OnMouseMove<T>(this IGuiComponentBuilder<T> builder, Action<GuiMouseEventArgs> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Move, handler);

    public static IGuiComponentBuilder<T> OnMouseMove<T>(this IGuiComponentBuilder<T> builder, Func<GuiMouseEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Move, handler);

    public static IGuiComponentBuilder<T> OnMouseEnter<T>(this IGuiComponentBuilder<T> builder, Action<GuiMouseEventArgs> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Enter, handler);

    public static IGuiComponentBuilder<T> OnMouseEnter<T>(this IGuiComponentBuilder<T> builder, Func<GuiMouseEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Enter, handler);

    public static IGuiComponentBuilder<T> OnMouseLeave<T>(this IGuiComponentBuilder<T> builder, Action<GuiMouseEventArgs> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Leave, handler);

    public static IGuiComponentBuilder<T> OnMouseLeave<T>(this IGuiComponentBuilder<T> builder, Func<GuiMouseEventArgs, Task> handler)
        where T : IGuiNode
        => builder.SetMouseHandler(GuiMouseEventKind.Leave, handler);
}

