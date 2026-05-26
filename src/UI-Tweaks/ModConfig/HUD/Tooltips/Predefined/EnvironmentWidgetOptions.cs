using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Config;

public record EnvironmentWidgetOptions : TooltipOptions
{
    public override bool Enable { get; set; } = false;

    public override string ComponentName => "ui-tweaks-env-widget";
    public override string Format { get; set; } = "<font align=left>{world-date-time:HH:mm}</font><font align=right>{player-location-temperature-celsius}°C</font><br>X: {player-location-coordinates-x}   Y: {player-location-coordinates-y}   Z: {player-location-coordinates-z}";
    public override ICollection<string>? ExtraElements { get; set; } =
    [
        "{world-date-time:d MMMM, Year y}"
    ];

    public override bool CenterText { get; set; } = true;

    public override string DialogArea { get; set; } = EnumDialogArea.RightTop.ToString();
    public override double Height { get; set; } = 54;
    public override double Width { get; set; } = 255;

    public override ComponentOffset Offset { get; set; } = new()
    {
        X = -9.0,
        Y = 270.0
    };

    public override ComponentPadding Padding { get; set; } = new(2, 17);

    public override bool HasBackground { get; set; } = true;
    public override double BackgroundOpacity { get; set; } = 0.25;
    public override double BackgroundCornerRadius { get; set; } = 0.0;
}

