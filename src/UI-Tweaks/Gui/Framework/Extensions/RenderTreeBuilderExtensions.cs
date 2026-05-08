using System;
using System.Collections.Generic;

namespace BitzArt.UI.Tweaks.Gui;

public static class RenderTreeBuilderExtensions
{
    /// <summary>
    /// Declares a component slot of type <typeparamref name="T"/> at <paramref name="key"/>.
    /// All layout parameters may be set inline as named arguments.
    /// </summary>
    public static IGuiComponentBuilder<T> Add<T>(
        this IGuiRenderTreeBuilder builder,
        int key,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiDirection? direction = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        where T : IGuiComponent, new()
        => ApplyLayout(builder.AddComponent<T>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction, positioning,
            horizontalAlignment, verticalAlignment);

    /// <summary>
    /// Declares a <see cref="GuiContainer"/> slot at <paramref name="key"/>.
    /// All layout parameters may be set inline as named arguments. Pass
    /// <paramref name="background"/> to paint a solid fill behind <paramref name="content"/> —
    /// omit it for an invisible flow box.
    /// <paramref name="content"/> may be supplied here as the last argument, or set afterwards
    /// via <see cref="WithContent"/>; passing it here is marginally more efficient as it avoids
    /// an extra builder call.
    /// <para>
    /// Set <paramref name="withInset"/> to overlay the vanilla recessed-border inset chrome
    /// over the container's viewport (excluding any visible scrollbar gutter — the scrollbar
    /// sits beside the inset). When no scrollbar is visible the inset fills the entire
    /// container. Use <paramref name="configureInset"/> to tweak the inset's depth /
    /// brightness / radius without subclassing.
    /// </para>
    /// </summary>
    public static IGuiComponentBuilder<GuiContainer> AddContainer(
        this IGuiRenderTreeBuilder builder,
        int key,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiDirection? direction = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null,
        GuiColor? background = null,
        GuiScroll? scroll = null,
        GuiScroll? scrollbar = null,
        GuiScroll? alwaysShowScrollbar = null,
        bool withInset = false,
        Action<GuiInset>? configureInset = null,
        GuiRenderFragment? content = null)
    {
        var b = ApplyLayout(builder.AddComponent<GuiContainer>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction, positioning,
            horizontalAlignment, verticalAlignment);
        if (background is not null)
            b = b.Configure(c => c.Background = background.Value);
        if (scroll is not null)
            b = b.Configure(c => c.Scroll = scroll.Value);
        if (scrollbar is not null)
            b = b.Configure(c => c.Scrollbar = scrollbar.Value);
        if (alwaysShowScrollbar is not null)
            b = b.Configure(c => c.AlwaysShowScrollbar = alwaysShowScrollbar.Value);
        if (withInset)
            b = b.Configure(c => c.HasInset = true);
        if (configureInset is not null)
            b = b.Configure(c => c.InsetConfiguration = configureInset);
        return content is null ? b : b.Configure(c => c.Content = content);
    }

    /// <summary>
    /// Sets the <see cref="GuiContainer.Content"/> render fragment on an already-declared
    /// container slot. Prefer passing <c>content:</c> directly to
    /// <see cref="AddContainer"/> when possible.
    /// </summary>
    public static IGuiComponentBuilder<GuiContainer> WithContent(
        this IGuiComponentBuilder<GuiContainer> builder,
        GuiRenderFragment content)
        => builder.Configure(c => c.Content = content);

    /// <summary>
    /// Generic container helper: declares a slot of any <typeparamref name="TContainer"/> subtype
    /// (e.g. <see cref="GuiDialogBackground"/>) at <paramref name="key"/>, applies the layout
    /// parameters, and optionally attaches <paramref name="content"/>. Use this as the
    /// building block when wrapping custom container components in their own fluent helpers.
    /// </summary>
    public static IGuiComponentBuilder<TContainer> AddContainer<TContainer>(
        this IGuiRenderTreeBuilder builder,
        int key,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiDirection? direction = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null,
        GuiRenderFragment? content = null)
        where TContainer : GuiContainer, new()
    {
        var b = ApplyLayout(
            builder.AddComponent<TContainer>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction, positioning,
            horizontalAlignment, verticalAlignment);
        return content is null ? b : b.Configure(c => c.Content = content);
    }

    /// <summary>
    /// Declares a <see cref="GuiDialogBackground"/> slot at <paramref name="key"/>.
    /// Paints the vanilla shaded-dialog look (rounded fill colour overlaid with a tiled
    /// texture and an outer stroke). Override any of the texture / colour / stroke
    /// parameters to retune the recipe; pass <paramref name="content"/> to populate the
    /// inner area, or set it later via the container's <see cref="GuiContainer.Content"/>.
    /// <para>
    /// This is a thin convenience wrapper over <see cref="AddContainer{TContainer}"/> — call
    /// that directly when you need full control over the container's properties.
    /// </para>
    /// </summary>
    public static IGuiComponentBuilder<GuiDialogBackground> AddDialogBackground(
        this IGuiRenderTreeBuilder builder,
        int key,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiDirection? direction = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null,
        GuiRenderFragment? content = null)
    {
        return builder.AddContainer<GuiDialogBackground>(
            key, width, height, widthMode, heightMode, fill,
            margin, padding, direction, positioning,
            horizontalAlignment, verticalAlignment, content);
    }

    /// <summary>
    /// Declares a <see cref="GuiDialogTitleBar"/> slot at <paramref name="key"/>.
    /// Paints the vanilla title-bar chrome (lighter rounded fill, inner highlight bevel,
    /// open three-sided dark border) with <paramref name="title"/> drawn inside.
    /// Defaults the bar height to <see cref="GuiVanillaStyle.TitleBarHeight"/> when no
    /// <paramref name="height"/> is specified, and full width when no
    /// <paramref name="width"/>/<paramref name="widthMode"/> is given — matching how
    /// vanilla title bars are typically laid out at the top of a dialog.
    /// </summary>
    public static IGuiComponentBuilder<GuiDialogTitleBar> AddDialogTitleBar(
        this IGuiRenderTreeBuilder builder,
        int key,
        string title,
        GuiFontStyle? titleFont = null,
        Action<double, double>? onDrag = null,
        Action? onClose = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        => AddDialogTitleBarCore(builder, key, title, titleFont, onDrag,
            onClose is null ? default : (GuiCallback)onClose,
            width, height, widthMode, heightMode, margin, padding, positioning,
            horizontalAlignment, verticalAlignment);

    /// <summary>
    /// Asynchronous-handler overload of
    /// <see cref="AddDialogTitleBar(IGuiRenderTreeBuilder, int, string, GuiFontStyle?, Action{double, double}, Action, double?, double?, GuiSizeMode?, GuiSizeMode?, GuiThickness?, GuiThickness?, GuiComponentPositioning?)"/>.
    /// </summary>
    public static IGuiComponentBuilder<GuiDialogTitleBar> AddDialogTitleBar(
        this IGuiRenderTreeBuilder builder,
        int key,
        string title,
        Func<System.Threading.Tasks.Task> onClose,
        GuiFontStyle? titleFont = null,
        Action<double, double>? onDrag = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        => AddDialogTitleBarCore(builder, key, title, titleFont, onDrag, (GuiCallback)onClose,
            width, height, widthMode, heightMode, margin, padding, positioning,
            horizontalAlignment, verticalAlignment);

    private static IGuiComponentBuilder<GuiDialogTitleBar> AddDialogTitleBarCore(
        IGuiRenderTreeBuilder builder,
        int key,
        string title,
        GuiFontStyle? titleFont,
        Action<double, double>? onDrag,
        GuiCallback onClose,
        double? width,
        double? height,
        GuiSizeMode? widthMode,
        GuiSizeMode? heightMode,
        GuiThickness? margin,
        GuiThickness? padding,
        GuiComponentPositioning? positioning,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        // Default to "fill width, 31 px high" when the caller omits sizing — that's the
        // shape vanilla composer-based title bars take.
        var resolvedHeight = height ?? GuiVanillaStyle.TitleBarHeight;
        var resolvedWidthMode = widthMode ?? (width is null ? GuiSizeMode.Fill : (GuiSizeMode?)null);

        var b = ApplyLayout(
            builder.AddComponent<GuiDialogTitleBar>(key).Configure(t =>
            {
                t.Title = title;
                t.OnDrag = onDrag;
                t.OnClose = onClose;
            }),
            width, resolvedHeight, resolvedWidthMode, heightMode, fill: false,
            margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);

        if (titleFont is not null)
            b = b.Configure(t => t.TitleFont = titleFont.Value);

        return b;
    }

    /// <summary>
    /// Declares a <see cref="GuiLabel"/> slot at <paramref name="key"/> and sets its
    /// <see cref="GuiLabel.Text"/>. All layout parameters may be set inline as named arguments.
    /// Optionally supply a <paramref name="font"/>; if omitted the label uses
    /// <see cref="GuiFontStyle.Default"/>.
    /// </summary>
    public static IGuiComponentBuilder<GuiLabel> AddLabel(
        this IGuiRenderTreeBuilder builder,
        int key,
        string text,
        GuiFontStyle? font = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        var b = ApplyLayout(
            builder.AddComponent<GuiLabel>(key).Configure(l => l.Text = text),
            width, height, widthMode, heightMode, fill: false, margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);
        return font is null ? b : b.Configure(l => l.Font = font.Value);
    }

    /// <summary>
    /// Declares a <see cref="GuiInset"/> slot at <paramref name="key"/>.
    /// Behaves as a normal layout component: both axes default to
    /// <see cref="GuiSizeMode.FitContent"/> and positioning defaults to
    /// <see cref="GuiComponentPositioning.Relative"/>. Override via the standard inline
    /// named arguments (<c>fill</c>, <c>widthMode</c>, <c>positioning</c>, etc.) or by
    /// chaining <c>.ConfigureLayout(...)</c>.
    /// <para>
    /// Pass <paramref name="content"/> to nest a render fragment inside the inset — children
    /// are drawn between the brightness overlay and the emboss ring, producing a recessed
    /// look. Leave it null when using the inset purely as chrome over absolute-positioned
    /// content.
    /// </para>
    /// </summary>
    public static IGuiComponentBuilder<GuiInset> AddInset(
        this IGuiRenderTreeBuilder builder,
        int key,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiDirection? direction = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null,
        int? depth = null,
        float? brightness = null,
        double? radius = null,
        GuiRenderFragment? content = null)
    {
        var b = ApplyLayout(builder.AddComponent<GuiInset>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction, positioning,
            horizontalAlignment, verticalAlignment);
        if (depth is not null) b = b.Configure(c => c.Depth = depth.Value);
        if (brightness is not null) b = b.Configure(c => c.Brightness = brightness.Value);
        if (radius is not null) b = b.Configure(c => c.Radius = radius.Value);
        return content is null ? b : b.Configure(c => c.Content = content);
    }

    /// <summary>
    /// Declares a <see cref="GuiButton"/> slot at <paramref name="key"/>.
    /// All layout parameters may be set inline as named arguments.
    /// <para>
    /// Synchronous overload — accepts an <see cref="System.Action"/> for <paramref name="onClick"/>.
    /// For asynchronous handlers, use the <see cref="System.Func{T}"/>-returning-<see cref="System.Threading.Tasks.Task"/>
    /// overload below. The two overloads exist (rather than a single <see cref="GuiCallback"/>
    /// parameter) so plain lambdas like <c>() =&gt; DoStuff()</c> bind unambiguously without
    /// requiring an explicit cast — same DX as Blazor's overloaded callback APIs.
    /// </para>
    /// </summary>
    public static IGuiComponentBuilder<GuiButton> AddButton(
        this IGuiRenderTreeBuilder builder,
        int key,
        string text,
        Action? onClick = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        => AddButtonCore(builder, key, text,
            onClick is null ? default : (GuiCallback)onClick,
            width, height, widthMode, heightMode, fill, margin, padding, positioning,
            horizontalAlignment, verticalAlignment);

    /// <summary>
    /// Asynchronous-handler overload of <see cref="AddButton(IGuiRenderTreeBuilder, int, string, System.Action, double?, double?, GuiSizeMode?, GuiSizeMode?, bool, GuiThickness?, GuiThickness?, GuiComponentPositioning?)"/>.
    /// </summary>
    public static IGuiComponentBuilder<GuiButton> AddButton(
        this IGuiRenderTreeBuilder builder,
        int key,
        string text,
        System.Func<System.Threading.Tasks.Task> onClick,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        => AddButtonCore(builder, key, text, onClick,
            width, height, widthMode, heightMode, fill, margin, padding, positioning,
            horizontalAlignment, verticalAlignment);

    private static IGuiComponentBuilder<GuiButton> AddButtonCore(
        IGuiRenderTreeBuilder builder,
        int key,
        string text,
        GuiCallback onClick,
        double? width,
        double? height,
        GuiSizeMode? widthMode,
        GuiSizeMode? heightMode,
        bool fill,
        GuiThickness? margin,
        GuiThickness? padding,
        GuiComponentPositioning? positioning,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        var b = ApplyLayout(
            builder.AddComponent<GuiButton>(key).Configure(btn =>
            {
                btn.Text = text;
                btn.OnClick = onClick;
            }),
            width, height, widthMode, heightMode, fill, margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);
        return b;
    }

    /// <summary>Sets <see cref="GuiButton.OnClick"/> to a synchronous handler. Method-group friendly.</summary>
    public static IGuiComponentBuilder<GuiButton> OnClick(this IGuiComponentBuilder<GuiButton> builder, System.Action handler)
        => builder.Configure(btn => btn.OnClick = handler);

    /// <summary>Sets <see cref="GuiButton.OnClick"/> to an asynchronous handler. Method-group friendly.</summary>
    public static IGuiComponentBuilder<GuiButton> OnClick(this IGuiComponentBuilder<GuiButton> builder, System.Func<System.Threading.Tasks.Task> handler)
        => builder.Configure(btn => btn.OnClick = handler);

    /// <summary>
    /// Opens a cascading value scope: <paramref name="value"/> is made available to every
    /// component slot declared anywhere inside <paramref name="content"/> (at any nesting
    /// depth) via <see cref="GuiComponent.GetCascadingValue{T}()"/> /
    /// <see cref="IGuiRenderHandle.TryGetCascadingValue{T}(out T)"/>.
    /// <para>
    /// Pass <paramref name="name"/> to distinguish multiple scopes of the same
    /// <typeparamref name="T"/> in the same ancestry; consumers must request the matching
    /// name. Inner scopes shadow outer scopes with the same <c>(Type, Name)</c> key.
    /// </para>
    /// <para>
    /// This is a purely logical operation — no component slot is created, nothing is added
    /// to the layout tree, and the scope closes automatically when <paramref name="content"/>
    /// returns.
    /// </para>
    /// </summary>
    public static void AddCascadingValue<T>(
        this IGuiRenderTreeBuilder builder,
        T value,
        GuiRenderFragment content,
        string? name = null)
        => builder.PushCascadeScope(value, name, content);

    /// <summary>
    /// Declares a <see cref="GuiTooltip"/> wrapper at <paramref name="key"/>, attaching the
    /// floating <paramref name="tooltip"/> fragment to the regular layout child given by
    /// <paramref name="content"/>. The tooltip surfaces whenever the cursor hovers anywhere
    /// over the wrapped content's bounds. The tooltip is drawn on a separate Cairo surface
    /// managed by the dialog's <c>TooltipRenderer</c>, so it is free to extend beyond the
    /// wrapped content's parent bounds — and even beyond the dialog's surface — without
    /// clipping.
    /// <para>
    /// <b>Layout-transparent</b>: the wrapper itself does not occupy layout space and
    /// exposes no layout parameters. The slots declared inside <paramref name="content"/>
    /// flow at this declaration site exactly as if they had been added directly to
    /// <paramref name="builder"/>. The wrapper's hover region is derived from the union of
    /// those children's allocated bounds. Because <see cref="GuiTooltip"/> implements only
    /// <see cref="IGuiNode"/> (not <see cref="IGuiComponent"/>), it has no layout
    /// parameters of its own — set width/height/margin/padding etc. on <paramref name="content"/>'s
    /// inner components instead.
    /// </para>
    /// <para>
    /// The tooltip content is automatically wrapped in a <see cref="GuiTooltipBackground"/>
    /// panel painted with vanilla styling (DialogStrongBgColor fill, DialogBorderColor
    /// stroke, 5px content padding). Pass <paramref name="configureBackground"/> to retune
    /// any of those properties; for full chrome replacement, declare your own panel
    /// inside <paramref name="tooltip"/>.
    /// </para>
    /// <para>
    /// Requires a <see cref="TooltipHost"/> in scope — published automatically at the
    /// root of every <see cref="GuiDialog"/>. When the host is missing (e.g. a tooltip
    /// declared outside any dialog tree), the wrapped content still lays out normally
    /// but the tooltip never shows.
    /// </para>
    /// </summary>
    /// <returns>
    /// The fluent builder for the <see cref="GuiTooltip"/> slot, so callers can attach
    /// mouse handlers (e.g. <c>OnMouseEnter</c>) or chain further <c>Configure</c> calls.
    /// </returns>
    public static IGuiComponentBuilder<GuiTooltip> AddTooltip(
        this IGuiRenderTreeBuilder builder,
        int key,
        GuiRenderFragment tooltip,
        GuiRenderFragment content,
        Action<GuiTooltipBackground>? configureBackground = null)
    {
        return builder.AddComponent<GuiTooltip>(key).Configure(t =>
        {
            t.Content = content;
            t.TooltipContent = tooltip;
            t.ConfigureBackground = configureBackground;
        });
    }

    /// <summary>
    /// Declares a <see cref="GuiTextInput"/> slot at <paramref name="key"/>. Provides the
    /// initial <paramref name="text"/> and an <paramref name="onTextChanged"/> handler;
    /// pass <paramref name="mode"/> to restrict input to integer / decimal numbers, and
    /// optionally enable the right-edge spinner buttons via <paramref name="showSpinnerButtons"/>
    /// / <paramref name="spinnerInterval"/>.
    /// </summary>
    public static IGuiComponentBuilder<GuiTextInput> AddTextInput(
        this IGuiRenderTreeBuilder builder,
        int key,
        string? text = null,
        Action<string>? onTextChanged = null,
        GuiTextInputMode? mode = null,
        string? placeholder = null,
        int? maxLength = null,
        GuiFontStyle? font = null,
        bool? showSpinnerButtons = null,
        double? spinnerInterval = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        var b = ApplyLayout(
            builder.AddComponent<GuiTextInput>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);
        if (text is not null) b = b.Configure(c => c.Text = text);
        if (onTextChanged is not null) b = b.Configure(c => c.OnTextChanged = onTextChanged);
        if (mode is not null) b = b.Configure(c => c.Mode = mode.Value);
        if (placeholder is not null) b = b.Configure(c => c.Placeholder = placeholder);
        if (maxLength is not null) b = b.Configure(c => c.MaxLength = maxLength.Value);
        if (font is not null) b = b.Configure(c => c.Font = font.Value);
        if (showSpinnerButtons is not null) b = b.Configure(c => c.ShowSpinnerButtons = showSpinnerButtons.Value);
        if (spinnerInterval is not null) b = b.Configure(c => c.SpinnerInterval = spinnerInterval.Value);
        return b;
    }

    /// <summary>
    /// Declares a numeric <see cref="GuiTextInput"/> slot at <paramref name="key"/> —
    /// shorthand for <see cref="AddTextInput"/> with <see cref="GuiTextInput.Mode"/>
    /// preset to <see cref="GuiTextInputMode.Decimal"/> (or <see cref="GuiTextInputMode.Integer"/>
    /// when <paramref name="integer"/> is true) and <see cref="GuiTextInput.ShowSpinnerButtons"/>
    /// enabled by default. Step size is configured via <paramref name="interval"/>
    /// (default <c>1</c>).
    /// </summary>
    public static IGuiComponentBuilder<GuiTextInput> AddNumberInput(
        this IGuiRenderTreeBuilder builder,
        int key,
        string? text = null,
        Action<string>? onTextChanged = null,
        bool integer = false,
        double interval = 1,
        bool showSpinnerButtons = true,
        string? placeholder = null,
        int? maxLength = null,
        GuiFontStyle? font = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        => builder.AddTextInput(key,
            text: text,
            onTextChanged: onTextChanged,
            mode: integer ? GuiTextInputMode.Integer : GuiTextInputMode.Decimal,
            placeholder: placeholder,
            maxLength: maxLength,
            font: font,
            showSpinnerButtons: showSpinnerButtons,
            spinnerInterval: interval,
            width: width, height: height,
            widthMode: widthMode, heightMode: heightMode, fill: fill,
            margin: margin, padding: padding, positioning: positioning,
            horizontalAlignment: horizontalAlignment, verticalAlignment: verticalAlignment);

    /// <summary>
    /// Declares a <see cref="GuiCheckbox"/> slot at <paramref name="key"/>. Provides the
    /// initial <paramref name="checked_"/> state and an <paramref name="onCheckedChanged"/>
    /// handler.
    /// </summary>
    public static IGuiComponentBuilder<GuiCheckbox> AddCheckbox(
        this IGuiRenderTreeBuilder builder,
        int key,
        bool? checked_ = null,
        Action<bool>? onCheckedChanged = null,
        double? size = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        var b = ApplyLayout(
            builder.AddComponent<GuiCheckbox>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);
        if (checked_ is not null) b = b.Configure(c => c.Checked = checked_.Value);
        if (onCheckedChanged is not null) b = b.Configure(c => c.OnCheckedChanged = onCheckedChanged);
        if (size is not null) b = b.Configure(c => c.Size = size.Value);
        return b;
    }

    /// <summary>
    /// Declares a <see cref="GuiSlider"/> slot at <paramref name="key"/>. Provides the
    /// initial <paramref name="value"/>, range / step / unit, and an
    /// <paramref name="onValueChanged"/> handler. Set <paramref name="triggerOnMouseUp"/>
    /// to defer the callback until the user releases the mouse — the visual still
    /// updates live during a drag, but the callback fires once with the final value.
    /// </summary>
    public static IGuiComponentBuilder<GuiSlider> AddSlider(
        this IGuiRenderTreeBuilder builder,
        int key,
        int? value = null,
        int? minValue = null,
        int? maxValue = null,
        int? step = null,
        string? unit = null,
        Action<int>? onValueChanged = null,
        Func<int, string>? onTooltipText = null,
        bool? triggerOnMouseUp = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        var b = ApplyLayout(
            builder.AddComponent<GuiSlider>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);
        if (minValue is not null) b = b.Configure(c => c.MinValue = minValue.Value);
        if (maxValue is not null) b = b.Configure(c => c.MaxValue = maxValue.Value);
        if (step is not null) b = b.Configure(c => c.Step = step.Value);
        if (unit is not null) b = b.Configure(c => c.Unit = unit);
        if (value is not null) b = b.Configure(c => c.Value = value.Value);
        if (onValueChanged is not null) b = b.Configure(c => c.OnValueChanged = onValueChanged);
        if (onTooltipText is not null) b = b.Configure(c => c.OnTooltipText = onTooltipText);
        if (triggerOnMouseUp is not null) b = b.Configure(c => c.TriggerOnMouseUp = triggerOnMouseUp.Value);
        return b;
    }

    /// <summary>
    /// Declares a <see cref="GuiDropdown{T}"/> slot at <paramref name="key"/>. Provides
    /// the initial <paramref name="items"/> list, current <paramref name="selectedIndex"/>
    /// (or <c>-1</c> for "no selection"), and a selection callback. Pass
    /// <paramref name="itemTemplate"/> to render each row with a custom subtree (e.g. an
    /// icon + label) — by default the dropdown falls back to <c>item?.ToString()</c>
    /// rendered as a <see cref="GuiLabel"/>, which suits plain-string lists. Use a
    /// separate <paramref name="selectedTemplate"/> when the closed-state visual differs
    /// from the popup row (otherwise <paramref name="itemTemplate"/> is reused for both).
    /// </summary>
    public static IGuiComponentBuilder<GuiDropdown<T>> AddDropdown<T>(
        this IGuiRenderTreeBuilder builder,
        int key,
        IReadOnlyList<T>? items = null,
        int? selectedIndex = null,
        Action<int>? onSelectionChanged = null,
        Action<T>? onItemSelected = null,
        GuiRenderFragment<T>? itemTemplate = null,
        GuiRenderFragment<T>? selectedTemplate = null,
        string? placeholder = null,
        GuiFontStyle? font = null,
        double? itemHeight = null,
        double? maxPopupHeight = null,
        double? width = null,
        double? height = null,
        GuiSizeMode? widthMode = null,
        GuiSizeMode? heightMode = null,
        bool fill = false,
        GuiThickness? margin = null,
        GuiThickness? padding = null,
        GuiComponentPositioning? positioning = null,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
    {
        var b = ApplyLayout(
            builder.AddComponent<GuiDropdown<T>>(key),
            width, height, widthMode, heightMode, fill, margin, padding, direction: null, positioning,
            horizontalAlignment, verticalAlignment);
        if (items is not null) b = b.Configure(c => c.Items = items);
        if (selectedIndex is not null) b = b.Configure(c => c.SelectedIndex = selectedIndex.Value);
        if (onSelectionChanged is not null) b = b.Configure(c => c.OnSelectionChanged = onSelectionChanged);
        if (onItemSelected is not null) b = b.Configure(c => c.OnItemSelected = onItemSelected);
        if (itemTemplate is not null) b = b.Configure(c => c.ItemTemplate = itemTemplate);
        if (selectedTemplate is not null) b = b.Configure(c => c.SelectedTemplate = selectedTemplate);
        if (placeholder is not null) b = b.Configure(c => c.Placeholder = placeholder);
        if (font is not null) b = b.Configure(c => c.Font = font.Value);
        if (itemHeight is not null) b = b.Configure(c => c.ItemHeight = itemHeight.Value);
        if (maxPopupHeight is not null) b = b.Configure(c => c.MaxPopupHeight = maxPopupHeight.Value);
        return b;
    }

    // Composes an Action<GuiComponentLayoutParameters> from only the non-null parameters,
    // then passes it to ConfigureLayout. Returns the builder unchanged (no allocation) when
    // nothing was provided.
    private static IGuiComponentBuilder<T> ApplyLayout<T>(
        IGuiComponentBuilder<T> builder,
        double? width,
        double? height,
        GuiSizeMode? widthMode,
        GuiSizeMode? heightMode,
        bool fill,
        GuiThickness? margin,
        GuiThickness? padding,
        GuiDirection? direction,
        GuiComponentPositioning? positioning,
        GuiHorizontalAlignment? horizontalAlignment = null,
        GuiVerticalAlignment? verticalAlignment = null)
        where T : IGuiComponent
    {
        Action<GuiComponentLayoutParameters> action = null!;

        if (width != null) action += lp => lp.Width = width.Value;
        if (height != null) action += lp => lp.Height = height.Value;
        if (widthMode != null) action += lp => lp.WidthMode = widthMode.Value;
        if (heightMode != null) action += lp => lp.HeightMode = heightMode.Value;
        if (margin != null) action += lp => lp.Margin = margin.Value;
        if (padding != null) action += lp => lp.Padding = padding.Value;
        if (direction != null) action += lp => lp.Direction = direction.Value;
        if (positioning != null) action += lp => lp.Positioning = positioning.Value;
        if (horizontalAlignment != null) action += lp => lp.HorizontalAlignment = horizontalAlignment.Value;
        if (verticalAlignment != null) action += lp => lp.VerticalAlignment = verticalAlignment.Value;

        if (fill) action += lp =>
        {
            lp.WidthMode = GuiSizeMode.Fill;
            lp.HeightMode = GuiSizeMode.Fill;
        };

        if (action is null)
        {
            return builder;
        }

        return builder.ConfigureLayout(action);
    }
}

