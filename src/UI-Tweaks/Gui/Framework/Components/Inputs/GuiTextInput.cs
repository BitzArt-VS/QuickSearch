using System;
using System.Globalization;
using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Single-line text input. Matches vanilla <c>GuiElementTextInput</c> visuals — an
/// embossed (recessed) dark rounded panel with a subtle white wash while focused — and
/// supports the basic editing shortcuts a user expects: caret arrow keys,
/// <c>Home</c> / <c>End</c>, <c>BackSpace</c> / <c>Delete</c>, and printable character
/// insertion via <c>OnKeyPress</c>.
/// <para>
/// The <see cref="Mode"/> property restricts which characters can land in <see cref="Text"/>:
/// <see cref="GuiTextInputMode.Integer"/> and <see cref="GuiTextInputMode.Decimal"/> reject
/// any candidate string that would not parse as a number (the input simply ignores the
/// keystroke), while <see cref="GuiTextInputMode.Text"/> accepts anything. Validation is
/// performed on every candidate transition (insert, backspace, delete) so out-of-band
/// edits via <see cref="SetText"/> are also enforced.
/// </para>
/// <para>
/// Selection / drag-to-select / clipboard / multi-line / scissor-clipped horizontal
/// scrolling are intentionally out of scope for this initial framework input — they can
/// be layered on later without touching the public surface.
/// </para>
/// </summary>
public sealed class GuiTextInput : GuiInputBase
{
    /// <summary>The current text content. Setting this directly bypasses validation;
    /// prefer <see cref="SetText"/> for an external write that should still respect
    /// <see cref="Mode"/>.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Restricts which characters may be typed. Default <see cref="GuiTextInputMode.Text"/>.</summary>
    public GuiTextInputMode Mode { get; set; } = GuiTextInputMode.Text;

    /// <summary>Maximum number of characters accepted, or <c>-1</c> for no limit (default).</summary>
    public int MaxLength { get; set; } = -1;

    /// <summary>Placeholder text rendered with reduced opacity while <see cref="Text"/> is
    /// empty and the input is not focused.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Font used for the rendered text. Defaults to <see cref="GuiFontStyle.Default"/>.</summary>
    public GuiFontStyle Font { get; set; } = GuiFontStyle.Default;

    /// <summary>Fired after <see cref="Text"/> changes due to user input or
    /// <see cref="SetText"/>. Not raised when the candidate is rejected by <see cref="Mode"/>.</summary>
    public GuiCallback<string> OnTextChanged { get; set; }

    /// <summary>Horizontal padding between the input chrome and the text, in logical pixels.</summary>
    public double TextPaddingX { get; set; } = 4;

    /// <summary>
    /// When true and <see cref="Mode"/> is <see cref="GuiTextInputMode.Integer"/> or
    /// <see cref="GuiTextInputMode.Decimal"/>, two stacked up/down buttons are drawn on
    /// the right edge of the input. Clicking a button steps <see cref="Text"/> by
    /// <see cref="SpinnerInterval"/> (held <c>Shift</c> divides the step by 10, held
    /// <c>Ctrl</c> by 100 — matching vanilla <c>GuiElementNumberInput</c>) and fires
    /// <see cref="OnTextChanged"/>. Has no effect in <see cref="GuiTextInputMode.Text"/>.
    /// Default false.
    /// </summary>
    public bool ShowSpinnerButtons { get; set; }

    /// <summary>Step size applied by the up/down spinner buttons. Modifier keys further
    /// scale this value at click time. Default <c>1</c>.</summary>
    public double SpinnerInterval { get; set; } = 1;

    // Caret position in characters into Text, in [0, Text.Length].
    private int _caret;

    // Spinner button state — driven by their own slot mouse handlers, read by Render.
    // Mirrors GuiButton's pattern: the renderer marks the surface dirty after every
    // mouse dispatch, so the next frame redraws and picks up updated visuals without
    // requiring StateHasChanged.
    private bool _spinnerUpHovered;
    private bool _spinnerDownHovered;
    private bool _spinnerUpPressed;
    private bool _spinnerDownPressed;

    /// <summary>Width of the spinner-button gutter on the right edge of the input, in
    /// logical pixels. Matches vanilla <c>GuiElementNumberInput.rightSpacing</c>.</summary>
    private const double SpinnerGutterWidth = 17;

