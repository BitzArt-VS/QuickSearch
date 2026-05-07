using System;
using System.Collections.Generic;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Per-dialog tooltip controller. Published at the dialog root as a cascading value so
/// every <see cref="GuiTooltip"/> in the subtree can register itself during the main
/// render walk. Owns a per-frame tooltip-region table that the dialog renderer queries
/// from <c>DispatchMouseMove</c> to drive hover transitions independently of the regular
/// slot-region hover (which is single-region topmost — a tooltip wrapper's hover would
/// otherwise be shadowed by an inner interactive child like a button).
/// <para>
/// Hover transitions are forwarded to the dialog's <see cref="TooltipRenderer"/>, which
/// owns the actual Cairo surface that the active tooltip is drawn into.
/// </para>
/// </summary>
public sealed class TooltipHost
{
    private readonly TooltipRenderer _renderer;

    // Regions registered during the current frame's render walk. Repopulated from scratch
    // each render walk via ResetFrame() + AddRegion(); reused across non-render frames so
    // hover updates against an unchanged layout still hit-test correctly.
    private readonly List<Region> _regions = [];

    // The tooltip currently shown, identified by the GuiTooltip instance reference.
    // Used to detect transitions in UpdateHover (cursor moved off the trigger ⇒ hide).
    private object? _activeToken;

    private readonly struct Region
    {
        public readonly GuiComponentBounds Bounds;
        public readonly object Token;
        public readonly GuiRenderFragment Content;
        public readonly Action<GuiTooltipBackground>? ConfigureBackground;

        public Region(GuiComponentBounds bounds, object token, GuiRenderFragment content, Action<GuiTooltipBackground>? configureBackground)
        {
            Bounds = bounds;
            Token = token;
            Content = content;
            ConfigureBackground = configureBackground;
        }

        public bool Contains(double x, double y) =>
            x >= Bounds.X && x < Bounds.Right &&
            y >= Bounds.Y && y < Bounds.Bottom;
    }

    internal TooltipHost(TooltipRenderer renderer) => _renderer = renderer;

    /// <summary>
    /// Clears the per-frame region table. Called by the dialog renderer at the start of
    /// each render walk before <see cref="GuiTooltip.Render"/> repopulates it.
    /// </summary>
    internal void ResetFrame() => _regions.Clear();

    /// <summary>
    /// Registers a tooltip-trigger region during the render walk. Called by
    /// <see cref="GuiTooltip"/>.<see cref="GuiTooltip.Render"/> after its bounds are known.
    /// </summary>
    internal void AddRegion(object token, GuiComponentBounds bounds, GuiRenderFragment content, Action<GuiTooltipBackground>? configureBackground)
        => _regions.Add(new Region(bounds, token, content, configureBackground));

    /// <summary>
    /// Hit-tests the cursor against the per-frame tooltip region table and updates the
    /// active tooltip on the underlying <see cref="TooltipRenderer"/>.
    /// <para>
    /// Walks regions in reverse so a later-rendered tooltip wraps a deeper / topmost one
    /// when nested. Tracks identity via the <see cref="GuiTooltip"/> instance reference,
    /// which is stable across rebuilds (slots are keyed by <c>(Type, key)</c>).
    /// </para>
    /// </summary>
    internal void UpdateHover(double lx, double ly)
    {
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            var r = _regions[i];
            if (!r.Contains(lx, ly)) continue;

            if (!ReferenceEquals(_activeToken, r.Token))
            {
                _activeToken = r.Token;
                _renderer.SetTooltip(r.Content, r.ConfigureBackground);
            }
            return;
        }

        if (_activeToken is not null)
        {
            _activeToken = null;
            _renderer.SetTooltip(null, null);
        }
    }

    /// <summary>
    /// Unconditionally hides any active tooltip. Called when the dialog closes, loses focus,
    /// or a drag begins (the cursor is captured and tooltips would visually trail the drag).
    /// </summary>
    internal void Hide()
    {
        if (_activeToken is null) return;
        _activeToken = null;
        _renderer.SetTooltip(null, null);
    }
}
