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
        SetDefaultLayoutParameters();
    }

    /// <inheritdoc/>
    public virtual GuiSize Measure(double availableWidth, double availableHeight)
        => default;

    /// <summary>
    /// Resets <see cref="LayoutParameters"/> to this component's canonical defaults.
    /// Called by the reconciler on every reuse before the new pass's configuration
    /// actions are applied, so that declarative blueprints express full state rather
    /// than deltas. The base implementation resets to <see cref="GuiComponentLayoutParameters"/>
    /// global defaults, then calls <see cref="SetDefaultLayoutParameters"/> to let
    /// subclasses restore component-specific defaults.
    /// </summary>
    internal void ResetLayoutParameters()
    {
        LayoutParameters.Reset();
        SetDefaultLayoutParameters();
    }

    /// <summary>
    /// Override to set component-specific <see cref="LayoutParameters"/> defaults.
    /// Called once during construction and again at the end of every
    /// <see cref="ResetLayoutParameters"/> cycle (i.e. on every reconciler reuse), so
    /// declared defaults are always in effect before configuration actions are applied.
    /// The base implementation is a no-op.
    /// </summary>
    protected virtual void SetDefaultLayoutParameters() { }
}