    /// <summary>True when the spinner buttons should be declared / drawn for the current
    /// configuration. Disabled inputs still render the gutter (dimmed) so the visual
    /// width is stable across enable/disable transitions, but mouse-down handlers bail.</summary>
    private bool SpinnerButtonsVisible => ShowSpinnerButtons && Mode != GuiTextInputMode.Text;

    public GuiTextInput()
    {
        // Default to a fixed height matching vanilla's standard text-input rows; width
        // grows to fill available space unless the caller overrides.
        LayoutParameters.Height = 30;
        LayoutParameters.WidthMode = GuiSizeMode.Fill;
    }

    internal override void ResetLayoutParameters()
    {
        base.ResetLayoutParameters();
        LayoutParameters.Height = 30;
        LayoutParameters.WidthMode = GuiSizeMode.Fill;
    }

    /// <summary>
    /// Sets the text from external code. Returns true when accepted (text and caret
    /// updated, <see cref="OnTextChanged"/> fired); false when <paramref name="text"/>
    /// fails the current <see cref="Mode"/> validation.
    /// </summary>
    public bool SetText(string text)
    {
        text ??= string.Empty;
        if (MaxLength >= 0 && text.Length > MaxLength) text = text[..MaxLength];
        if (!IsValidCandidate(text)) return false;
        if (Text == text) return true;
        Text = text;
        _caret = Math.Min(_caret, Text.Length);
        OnTextChanged.Invoke(Text);
        return true;
    }

    /// <inheritdoc/>
    public override GuiSize Measure(double availableWidth, double availableHeight)
    {
        // The input doesn't shrink to its text — it's an interactive box. Returning a
        // small minimum just keeps fit-content parents from collapsing to zero.
        return new GuiSize(80, LayoutParameters.Height ?? 30);
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        // Base declares the full-fill mouse-capture container at key 0. The spinner
        // buttons are added afterwards as separately-keyed absolute-positioned slots,
        // so they are appended to the interactive-region table after the base container
        // and win the topmost-wins reverse hit-test for any pixel inside the gutter.
        base.BuildRenderTree(builder);

        if (!SpinnerButtonsVisible) return;

        // Vanilla stacks two half-height buttons on the right; here we use the input's
        // configured height (defaults to 30) and split exactly in half. The 1px vertical
        // gap vanilla draws between the two buttons is intentionally collapsed — without
        // surface-level blur the gap reads as a stripe rather than a separator.
        double inputHeight = LayoutParameters.Height ?? 30;
        double btnHeight = inputHeight / 2.0;

        builder.Add<GuiContainer>(1,
            width: SpinnerGutterWidth,
            height: btnHeight,
            positioning: GuiComponentPositioning.Absolute,
            horizontalAlignment: GuiHorizontalAlignment.Right,
            verticalAlignment: GuiVerticalAlignment.Top)
            .OnMouseDown(e => HandleSpinnerMouseDown(e, direction: +1))
            .OnMouseUp(_ => _spinnerUpPressed = false)
            .OnMouseEnter(_ => _spinnerUpHovered = true)
            .OnMouseLeave(_ => _spinnerUpHovered = false);

        builder.Add<GuiContainer>(2,
            width: SpinnerGutterWidth,
            height: btnHeight,
            positioning: GuiComponentPositioning.Absolute,
            horizontalAlignment: GuiHorizontalAlignment.Right,
            verticalAlignment: GuiVerticalAlignment.Bottom)
            .OnMouseDown(e => HandleSpinnerMouseDown(e, direction: -1))
            .OnMouseUp(_ => _spinnerDownPressed = false)
            .OnMouseEnter(_ => _spinnerDownHovered = true)
            .OnMouseLeave(_ => _spinnerDownHovered = false);
    }

