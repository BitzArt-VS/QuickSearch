using BitzArt.UI.Tweaks.Config;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal class ModConfigGuiDialog : GuiDialog
{
    private const string DialogComposerKey = "mod-config-dialog";
    private const string ScrollbarKey = "content-scrollbar";
    private const string BackButtonKey = "back-button";

    private const int DialogContentWidth = 400;
    private const int ContentAreaHeight = 400;
    private const int ContentPaddingX = 8;
    private const int ScrollbarWidth = 20;
    private const int BackButtonWidth = 100;
    private const int BackButtonHeight = 28;
    private const int NavigationContentGap = 12;
    private const int NavigationAreaHeight = BackButtonHeight + NavigationContentGap;
    private const int SaveDebounceMs = 10000;

    private readonly UiTweaksModConfig _config;
    private readonly List<ConfigPage> _pageStack;
    private CancellationTokenSource? _saveDebounce;
    private readonly Lock _saveDebounceLock = new();
    private ElementBounds? _contentScrollBounds;

    public override double DrawOrder => 0.2;

    public ModConfigGuiDialog(ICoreClientAPI clientApi, UiTweaksModConfig config) : base(clientApi)
    {
        _config = config;
        _pageStack = [new RootConfigPage(_config)];

        Compose();
    }

    public override void OnGuiOpened() => Compose();

    public override void OnGuiClosed()
    {
        if (_pageStack.Count > 1)
        {
            PopToPage(0);
        }
    }

    public override void Dispose()
    {
        if (_saveDebounce is not null)
        {
            lock (_saveDebounceLock)
            {
                _saveDebounce.Cancel();
                _saveDebounce.Dispose();
            }
            
            _saveDebounce = null;

            ClientApi.StoreModConfig(_config, Constants.ModConfigFileName);
        }

        base.Dispose();
    }

    private void PushPage(ConfigPage page)
    {
        _pageStack.Add(page);
        Compose();
    }

    private void PopPage()
    {
        if (_pageStack.Count <= 1)
        {
            throw new InvalidOperationException("Cannot pop the root config page.");
        }

        _pageStack.RemoveAt(_pageStack.Count - 1);
        Compose();
    }

    private void PopToPage(int index)
    {
        _pageStack.RemoveRange(index + 1, _pageStack.Count - index - 1);
        Compose();
    }

    private void Compose()
    {
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;

        var composer = ClientApi.Gui
            .CreateCompo(DialogComposerKey, ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.CenterMiddle))
            .AddShadedDialogBG(bgBounds, true)
            .AddDialogTitleBar(Lang.Get($"{Constants.ModId}:ui-tweaks-config"), () => TryClose())
            .BeginChildElements(bgBounds);

        double contentY = 24;

        if (_pageStack.Count > 1)
        {
            ComposeNavigationHeader(composer, contentY);
        }

        contentY += NavigationAreaHeight;
        int viewportHeight = ContentAreaHeight;

        _contentScrollBounds = ElementBounds.Fixed(0, 0, DialogContentWidth, 0);
        _contentScrollBounds.BothSizing = ElementSizing.FitToChildren;

        var viewportBounds = ElementBounds.Fixed(0, contentY, DialogContentWidth, viewportHeight);
        var clipBounds = viewportBounds.ForkBoundingParent();
        var scrollbarBounds = ElementBounds.Fixed(DialogContentWidth + 4, contentY, ScrollbarWidth, viewportHeight);
        var localBounds = ElementBounds.Fixed(ContentPaddingX, 0, DialogContentWidth - ContentPaddingX * 2, 0);
        localBounds.BothSizing = ElementSizing.FitToChildren;

        composer
            .AddInset(viewportBounds)
            .BeginClip(clipBounds)
            .BeginChildElements(_contentScrollBounds);

        double contentHeight = _pageStack[^1].ComposeContent(ClientApi, composer, localBounds, LaunchSaveConfig, PushPage);

        composer
            .EndChildElements()
            .EndClip()
            .AddVerticalScrollbar(OnScrollbarValueChanged, scrollbarBounds, ScrollbarKey);

        SingleComposer = composer
            .EndChildElements()
            .Compose();

        _pageStack[^1].OnComposed(SingleComposer);

        var scrollbar = SingleComposer.GetScrollbar(ScrollbarKey);
        scrollbar?.SetHeights(viewportHeight, (float)contentHeight);
    }

    private void OnScrollbarValueChanged(float value)
    {
        if (_contentScrollBounds == null)
        {
            return;
        }
        
        _contentScrollBounds.fixedY = -value;
        _contentScrollBounds.CalcWorldBounds();
    }

    private void LaunchSaveConfig()
    {
        Task.Run(async () =>
        {
            if (_saveDebounce is not null)
            {
                lock (_saveDebounceLock)
                {
                    _saveDebounce.Cancel();
                    _saveDebounce.Dispose();
                }
            }
            
            _saveDebounce = new CancellationTokenSource();

            await Task.Delay(SaveDebounceMs, _saveDebounce.Token);
            ClientApi.StoreModConfig(_config, Constants.ModConfigFileName);

            _saveDebounce = null;
        });
    }

    private void ComposeNavigationHeader(GuiComposer composer, double yOffset)
    {
        var backButtonBounds = ElementBounds.Fixed(0, yOffset, BackButtonWidth, BackButtonHeight);
        composer.AddSmallButton(
            Lang.Get($"{Constants.ModId}:config-back"),
            () =>
            {
                PopPage();
                return true;
            },
            backButtonBounds,
            key: BackButtonKey);
    }
}
