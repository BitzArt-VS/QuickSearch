namespace BitzArt.UI.Tweaks.Gui;

internal readonly record struct FloatingLayerPlacement
{
    public FloatingLayerAnchor Anchor { get; init; }

    public GuiLayoutSize? FixedLogicalSize { get; init; }

    public double MaxLogicalWidth { get; init; }

    public double MaxLogicalHeight { get; init; }

    public GuiSurfaceRenderer? InputHost { get; init; }

    public double InputRegionOffsetX { get; init; }

    public double InputRegionOffsetY { get; init; }

    public bool AutoClearWhenNotRefreshed { get; init; }

    public bool RewalkOnDialogWalk { get; init; }
}
