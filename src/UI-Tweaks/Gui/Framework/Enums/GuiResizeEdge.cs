using System;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Bit-flagged set of dialog edges currently engaged in a resize interaction. Sides combine
/// into corners (e.g. <c>North | East</c> = NE corner). <see cref="None"/> means no resize
/// is in progress / the cursor is not over any resize zone.
/// </summary>
[Flags]
internal enum GuiResizeEdge
{
    None  = 0,
    North = 1 << 0,
    South = 1 << 1,
    West  = 1 << 2,
    East  = 1 << 3,
}
