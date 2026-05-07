using BitzArt.UI.Tweaks.Config;
using System;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks;

internal sealed class TooltipConfigPage(TooltipOptions config, string titleLangKey) : ConfigPage(Lang.Get(titleLangKey))
{
    private const string EnableKey = "tooltip-enable";
    private const string DialogAreaKey = "tooltip-dialog-area";
    private const string HeightKey = "tooltip-height";
    private const string WidthKey = "tooltip-width";
    private const string CenterTextKey = "tooltip-center-text";
    private const string OffsetXKey = "tooltip-offset-x";
    private const string OffsetYKey = "tooltip-offset-y";
    private const string PaddingTopKey = "tooltip-padding-top";
    private const string PaddingRightKey = "tooltip-padding-right";
    private const string PaddingBottomKey = "tooltip-padding-bottom";
    private const string PaddingLeftKey = "tooltip-padding-left";
    private const string HasBackgroundKey = "tooltip-has-background";
    private const string BackgroundOpacityKey = "tooltip-background-opacity";
    private const string BackgroundCornerRadiusKey = "tooltip-background-corner-radius";

    private const int RowHeight = 30;
    private const int RowGap = 16;
    private const int ControlWidth = 160;
    private const int SwitchSize = 28;

    private static readonly EnumDialogArea[] AllowedDialogAreaValues =
    [
        EnumDialogArea.LeftTop,
        EnumDialogArea.CenterTop,
        EnumDialogArea.RightTop,
        EnumDialogArea.LeftMiddle,
        EnumDialogArea.CenterMiddle,
        EnumDialogArea.RightMiddle,
        EnumDialogArea.LeftBottom,
        EnumDialogArea.CenterBottom,
        EnumDialogArea.RightBottom,
    ];

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

