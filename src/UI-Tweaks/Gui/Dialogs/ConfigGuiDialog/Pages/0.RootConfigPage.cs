using BitzArt.UI.Tweaks.Config;
using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

internal sealed class RootConfigPage(UiTweaksModConfig config) : ConfigPage(Constants.ModName)
{
    public override double ComposeContent(ICoreClientAPI clientApi, GuiComposer composer, ElementBounds bounds, Action saveConfig, Action<ConfigPage> pushPage)
    {
        double buttonX = bounds.fixedX + (bounds.fixedWidth - NavButtonWidth) / 2.0;
        double y = bounds.fixedY + ContentTopPadding;

        AddPageTitle(composer, bounds, ref y);

        AddNavButton(composer, $"{Constants.ModId}:config-page-hud", "nav-hud", buttonX, ref y,
            () => pushPage(new HudConfigPage(config.Hud)));
        AddNavButton(composer, $"{Constants.ModId}:config-page-quicksearch", "nav-quicksearch", buttonX, ref y,
            () => pushPage(new QuickSearchConfigPage(config.QuickSearch)));

        return y;
    }
}
