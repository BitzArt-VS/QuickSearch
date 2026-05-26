using Newtonsoft.Json;

namespace BitzArt.UI.Tweaks.Config;

public record TooltipsConfig
{
    [JsonProperty("environmentWidget", Order = 01)]
    public EnvironmentWidgetOptions EnvironmentWidget { get; set; } = new();

    [JsonProperty("healthbarTooltip", Order = 11)]
    public HealthbarTooltipOptions HealthbarTooltip { get; set; } = new();

    [JsonProperty("satietyTooltip", Order = 12)]
    public SatietyTooltipOptions SatietyTooltip { get; set; } = new();

    [JsonProperty("hungerTooltip", Order = 13)]
    public HungerRateTooltipOptions HungerTooltip { get; set; } = new();

    [JsonProperty("temporalStabilityTooltip", Order = 14)]
    public TemporalStabilityTooltipOptions TemporalStabilityTooltip { get; set; } = new();

    [JsonProperty("exampleCustomTooltip", Order = 21)]
    public ExampleCustomTooltipOptions ExampleCustomTooltip { get; set; } = new();

    [JsonProperty("customTooltips", Order = 22)]
    public List<CustomTooltipOptions> CustomTooltips { get; set; } = [];
}

