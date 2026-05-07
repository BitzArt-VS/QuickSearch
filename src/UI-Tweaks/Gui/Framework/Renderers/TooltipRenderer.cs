using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Floating-layer renderer for the per-dialog tooltip overlay. Wraps user-supplied
/// tooltip content in a default <see cref="GuiTooltipBackground"/> panel, follows the
/// cursor, and is purely visual — interactive regions registered by tooltip-internal
/// components are dropped.
/// <para>
/// All Cairo surface lifecycle, active-fragment storage, reconcile + measure + draw +
/// GPU upload, texture blit, and per-frame <see cref="Render"/> dispatch live in the
/// shared <see cref="FloatingLayerRenderer"/> base; this class only contributes the
/// cursor-tracking position and the tooltip-specific wrapping fragment. The default
/// base lifecycle (no-op <see cref="OnFrameStart"/> / <see cref="RunWalk"/>, full
/// <see cref="Render"/> = <c>Update + Blit</c>) is exactly what tooltips need.
/// </para>
/// </summary>
internal sealed class TooltipRenderer : FloatingLayerRenderer
{
    // Layout budget the tooltip is measured against. Caps the wrapper's natural width so
    // long single-line content doesn't run off-screen; height is generous since vanilla
    // tooltips can grow vertically.
    private const double MaxWidth = 320;
    private const double MaxHeight = 600;

    // Mouse-relative offset (vanilla's GuiElementHoverText: +10x, +15y from cursor in
    // logical pixels). Anchored on the cursor each frame so tooltips track the pointer.
    private const double CursorOffsetX = 10;
    private const double CursorOffsetY = 15;

    public TooltipRenderer(ICoreClientAPI clientApi) : base(clientApi) { }

    /// <summary>True when a tooltip is currently shown (or pending its first redraw).</summary>
    internal bool HasActiveTooltip => IsActive;

    /// <summary>
    /// Replaces the active tooltip content. Pass <c>null</c> to hide the tooltip.
    /// Wraps the user-supplied fragment in a default <see cref="GuiTooltipBackground"/>
    /// with vanilla styling; <paramref name="configureBackground"/> tunes that wrapper.
    /// Idempotent for repeated calls with the same content (no-op if nothing changes).
    /// </summary>
    internal void SetTooltip(GuiRenderFragment? userContent, Action<GuiTooltipBackground>? configureBackground)
    {
        if (userContent is null)
        {
            if (ActiveFragment is null) return;
            ActiveFragment = null;
            MarkDirty();
            return;
        }

        // Capture the user content + configure-action by closure once per Show. The
        // resulting delegate is reused across frames where the tooltip stays the same
        // (dirty flag stays false ⇒ no reconcile). Same key (0) every time so the wrapping
        // container slot persists and reuses its instance whenever the wrapped content changes.
        ActiveFragment = b =>
        {
            var slot = b.AddContainer<GuiTooltipBackground>(
                0,
                padding: new GuiThickness(GuiTooltipBackground.DefaultPadding),
                content: userContent);
            if (configureBackground is not null)
                slot.Configure(configureBackground);
        };
        MarkDirty();
    }

    protected override GuiSize ResolveLogicalSize() =>
        _builder.MeasureChildren(MaxWidth, MaxHeight, GuiDirection.Vertical);

    protected override (double posX, double posY) GetScreenPosition(int physW, int physH, float scale)
    {
        // Position relative to the cursor (vanilla offset 10/15 in logical pixels,
        // scaled to physical for the on-screen blit). Clamped to the viewport on both
        // axes; if the tooltip would overflow the bottom edge we flip it above the
        // cursor (mirroring vanilla GuiElementHoverText).
        int mx = _clientApi.Input.MouseX;
        int my = _clientApi.Input.MouseY;
        double posX = mx + CursorOffsetX * scale;
        double posY = my + CursorOffsetY * scale;

        double frameW = _clientApi.Render.FrameWidth;
        double frameH = _clientApi.Render.FrameHeight;

        if (posX + physW > frameW) posX = frameW - physW;
        if (posY + physH > frameH) posY = my - physH - 5 * scale; // flip above
        if (posX < 0) posX = 0;
        if (posY < 0) posY = 0;

        return (posX, posY);
    }
}
