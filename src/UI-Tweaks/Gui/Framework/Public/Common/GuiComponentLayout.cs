namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Shared layout and measurement helpers for layout-participating components.
/// Use these from custom <see cref="IGuiComponent"/> implementations when you want
/// the same default sizing rules as the framework's built-in <see cref="GuiComponent"/> base.
/// </summary>
public static class GuiComponentLayout
{
    /// <summary>
    /// Measures a component's child slots using the framework's default stack-layout rules.
    /// Relative children participate in the sum; absolute children are skipped; transparent
    /// wrappers inline their inner children.
    /// </summary>
    public static GuiMeasuredSize MeasureContent(
        IReadOnlyList<IGuiComponentSlot> slots,
        double availableWidth,
        double availableHeight,
        GuiDirection direction)
    {
        double totalWidth = 0;
        double totalHeight = 0;

        AccumulateContent(slots, availableWidth, availableHeight, direction, ref totalWidth, ref totalHeight);

        return new GuiMeasuredSize(totalWidth, totalHeight);
    }

    /// <summary>
    /// Resolves the allocated component size for a layout-participating slot.
    /// The returned size includes padding and excludes margin.
    /// </summary>
    public static GuiMeasuredSize ResolveAllocatedSize(
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

    private static void AccumulateContent(
        IReadOnlyList<IGuiComponentSlot> slots,
        double availableWidth,
        double availableHeight,
        GuiDirection direction,
        ref double totalWidth,
        ref double totalHeight)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];

            if (slot.Node is not IGuiComponent component)
            {
                AccumulateContent(slot.Children, availableWidth, availableHeight, direction, ref totalWidth, ref totalHeight);
                continue;
            }

            var layoutParameters = component.LayoutParameters;
            if (layoutParameters.Positioning == GuiComponentPositioning.Absolute)
            {
                continue;
            }

            double childAvailableWidth = ClampNonNegative(availableWidth - layoutParameters.Margin.Horizontal);
            double childAvailableHeight = ClampNonNegative(availableHeight - layoutParameters.Margin.Vertical);

            var childSize = ResolveAllocatedSize(component, childAvailableWidth, childAvailableHeight);

            if (direction == GuiDirection.Vertical)
            {
                totalWidth = Math.Max(totalWidth, layoutParameters.Margin.Horizontal + childSize.Width);
                totalHeight += layoutParameters.Margin.Vertical + childSize.Height;
            }
            else
            {
                totalWidth += layoutParameters.Margin.Horizontal + childSize.Width;
                totalHeight = Math.Max(totalHeight, layoutParameters.Margin.Vertical + childSize.Height);
            }
        }
    }

    private static double ClampNonNegative(double value)
        => value > 0 ? value : 0;
}
