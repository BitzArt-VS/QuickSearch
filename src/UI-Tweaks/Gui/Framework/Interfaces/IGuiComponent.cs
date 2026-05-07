namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// The framework contract for a layout-participating component. Extends <see cref="IGuiNode"/>
/// with the properties consumed by the layout pass: a <see cref="LayoutParameters"/>
/// bundle (margin, padding, size mode, direction, positioning) and an intrinsic
/// <see cref="Measure"/> hook for <see cref="GuiSizeMode.FitContent"/> sizing.
/// <para>
/// Custom components may implement this interface directly instead of inheriting from
/// <see cref="GuiComponent"/>, which is only the default base-class implementation. Pure
/// decorators that do not occupy layout space should implement only <see cref="IGuiNode"/>.
/// </para>
/// </summary>
public interface IGuiComponent : IGuiNode
{
    /// <summary>
    /// The component's layout parameters.
    /// </summary>
    public GuiComponentLayoutParameters LayoutParameters { get; }

    /// <summary>
    /// Returns the component's intrinsic (natural) size given the available content space.
    /// Called by the layout pass for <see cref="GuiSizeMode.FitContent"/> dimensions alongside
    /// child measurement; the larger of the two is used per dimension.
    /// The default returns <c>(0, 0)</c> — no intrinsic size, children alone determine the extent.
    /// Override in leaf components (e.g. text, icons) to report their natural size.
    /// Not called for <see cref="GuiSizeMode.Fill"/> — that always resolves to the available space.
    /// </summary>
    GuiSize Measure(double availableWidth, double availableHeight)
        => default;
}
