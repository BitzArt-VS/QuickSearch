namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Default base class for layout-participating components. Extends <see cref="GuiNode"/>
/// with the <see cref="LayoutParameters"/> bundle and a virtual <see cref="Measure"/>
/// hook consumed by the layout pass. The default measurement walks the component's
/// mounted child slots and applies the framework's stack-layout sizing rules. Pure
/// decorators that do not occupy layout space should inherit from <see cref="GuiNode"/>
/// directly instead.
/// </summary>
public abstract class GuiComponent : GuiNode, IGuiComponent
{
    public GuiComponentLayoutParameters LayoutParameters { get; }

    protected GuiComponent()
    {
        LayoutParameters = new GuiComponentLayoutParameters();
    }

    /// <inheritdoc/>
    public virtual GuiMeasuredSize Measure(double availableWidth, double availableHeight)
    {
        if (RenderHandle is null)
        {
            return default;
        }

        return MeasureContent(
            RenderHandle.Slot.Children,
            availableWidth,
            availableHeight,
            LayoutParameters.Direction);
    }

    private static GuiMeasuredSize MeasureContent(
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

            var childSize = GuiComponentLayout.ResolveAllocatedSize(component, childAvailableWidth, childAvailableHeight);

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

    /// <summary>
    /// Requests a fresh arrange pass for the existing component tree. Arrange cascades into paint.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if the component is not attached to a render handle.</exception>
    protected void RequestArrange()
    {
        GetAttachedRenderHandle(nameof(RequestArrange)).RequestArrange();
    }

    /// <summary>
    /// Requests a repaint of the latest arranged component tree.
    /// </summary>
    /// <exception cref="System.InvalidOperationException">Thrown if the component is not attached to a render handle.</exception>
    protected void RequestPaint()
    {
        GetAttachedRenderHandle(nameof(RequestPaint)).RequestPaint();
    }

    /// <summary>
    /// Requests a redraw of the existing component tree without scheduling this component's
    /// render fragment for reconciliation.
    /// </summary>
    /// <remarks>
    /// Compatibility alias for <see cref="RequestPaint"/>.
    /// </remarks>
    /// <exception cref="System.InvalidOperationException">Thrown if the component is not attached to a render handle.</exception>
    protected void RequestRender()
    {
        RequestPaint();
    }

}
