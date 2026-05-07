using System;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Bitwise combination of scroll axes. Used by <see cref="GuiContainer.Scroll"/>,
/// <see cref="GuiContainer.Scrollbar"/> and <see cref="GuiContainer.AlwaysShowScrollbar"/>
/// to describe which axes participate in a given aspect of scrolling behaviour.
/// </summary>
[Flags]
public enum GuiScroll
{
    /// <summary>Neither axis is selected.</summary>
    None = 0,

    /// <summary>The vertical (Y) axis is selected.</summary>
    Vertical = 1 << 0,

    /// <summary>The horizontal (X) axis is selected.</summary>
    Horizontal = 1 << 1,

    /// <summary>Both axes are selected.</summary>
    Both = Vertical | Horizontal,
}
