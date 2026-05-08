using Cairo;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Horizontal integer slider — equivalent to vanilla <c>GuiElementSlider</c>. Drag the
/// handle (or click anywhere on the track) to set <see cref="Value"/>; the value is
/// quantised to <see cref="Step"/> and clamped into <c>[<see cref="MinValue"/>,
/// <see cref="MaxValue"/>]</c>. While focused, <c>Left</c> / <c>Right</c> arrows step the
/// value, matching vanilla.
/// <para>
/// Visuals follow the framework's "drop the tiled pattern fills" convention: a recessed
/// embossed track (<see cref="GuiInset"/> recipe) with a flat dark filled portion left of
/// the handle, and a button-style bevelled handle with vanilla <see cref="GuiVanillaStyle.ButtonBackColor"/>
/// fill. While the cursor hovers or the user is dragging, a small floating value tooltip
/// is drawn just above the handle (vanilla recipe — <see cref="GuiVanillaStyle.DialogStrongBgColor"/>
/// fill + <see cref="GuiVanillaStyle.DialogBorderColor"/> stroke).
/// </para>
/// <para>
/// By default <see cref="OnValueChanged"/> fires on every quantised step during a drag.
/// Set <see cref="TriggerOnMouseUp"/> to defer the callback until the mouse is released —
/// useful when the value drives an expensive operation (texture rebuild, network call,
/// world reload, …).
/// </para>
/// </summary>
public sealed class GuiSlider : GuiInputBase
{
    /// <summary>Current slider value. Always in <c>[<see cref="MinValue"/>, <see cref="MaxValue"/>]</c>
    /// and aligned to <see cref="Step"/>. Setting this directly does not fire
    /// <see cref="OnValueChanged"/>; prefer <see cref="SetValue"/> for an external write
    /// that should still respect the configured range/step.</summary>
    public int Value { get; set; }

    /// <summary>Lowest selectable value. Default 0.</summary>
    public int MinValue { get; set; } = 0;

    /// <summary>Highest selectable value. Default 100.</summary>
    public int MaxValue { get; set; } = 100;

    /// <summary>Step between adjacent allowed values. Must be ≥ 1. Default 1.</summary>
    public int Step { get; set; } = 1;

    /// <summary>Optional unit string appended to the default tooltip text (e.g. <c>"%"</c>,
    /// <c>" px"</c>). Ignored when <see cref="OnTooltipText"/> is set.</summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>When set, called to format the floating tooltip text shown while the user
    /// is interacting with the slider. Defaults to <c>Value + Unit</c>.</summary>
    public System.Func<int, string>? OnTooltipText { get; set; }

    /// <summary>When true, defer <see cref="OnValueChanged"/> until the mouse button is
    /// released — the slider visual still updates live, but the callback fires once with
    /// the final value. Default false (callback fires on every step).</summary>
    public bool TriggerOnMouseUp { get; set; }

    /// <summary>Fired after <see cref="Value"/> changes due to user interaction or
    /// <see cref="SetValue"/>.</summary>
    public GuiCallback<int> OnValueChanged { get; set; }

    /// <summary>Width of the draggable handle in logical pixels. Default 12.</summary>
    public double HandleWidth { get; set; } = 12;

    /// <summary>Horizontal padding between the track ends and the centre of the handle's
    /// extreme positions, in logical pixels. Default 4.</summary>
    public double Padding { get; set; } = 4;

    /// <summary>Font used to render the floating value tooltip while the user is
    /// hovering/dragging the slider.</summary>
    public GuiFontStyle TooltipFont { get; set; } = GuiFontStyle.Default;

    public GuiSlider()
    {
        // Vanilla sliders are typically full-row width with a fixed pixel height. 24 is a
        // touch taller than vanilla's 20 so the bevelled handle has more presence without
        // the wood-pattern fill we deliberately drop here.
        LayoutParameters.Height = 24;
        LayoutParameters.WidthMode = GuiSizeMode.Fill;
    }

    internal override void ResetLayoutParameters()
    {
        base.ResetLayoutParameters();
        LayoutParameters.Height = 24;
        LayoutParameters.WidthMode = GuiSizeMode.Fill;
    }

    /// <summary>
    /// Sets the value from external code, clamping to <c>[<see cref="MinValue"/>,
    /// <see cref="MaxValue"/>]</c> and snapping to <see cref="Step"/>. Fires
    /// <see cref="OnValueChanged"/> only when the resolved value differs from the current
    /// <see cref="Value"/>.
    /// </summary>
    public void SetValue(int value)
    {
        int snapped = Snap(value);
        if (snapped == Value) return;
        Value = snapped;
        OnValueChanged.Invoke(Value);
    }

