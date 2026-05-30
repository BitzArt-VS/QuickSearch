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
    public static GuiLayoutSize MeasureContent(
        IReadOnlyList<IGuiComponentSlot> slots,
        GuiLayoutSize available,
        GuiDirection direction)
    {
        double totalWidth = 0;
        double totalHeight = 0;

        AccumulateContent(slots, available, direction, ref totalWidth, ref totalHeight);

        return new GuiLayoutSize(totalWidth, totalHeight);
    }

    /// <summary>
    /// Resolves the allocated component size for a layout-participating slot.
    /// The returned size includes padding and excludes margin.
    /// </summary>
    public static GuiLayoutSize ResolveAllocatedSize(
        IGuiComponent component,
        GuiLayoutSize available)
    {
        var layoutParameters = component.LayoutParameters;

        GuiLayoutSize? measuredContent = null;
        GuiLayoutSize GetMeasuredContent() =>
            measuredContent ??= component.Measure(
                new GuiLayoutSize(
                    ClampNonNegative(available.Width - layoutParameters.Padding.Horizontal),
                    ClampNonNegative(available.Height - layoutParameters.Padding.Vertical)));

        double width = layoutParameters.Width.CanResolve(available.Width)
            ? layoutParameters.Width.Resolve(available.Width)
            : layoutParameters.WidthMode == GuiSizeMode.FitContent || double.IsPositiveInfinity(available.Width)
                ? ClampNonNegative(GetMeasuredContent().Width) + layoutParameters.Padding.Horizontal
                : ClampNonNegative(available.Width);

        double height = layoutParameters.Height.CanResolve(available.Height)
            ? layoutParameters.Height.Resolve(available.Height)
            : layoutParameters.HeightMode == GuiSizeMode.FitContent || double.IsPositiveInfinity(available.Height)
                ? ClampNonNegative(GetMeasuredContent().Height) + layoutParameters.Padding.Vertical
                : ClampNonNegative(available.Height);

        return new GuiLayoutSize(width, height);
    }

    private static void AccumulateContent(
        IReadOnlyList<IGuiComponentSlot> slots,
        GuiLayoutSize available,
        GuiDirection direction,
        ref double totalWidth,
        ref double totalHeight)
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];

            if (slot.Node is not IGuiComponent component)
            {
                AccumulateContent(slot.Children, available, direction, ref totalWidth, ref totalHeight);
                continue;
            }

            var layoutParameters = component.LayoutParameters;
            if (layoutParameters.Positioning == GuiComponentPositioning.Absolute)
            {
                continue;
            }

            double childAvailableWidth = ClampNonNegative(available.Width - layoutParameters.Margin.Horizontal);
            double childAvailableHeight = ClampNonNegative(available.Height - layoutParameters.Margin.Vertical);

            var childSize = ResolveAllocatedSize(component, new GuiLayoutSize(childAvailableWidth, childAvailableHeight));

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
