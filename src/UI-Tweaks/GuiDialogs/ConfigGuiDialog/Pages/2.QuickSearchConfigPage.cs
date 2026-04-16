using BitzArt.UI.Tweaks.Config;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class QuickSearchConfigPage(QuickSearchConfig config) : ConfigPage(Lang.Get($"{Constants.ModId}:config-page-quicksearch"))
{
    private const string EnableKey = "qs-enable";
    private const string ResultListHeightKey = "qs-result-list-height";

    private const int RowHeight = 30;
    private const int RowGap = 16;
    private const int ControlWidth = 160;
    private const int SwitchSize = 28;
    private const int SliderHeight = 20;

    private const int MinResultListHeight = 50;
    private const int MaxResultListHeight = 600;
    private const int ResultListHeightStep = 10;

    private readonly QuickSearchConfig _config = config;

    public override double ComposeContent(ICoreClientAPI clientApi, GuiComposer composer, ElementBounds bounds, Action saveConfig, Action<ConfigPage> pushPage)
    {
        double x = bounds.fixedX;
        double y = bounds.fixedY + ContentTopPadding;
        double labelWidth = bounds.fixedWidth - ControlWidth;

        AddPageTitle(composer, bounds, ref y);

        var enableOptionBounds = ElementBounds.Fixed(x, y, bounds.fixedWidth, RowHeight);
        var enableLabelBounds = ElementBounds.Fixed(enableOptionBounds.fixedX, enableOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, labelWidth, SwitchSize);
        var enableSwitchBounds = ElementBounds.Fixed(enableOptionBounds.fixedX + labelWidth, enableOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, SwitchSize, SwitchSize);

        var heightOptionBounds = ElementBounds.Fixed(x, enableOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var heightLabelBounds = ElementBounds.Fixed(heightOptionBounds.fixedX, heightOptionBounds.fixedY + (RowHeight - SliderHeight) / 2.0, labelWidth, SliderHeight);
        var heightSliderBounds = ElementBounds.Fixed(heightOptionBounds.fixedX + labelWidth, heightOptionBounds.fixedY + (RowHeight - SliderHeight) / 2.0, ControlWidth, SliderHeight);

        composer
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-quicksearch-enable"), TextFont, enableLabelBounds)
            .AddSwitch(val =>
            {
                _config.Enable = val;
                _config.NotifyPropertyChanged(nameof(QuickSearchConfig.Enable));
                saveConfig.Invoke();
            }, enableSwitchBounds, EnableKey, SwitchSize)
            .AddConfigHoverText("config-quicksearch-enable-tooltip", TextFont, enableOptionBounds, requiresRestart: true)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-quicksearch-result-list-height"), TextFont, heightLabelBounds)
            .AddSlider(val =>
            {
                _config.ResultListHeight = val;
                _config.NotifyPropertyChanged(nameof(QuickSearchConfig.ResultListHeight));
                saveConfig.Invoke();
                return true;
            }, heightSliderBounds, ResultListHeightKey)
            .AddConfigHoverText("config-quicksearch-result-list-height-tooltip", TextFont, heightOptionBounds);

        return heightOptionBounds.fixedY + heightOptionBounds.fixedHeight;
    }

    public override void OnComposed(GuiComposer composer)
    {
        composer.GetSwitch(EnableKey).SetValue(_config.Enable);
        composer.GetSlider(ResultListHeightKey).SetValues(_config.ResultListHeight, MinResultListHeight, MaxResultListHeight, ResultListHeightStep);
    }
}
