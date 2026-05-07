namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Default base class for layout-participating components. Extends <see cref="GuiNode"/>
/// with the <see cref="LayoutParameters"/> bundle and a virtual <see cref="Measure"/>
/// hook consumed by the layout pass. Pure decorators that do not occupy layout space
/// should inherit from <see cref="GuiNode"/> directly instead.
/// </summary>
public abstract class GuiComponent : GuiNode, IGuiComponent
{
    public GuiComponentLayoutParameters LayoutParameters { get; }

    protected GuiComponent()
    {
        LayoutParameters = new GuiComponentLayoutParameters();
    }

    /// <inheritdoc/>
    public virtual GuiSize Measure(double availableWidth, double availableHeight)
        => default;

    /// <summary>
    /// Resets <see cref="LayoutParameters"/> to this component's canonical defaults.
    /// Called by the reconciler on every reuse before the new pass's configuration
    /// actions are applied, so that declarative blueprints express full state rather
    /// than deltas. The base implementation resets to <see cref="GuiComponentLayoutParameters"/>
    /// global defaults. Subclasses that set component-specific defaults in their
    /// constructor (e.g. a fixed height or <see cref="GuiSizeMode.Fill"/> width) must
    /// override and restore those defaults after calling <c>base</c>.
    /// </summary>
    internal virtual void ResetLayoutParameters() => LayoutParameters.Reset();
}
