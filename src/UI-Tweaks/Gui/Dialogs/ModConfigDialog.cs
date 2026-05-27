using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

public class ModConfigDialog : Gui.GuiDialog, IDisposable
{
    private const int SaveDebounceMs = 10000;
    private static readonly GuiSize SidebarWidth = GuiSize.Fraction(0.2, minimum: 200);
    private const double SidebarSeparatorWidth = 1;
    private const double NavigationRowHeight = 44;

    private static readonly GuiColor SidebarPanelFillColor = GuiColor.FromRgba(0.08, 0.06, 0.04, 0.32);
    private static readonly GuiColor ContentPanelFillColor = GuiColor.FromRgba(0.15, 0.11, 0.08, 0.20);
    private static readonly GuiColor SidebarSeparatorColor = GuiColor.FromRgba(0, 0, 0, 0.34);
    private static readonly GuiColor BreadcrumbSeparatorColor = GuiColor.FromRgba(0.78, 0.69, 0.58, 0.10);

    private sealed record NavPage(string Label, GuiRenderFragment Content);

    private static readonly NavPage[] NavItems =
    [
        CreateNavPage<GeneralModConfigPage>(),
        CreateNavPage<ZoomModConfigPage>(),
        CreateNavPage<TooltipsModConfigPage>(),
    ];

    private static NavPage CreateNavPage<T>() where T : IModConfigPage, new()
        => new(T.PageName, b => b.Add<T>(0, widthMode: GuiSizeMode.Fill));

    private UiTweaksModConfig? _config;
    private ModConfigContext? _context;
    private Debouncer? _saveDebouncer;
    private ModConfigPageNavigator? _navigator;

    private ModConfigContext Context => _context
        ?? throw new InvalidOperationException($"{nameof(ModConfigDialog)} has not been configured.");

    private ModConfigPageNavigator Navigator => _navigator
        ?? throw new InvalidOperationException($"{nameof(ModConfigDialog)} has not been configured.");

    public ModConfigDialog()
    {
        IsResizable = true;
        MinWidth = 600;
        MinHeight = 360;
    }

    public void Configure(UiTweaksModConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (ReferenceEquals(_config, config) && _context is not null && _navigator is not null)
        {
            return;
        }

        _saveDebouncer?.Flush();
        _saveDebouncer?.Dispose();

        _config = config;
        _saveDebouncer = new Debouncer(
            TimeSpan.FromMilliseconds(SaveDebounceMs),
            () => ClientApi.StoreModConfig(config, Constants.ModConfigFileName));
        _context = new ModConfigContext(config, _saveDebouncer.Trigger);

        var initialPage = CreateNavPage<GeneralModConfigPage>();
        _navigator = new ModConfigPageNavigator(() => RequestReconcile(), initialPage.Label, initialPage.Content);
    }

    public void Dispose()
    {
        _saveDebouncer?.Flush();
        _saveDebouncer?.Dispose();
        _saveDebouncer = null;
    }

    protected override void OnResizeUpdated(bool sizeChanged)
    {
        RequestArrange();
    }

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        base.ConfigureSlot(builder);
        builder.ConfigureLayout(layoutParameters =>
        {
            layoutParameters.Width = 650;
            layoutParameters.Height = 520;
            layoutParameters.Padding = new GuiThickness(0);
        });
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        builder.AddCascadingValue(Context, builder =>
        builder.AddCascadingValue(Navigator, builder =>
        {
            builder.AddContainer(0, fill: true,
                content: builder =>
                {
                    builder
                        .AddDialogTitleBar(0, Lang.Get($"{Constants.ModId}:ui-tweaks-config"),
                            onDrag: Move, onClose: RequestClose);

                    builder
                        .AddDialogBackground(1, fill: true,
                            content: BuildBody);
                });
        }));
    }

    private void BuildBody(IGuiRenderTreeBuilder builder)
    {
        builder.AddContainer(0, fill: true, direction: GuiDirection.Horizontal,
            content: builder =>
            {
                builder.AddContainer(0,
                    width: SidebarWidth,
                    heightMode: GuiSizeMode.Fill,
                    background: SidebarPanelFillColor,
                    content: builder =>
                    {
                        for (int i = 0; i < NavItems.Length; i++)
                        {
                            int index = i;
                            var page = NavItems[index];
                            builder.Add<ConfigNavigationRow>(index,
                                height: NavigationRowHeight,
                                widthMode: GuiSizeMode.Fill)
                                .Configure(row =>
                                {
                                    row.Text = page.Label;
                                    row.IsSelected = Navigator.RootPageName == page.Label;
                                    row.OnClick = (Action)(() => SelectPage(page));
                                });
                        }
                    });

                builder.AddRectangle(1,
                    color: SidebarSeparatorColor,
                    width: SidebarSeparatorWidth,
                    heightMode: GuiSizeMode.Fill);

                builder.AddContainer(2,
                    fill: true,
                    background: ContentPanelFillColor,
                    content: builder =>
                    {
                        builder.AddContainer(0,
                            fill: true,
                            content: builder =>
                            {
                                builder.AddContainer(0,
                                    widthMode: GuiSizeMode.Fill,
                                    padding: new GuiThickness(Top: 14, Right: 10, Bottom: 8, Left: 10),
                                    content: builder =>
                                    {
                                        builder.Add<GuiBreadcrumbs>(0, widthMode: GuiSizeMode.Fill)
                                            .Configure(c =>
                                            {
                                                c.CurrentItem = Navigator.CurrentPageName;
                                                c.PreviousItems = Navigator.BreadcrumbPreviousItems;
                                                c.OnItemClicked = name => Navigator.PopToName(name);
                                            });

                                        builder.AddRectangle(1,
                                            color: BreadcrumbSeparatorColor,
                                            height: 2,
                                            widthMode: GuiSizeMode.Fill);
                                    });

                                builder.AddContainer<ConfigScrollPanel>(1,
                                    fill: true,
                                    margin: new GuiThickness(0, 8, 8, 8),
                                    content: Navigator.CurrentContent);
                            });
                    });
            });
    }

    private void SelectPage(NavPage page)
    {
        if (Navigator.IsAtRoot(page.Label))
        {
            return;
        }

        Navigator.NavigateToRoot(page.Label, page.Content);
    }

}
