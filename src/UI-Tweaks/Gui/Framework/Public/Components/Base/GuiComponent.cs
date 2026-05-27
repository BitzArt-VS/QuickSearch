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

        return GuiComponentLayout.MeasureContent(
            RenderHandle.Slot.Children,
            availableWidth,
            availableHeight,
            LayoutParameters.Direction);
    }

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
