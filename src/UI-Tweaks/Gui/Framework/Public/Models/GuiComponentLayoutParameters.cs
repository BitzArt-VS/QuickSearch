namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Carries the layout and spacing configuration for a single slot in the render tree.
/// Created by <see cref="GuiRenderTreeBuilder"/> when a component is declared, then mutated
/// in place through the fluent <see cref="IGuiComponentBuilder"/> API; consumed by the layout pass.
/// </summary>
public sealed class GuiComponentLayoutParameters
{
    public GuiComponentPositioning Positioning { get; set; } = GuiComponentPositioning.Relative;

    /// <summary>Space outside the component's border, separating it from siblings and the parent edge.</summary>
    public GuiThickness Margin { get; set; } = GuiThickness.Zero;

    /// <summary>Space between the component's border and its content / children.</summary>
    public GuiThickness Padding { get; set; } = GuiThickness.Zero;

    /// <summary>
    /// Explicit width override. When <see cref="GuiSize.Auto"/> the size is determined by <see cref="WidthMode"/>.
    /// Takes priority over <see cref="WidthMode"/> when set.
    /// </summary>
    public GuiSize Width { get; set; } = GuiSize.Auto;

    /// <summary>
    /// Explicit height override. When <see cref="GuiSize.Auto"/> the size is determined by <see cref="HeightMode"/>.
    /// Takes priority over <see cref="HeightMode"/> when set.
    /// </summary>
    public GuiSize Height { get; set; } = GuiSize.Auto;

    /// <summary>
    /// How to resolve width when <see cref="Width"/> is <see cref="GuiSize.Auto"/>.
    /// <see cref="GuiSizeMode.Fill"/> stretches to available space.
    /// <see cref="GuiSizeMode.FitContent"/> uses measured content width plus padding.
    /// </summary>
    public GuiSizeMode WidthMode { get; set; } = GuiSizeMode.FitContent;

    /// <summary>
    /// How to resolve height when <see cref="Height"/> is <see cref="GuiSize.Auto"/>.
    /// <see cref="GuiSizeMode.Fill"/> stretches to available space.
    /// <see cref="GuiSizeMode.FitContent"/> uses measured content height plus padding.
    /// </summary>
    public GuiSizeMode HeightMode { get; set; } = GuiSizeMode.FitContent;

    /// <summary>Direction in which this component stacks its children.</summary>
    public GuiDirection Direction { get; set; } = GuiDirection.Vertical;

    /// <summary>
    /// Horizontal alignment of this slot within the available cross-axis space. Applies on
    /// the cross axis of relative slots whose parent stacks vertically, and on both axes of
    /// absolute slots. Has no effect when <see cref="WidthMode"/> is <see cref="GuiSizeMode.Fill"/>
    /// (no slack to align against). See <see cref="GuiHorizontalAlignment"/>.
    /// </summary>
    public GuiHorizontalAlignment HorizontalAlignment { get; set; } = GuiHorizontalAlignment.Left;

    /// <summary>
    /// Vertical alignment of this slot within the available cross-axis space. Applies on
    /// the cross axis of relative slots whose parent stacks horizontally, and on both axes
    /// of absolute slots. Has no effect when <see cref="HeightMode"/> is <see cref="GuiSizeMode.Fill"/>
    /// (no slack to align against). See <see cref="GuiVerticalAlignment"/>.
    /// </summary>
    public GuiVerticalAlignment VerticalAlignment { get; set; } = GuiVerticalAlignment.Top;

    /// <summary>
    /// Resets all properties to their documented defaults. Called by the reconciler on
    /// reused component slots before applying the current pass's configuration actions so
    /// that each blueprint pass declares a full, fresh state rather than a delta on top of
    /// the previous pass.
    /// </summary>
    internal void Reset()
    {
        Positioning = GuiComponentPositioning.Relative;
        Margin = GuiThickness.Zero;
        Padding = GuiThickness.Zero;
        Width = GuiSize.Auto;
        Height = GuiSize.Auto;
        WidthMode = GuiSizeMode.FitContent;
        HeightMode = GuiSizeMode.FitContent;
        Direction = GuiDirection.Vertical;
        HorizontalAlignment = GuiHorizontalAlignment.Left;
        VerticalAlignment = GuiVerticalAlignment.Top;
    }
}
