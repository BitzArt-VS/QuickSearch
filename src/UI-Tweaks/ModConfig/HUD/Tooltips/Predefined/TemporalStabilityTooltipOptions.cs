using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Config;

public record TemporalStabilityTooltipOptions : TooltipOptions
{
    public override bool Enable { get; set; } = false;

    public override string ComponentName => "ui-tweaks-tooltips-temporal-stability";
    public override string Format { get; set; } = "<font align=left>{player-temporal-stability}%</font><font align=right>({player-location-temporal-stability}%)</font>";

    public override string DialogArea { get; set; } = EnumDialogArea.CenterBottom.ToString();
    public override double Height { get; set; } = 20;
    public override double Width { get; set; } = 100;
    public override ComponentOffset Offset { get; set; } = new()
    {
        X = 0.0,
        Y = -60.0
    };

    public override ComponentPadding Padding { get; set; } = new(-2, 4, 0, 4);

    public override bool HasBackground { get; set; } = true;
    public override double BackgroundOpacity { get; set; } = 1.0;
    public override double BackgroundCornerRadius { get; set; } = 0.0;
}

