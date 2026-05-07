namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Vertical alignment of a slot within the cross-axis space made available to it by
/// its parent (or, for absolute slots, within the parent's content area).
/// <para>
/// Effective only when the slot does not fill the available vertical extent — i.e.
/// height resolved via <see cref="GuiSizeMode.FitContent"/> or an explicit
/// <see cref="GuiComponentLayoutParameters.Height"/> smaller than the available height.
/// <see cref="GuiSizeMode.Fill"/> consumes the full extent, leaving no slack to align
/// against, so this property is a no-op in that case.
/// </para>
/// <para>
/// For relative slots, alignment only takes effect on the cross axis — that is, when the
/// parent stacks horizontally (<see cref="GuiDirection.Horizontal"/>). In a vertical
/// stack the flow axis is Y and per-slot vertical alignment does not apply; siblings
/// flow adjacently.
/// </para>
/// </summary>
public enum GuiVerticalAlignment
{
    /// <summary>Pin to the top edge (default — preserves pre-alignment behaviour).</summary>
    Top = 0,

    /// <summary>Centre vertically within the available extent.</summary>
    Center = 1,

    /// <summary>Pin to the bottom edge.</summary>
    Bottom = 2,
}
