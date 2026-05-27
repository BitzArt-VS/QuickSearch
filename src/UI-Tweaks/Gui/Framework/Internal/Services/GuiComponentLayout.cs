namespace BitzArt.UI.Tweaks.Gui;

internal static class GuiComponentLayout
{
    /// <summary>
    /// Resolves the allocated component size for a layout-participating slot.
    /// The returned size includes padding and excludes margin.
    /// </summary>
    internal static GuiMeasuredSize ResolveAllocatedSize(
        IGuiComponent component,
        double availableWidth,
        double availableHeight)
    {
        var layoutParameters = component.LayoutParameters;

        GuiMeasuredSize? measuredContent = null;
        GuiMeasuredSize GetMeasuredContent() =>
            measuredContent ??= component.Measure(
                ClampNonNegative(availableWidth - layoutParameters.Padding.Horizontal),
                ClampNonNegative(availableHeight - layoutParameters.Padding.Vertical));

        double width = layoutParameters.Width.CanResolve(availableWidth)
            ? layoutParameters.Width.Resolve(availableWidth)
            : layoutParameters.WidthMode == GuiSizeMode.FitContent || double.IsPositiveInfinity(availableWidth)
                ? ClampNonNegative(GetMeasuredContent().Width) + layoutParameters.Padding.Horizontal
                : ClampNonNegative(availableWidth);

        double height = layoutParameters.Height.CanResolve(availableHeight)
            ? layoutParameters.Height.Resolve(availableHeight)
            : layoutParameters.HeightMode == GuiSizeMode.FitContent || double.IsPositiveInfinity(availableHeight)
                ? ClampNonNegative(GetMeasuredContent().Height) + layoutParameters.Padding.Vertical
                : ClampNonNegative(availableHeight);

        return new GuiMeasuredSize(width, height);
    }

    private static double ClampNonNegative(double value)
        => value > 0 ? value : 0;
}
