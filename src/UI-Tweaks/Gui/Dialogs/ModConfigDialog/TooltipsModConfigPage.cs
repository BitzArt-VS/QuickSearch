using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class TooltipsModConfigPage : GuiComponent, IModConfigPage
{
    public static string PageName => Lang.Get($"{Constants.ModId}:config-page-tooltips");

    private const double ListItemSpacing = 6;
    private const double ListColumnWidth = 320;

    private readonly record struct TooltipEntry(string LangKey, Func<TooltipsConfig, TooltipOptions> Resolve);

    private static readonly TooltipEntry[] PredefinedTooltips =
    [
        new($"{Constants.ModId}:config-page-env-widget",            c => c.EnvironmentWidget),
        new($"{Constants.ModId}:config-page-healthbar",             c => c.HealthbarTooltip),
        new($"{Constants.ModId}:config-page-satiety",               c => c.SatietyTooltip),
        new($"{Constants.ModId}:config-page-hunger-rate",           c => c.HungerTooltip),
        new($"{Constants.ModId}:config-page-temporal-stability",    c => c.TemporalStabilityTooltip),
    ];

    private ModConfigContext? _context;
    private ModConfigPageNavigator? _navigator;

    protected override void SetDefaultLayoutParameters()
    {
        LayoutParameters.Padding = new(8);
    }

    public override void OnParametersSet()
    {
        _context = GetCascadingValue<ModConfigContext>();
        _navigator = GetCascadingValue<ModConfigPageNavigator>();
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        builder.AddContainer(0,
            width: ListColumnWidth,
            horizontalAlignment: GuiHorizontalAlignment.Center,
            content: column =>
            {
                for (int i = 0; i < PredefinedTooltips.Length; i++)
                {
                    int idx = i;
                    column.AddButton(idx, Lang.Get(PredefinedTooltips[idx].LangKey),
                        onClick: () => OpenTooltip(idx),
                        widthMode: GuiSizeMode.Fill,
                        margin: new GuiThickness(
                            Top: idx == 0 ? 0 : ListItemSpacing,
                            Right: 0, Bottom: 0, Left: 0));
                }
            });
    }

    private void OpenTooltip(int index)
    {
        var entry = PredefinedTooltips[index];
        var tooltipName = Lang.Get(entry.LangKey);
        var options = entry.Resolve(_context!.Config.Hud.Tooltips);
        _navigator!.Push(tooltipName,
            builder => builder.Add<TooltipDetailModConfigPage>(0, widthMode: GuiSizeMode.Fill)
                .Configure(c => c.Options = options));
    }
}
