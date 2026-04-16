using BitzArt.UI.Tweaks.Config;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class TooltipConfigPage(TooltipOptions config, string titleLangKey) : ConfigPage(Lang.Get(titleLangKey))
{
    private const string EnableKey = "tooltip-enable";

    private const int RowHeight = 30;
    private const int ControlWidth = 160;
    private const int SwitchSize = 28;

    private readonly TooltipOptions _config = config;

    public override double ComposeContent(ICoreClientAPI clientApi, GuiComposer composer, ElementBounds bounds, Action saveConfig, Action<ConfigPage> pushPage)
    {
        double x = bounds.fixedX;
        double y = bounds.fixedY + ContentTopPadding;
        double labelWidth = bounds.fixedWidth - ControlWidth;

        AddPageTitle(composer, bounds, ref y);

        var enableOptionBounds = ElementBounds.Fixed(x, y, bounds.fixedWidth, RowHeight);
        var enableLabelBounds = ElementBounds.Fixed(enableOptionBounds.fixedX, enableOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, labelWidth, SwitchSize);
        var enableSwitchBounds = ElementBounds.Fixed(enableOptionBounds.fixedX + labelWidth, enableOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, SwitchSize, SwitchSize);

        composer
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-enable"), TextFont, enableLabelBounds)
            .AddSwitch(val =>
            {
                _config.Enable = val;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Enable));
                saveConfig.Invoke();
            }, enableSwitchBounds, EnableKey, SwitchSize)
            .AddConfigHoverText("config-tooltip-enable-tooltip", TextFont, enableOptionBounds);

        return enableOptionBounds.fixedY + enableOptionBounds.fixedHeight;
    }

    public override void OnComposed(GuiComposer composer)
    {
        composer.GetSwitch(EnableKey).SetValue(_config.Enable);
    }
}