    private void HandleSpinnerMouseDown(GuiMouseEventArgs e, int direction)
    {
        if (!Enabled) return;
        if (e.Button != EnumMouseButton.Left) return;

        if (direction > 0) _spinnerUpPressed = true;
        else _spinnerDownPressed = true;

        // Modifier-aware step — match vanilla GuiElementNumberInput exactly: ShiftLeft
        // divides the interval by 10, ControlLeft by 100. Both pressed → /1000.
        double step = SpinnerInterval;
        if (ClientApi?.Input is { } input)
        {
            if (input.KeyboardKeyState[(int)GlKeys.ShiftLeft]) step /= 10;
            if (input.KeyboardKeyState[(int)GlKeys.ControlLeft]) step /= 100;
        }

        // In integer mode round the step magnitude up to at least 1 so modifier keys
        // can't collapse a click into a no-op (matches vanilla's IntMode handling).
        if (Mode == GuiTextInputMode.Integer)
        {
            step = Math.Max(1, Math.Ceiling(Math.Abs(step)));
        }

        double.TryParse(Text, NumberStyles.Any, GlobalConstants.DefaultCultureInfo, out double current);
        double next = current + direction * step;

        // Round to 4 decimals for decimal mode (matches vanilla) to avoid cumulative
        // floating-point drift from repeated additions like 0.1 + 0.1 + ….
        string nextText = Mode == GuiTextInputMode.Integer
            ? ((long)Math.Round(next)).ToString(GlobalConstants.DefaultCultureInfo)
            : Math.Round(next, 4).ToString(GlobalConstants.DefaultCultureInfo);

        if (MaxLength >= 0 && nextText.Length > MaxLength) return;
        if (!IsValidCandidate(nextText)) return;

        ClientApi?.Gui.PlaySound("tick");
        // Vanilla focuses the input on a button click; we mirror that so the user can
        // immediately type into the field after stepping the value with the buttons.
        FocusManager?.RequestFocus(this);

        if (Text == nextText) return;
        Text = nextText;
        _caret = Text.Length;
        OnTextChanged.Invoke(Text);
    }

    protected override void OnInputMouseDown(GuiMouseEventArgs e)
    {
        // Place caret near the click position. Walk character widths until we exceed the
        // click x — same logic as vanilla, simplified for single-line / no scrolling.
        double localX = e.X - LastBounds.X - TextPaddingX;
        if (string.IsNullOrEmpty(Text) || localX <= 0)
        {
            _caret = 0;
            return;
        }

        // Build width incrementally character-by-character; inexpensive for typical input
        // lengths and avoids allocating a substring per measurement.
        double cumulative = 0;
        for (int i = 1; i <= Text.Length; i++)
        {
            double w = Font.Measure(Text[..i]).Width;
            // Click closer to the left edge of this character → caret before it.
            double mid = (cumulative + w) / 2.0;
            if (localX < mid) { _caret = i - 1; return; }
            cumulative = w;
        }
        _caret = Text.Length;
    }

    /// <inheritdoc/>
    public override void OnKeyDown(GuiKeyEventArgs args)
    {
        if (!Enabled || !IsFocused) return;

        int keyCode = args.KeyCode;
        switch (keyCode)
        {
            case (int)GlKeys.Left:
                _caret = Math.Max(0, _caret - 1);
                break;
            case (int)GlKeys.Right:
                _caret = Math.Min(Text.Length, _caret + 1);
                break;
            case (int)GlKeys.Home:
                _caret = 0;
                break;
            case (int)GlKeys.End:
                _caret = Text.Length;
                break;
            case (int)GlKeys.BackSpace:
                if (_caret > 0)
                {
                    var candidate = Text.Remove(_caret - 1, 1);
                    if (IsValidCandidate(candidate))
                    {
                        Text = candidate;
                        _caret--;
                        OnTextChanged.Invoke(Text);
                    }
                }
                break;
            case (int)GlKeys.Delete:
                if (_caret < Text.Length)
                {
                    var candidate = Text.Remove(_caret, 1);
                    if (IsValidCandidate(candidate))
                    {
                        Text = candidate;
                        OnTextChanged.Invoke(Text);
                    }
                }
                break;
            case (int)GlKeys.Tab:
            case (int)GlKeys.Escape:
                // Let these escape so the dialog can react (focus traversal / dialog close).
                return;
        }

        // Swallow every other key while focused so global hotkeys (e.g. T → chat) don't
        // leak through and act on whatever the user types into the field. Mirrors
        // vanilla GuiElementEditableTextBase.OnKeyDownInternal which marks args.Handled
        // for everything except Tab / Enter / Escape.
        args.Handled = true;
    }

    /// <inheritdoc/>
    public override void OnKeyPress(GuiKeyEventArgs args)
    {
        if (!Enabled || !IsFocused) return;

        char ch = args.KeyChar;
        // Filter non-printable and control characters — OnKeyDown already handled
        // BackSpace / Delete / arrows. Tab / Enter are intentionally ignored for the
        // initial single-line implementation. We still mark these handled so they
        // don't leak to global hotkeys / chat input while the field is focused.
        if (ch < 0x20 || ch == 0x7F) { args.Handled = true; return; }

        if (MaxLength >= 0 && Text.Length >= MaxLength) { args.Handled = true; return; }

        var candidate = Text.Insert(_caret, ch.ToString());
        if (!IsValidCandidate(candidate)) { args.Handled = true; return; }

        Text = candidate;
        _caret++;
        OnTextChanged.Invoke(Text);
        args.Handled = true;
    }

