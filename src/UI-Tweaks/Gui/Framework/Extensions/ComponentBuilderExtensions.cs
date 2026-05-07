using System;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Extension methods on <see cref="IGuiComponentBuilder{T}"/> for configuring components after declaration.
/// </summary>
public static class ComponentBuilderExtensions
{
    /// <summary>
    /// Configures <see cref="GuiComponentLayoutParameters"/>. Constrained to
    /// <see cref="IGuiComponent"/> — only layout-participating components have layout
    /// parameters; layout-transparent decorators (pure <see cref="IGuiNode"/> types) do
    /// not.
    /// </summary>
    public static IGuiComponentBuilder<T> ConfigureLayout<T>(this IGuiComponentBuilder<T> builder, Action<GuiComponentLayoutParameters> configure)
        where T : IGuiComponent
        => builder.Configure(component => configure.Invoke(component.LayoutParameters));

    public static IGuiComponentBuilder<T> Configure<T>(this IGuiComponentBuilder<T> builder, Action<T> configure)
        where T : IGuiNode
        => builder.AddConfigurationAction(configure);
}
