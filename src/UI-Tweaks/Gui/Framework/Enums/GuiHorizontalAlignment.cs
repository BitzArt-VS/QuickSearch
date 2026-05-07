namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Horizontal alignment of a slot within the cross-axis space made available to it by
/// its parent (or, for absolute slots, within the parent's content area).
/// <para>
/// Effective only when the slot does not fill the available horizontal extent — i.e.
/// width resolved via <see cref="GuiSizeMode.FitContent"/> or an explicit
/// <see cref="GuiComponentLayoutParameters.Width"/> smaller than the available width.
/// <see cref="GuiSizeMode.Fill"/> consumes the full extent, leaving no slack to align
/// against, so this property is a no-op in that case.
/// </para>
/// <para>
/// For relative slots, alignment only takes effect on the cross axis — that is, when the
/// parent stacks vertically (<see cref="GuiDirection.Vertical"/>). In a horizontal stack
/// the flow axis is X and per-slot horizontal alignment does not apply; siblings flow
/// adjacently.
/// </para>
/// </summary>
public enum GuiHorizontalAlignment
{
    /// <summary>Pin to the left edge (default — preserves pre-alignment behaviour).</summary>
    Left = 0,

    /// <summary>Centre horizontally within the available extent.</summary>
    Center = 1,

    /// <summary>Pin to the right edge.</summary>
    Right = 2,
}