    /// <summary>
    /// Mode-aware candidate validator. The intermediate-state allowances (empty, lone
    /// <c>"-"</c>, trailing <c>'.'</c> in decimal mode) exist so the user can transit
    /// through them while editing — fully invalid strings (letters in numeric mode,
    /// multiple dots, etc.) are rejected.
    /// </summary>
    private bool IsValidCandidate(string candidate)
    {
        if (Mode == GuiTextInputMode.Text) return true;
        if (string.IsNullOrEmpty(candidate)) return true;

        bool allowDecimal = Mode == GuiTextInputMode.Decimal;
        int dotCount = 0;

        for (int i = 0; i < candidate.Length; i++)
        {
            char c = candidate[i];
            if (c == '-')
            {
                if (i != 0) return false;
                continue;
            }
            if (c == '.')
            {
                if (!allowDecimal) return false;
                if (++dotCount > 1) return false;
                continue;
            }
            if (c < '0' || c > '9') return false;
        }

        // Ensure the result isn't just "-" by itself? Allow it as an intermediate state —
        // the user is still typing. Same rationale for "-." in decimal mode.
        return true;
    }

    /// <inheritdoc/>
    public override void Render(Context ctx, GuiComponentBounds bounds)
    {
        base.Render(ctx, bounds);

        // 1. Chrome — vanilla recipe: dark fill overlay + 2-deep emboss ring with corner
        //    radius 1. Matches GuiElementTextInput.ComposeTextElements visuals.
        GuiInset.Draw(ctx, bounds, depth: 2, brightness: 0.8f, radius: 1);

        // 2. Focus highlight — vanilla *intends* a 20% white wash, but its highlight
        //    texture is generated premultiplied (alpha = 0.2 · white) and then rendered
        //    through the non-premultiplied path, so the visible lift is closer to 4%.
        //    We match vanilla's actual on-screen appearance with a faint flat fill
        //    rather than the loud 20% the source code suggests.
        if (IsFocused && Enabled)
        {
            ctx.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            ctx.SetSourceRGBA(1, 1, 1, 0.05);
            ctx.Fill();
        }

        // 3. Disable wash — soften everything when the input is read-only.
        double textAlphaMul = Enabled ? 1.0 : 0.35;

        // 4. Text or placeholder. Clip to the inner area so overflow doesn't bleed past
        //    the right edge of the chrome (no horizontal scroll yet — long content is
        //    simply hidden on the right). When the spinner gutter is shown the clip
        //    excludes it so text can't bleed under the buttons.
        bool spinnerVisible = SpinnerButtonsVisible;
        double rightInset = spinnerVisible ? SpinnerGutterWidth + 1 : 1;
        ctx.Save();
        ctx.Rectangle(bounds.X + 1, bounds.Y + 1, Math.Max(0, bounds.Width - 1 - rightInset), Math.Max(0, bounds.Height - 2));
        ctx.Clip();

        double lineHeight = Font.MeasureHeight();
        double textY = bounds.Y + (bounds.Height - lineHeight) / 2.0;
        double textX = bounds.X + TextPaddingX;

        if (string.IsNullOrEmpty(Text) && !IsFocused && !string.IsNullOrEmpty(Placeholder))
        {
            var c = Font.Color;
            var placeholderFont = Font with
            {
                Color = GuiColor.FromRgba(c.R, c.G, c.B, c.A * 0.5 * textAlphaMul)
            };
            ctx.DrawText(Placeholder, placeholderFont, textX, textY);
        }
        else if (!string.IsNullOrEmpty(Text))
        {
            GuiFontStyle textFont;
            if (textAlphaMul < 1.0)
            {
                var c = Font.Color;
                textFont = Font with { Color = GuiColor.FromRgba(c.R, c.G, c.B, c.A * textAlphaMul) };
            }
            else
            {
                textFont = Font;
            }
            ctx.DrawText(Text, textFont, textX, textY);
        }

        // 5. Caret — vertical line at the caret position, drawn only while focused and
        //    in the visible blink phase. Width 1 logical pixel matches vanilla.
        if (IsFocused && Enabled && FocusManager?.CaretBlinkOn == true)
        {
            double caretAdvance = _caret == 0 ? 0 : Font.Measure(Text[.._caret]).Width;
            double cx = textX + caretAdvance;
            // Use a small inset on top/bottom so the caret sits inside the line height
            // rather than touching the chrome — matches vanilla's caret height.
            double cTop = textY + 1;
            double cBot = textY + lineHeight - 1;
            ctx.MoveTo(cx + 0.5, cTop);
            ctx.LineTo(cx + 0.5, cBot);
            ctx.SetSourceRGBA(1, 1, 1, 1);
            ctx.LineWidth = 1;
            ctx.Stroke();
        }

        ctx.Restore();

        // 6. Spinner buttons — drawn last so they paint on top of any text / focus wash
        //    that bled close to the right edge inside the clip.
        if (spinnerVisible)
        {
            DrawSpinnerButtons(ctx, bounds);
        }
    }

