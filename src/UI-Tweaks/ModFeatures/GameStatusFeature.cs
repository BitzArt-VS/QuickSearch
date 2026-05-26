using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Services;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

public class GameStatusFeature(UiTweaksModSystem modSystem, UiTweaksModConfig config)
    : ModSystemFeature<UiTweaksModSystem, UiTweaksModConfig>(modSystem, config)
{
    private GameStatusService? _gameStatusService;
    private readonly List<HudTooltipLabel> _tooltipLabels = [];

    public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Client;

    public override void Start(ICoreClientAPI clientApi)
    {
        _gameStatusService = new(clientApi, Config.GameTweaks);

        List<IHudTooltipConfiguration> tooltipConfigurations =
        [
            Config.Hud.Tooltips.EnvironmentWidget,

            Config.Hud.Tooltips.HealthbarTooltip,
            Config.Hud.Tooltips.TemporalStabilityTooltip,
            Config.Hud.Tooltips.SatietyTooltip,
            Config.Hud.Tooltips.HungerTooltip,

            // Not enabling custom tooltip,
            // it is provided for config demonstration purposes only.
            // config.Hud.Tooltips.ExampleCustomTooltip

            ..Config.Hud.Tooltips.CustomTooltips
        ];

        foreach (var tooltipConfig in tooltipConfigurations)
        {
            try
            {
                _tooltipLabels.Add(new(clientApi, _gameStatusService, tooltipConfig));
            }
            catch (Exception ex)
            {
                clientApi.Logger.Error($"Failed to initialize tooltip '{tooltipConfig.ComponentName}'. It will not be added.");
                clientApi.Logger.Error(ex);
            }

        }
    }

    public override void Dispose()
    {
        _gameStatusService?.Dispose();
        _gameStatusService = null;

        foreach (var label in _tooltipLabels)
        {
            label.Dispose();
        }

        _tooltipLabels.Clear();

        GC.SuppressFinalize(this);
    }
}
