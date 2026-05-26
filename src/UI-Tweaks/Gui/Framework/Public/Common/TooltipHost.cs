using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Per-dialog tooltip controller. Published at the dialog root as a cascading value so
/// every <see cref="GuiTooltip"/> in the subtree can register itself during the main
/// paint walk. Owns a per-paint tooltip-region table that the dialog renderer queries
/// from <c>DispatchMouseMove</c> to drive hover transitions independently of the regular
/// slot-region hover (which is single-region topmost — a tooltip wrapper's hover would
/// otherwise be shadowed by an inner interactive child like a button).
/// <para>
/// Hover transitions activate the dialog's floating tooltip layer with a cursor-anchored
/// placement and an automatic <see cref="GuiTooltipBackground"/> wrapper around the user
/// content. The floating layer owns the Cairo surface and the blit.
/// </para>
/// </summary>
public sealed class TooltipHost
{
    private const double TooltipMaxLogicalWidth = 320;
    private const double TooltipMaxLogicalHeight = 600;

    // Matches vanilla GuiElementHoverText cursor offset (+10x, +15y logical pixels).
    private const double CursorOffsetX = 10;
    private const double CursorOffsetY = 15;

    private static readonly object LayerToken = new();
    private static readonly FloatingLayerAnchor CursorAnchor = ComputeCursorAnchor;

    private readonly FloatingLayerRenderer _layer;

    private readonly List<Region> _regions = [];

    // Identity of the tooltip currently shown — the GuiTooltip instance reference, stable
    // across rebuilds because slots are keyed by (Type, key). Used to detect transitions
    // (cursor moved onto a different / no tooltip ⇒ swap or hide).
    private object? _activeRegionToken;

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

    internal TooltipHost(FloatingLayerRenderer layer) => _layer = layer;

    internal void ResetFrame() => _regions.Clear();

    internal void AddRegion(object token, GuiComponentBounds bounds, GuiRenderFragment content, Action<GuiTooltipBackground>? configureBackground)
        => _regions.Add(new Region(bounds, token, content, configureBackground));

    /// <summary>
    /// Hit-tests the cursor against the latest tooltip region table and activates the
    /// matching tooltip on the floating layer. Walks regions in reverse so a later-rendered
    /// tooltip wraps a deeper / topmost one when nested.
    /// </summary>
    internal void UpdateHover(double lx, double ly)
    {
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            var r = _regions[i];
            if (!r.Contains(lx, ly))
            {
                continue;
            }

            if (!ReferenceEquals(_activeRegionToken, r.Token))
            {
                _activeRegionToken = r.Token;
                ShowTooltip(r.Content, r.ConfigureBackground);
            }
            return;
        }

        if (_activeRegionToken is not null)
        {
            _activeRegionToken = null;
            _layer.Hide(LayerToken);
        }
    }

    private void ShowTooltip(GuiRenderFragment userContent, Action<GuiTooltipBackground>? configureBackground)
    {
        // Wrap the user content in the standard tooltip chrome. The wrapping container
        // slot is keyed by 0 so its instance persists across transitions where only the
        // wrapped fragment changes.
        GuiRenderFragment wrapped = builder =>
        {
            var slot = builder.AddContainer<GuiTooltipBackground>(
                0,
                padding: new GuiThickness(GuiTooltipBackground.DefaultPadding),
                content: userContent);
            if (configureBackground is not null)
            {
                slot.Configure(configureBackground);
            }
        };

        var placement = new FloatingLayerPlacement
        {
            Anchor = CursorAnchor,
            MaxLogicalWidth = TooltipMaxLogicalWidth,
            MaxLogicalHeight = TooltipMaxLogicalHeight,
        };

        _layer.Show(LayerToken, wrapped, placement);
    }

    /// <summary>
    /// Unconditionally hides any active tooltip. Called when the dialog closes, loses focus,
    /// or a drag begins (the cursor is captured and tooltips would visually trail the drag).
    /// </summary>
    internal void Hide()
    {
        if (_activeRegionToken is null)
        {
            return;
        }

        _activeRegionToken = null;
        _layer.Hide(LayerToken);
    }

    private static (double posX, double posY) ComputeCursorAnchor(double physW, double physH, float scale, ICoreClientAPI clientApi)
    {
        int mouseX = clientApi.Input.MouseX;
        int mouseY = clientApi.Input.MouseY;
        double posX = mouseX + CursorOffsetX * scale;
        double posY = mouseY + CursorOffsetY * scale;

        double frameWidth = clientApi.Render.FrameWidth;
        double frameHeight = clientApi.Render.FrameHeight;

        if (posX + physW > frameWidth)
        {
            posX = frameWidth - physW;
        }

        if (posY + physH > frameHeight)
        {
            posY = mouseY - physH - 5 * scale; // flip above
        }

        if (posX < 0)
        {
            posX = 0;
        }

        if (posY < 0)
        {
            posY = 0;
        }

        return (posX, posY);
    }
}