    /// <inheritdoc/>
    public override GuiSize Measure(double availableWidth, double availableHeight)
    {
        // The slider has no intrinsic minimum width — it expands to fill. Return a small
        // height-dominant size so fit-content parents don't collapse it to zero.
        return new GuiSize(80, LayoutParameters.Height ?? 24);
    }

    /// <inheritdoc/>
    public override void OnKeyDown(GuiKeyEventArgs args)
    {
        if (!Enabled || !IsFocused) return;

        int dir = args.KeyCode switch
        {
            (int)GlKeys.Left => -1,
            (int)GlKeys.Right => 1,
            _ => 0,
        };
        if (dir != 0)
        {
            int next = Snap(Value + dir * Math.Max(1, Step));
            if (next != Value)
            {
                Value = next;
                OnValueChanged.Invoke(Value);
            }
            args.Handled = true;
            return;
        }

        // Let Tab / Escape pass through for dialog-level traversal / close.
        if (args.KeyCode == (int)GlKeys.Tab || args.KeyCode == (int)GlKeys.Escape) return;

        // Swallow other keys so global hotkeys don't fire while the slider is focused.
        args.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnInputMouseDown(GuiMouseEventArgs e)
    {
        _pressStartValue = Value;
        UpdateValueFromMouse(e.X);
    }

    /// <inheritdoc/>
    protected override void OnInputMouseMove(GuiMouseEventArgs e)
    {
        if (!IsPressed) return;
        UpdateValueFromMouse(e.X);
    }

    /// <inheritdoc/>
    protected override void OnInputMouseUp(GuiMouseEventArgs e)
    {
        if (TriggerOnMouseUp && Value != _pressStartValue)
            OnValueChanged.Invoke(Value);
    }

    /// <summary>
    /// Override the standard input handling: the slider triggers on press/drag (no click
    /// concept). Final commit (when <see cref="TriggerOnMouseUp"/> is true) happens on
    /// <see cref="OnInputMouseUp"/>.
    /// </summary>
    protected override void OnInputClick(GuiMouseEventArgs e)
    {
        // No-op — value updates happen on Down/Move; release commit handled in MouseUp.
    }

    private int _pressStartValue;

    private void UpdateValueFromMouse(double mouseX)
    {
        double sliderSpan = LastBounds.Width - 2 * Padding - HandleWidth;
        if (sliderSpan <= 0) return;

        double localX = mouseX - LastBounds.X - Padding - HandleWidth / 2.0;
        double t = Math.Clamp(localX / sliderSpan, 0.0, 1.0);
        double raw = MinValue + (MaxValue - MinValue) * t;
        int snapped = Snap((int)Math.Round(raw));

        if (snapped == Value) return;
        Value = snapped;
        if (!TriggerOnMouseUp)
            OnValueChanged.Invoke(Value);
    }

    /// <summary>Snap <paramref name="value"/> to the nearest allowed step inside
    /// <c>[<see cref="MinValue"/>, <see cref="MaxValue"/>]</c>.</summary>
    private int Snap(int value)
    {
        int clamped = Math.Clamp(value, MinValue, MaxValue);
        int step = Math.Max(1, Step);
        int rel = clamped - MinValue;
        // Round to nearest multiple of step using integer math.
        int snappedRel = ((rel + step / 2) / step) * step;
        int result = MinValue + snappedRel;
        if (result > MaxValue) result = MinValue + ((MaxValue - MinValue) / step) * step;
        return result;
    }

    /// <inheritdoc/>
    public override void Render(Context ctx, GuiComponentBounds bounds)
    {
        base.Render(ctx, bounds);

        // 1. Track chrome — emboss + dark fill, same recipe as GuiTextInput / GuiCheckbox
        //    so all three input chromes look like siblings.
        GuiInset.Draw(ctx, bounds, depth: 2, brightness: 0.8f, radius: 1);

        // 2. Filled portion — paints from the left edge to the centre of the handle.
        //    Uses GuiVanillaStyle.SliderFillColor (a low-alpha sibling of the checkbox
        //    accent) so the wide flat region tints the dark track rather than reading as
        //    a loud orange stripe — see SliderFillColor's doc for the rationale.
        double handleCenterX = ComputeHandleCenterX(bounds);
        double fillRight = Math.Min(handleCenterX, bounds.X + bounds.Width - Padding);
        double fillLeft = bounds.X + Padding;
        if (fillRight > fillLeft)
        {
            ctx.RoundRect(fillLeft, bounds.Y + Padding, fillRight - fillLeft, bounds.Height - 2 * Padding, 1);
            var fc = GuiVanillaStyle.SliderFillColor;
            ctx.SetSourceRGBA(fc.R, fc.G, fc.B, fc.A * (Enabled ? 1.0 : 0.45));
            ctx.Fill();
        }

        // 3. Focus / hover wash on the track — subtle, matches GuiTextInput.
        if (Enabled && (IsHovered || IsFocused))
        {
            ctx.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            ctx.SetSourceRGBA(1, 1, 1, 0.05);
            ctx.Fill();
        }

        // 4. Handle — button-style bevelled rectangle centred vertically on the track.
        double hx = handleCenterX - HandleWidth / 2.0;
        double hy = bounds.Y;
        double hw = HandleWidth;
        double hh = bounds.Height;
        DrawHandle(ctx, hx, hy, hw, hh, Enabled);

        // 5. Floating value tooltip — only while the user is touching the slider. Drawn
        //    *after* the handle so it sits above. We let it overflow the bounds vertically;
        //    the dialog surface will clip if it extends past the dialog edge — acceptable
        //    for an initial implementation, matches vanilla without TooltipExceedClipBounds.
        if (Enabled && (IsPressed || IsHovered))
            DrawValueTooltip(ctx, handleCenterX, bounds.Y);
    }

    private double ComputeHandleCenterX(GuiComponentBounds bounds)
    {
        int range = MaxValue - MinValue;
        double t = range <= 0 ? 0 : (double)(Value - MinValue) / range;
        double sliderSpan = bounds.Width - 2 * Padding - HandleWidth;
        return bounds.X + Padding + HandleWidth / 2.0 + t * sliderSpan;
    }

    private static void DrawHandle(Context ctx, double x, double y, double w, double h, bool enabled)
    {
        // 1. Drop shadow — dialed back from the default GuiShadow values: a touch lower
        //    alpha and a wider spread so the falloff feels softer rather than crisp.
        if (enabled)
            GuiShadow.Draw(ctx, x, y, w, h,
                steps: 3, offset: 1.0, spread: 0.85, alpha: 0.14, radius: 1);

        // 2. Body fill. ButtonBackColor carries vanilla's 0.8 alpha which would let the
        //    dark filled track bleed through the handle; force opaque so the handle reads
        //    as a solid object sitting on top of the track.
        ctx.RoundRect(x, y, w, h, 1);
        var fill = GuiVanillaStyle.ButtonBackColor;
        ctx.SetSourceRGBA(fill.R, fill.G, fill.B, enabled ? 1.0 : 0.55);
        ctx.Fill();

        // 3. Subtle top-down highlight gradient on the body — adds the "rounded top"
        //    impression that real raised buttons have, before the emboss ring kicks in.
        if (enabled)
        {
            using var grad = new LinearGradient(x, y, x, y + h);
            grad.AddColorStop(0.0, new Cairo.Color(1, 1, 1, 0.12));
            grad.AddColorStop(0.5, new Cairo.Color(1, 1, 1, 0.0));
            grad.AddColorStop(1.0, new Cairo.Color(0, 0, 0, 0.18));
            ctx.RoundRect(x, y, w, h, 1);
            ctx.SetSource(grad);
            ctx.Fill();
        }

        // 4. Raised emboss — same recipe as GuiInset but with highlight/shadow swapped so
        //    the handle reads as lifted off the recessed track. Depth 3 gives a chunkier
        //    bevel than the track's depth-2 recess so the asymmetry is visible.
        var handleBounds = new GuiComponentBounds(x, y, w, h);
        GuiInset.Draw(ctx, handleBounds, depth: 3, brightness: 1f, radius: 1, raised: true);
    }

    private void DrawValueTooltip(Context ctx, double anchorX, double trackTop)
    {
        string text = OnTooltipText?.Invoke(Value) ?? (Value + Unit);
        if (string.IsNullOrEmpty(text)) return;

        const double padX = 7;
        const double padY = 4;
        const double gap = 4;        // space between tooltip and track
        const double radius = 1;

        var ts = TooltipFont.Measure(text);
        double w = ts.Width + 2 * padX;
        double h = ts.Height + 2 * padY;
        double x = anchorX - w / 2.0;
        double y = trackTop - gap - h;

        var fill = GuiVanillaStyle.DialogStrongBgColor;
        var border = GuiVanillaStyle.DialogBorderColor;
        double sw = 3.0 / RuntimeEnv.GUIScale;

        ctx.RoundRect(x, y, w, h, radius);
        ctx.SetSourceRGBA(fill.R, fill.G, fill.B, fill.A);
        ctx.FillPreserve();
        ctx.SetSourceRGBA(border.R, border.G, border.B, border.A);
        ctx.LineWidth = sw;
        ctx.Stroke();

        ctx.DrawText(text, TooltipFont, x + padX, y + padY);
    }
}