    /// <summary>
    /// Paints the two stacked spinner buttons in the right gutter. Each button is a
    /// raised emboss over the dialog highlight colour with an arrow triangle in the
    /// centre; hover and press state are layered as semi-transparent washes on top.
    /// Disabled inputs render with reduced alpha across the whole composition.
    /// </summary>
    private void DrawSpinnerButtons(Context ctx, GuiComponentBounds b)
    {
        double btnH = b.Height / 2.0;
        double x = b.Right - SpinnerGutterWidth;
        double upY = b.Y;
        double downY = b.Y + btnH;

        DrawSpinnerButton(ctx, x, upY, SpinnerGutterWidth, btnH, arrowUp: true,
            hovered: _spinnerUpHovered, pressed: _spinnerUpPressed);
        DrawSpinnerButton(ctx, x, downY, SpinnerGutterWidth, btnH, arrowUp: false,
            hovered: _spinnerDownHovered, pressed: _spinnerDownPressed);
    }

    private void DrawSpinnerButton(Context ctx, double x, double y, double w, double h,
        bool arrowUp, bool hovered, bool pressed)
    {
        double alphaMul = Enabled ? 1.0 : 0.35;

        // Base fill — matches vanilla's DialogHighlightColor for number-input buttons.
        var bg = GuiVanillaStyle.DialogHighlightColor;
        ctx.Rectangle(x, y, w, h);
        ctx.SetSourceRGBA(bg.R, bg.G, bg.B, bg.A * alphaMul);
        ctx.Fill();

        // Raised emboss (highlight on top-left, shadow on bottom-right). Depth 2 / radius 1
        // mirrors the input's recessed chrome at the same intensity, just inverted.
        GuiInset.Draw(ctx, new GuiComponentBounds(x, y, w, h),
            depth: 2, brightness: 1f, radius: 1, raised: true);

        // Hover / press washes — only meaningful while enabled.
        if (Enabled && pressed)
        {
            ctx.Rectangle(x, y, w, h);
            ctx.SetSourceRGBA(0, 0, 0, 0.35);
            ctx.Fill();
        }
        else if (Enabled && hovered)
        {
            ctx.Rectangle(x, y, w, h);
            ctx.SetSourceRGBA(1, 1, 1, 0.18);
            ctx.Fill();
        }

        // Arrow triangle — geometry mirrors vanilla GuiElementNumberInput (5px half-width,
        // 4px tip rise) anchored relative to the button's local rect so the framework's
        // logical-coord rendering stays self-consistent.
        double cx = x + w / 2.0;
        double cy = y + h / 2.0;
        const double half = 5;   // half-width of the triangle base
        const double rise = 4;   // tip offset from centre along the vertical axis

        ctx.NewPath();
        if (arrowUp)
        {
            // Apex up.
            ctx.MoveTo(cx,        cy - rise);
            ctx.LineTo(cx - half, cy + rise);
            ctx.LineTo(cx + half, cy + rise);
        }
        else
        {
            // Apex down.
            ctx.MoveTo(cx - half, cy - rise);
            ctx.LineTo(cx + half, cy - rise);
            ctx.LineTo(cx,        cy + rise);
        }
        ctx.ClosePath();
        ctx.SetSourceRGBA(1, 1, 1, Enabled ? 0.6 : 0.18);
        ctx.Fill();
    }
}
