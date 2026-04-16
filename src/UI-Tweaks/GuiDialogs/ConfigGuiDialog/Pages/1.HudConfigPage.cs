using BitzArt.UI.Tweaks.Config;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class HudConfigPage(HudConfig config) : ConfigPage(Lang.Get($"{Constants.ModId}:config-page-hud"))
{
    private readonly HudConfig _config = config;

    public override double ComposeContent(ICoreClientAPI clientApi, GuiComposer composer, ElementBounds bounds, Action saveConfig, Action<ConfigPage> pushPage)
    {
        double buttonX = bounds.fixedX + (bounds.fixedWidth - NavButtonWidth) / 2.0;
        double y = bounds.fixedY + ContentTopPadding;

        AddPageTitle(composer, bounds, ref y);

        AddNavButton(composer, $"{Constants.ModId}:config-page-tooltips", "nav-tooltips", buttonX, ref y,
            () => pushPage(new TooltipsConfigPage(_config.Tooltips)));

        return y;
    }
}