        var dialogAreaOptionBounds = ElementBounds.Fixed(x, enableOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var dialogAreaLabelBounds = ElementBounds.Fixed(dialogAreaOptionBounds.fixedX, dialogAreaOptionBounds.fixedY, labelWidth, RowHeight);
        var dialogAreaDropdownBounds = ElementBounds.Fixed(dialogAreaOptionBounds.fixedX + labelWidth, dialogAreaOptionBounds.fixedY, ControlWidth, RowHeight);

        var heightOptionBounds = ElementBounds.Fixed(x, dialogAreaOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var heightLabelBounds = ElementBounds.Fixed(heightOptionBounds.fixedX, heightOptionBounds.fixedY, labelWidth, RowHeight);
        var heightInputBounds = ElementBounds.Fixed(heightOptionBounds.fixedX + labelWidth, heightOptionBounds.fixedY, ControlWidth, RowHeight);

        var widthOptionBounds = ElementBounds.Fixed(x, heightOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var widthLabelBounds = ElementBounds.Fixed(widthOptionBounds.fixedX, widthOptionBounds.fixedY, labelWidth, RowHeight);
        var widthInputBounds = ElementBounds.Fixed(widthOptionBounds.fixedX + labelWidth, widthOptionBounds.fixedY, ControlWidth, RowHeight);

        var centerTextOptionBounds = ElementBounds.Fixed(x, widthOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var centerTextLabelBounds = ElementBounds.Fixed(centerTextOptionBounds.fixedX, centerTextOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, labelWidth, SwitchSize);
        var centerTextSwitchBounds = ElementBounds.Fixed(centerTextOptionBounds.fixedX + labelWidth, centerTextOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, SwitchSize, SwitchSize);

        var offsetXOptionBounds = ElementBounds.Fixed(x, centerTextOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var offsetXLabelBounds = ElementBounds.Fixed(offsetXOptionBounds.fixedX, offsetXOptionBounds.fixedY, labelWidth, RowHeight);
        var offsetXInputBounds = ElementBounds.Fixed(offsetXOptionBounds.fixedX + labelWidth, offsetXOptionBounds.fixedY, ControlWidth, RowHeight);

        var offsetYOptionBounds = ElementBounds.Fixed(x, offsetXOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var offsetYLabelBounds = ElementBounds.Fixed(offsetYOptionBounds.fixedX, offsetYOptionBounds.fixedY, labelWidth, RowHeight);
        var offsetYInputBounds = ElementBounds.Fixed(offsetYOptionBounds.fixedX + labelWidth, offsetYOptionBounds.fixedY, ControlWidth, RowHeight);

        var paddingTopOptionBounds = ElementBounds.Fixed(x, offsetYOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var paddingTopLabelBounds = ElementBounds.Fixed(paddingTopOptionBounds.fixedX, paddingTopOptionBounds.fixedY, labelWidth, RowHeight);
        var paddingTopInputBounds = ElementBounds.Fixed(paddingTopOptionBounds.fixedX + labelWidth, paddingTopOptionBounds.fixedY, ControlWidth, RowHeight);

        var paddingRightOptionBounds = ElementBounds.Fixed(x, paddingTopOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var paddingRightLabelBounds = ElementBounds.Fixed(paddingRightOptionBounds.fixedX, paddingRightOptionBounds.fixedY, labelWidth, RowHeight);
        var paddingRightInputBounds = ElementBounds.Fixed(paddingRightOptionBounds.fixedX + labelWidth, paddingRightOptionBounds.fixedY, ControlWidth, RowHeight);

        var paddingBottomOptionBounds = ElementBounds.Fixed(x, paddingRightOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var paddingBottomLabelBounds = ElementBounds.Fixed(paddingBottomOptionBounds.fixedX, paddingBottomOptionBounds.fixedY, labelWidth, RowHeight);
        var paddingBottomInputBounds = ElementBounds.Fixed(paddingBottomOptionBounds.fixedX + labelWidth, paddingBottomOptionBounds.fixedY, ControlWidth, RowHeight);

        var paddingLeftOptionBounds = ElementBounds.Fixed(x, paddingBottomOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var paddingLeftLabelBounds = ElementBounds.Fixed(paddingLeftOptionBounds.fixedX, paddingLeftOptionBounds.fixedY, labelWidth, RowHeight);
        var paddingLeftInputBounds = ElementBounds.Fixed(paddingLeftOptionBounds.fixedX + labelWidth, paddingLeftOptionBounds.fixedY, ControlWidth, RowHeight);

        var hasBackgroundOptionBounds = ElementBounds.Fixed(x, paddingLeftOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var hasBackgroundLabelBounds = ElementBounds.Fixed(hasBackgroundOptionBounds.fixedX, hasBackgroundOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, labelWidth, SwitchSize);
        var hasBackgroundSwitchBounds = ElementBounds.Fixed(hasBackgroundOptionBounds.fixedX + labelWidth, hasBackgroundOptionBounds.fixedY + (RowHeight - SwitchSize) / 2.0, SwitchSize, SwitchSize);

        var backgroundOpacityOptionBounds = ElementBounds.Fixed(x, hasBackgroundOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var backgroundOpacityLabelBounds = ElementBounds.Fixed(backgroundOpacityOptionBounds.fixedX, backgroundOpacityOptionBounds.fixedY, labelWidth, RowHeight);
        var backgroundOpacityInputBounds = ElementBounds.Fixed(backgroundOpacityOptionBounds.fixedX + labelWidth, backgroundOpacityOptionBounds.fixedY, ControlWidth, RowHeight);

        var backgroundCornerRadiusOptionBounds = ElementBounds.Fixed(x, backgroundOpacityOptionBounds.fixedY + RowHeight + RowGap, bounds.fixedWidth, RowHeight);
        var backgroundCornerRadiusLabelBounds = ElementBounds.Fixed(backgroundCornerRadiusOptionBounds.fixedX, backgroundCornerRadiusOptionBounds.fixedY, labelWidth, RowHeight);
        var backgroundCornerRadiusInputBounds = ElementBounds.Fixed(backgroundCornerRadiusOptionBounds.fixedX + labelWidth, backgroundCornerRadiusOptionBounds.fixedY, ControlWidth, RowHeight);

        string[] dialogAreaValues = [.. AllowedDialogAreaValues.Select(area => area.ToString())];
        int dialogAreaIndex = Math.Max(0, Array.IndexOf(dialogAreaValues, _config.DialogArea));

        composer
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-enable"), TextFont, enableLabelBounds)
            .AddSwitch(val =>
            {
                _config.Enable = val;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Enable));
                saveConfig.Invoke();
            }, enableSwitchBounds, EnableKey, SwitchSize)
            .AddConfigHoverText("config-tooltip-enable-tooltip", TextFont, enableOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-dialog-area"), TextFont, dialogAreaLabelBounds)
            .AddDropDown(dialogAreaValues, dialogAreaValues, dialogAreaIndex, (val, _) =>
            {
                _config.DialogArea = val;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.DialogArea));
                saveConfig.Invoke();
            }, dialogAreaDropdownBounds, DialogAreaKey)
            .AddConfigHoverText("config-tooltip-dialog-area-tooltip", TextFont, dialogAreaOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-height"), TextFont, heightLabelBounds)
            .AddNumberInput(heightInputBounds, val =>
            {
                if (!double.TryParse(val, out double height) || height <= 0)
                {
                    return;
                }
                _config.Height = height;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Height));
                saveConfig.Invoke();
            }, key: HeightKey)
            .AddConfigHoverText("config-tooltip-height-tooltip", TextFont, heightOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-width"), TextFont, widthLabelBounds)
            .AddNumberInput(widthInputBounds, val =>
            {
                if (!double.TryParse(val, out double width) || width <= 0)
                {
                    return;
                }
                _config.Width = width;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Width));
                saveConfig.Invoke();
            }, key: WidthKey)
            .AddConfigHoverText("config-tooltip-width-tooltip", TextFont, widthOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-center-text"), TextFont, centerTextLabelBounds)
            .AddSwitch(val =>
            {
                _config.CenterText = val;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.CenterText));
                saveConfig.Invoke();
            }, centerTextSwitchBounds, CenterTextKey, SwitchSize)
            .AddConfigHoverText("config-tooltip-center-text-tooltip", TextFont, centerTextOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-offset-x"), TextFont, offsetXLabelBounds)
            .AddNumberInput(offsetXInputBounds, val =>
            {
                if (!double.TryParse(val, out double offsetX))
                {
                    return;
                }
                _config.Offset.X = offsetX;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Offset));
                saveConfig.Invoke();
            }, key: OffsetXKey)
            .AddConfigHoverText("config-tooltip-offset-x-tooltip", TextFont, offsetXOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-offset-y"), TextFont, offsetYLabelBounds)
            .AddNumberInput(offsetYInputBounds, val =>
            {
                if (!double.TryParse(val, out double offsetY))
                {
                    return;
                }
                _config.Offset.Y = offsetY;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Offset));
                saveConfig.Invoke();
            }, key: OffsetYKey)
            .AddConfigHoverText("config-tooltip-offset-y-tooltip", TextFont, offsetYOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-padding-top"), TextFont, paddingTopLabelBounds)
            .AddNumberInput(paddingTopInputBounds, val =>
            {
                if (!double.TryParse(val, out double paddingTop))
                {
                    return;
                }
                _config.Padding.Top = paddingTop;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                saveConfig.Invoke();
            }, key: PaddingTopKey)
            .AddConfigHoverText("config-tooltip-padding-top-tooltip", TextFont, paddingTopOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-padding-right"), TextFont, paddingRightLabelBounds)
            .AddNumberInput(paddingRightInputBounds, val =>
            {
                if (!double.TryParse(val, out double paddingRight))
                {
                    return;
                }
                _config.Padding.Right = paddingRight;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                saveConfig.Invoke();
            }, key: PaddingRightKey)
            .AddConfigHoverText("config-tooltip-padding-right-tooltip", TextFont, paddingRightOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-padding-bottom"), TextFont, paddingBottomLabelBounds)
            .AddNumberInput(paddingBottomInputBounds, val =>
            {
                if (!double.TryParse(val, out double paddingBottom))
                {
                    return;
                }
                _config.Padding.Bottom = paddingBottom;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                saveConfig.Invoke();
            }, key: PaddingBottomKey)
            .AddConfigHoverText("config-tooltip-padding-bottom-tooltip", TextFont, paddingBottomOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-padding-left"), TextFont, paddingLeftLabelBounds)
            .AddNumberInput(paddingLeftInputBounds, val =>
            {
                if (!double.TryParse(val, out double paddingLeft))
                {
                    return;
                }
                _config.Padding.Left = paddingLeft;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.Padding));
                saveConfig.Invoke();
            }, key: PaddingLeftKey)
            .AddConfigHoverText("config-tooltip-padding-left-tooltip", TextFont, paddingLeftOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-has-background"), TextFont, hasBackgroundLabelBounds)
            .AddSwitch(val =>
            {
                _config.HasBackground = val;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.HasBackground));
                saveConfig.Invoke();
            }, hasBackgroundSwitchBounds, HasBackgroundKey, SwitchSize)
            .AddConfigHoverText("config-tooltip-has-background-tooltip", TextFont, hasBackgroundOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-background-opacity"), TextFont, backgroundOpacityLabelBounds)
            .AddNumberInput(backgroundOpacityInputBounds, val =>
            {
                if (!double.TryParse(val, out double backgroundOpacity))
                {
                    return;
                }
                _config.BackgroundOpacity = backgroundOpacity;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.BackgroundOpacity));
                saveConfig.Invoke();
            }, key: BackgroundOpacityKey)
            .AddConfigHoverText("config-tooltip-background-opacity-tooltip", TextFont, backgroundOpacityOptionBounds)
            .AddStaticText(Lang.Get($"{Constants.ModId}:config-tooltip-background-corner-radius"), TextFont, backgroundCornerRadiusLabelBounds)
            .AddNumberInput(backgroundCornerRadiusInputBounds, val =>
            {
                if (!double.TryParse(val, out double backgroundCornerRadius))
                {
                    return;
                }
                _config.BackgroundCornerRadius = backgroundCornerRadius;
                _config.NotifyPropertyChanged(nameof(TooltipOptions.BackgroundCornerRadius));
                saveConfig.Invoke();
            }, key: BackgroundCornerRadiusKey)
            .AddConfigHoverText("config-tooltip-background-corner-radius-tooltip", TextFont, backgroundCornerRadiusOptionBounds);

        return backgroundCornerRadiusOptionBounds.fixedY + backgroundCornerRadiusOptionBounds.fixedHeight;
    }

    public override void OnComposed(GuiComposer composer)
    {
        composer.GetSwitch(EnableKey).SetValue(_config.Enable);
        composer.GetDropDown(DialogAreaKey).SetSelectedValue(_config.DialogArea);
        composer.GetNumberInput(HeightKey).SetValue(_config.Height);
        composer.GetNumberInput(HeightKey).Interval = 1f;
        composer.GetNumberInput(WidthKey).SetValue(_config.Width);
        composer.GetNumberInput(WidthKey).Interval = 1f;
        composer.GetSwitch(CenterTextKey).SetValue(_config.CenterText);
        composer.GetNumberInput(OffsetXKey).SetValue(_config.Offset.X);
        composer.GetNumberInput(OffsetXKey).Interval = 1f;
        composer.GetNumberInput(OffsetYKey).SetValue(_config.Offset.Y);
        composer.GetNumberInput(OffsetYKey).Interval = 1f;
        composer.GetNumberInput(PaddingTopKey).SetValue(_config.Padding.Top);
        composer.GetNumberInput(PaddingTopKey).Interval = 1f;
        composer.GetNumberInput(PaddingRightKey).SetValue(_config.Padding.Right);
        composer.GetNumberInput(PaddingRightKey).Interval = 1f;
        composer.GetNumberInput(PaddingBottomKey).SetValue(_config.Padding.Bottom);
        composer.GetNumberInput(PaddingBottomKey).Interval = 1f;
        composer.GetNumberInput(PaddingLeftKey).SetValue(_config.Padding.Left);
        composer.GetNumberInput(PaddingLeftKey).Interval = 1f;
        composer.GetSwitch(HasBackgroundKey).SetValue(_config.HasBackground);
        composer.GetNumberInput(BackgroundOpacityKey).SetValue(_config.BackgroundOpacity);
        composer.GetNumberInput(BackgroundOpacityKey).Interval = 0.05f;
        composer.GetNumberInput(BackgroundCornerRadiusKey).SetValue(_config.BackgroundCornerRadius);
        composer.GetNumberInput(BackgroundCornerRadiusKey).Interval = 1f;
    }
}
