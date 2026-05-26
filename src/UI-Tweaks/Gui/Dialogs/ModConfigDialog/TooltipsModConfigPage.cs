using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class TooltipsModConfigPage : GuiComponent, IModConfigPage
{
    public static string PageName => Lang.Get($"{Constants.ModId}:config-page-tooltips");

    private const double ListItemHeight = 44;

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

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        builder.ConfigureLayout(layout => layout.Padding = new(0));
    }

    public override void OnParametersSet()
    {
        _context = GetCascadingValue<ModConfigContext>();
        _navigator = GetCascadingValue<ModConfigPageNavigator>();
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        builder.AddContainer(0,
            widthMode: GuiSizeMode.Fill,
            content: column =>
            {
                for (int i = 0; i < PredefinedTooltips.Length; i++)
                {
                    int index = i;
                    column.Add<ConfigListRow>(index,
                        height: ListItemHeight,
                        widthMode: GuiSizeMode.Fill)
                        .Configure(row =>
                        {
                            row.Text = Lang.Get(PredefinedTooltips[index].LangKey);
                            row.OnClick = (Action)(() => OpenTooltip(index));
                        });
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
