namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Extension methods for configuring slots and components after declaration.
/// </summary>
public static class ComponentBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="GuiComponentLayoutParameters"/> for this slot.
    /// </summary>
    public static TBuilder ConfigureLayout<TBuilder>(this TBuilder builder, Action<GuiComponentLayoutParameters> configure)
        where TBuilder : IGuiSlotBuilder
    {
        builder.AddLayoutConfiguration(configure);
        return builder;
    }

    public static IGuiComponentBuilder<T> Configure<T>(this IGuiComponentBuilder<T> builder, Action<T> configure)
        where T : IGuiNode
        => builder.AddConfigurationAction(configure);
}
