using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Mouse event payload delivered to interactive component handlers.
/// <para>
/// <see cref="X"/>/<see cref="Y"/> are in <b>logical (unscaled) pixels</b>, relative to the
/// dialog's render surface — the same coordinate space the layout pass operates in. Use them
/// to test against <see cref="GuiComponentBounds"/> directly.
/// </para>
/// <para>
/// <see cref="ScreenX"/>/<see cref="ScreenY"/> are the raw vanilla screen coordinates in
/// <b>physical pixels</b>. They are useful for handlers (such as dialog dragging) that need a
/// frame-of-reference unaffected by movement of the dialog itself between events.
/// </para>
/// </summary>
public readonly record struct GuiMouseEventArgs(
    double X,
    double Y,
    int ScreenX,
    int ScreenY,
    EnumMouseButton Button,
    int Modifiers);
