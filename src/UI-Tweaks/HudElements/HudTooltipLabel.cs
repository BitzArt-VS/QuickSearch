using BitzArt.UI.Tweaks.Services;
using Cairo;
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

    private CairoFont? _font;
    private List<string>? _formatStrings;
    private string?[]? _resultStrings;
    private bool _isSubscribedToUpdates;

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

        switch (_config.Enable)
        {
            case true:
                TrySubscribeToUpdates();
                TryOpen();
                break;
            case false:
                TryClose();
                break;
        }
    }

    private void TrySubscribeToUpdates()
    {
        if (_isSubscribedToUpdates || _formatStrings == null) return;

        for (int i = 0; i < _formatStrings.Count; i++)
        {
            var index = i; // Capture loop variable for closure
            var format = _formatStrings[index];

            if (!_statusService.Subscribe(format, (value) => OnStatsUpdate(value, index)))
            {
                // No subscription created, likely no variable placeholders found in the format string.
                // Still need to update the text once with the static format.
                OnStatsUpdate(format, index);
            }
        }

        _isSubscribedToUpdates = true;
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
}

file static class TooltipBackgroundExtensions
{
    public static GuiComposer AddTooltipBackground(this GuiComposer composer, ElementBounds bounds, double backgroundOpacity, double cornerRadius)
        => composer.AddStaticElement(new TooltipBackgroundElement(composer.Api, bounds, backgroundOpacity, cornerRadius));

    private class TooltipBackgroundElement(ICoreClientAPI capi, ElementBounds bounds, double opacity, double cornerRadius)
        : GuiElement(capi, bounds)
    {
        public override void ComposeElements(Context ctx, ImageSurface surface)
        {
            Bounds.CalcWorldBounds();

            // 1. Cast to int (or use Math.Floor) to match how the engine allocates texture bounds
            double x = (int)Bounds.drawX;
            double y = (int)Bounds.drawY;
            double w = (int)Bounds.OuterWidth;
            double h = (int)Bounds.OuterHeight;

            // 2. Setup border properties
            double lineWidth = 2.0;
            double inset = lineWidth / 2.0; // 1.0px inset for a 2.0px line

            // 3. Apply the inset PLUS a 1-pixel safety buffer on the right/bottom
            x += inset;
            y += inset;
            w -= (inset * 2) + 1.0;
            h -= (inset * 2) + 1.0;

            double r = cornerRadius;
            r = Math.Min(r, Math.Min(w / 2, h / 2));

            // ... (The rest of your drawing code stays exactly the same)
            ctx.NewPath();
            ctx.Arc(x + w - r, y + r, r, -Math.PI / 2, 0);
            ctx.Arc(x + w - r, y + h - r, r, 0, Math.PI / 2);
            ctx.Arc(x + r, y + h - r, r, Math.PI / 2, Math.PI);
            ctx.Arc(x + r, y + r, r, Math.PI, 3 * Math.PI / 2);
            ctx.ClosePath();

            ctx.SetSourceRGBA(0.12, 0.11, 0.10, opacity);
            ctx.FillPreserve();

            double borderOpacity = 1.0 - ((1.0 - opacity) / 2.0);
            ctx.SetSourceRGBA(0.35, 0.33, 0.30, borderOpacity);
            ctx.LineWidth = lineWidth;
            ctx.Stroke();
        }
    }
}
