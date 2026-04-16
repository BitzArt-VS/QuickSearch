using BitzArt.UI.Tweaks.Config;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class TooltipsConfigPage(TooltipsConfig config) : ConfigPage(Lang.Get($"{Constants.ModId}:config-page-tooltips"))
{
    private readonly TooltipsConfig _config = config;

    public override double ComposeContent(ICoreClientAPI clientApi, GuiComposer composer, ElementBounds bounds, Action saveConfig, Action<ConfigPage> pushPage)
    {
        double buttonX = bounds.fixedX + (bounds.fixedWidth - NavButtonWidth) / 2.0;
        double y = bounds.fixedY + ContentTopPadding;

        AddPageTitle(composer, bounds, ref y);

        AddNavButton(composer, $"{Constants.ModId}:config-page-env-widget", "nav-env-widget", buttonX, ref y,
            () => pushPage(new TooltipConfigPage(_config.EnvironmentWidget, $"{Constants.ModId}:config-page-env-widget")));
        AddNavButton(composer, $"{Constants.ModId}:config-page-healthbar", "nav-healthbar", buttonX, ref y,
            () => pushPage(new TooltipConfigPage(_config.HealthbarTooltip, $"{Constants.ModId}:config-page-healthbar")));
        AddNavButton(composer, $"{Constants.ModId}:config-page-satiety", "nav-satiety", buttonX, ref y,
            () => pushPage(new TooltipConfigPage(_config.SatietyTooltip, $"{Constants.ModId}:config-page-satiety")));
        AddNavButton(composer, $"{Constants.ModId}:config-page-hunger-rate", "nav-hunger-rate", buttonX, ref y,
            () => pushPage(new TooltipConfigPage(_config.HungerTooltip, $"{Constants.ModId}:config-page-hunger-rate")));
        AddNavButton(composer, $"{Constants.ModId}:config-page-temporal-stability", "nav-temporal-stability", buttonX, ref y,
            () => pushPage(new TooltipConfigPage(_config.TemporalStabilityTooltip, $"{Constants.ModId}:config-page-temporal-stability")));

        for (int i = 0; i < _config.CustomTooltips.Count; i++)
        {
            var buttonBounds = ElementBounds.Fixed(buttonX, y, NavButtonWidth, NavButtonHeight);
            composer.AddSmallButton(_config.CustomTooltips[i].Name, () => true, buttonBounds, key: $"nav-custom-{i}");
            y += NavButtonHeight + NavButtonGap;
        }

        return y;
    }
}
