using BitzArt.UI.Tweaks.Config;
using BitzArt.UI.Tweaks.Gui;
using System;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

public class ModConfigDialog : Gui.GuiDialog
{
    // Same debounce window as the legacy VanillaGuiDialog implementation — successive
    // edits collapse into a single write so we don't hammer the disk while a user drags
    // a slider.
    private const int SaveDebounceMs = 10000;

    // Sidebar nav model — entries are either a clickable page button or a non-clickable
    // section header that visually groups the buttons below it. Page entries are built
    // from the page type itself: the label comes from IModConfigPage.PageName so there
    // is no separate string to keep in sync.
    private abstract record NavItem;
    private sealed record NavSection(string Label) : NavItem;
    private sealed record NavPage(string Label, Type PageType) : NavItem;

    private static readonly NavItem[] NavItems =
    [
        new NavSection(Lang.Get($"{Constants.ModId}:config-page-general")),
        CreateNavPage<QuickSearchModConfigPage>(),
        new NavSection(Lang.Get($"{Constants.ModId}:config-page-hud")),
        CreateNavPage<TooltipsModConfigPage>(),
    ];

    private static NavPage CreateNavPage<T>() where T : IModConfigPage
        => new(T.PageName, typeof(T));

    private readonly ICoreClientAPI _clientApi;
    private readonly UiTweaksModConfig _config;
    private readonly ModConfigContext _context;

    private readonly Lock _saveDebounceLock = new();
    private CancellationTokenSource? _saveDebounce;

    private Type _selectedPageType = typeof(QuickSearchModConfigPage);

    public ModConfigDialog(ICoreClientAPI clientApi, UiTweaksModConfig config) : base(clientApi)
    {
        _clientApi = clientApi;
        _config = config;
        _context = new ModConfigContext(_config, LaunchSaveConfig);

        LayoutParameters.Width = 600;
        LayoutParameters.Height = 600;
        LayoutParameters.Padding = new GuiThickness(0);

        IsResizable = true;
        MinWidth = 600;
        MinHeight = 300;
    }

    public override void Dispose()
    {
        // Flush a pending debounced save synchronously before tearing the dialog down,
        // mirroring the legacy ModConfigGuiDialog.Dispose contract: "if there's an
        // outstanding save scheduled, write it out now so the user doesn't lose edits
        // when the mod unloads."
        bool hadPendingSave;
        lock (_saveDebounceLock)
        {
            hadPendingSave = _saveDebounce is not null;
            _saveDebounce?.Cancel();
            _saveDebounce?.Dispose();
            _saveDebounce = null;
        }

        if (hadPendingSave)
        {
            _clientApi.StoreModConfig(_config, Constants.ModConfigFileName);
        }

        base.Dispose();
    }

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        // Publish the shared (config + saveConfig) tuple to every page below — pages are
        // instantiated by the framework with a parameterless ctor, so they pull state via
        // GetCascadingValue<ModConfigContext>() in OnParametersSet rather than via
        // constructor injection.
        builder.AddCascadingValue(_context, builder =>
        {
            builder
                .AddDialogTitleBar(0, Lang.Get($"{Constants.ModId}:ui-tweaks-config"),
                    onDrag: Move, onClose: Close)
                .AddDialogBackground(1, fill: true,
                    padding: new(GuiVanillaStyle.ElementToDialogPadding),
                    content: BuildBody);
        });
    }

    private void BuildBody(IGuiRenderTreeBuilder builder)
    {
        // Two-column body: nav list | page area.
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
                                    // Section headers are non-interactive — they just visually
                                    // group the buttons below them. Slight extra top margin
                                    // when not the first entry to separate from prior group.
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
                                        onClick: () => SelectPage(page.PageType),
                                        widthMode: GuiSizeMode.Fill,
                                        margin: new(0, 0, GuiVanillaStyle.HalfPadding, 0));
                                    break;
                            }
                        }
                    });

                // Page column — scrollable, with the recessed inset frame drawn as
                // built-in chrome (the scrollbar sits beside the inset; when no overflow
                // occurs the inset fills the column).
                builder.AddContainer(1, fill: true, margin: new(0, 0, 0, 16),
                    scroll: GuiScroll.Vertical,
                    withInset: true,
                    content: builder =>
                    {
                        switch (_selectedPageType)
                        {
                            case var t when t == typeof(QuickSearchModConfigPage):
                                builder.Add<QuickSearchModConfigPage>(0, widthMode: GuiSizeMode.Fill);
                                break;

                            case var t when t == typeof(TooltipsModConfigPage):
                                builder.Add<TooltipsModConfigPage>(0, widthMode: GuiSizeMode.Fill);
                                break;
                        }
                    });
            });
    }

    private void SelectPage(Type pageType)
    {
        if (_selectedPageType == pageType)
        {
            return;
        }

        _selectedPageType = pageType;
        StateHasChanged();
    }

    /// <summary>
    /// Schedules a debounced save of the live config DTO. Each call cancels and replaces
    /// the previous pending write — the actual mod-config store happens once
    /// <see cref="SaveDebounceMs"/> ms elapse without further edits. Mirrors the legacy
    /// ModConfigGuiDialog.LaunchSaveConfig logic.
    /// </summary>
    private void LaunchSaveConfig()
    {
        CancellationToken token;
        lock (_saveDebounceLock)
        {
            _saveDebounce?.Cancel();
            _saveDebounce?.Dispose();
            _saveDebounce = new CancellationTokenSource();
            token = _saveDebounce.Token;
        }

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(SaveDebounceMs, token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (token.IsCancellationRequested)
            {
                return;
            }

            _clientApi.StoreModConfig(_config, Constants.ModConfigFileName);

            lock (_saveDebounceLock)
            {
                // Only clear if the slot still references *our* CTS — a newer save may
                // have replaced it while we were awaiting the delay.
                if (_saveDebounce is not null && _saveDebounce.Token == token)
                {
                    _saveDebounce.Dispose();
                    _saveDebounce = null;
                }
            }
        });
    }
}
