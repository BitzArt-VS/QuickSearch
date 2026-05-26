namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Slot-level focus-event registration extensions. Focus changes are reference-routed:
/// handlers fire when the slot's node becomes or stops being the focused node.
/// </summary>
public static class FocusEventBuilderExtensions
{
    public static TBuilder OnFocusChanged<TBuilder>(this TBuilder builder, Action<bool> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddFocusChangedHandler(handler);
        return builder;
    }

    public static TBuilder OnFocusChanged<TBuilder>(this TBuilder builder, Func<bool, Task> handler)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddFocusChangedHandler(handler);
        return builder;
    }
}
