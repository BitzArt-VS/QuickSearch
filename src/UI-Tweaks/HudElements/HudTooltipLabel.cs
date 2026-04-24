using BitzArt.UI.Tweaks.GameStatus;
using BitzArt.UI.Tweaks.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

public class HudTooltipLabel : HudElement
{
    private const string RichtextElementName = "tooltip-text";

    private readonly GameStatusService _statusService;
    private readonly IHudTooltipConfiguration _config;

    public override double DrawOrder => _config.DrawOrder;
    public override bool Focusable => false;
    public override bool ShouldReceiveMouseEvents() => false;

    private CairoFont? _font;
    private List<string>? _formatStrings;
    private string?[]? _resultStrings;
    private Action? _disposeSubscriptions;

    public HudTooltipLabel(ICoreClientAPI clientApi, GameStatusService statusService, IHudTooltipConfiguration config)
        : base(clientApi)
    {
        _statusService = statusService;
        _config = config;

        _config.PropertyChanged += (_, _) => Compose();

        if (_config.Enable)
        {
            Compose();
        }
    }

    private void Compose()
    {
        _font = new CairoFont
        {
            Color = (double[])GuiStyle.DialogDefaultTextColor.Clone(),
            Fontname = GuiStyle.DecorativeFontName,
            UnscaledFontsize = _config.FontSize
        };

        _formatStrings = [_config.Format, .. _config.ExtraElements ?? []];

        var componentBoundary = ElementBounds
            .FixedPos(_config.Area, _config.Offset.X, _config.Offset.Y)
            .WithFixedSize(_config.Width, _config.Height);

        var backgroundBoundary = ElementBounds.Fixed(0, 0, _config.Width, _config.Height);

        SingleComposer = ClientApi.Gui
            .CreateCompo(_config.ComponentName, componentBoundary)
            .AddIf(_config.HasBackground)
                .AddTooltipBackground(backgroundBoundary, _config.BackgroundOpacity, _config.BackgroundCornerRadius)
            .EndIf();

        _resultStrings ??= new string?[_formatStrings.Count];

        for (int i = 0; i < _formatStrings.Count; i++)
        {
            var contentBoundary = ElementBounds.Fixed(
                _config.Padding.Left,
                _config.Padding.Top,
                _config.Width - (_config.Padding.Left + _config.Padding.Right),
                _config.Height - (_config.Padding.Top + _config.Padding.Bottom));

            var resultText = _resultStrings.ElementAtOrDefault(i) ?? string.Empty;

            SingleComposer = SingleComposer
                .AddRichtext(resultText, _font, contentBoundary, $"{RichtextElementName}-{i + 1}");
        }

        SingleComposer = SingleComposer.Compose();

        _disposeSubscriptions?.Invoke();
        _disposeSubscriptions = null;

        switch (_config.Enable)
        {
            case true:
                Subscribe();
                TryOpen();
                break;
            case false:
                TryClose();
                break;
        }
    }

    private void Subscribe()
    {
        if (_formatStrings == null)
        {
            return;
        }

        for (int i = 0; i < _formatStrings.Count; i++)
        {
            var index = i; // Capture loop variable for closure
            var format = _formatStrings[index];

            var subscription = _statusService.Subscribe(format, (value) => OnStatsUpdate(value, index));

            if (subscription is not null)
            {
                _disposeSubscriptions = _disposeSubscriptions is not null
                    ? _disposeSubscriptions + subscription.Dispose
                    : subscription.Dispose;
            }
            else
            {
                // No subscription created, likely no variable placeholders found in the format string.
                // Still need to update the text once with the static format.
                OnStatsUpdate(format, index);
            }
        }
    }

    private void OnStatsUpdate(string? value, int index)
    {
        if (_config.CenterText)
        {
            value = $"<font align=center>{value}</font>";
        }

        _resultStrings![index] = value ?? string.Empty;

        if (!_config.Enable)
        {
            return;
        }

        var valueElement = SingleComposer.GetRichtext($"{RichtextElementName}-{index + 1}");
        var format = _formatStrings![index];

        ClientApi.Event.EnqueueMainThreadTask(() =>
        {
            valueElement.SetNewText(value, _font);
        }, "ui-tweaks-tooltip-value-update");
    }

    public override void Dispose()
    {
        _disposeSubscriptions?.Invoke();

        base.Dispose();

        GC.SuppressFinalize(this);
    }
}
