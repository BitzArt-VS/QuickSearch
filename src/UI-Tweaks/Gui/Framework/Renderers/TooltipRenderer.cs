using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class TooltipRenderer : FloatingLayerRenderer
{
    private const double MaxWidth = 320;
    private const double MaxHeight = 600;

    // Matches vanilla GuiElementHoverText cursor offset (+10x, +15y logical pixels).
    private const double CursorOffsetX = 10;
    private const double CursorOffsetY = 15;

    public TooltipRenderer(ICoreClientAPI clientApi) : base(clientApi) { }

    internal bool HasActiveTooltip => IsActive;

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
        Builder.MeasureChildren(MaxWidth, MaxHeight, GuiDirection.Vertical);

    protected override (double posX, double posY) GetScreenPosition(int physW, int physH, float scale)
    {
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
