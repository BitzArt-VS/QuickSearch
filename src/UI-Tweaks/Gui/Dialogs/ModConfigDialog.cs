using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

public class ModConfigDialog : Gui.GuiDialog
{
    private const int SaveDebounceMs = 10000;

    private abstract record NavItem;
    private sealed record NavSection(string Label) : NavItem;
    private sealed record NavPage(string Label, GuiRenderFragment Content) : NavItem;

    private static readonly NavItem[] NavItems =
    [
        new NavSection(Lang.Get($"{Constants.ModId}:config-page-general")),
        CreateNavPage<QuickSearchModConfigPage>(),
        new NavSection(Lang.Get($"{Constants.ModId}:config-page-hud")),
        CreateNavPage<TooltipsModConfigPage>(),
    ];

    private static NavPage CreateNavPage<T>() where T : IModConfigPage, new()
        => new(T.PageName, b => b.Add<T>(0, widthMode: GuiSizeMode.Fill));

    private readonly ICoreClientAPI _clientApi;
    private readonly UiTweaksModConfig _config;
    private readonly ModConfigContext _context;
    private readonly Debouncer _saveDebouncer;
    private readonly ModConfigPageNavigator _navigator;

    public ModConfigDialog(ICoreClientAPI clientApi, UiTweaksModConfig config) : base(clientApi)
    {
        _clientApi = clientApi;
        _config = config;
        _saveDebouncer = new Debouncer(
            TimeSpan.FromMilliseconds(SaveDebounceMs),
            () => _clientApi.StoreModConfig(_config, Constants.ModConfigFileName));
        _context = new ModConfigContext(_config, _saveDebouncer.Trigger);

        var initialPage = CreateNavPage<QuickSearchModConfigPage>();
        _navigator = new ModConfigPageNavigator(() => StateHasChanged(), initialPage.Label, initialPage.Content);

        LayoutParameters.Width = 600;
        LayoutParameters.Height = 600;
        LayoutParameters.Padding = new GuiThickness(0);

        IsResizable = true;
        MinWidth = 600;
        MinHeight = 300;
    }

    public override void Dispose()
    {
        _saveDebouncer.Flush();
        base.Dispose();
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        builder.AddCascadingValue(_context, builder =>
        builder.AddCascadingValue(_navigator, builder =>
        {
            builder
                .AddDialogTitleBar(0, Lang.Get($"{Constants.ModId}:ui-tweaks-config"),
                    onDrag: Move, onClose: Close)
                .AddDialogBackground(1, fill: true,
                    padding: new(GuiVanillaStyle.ElementToDialogPadding),
                    content: BuildBody);
        }));
    }

    private void BuildBody(IGuiRenderTreeBuilder builder)
    {
        builder.AddContainer(0, fill: true, direction: GuiDirection.Horizontal,
            content: builder =>
            {
                // Nav column — narrow, fixed width.
                builder.AddContainer(0,
                    width: Math.Max(150, (int)LayoutParameters.Width!.Value / 4),
                    heightMode: GuiSizeMode.Fill,
                    content: builder =>
                    {
                        for (int i = 0; i < NavItems.Length; i++)
                        {
                            int idx = i;
                            switch (NavItems[idx])
                            {
                                case NavSection section:
                                    builder.AddLabel(idx, section.Label,
                                        font: GuiFontStyle.MediumBold,
                                        horizontalAlignment: GuiHorizontalAlignment.Center,
                                        margin: new GuiThickness(
                                            Top: idx == 0 ? 0 : GuiVanillaStyle.HalfPadding,
                                            Right: 0,
                                            Bottom: GuiVanillaStyle.HalfPadding,
                                            Left: 0));
                                    break;

                                case NavPage page:
                                    builder.AddButton(idx, page.Label,
                                        onClick: () => SelectPage(page),
                                        widthMode: GuiSizeMode.Fill,
                                        margin: new(0, 0, GuiVanillaStyle.HalfPadding, 0));
                                    break;
                            }
                        }
                    });

                // Page column — breadcrumbs above the scrollable inset content area.
                builder.AddContainer(1, fill: true, margin: new(0, 0, 0, 16),
                    content: builder =>
                    {
                        builder.Add<GuiBreadcrumbs>(0, widthMode: GuiSizeMode.Fill)
                            .Configure(c =>
                            {
                                c.CurrentItem = _navigator.CurrentPageName;
                                c.PreviousItems = _navigator.BreadcrumbPreviousItems;
                                c.OnItemClicked = name => _navigator.PopToName(name);
                            });

                        builder.AddContainer(1, fill: true, scroll: GuiScroll.Vertical,
                            withInset: true,
                            content: _navigator.CurrentContent);
                    });
            });
    }

    private void SelectPage(NavPage page)
    {
        if (_navigator.IsAtRoot(page.Label)) return;
        _navigator.NavigateToRoot(page.Label, page.Content);
    }

}

