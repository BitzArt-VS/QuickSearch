using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Config;

public record HungerRateTooltipOptions : TooltipOptions
{
    public override bool Enable { get; set; } = false;

    public override string ComponentName => "ui-tweaks-tooltips-hunger";
    public override string Format { get; set; } = "{player-satiety-hunger}%";

    public override string DialogArea { get; set; } = EnumDialogArea.CenterBottom.ToString();
    public override double Height { get; set; } = 20;
    public override double Width { get; set; } = 44;
    public override bool CenterText { get; set; } = true;
    public override ComponentOffset Offset { get; set; } = new()
    {
        X = 354.0,
        Y = -88.0
    };

    public override ComponentPadding Padding { get; set; } = new(-2, 0, 0, 0);

    public override bool HasBackground { get; set; } = true;
    public override double BackgroundCornerRadius { get; set; } = 0.0;
}

