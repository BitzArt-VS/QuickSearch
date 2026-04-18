using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Config;

public record HealthbarTooltipOptions : TooltipOptions
{
    public override bool Enable { get; set; } = false;

    public override string ComponentName => "ui-tweaks-tooltips-healthbar";
    public override string Format { get; set; } = "<font align=left>{player-health-current} / {player-health-max}</font><font align=right>({player-health-percent}%)</font>";

    public override string DialogArea { get; set; } = EnumDialogArea.CenterBottom.ToString();
    public override double Height { get; set; } = 20;
    public override double Width { get; set; } = 140;

    public override ComponentOffset Offset { get; set; } = new()
    {
        X = -250.0,
        Y = -88.0
    };

    public override ComponentPadding Padding { get; set; } = new(-2, 4, 0, 4);

    public override bool HasBackground { get; set; } = true;
    public override double BackgroundCornerRadius { get; set; } = 0.0;
}

