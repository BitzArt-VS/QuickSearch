using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Generic dropdown / picker — a recessed text-input-style header showing the currently
/// selected item with a chevron, and a floating popup listing every item from
/// <see cref="Items"/>. Items are rendered through a caller-supplied
/// <see cref="ItemTemplate"/>; the closed-state header may use a different
/// <see cref="SelectedTemplate"/> (defaults to <see cref="ItemTemplate"/>). When neither
/// template is supplied the dropdown falls back to <c>item?.ToString()</c> rendered as a
/// <see cref="GuiLabel"/>, which is convenient for plain-string lists.
/// <para>
/// <b>State.</b> The canonical selection state is the integer <see cref="SelectedIndex"/>
/// (-1 = no selection). <see cref="SelectedItem"/> is a computed convenience.
/// <see cref="OnSelectionChanged"/> fires with the new index, and
/// <see cref="OnItemSelected"/> with the new item — pick whichever maps better to your
/// model.
/// </para>
/// <para>
/// <b>Popup placement.</b> The popup is hoisted onto the dialog's <see cref="OverlayHost"/>
/// so it paints on top of every regular slot and its rows always win the topmost-wins
/// reverse hit-test for hover / click dispatch — sibling slots declared after the
/// dropdown in the parent's flow can no longer paint over it. The dropdown registers /
/// refreshes the overlay from its own <see cref="Render"/> hook (where bounds are
/// resolved); when the popup closes, the registration is dropped and the overlay layer
/// prunes the subtree on the next frame.
/// </para>
/// <para>
/// <b>Extensibility.</b> The closed-state body and the popup body are produced by
/// <see cref="BuildHeader"/> / <see cref="BuildPopup"/>; the iteration drives off
/// <see cref="GetItemCount"/> / <see cref="GetItemAt"/>. A future <c>GuiAutocomplete&lt;T&gt;</c>
/// can override <see cref="BuildHeader"/> to swap the static label for a
/// <see cref="GuiTextInput"/>, and override the iteration helpers to drive a filtered view
/// of <see cref="Items"/> without copying — keyboard navigation, popup chrome and the
/// templating system are inherited as-is.
/// </para>
/// </summary>
public class GuiDropdown<T> : GuiInputBase
{
    /// <summary>The list of selectable items. May be null until populated.</summary>
    public IReadOnlyList<T>? Items { get; set; }

    /// <summary>Index into <see cref="Items"/> of the currently selected element, or
    /// <c>-1</c> when nothing is selected. Setting this directly bypasses
    /// <see cref="OnSelectionChanged"/> / <see cref="OnItemSelected"/>; prefer
    /// <see cref="SetSelectedIndex"/> for an external write that should still raise the
    /// callbacks.</summary>
    public int SelectedIndex { get; set; } = -1;

    /// <summary>Convenience accessor returning the element at <see cref="SelectedIndex"/>,
    /// or <c>default</c> when nothing is selected (or the index is out of range).</summary>
    public T? SelectedItem
    {
        get
        {
            if (Items is null || SelectedIndex < 0 || SelectedIndex >= Items.Count)
            {
                return default;
            }

            return Items[SelectedIndex];
        }
    }

    /// <summary>Text shown in the closed-state header when nothing is selected. Drawn at
    /// reduced opacity to mirror placeholder semantics elsewhere in the framework.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Whether the popup is currently open. Toggled by user interaction; the
    /// public setters <see cref="Open"/> / <see cref="Close"/> / <see cref="Toggle"/>
    /// schedule a rebuild automatically.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Renders a single item inside the popup list. Default: a <see cref="GuiLabel"/>
    /// containing <c>item?.ToString()</c>.</summary>
    public GuiRenderFragment<T>? ItemTemplate { get; set; }

    /// <summary>Renders the closed-state header content (excluding chrome and chevron).
    /// Defaults to <see cref="ItemTemplate"/> when null — pass a separate fragment when
    /// the closed-state visual differs from the popup row.</summary>
    public GuiRenderFragment<T>? SelectedTemplate { get; set; }

    /// <summary>Fired after <see cref="SelectedIndex"/> changes due to user interaction
    /// or <see cref="SetSelectedIndex"/>.</summary>
    public GuiCallback<int> OnSelectionChanged { get; set; }

    /// <summary>Fired after a non-empty selection is made — the new item is forwarded
    /// directly. Not fired when the selection is cleared (use
    /// <see cref="OnSelectionChanged"/> for that).</summary>
    public GuiCallback<T> OnItemSelected { get; set; }

    // ── Visual configuration ─────────────────────────────────────────────────

    /// <summary>Font used for the placeholder + the default label fallback.</summary>
    public GuiFontStyle Font { get; set; } = GuiFontStyle.Default;

    /// <summary>Horizontal padding between the chrome and the header text, in logical pixels.</summary>
    public double TextPaddingX { get; set; } = 6;

    /// <summary>Width of the chevron-icon area on the right edge of the header. The
    /// header content is laid out left of this region.</summary>
    public double ChevronAreaWidth { get; set; } = 24;

    /// <summary>Height of a single item row in the popup, in logical pixels.</summary>
    public double ItemHeight { get; set; } = 26;

    /// <summary>Maximum popup height, in logical pixels. When the total item content
    /// would exceed this, the popup container scrolls vertically.</summary>
    public double MaxPopupHeight { get; set; } = 240;

    /// <summary>Solid fill colour of the popup chrome. Default <see cref="GuiVanillaStyle.DialogStrongBgColor"/>.</summary>
    public GuiColor PopupBackground { get; set; } = GuiVanillaStyle.DialogStrongBgColor;

    /// <summary>Outer-stroke colour of the popup chrome. Default <see cref="GuiVanillaStyle.DialogBorderColor"/>.</summary>
    public GuiColor PopupBorder { get; set; } = GuiVanillaStyle.DialogBorderColor;

    /// <summary>Outer-stroke width of the popup chrome, in <i>physical</i> pixels (matches
    /// <see cref="GuiTooltipBackground"/>).</summary>
    public double PopupBorderWidth { get; set; } = 3;

    /// <summary>Corner radius of the popup chrome, in logical pixels.</summary>
    public double PopupRadius { get; set; } = GuiVanillaStyle.ElementBgRadius;

    /// <summary>Inner padding of the popup chrome — the gap between the popup border and
    /// the first/last item rows.</summary>
    public double PopupPadding { get; set; } = GuiTooltipBackground.DefaultPadding;

    /// <summary>Background fill applied to the row currently under the cursor. Set to
    /// <see cref="GuiColor.Transparent"/> to disable the hover highlight.</summary>
    public GuiColor HoveredItemBackground { get; set; } = GuiColor.FromRgba(1, 1, 1, 0.08);

    /// <summary>Background fill applied to the currently-selected row. Set to
    /// <see cref="GuiColor.Transparent"/> to disable the selected-row highlight.</summary>
    public GuiColor SelectedItemBackground { get; set; } = GuiColor.Transparent;

    // ── Internal state ───────────────────────────────────────────────────────

    private int _hoveredItemIndex = -1;

    /// <summary>The ambient overlay host resolved from the cascade chain. Null when the
    /// dropdown is declared outside a <see cref="GuiDialog"/> tree — in that case the
    /// popup falls back silently to nothing rather than throwing, since there is nowhere
    /// sensible to draw it.</summary>
    private OverlayHost? _overlayHost;

    /// <summary>Cached delegate so repeated <see cref="OverlayHost.Show"/> calls supply
    /// the same fragment reference (otherwise every frame would allocate a new closure
    /// over <see cref="BuildPopup"/>).</summary>
    private GuiRenderFragment? _popupFragment;

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        base.ConfigureSlot(builder);
        builder.OnKeyDown(HandleKeyDown);
        builder.ConfigureLayout(layout =>
        {
            // The header height is read back from LayoutParameters when positioning the popup.
            layout.Height = 30;
            layout.WidthMode = GuiSizeMode.Fill;
        });
    }

    /// <inheritdoc/>
    public override void OnParametersSet()
    {
        base.OnParametersSet();
        // Resolved every parameters-set so dropdowns picked up later via dialog reopen
        // also see the host without requiring a fresh component instance. Mirrors the
        // FocusManager lookup pattern in GuiInputBase.
        _overlayHost = GetCascadingValue<OverlayHost>();
    }

    // ── Public imperative API ────────────────────────────────────────────────

    /// <summary>Sets <see cref="SelectedIndex"/> from external code, clamps to
    /// <c>[0, Items.Count)</c> (or <c>-1</c> when out of range), and fires the change
    /// callbacks when the resolved index differs from the current one.</summary>
    public void SetSelectedIndex(int index)
    {
        int clamped = Items is null || index < 0 || index >= Items.Count ? -1 : index;
        if (clamped == SelectedIndex)
        {
            return;
        }

        SelectedIndex = clamped;
        OnSelectionChanged.Invoke(SelectedIndex);
        if (clamped >= 0)
        {
            OnItemSelected.Invoke(Items![clamped]);
        }

        RequestReconcile();
    }

    /// <summary>Opens the popup. No-op when already open or while disabled.</summary>
    public void Open()
    {
        if (IsOpen || !Enabled)
        {
            return;
        }

        IsOpen = true;
        RequestReconcile();
    }

    /// <summary>Closes the popup. No-op when already closed.</summary>
    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        _hoveredItemIndex = -1;
        RequestReconcile();
    }

    /// <summary>Flips the popup state.</summary>
    public void Toggle()
    {
        if (IsOpen)
        {
            Close();
        }
        else
        {
            Open();
        }
    }

    // ── Iteration hooks (extension points) ───────────────────────────────────

    /// <summary>Number of items currently visible in the popup. Default returns
    /// <c>Items?.Count ?? 0</c>. Override to drive a filtered view (e.g. autocomplete).</summary>
    protected virtual int GetItemCount() => Items?.Count ?? 0;

    /// <summary>Returns the item to render at popup position <paramref name="visibleIndex"/>
    /// in <c>[0, GetItemCount())</c>, alongside its index in <see cref="Items"/>. Default
    /// is a 1:1 mapping. Override to implement filtering — return the underlying index in
    /// <see cref="Items"/> via <paramref name="actualIndex"/> so selection semantics still
    /// reference the source list.</summary>
    protected virtual T GetItemAt(int visibleIndex, out int actualIndex)
    {
        actualIndex = visibleIndex;
        return Items![visibleIndex];
    }

    // ── Build hooks (override for custom shapes, e.g. autocomplete) ──────────

    /// <summary>Builds the closed-state header content (excluding chrome / chevron — those
    /// are drawn directly in <see cref="Render"/>). Default behaviour: invoke
    /// <see cref="SelectedTemplate"/> (falling back to <see cref="ItemTemplate"/>) with the
    /// current selection, or render <see cref="Placeholder"/> at half opacity when nothing
    /// is selected.</summary>
    protected virtual void BuildHeader(IGuiRenderTreeBuilder builder)
    {
        if (Items is { } items && SelectedIndex >= 0 && SelectedIndex < items.Count)
        {
            var template = SelectedTemplate ?? ItemTemplate;
            if (template is not null)
            {
                template(builder, items[SelectedIndex]);
            }
            else
            {
                builder.AddLabel(0, items[SelectedIndex]?.ToString() ?? string.Empty, font: Font);
            }

            return;
        }

        if (!string.IsNullOrEmpty(Placeholder))
        {
            var c = Font.Color;
            var placeholderFont = Font with { Color = GuiColor.FromRgba(c.R, c.G, c.B, c.A * 0.5) };
            builder.AddLabel(0, Placeholder, font: placeholderFont);
        }
    }

    /// <summary>Builds the popup body — the list of selectable rows. Default behaviour:
    /// iterate <see cref="GetItemCount"/> / <see cref="GetItemAt"/>, declaring a hover-
    /// and click-tracked <see cref="GuiContainer"/> per item that invokes
    /// <see cref="ItemTemplate"/> for its content.</summary>
    protected virtual void BuildPopup(IGuiRenderTreeBuilder builder)
    {
        int count = GetItemCount();
        for (int i = 0; i < count; i++)
        {
            T item = GetItemAt(i, out int actualIndex);
            int rowIdx = i;
            int selIdx = actualIndex;

            GuiColor rowBg = GuiColor.Transparent;
            if (selIdx == SelectedIndex)
            {
                rowBg = SelectedItemBackground;
            }
            // Hover wins over selected when both apply.
            if (rowIdx == _hoveredItemIndex)
            {
                rowBg = HoveredItemBackground;
            }

            // Always pass `rowBg` (even when transparent) so the row's GuiContainer.Background
            // gets reapplied every reconcile. AddContainer's `background:` parameter is
            // skip-when-null — passing null here would leave a previously-set non-transparent
            // background in place, which would otherwise leave a row stuck highlighted after
            // its hover state cleared. Transparent fills are already short-circuited in
            // GuiContainer.DrawBackground (A<=0), so the always-set path costs nothing extra.
            builder.AddContainer(i,
                widthMode: GuiSizeMode.Fill,
                height: ItemHeight,
                padding: new GuiThickness(0, TextPaddingX),
                background: rowBg,
                content: b => RenderItem(b, item))
                .OnMouseEnter(_ => SetHoveredItem(rowIdx))
                .OnMouseLeave(_ => SetHoveredItem(-1))
                // Keep dropdown focus while clicking inside the popup so the click-outside
                // close path doesn't fire on the selecting click itself.
                .OnMouseDown(_ => FocusManager?.RequestFocus(this))
                .OnMouseClick(_ => SelectAndClose(selIdx));
        }
    }

    private void RenderItem(IGuiRenderTreeBuilder b, T item)
    {
        if (ItemTemplate is { } t)
        {
            t(b, item);
        }
        else
        {
            b.AddLabel(0, item?.ToString() ?? string.Empty, font: Font);
        }
    }

    // ── Framework wiring ─────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override GuiLayoutSize Measure(GuiLayoutSize available)
    {
        // No intrinsic minimum width — fill the row. Height is controlled via
        // LayoutParameters.Height (set by own-slot defaults).
        // Include base child measurement so custom header templates can still participate
        // when a dropdown is measured as FitContent or inside an unbounded scroll axis.
        var content = base.Measure(available);
        return new GuiLayoutSize(
            Math.Max(content.Width, 120),
            Math.Max(content.Height, LayoutParameters.Height.FixedOrDefault(30)));
    }

    private void HandleKeyDown(GuiKeyEventArgs args)
    {
        if (!Enabled || !IsFocused)
        {
            return;
        }

        switch (args.KeyCode)
        {
            case (int)GlKeys.Enter:
            case (int)GlKeys.Space:
                Toggle();
                args.Handled = true;
                return;

            case (int)GlKeys.Escape:
                if (IsOpen) { Close(); args.Handled = true; }
                return;

            case (int)GlKeys.Up:
                if (Items is { Count: > 0 } itemsUp)
                {
                    int prev = SelectedIndex <= 0 ? itemsUp.Count - 1 : SelectedIndex - 1;
                    SetSelectedIndex(prev);
                    args.Handled = true;
                }
                return;

            case (int)GlKeys.Down:
                if (Items is { Count: > 0 } itemsDown)
                {
                    int next = SelectedIndex < 0 || SelectedIndex >= itemsDown.Count - 1 ? 0 : SelectedIndex + 1;
                    SetSelectedIndex(next);
                    args.Handled = true;
                }
                return;

            case (int)GlKeys.Tab:
                return; // let dialog-level traversal proceed
        }

        // Swallow other keys so global hotkeys don't fire while focused.
        args.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnInputClick(GuiMouseEventArgs e)
    {
        if (e.Button != EnumMouseButton.Left)
        {
            return;
        }

        Toggle();
    }

    /// <inheritdoc/>
    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        // Inner click capture (key 0) — added by GuiInputBase. Absolutely positioned,
        // fills the input's content area; handles down/up/click/enter/leave for the
        // closed-state trigger.
        base.BuildRenderTree(builder);

        // Header content (key 1) — relative, FitContent height with a vertical-centering
        // top margin computed from the font line metrics. The header container sits inside
        // the dropdown's content area; the input click capture (key 0) overlays it for
        // hit-testing without consuming layout space.
        // Right padding reserves the chevron strip; left padding pulls text away from
        // the recessed border. Top margin centres a single line of text within the
        // header height — templated rows that need different vertical layout should set
        // their own outer margin / heightMode.
        double headerHeight = LayoutParameters.Height.FixedOrDefault(30);
        double lineHeight = Font.MeasureHeight();
        double topMargin = Math.Max(0, (headerHeight - lineHeight) / 2.0);

        builder.AddContainer(1,
            widthMode: GuiSizeMode.Fill,
            margin: new GuiThickness(topMargin, ChevronAreaWidth, 0, TextPaddingX),
            content: BuildHeader);

        // Popup is no longer declared as a child slot here — it lives on the dialog's
        // OverlayHost so it paints on top of every regular slot and its rows always win
        // hover / click hit tests. Registration happens from Render (where bounds are
        // resolved) via _overlayHost.Show; see Render below.
    }

    /// <inheritdoc/>
    public override void Render(Context ctx, GuiComponentBounds bounds)
    {
        // Capture LastBounds so popup positioning can use the actual header height on
        // subsequent reconciles (handles the case where the user customised Height
        // through Fill / FitContent rather than an explicit value).
        base.Render(ctx, bounds);

        // Auto-close on focus loss — clicking outside the dialog or tabbing away
        // should dismiss the popup. The mouse-down dispatch path either re-claims
        // focus (popup item handler) or blurs us (click outside). We schedule a
        // single rebuild here; subsequent frames see IsOpen == false and skip.
        if (IsOpen && !IsFocused)
        {
            IsOpen = false;
            _hoveredItemIndex = -1;
            RequestReconcile();
        }

        // 1. Recessed chrome — vanilla text-input recipe (depth 2, brightness 0.8, r=1).
        GuiInset.Draw(ctx, bounds, depth: 2, brightness: 0.8f, radius: GuiVanillaStyle.ElementBgRadius);

        // 2. Hover / focus wash — same faint flat fill the text input uses.
        if (Enabled && (IsHovered || IsFocused))
        {
            ctx.Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            ctx.SetSourceRGBA(1, 1, 1, 0.05);
            ctx.Fill();
        }

        // 3. Chevron — a small downward-pointing triangle on the right side. Flips to
        //    point upward while the popup is open (mirrors common dropdown UX).
        DrawChevron(ctx, bounds);

        // 4. Popup overlay registration. Done from Render (rather than BuildRenderTree)
        //    so the overlay's bounds are computed from the just-resolved header bounds —
        //    no need to read back stale LastBounds from a prior frame. When closed (or
        //    when no items / no overlay host is available), simply skip registration and
        //    the OverlayHost prunes any existing subtree on the next frame.
        if (!IsOpen)
        {
            return;
        }

        if (_overlayHost is null)
        {
            return;
        }

        int count = GetItemCount();
        if (count <= 0)
        {
            return;
        }

        double inner = count * ItemHeight;
        double frame = inner + PopupPadding * 2;
        bool overflow = frame > MaxPopupHeight;
        double popupHeight = overflow ? MaxPopupHeight : frame;
        _popupOverflow = overflow;

        var popupBounds = new GuiComponentBounds(
            bounds.X,
            bounds.Bottom,
            bounds.Width,
            popupHeight);

        // Cache the fragment so its delegate identity is stable across frames — repeated
        // Show calls with the same fragment let the overlay layer's reuse path skip
        // any per-frame allocations.
        _popupFragment ??= BuildPopupOverlay;
        _overlayHost.Show(this, popupBounds, _popupFragment);
    }

    /// <summary>Tracks whether the popup currently overflows its max height — set during
    /// <see cref="Render"/> and consumed by <see cref="BuildPopupOverlay"/> to toggle
    /// vertical scrolling on the popup chrome.</summary>
    private bool _popupOverflow;

    /// <summary>Stable popup fragment — declares the popup chrome (a
    /// <see cref="GuiDropdownPopup"/> filling the registered overlay bounds) wrapped
    /// around <see cref="BuildPopup"/>.</summary>
    private void BuildPopupOverlay(IGuiRenderTreeBuilder builder)
    {
        bool overflow = _popupOverflow;
        builder.AddContainer<GuiDropdownPopup>(0,
                widthMode: GuiSizeMode.Fill,
                heightMode: GuiSizeMode.Fill,
                content: BuildPopup)
            .Configure(p =>
            {
                p.FillColor = PopupBackground;
                p.BorderColor = PopupBorder;
                p.BorderWidth = PopupBorderWidth;
                p.Radius = PopupRadius;
                p.LayoutParameters.Padding = new GuiThickness(PopupPadding);
                p.Scroll = overflow ? GuiScrollDirection.Vertical : GuiScrollDirection.None;
                p.Scrollbar = overflow ? GuiScrollDirection.Vertical : GuiScrollDirection.None;
            });
    }

    private void SetHoveredItem(int idx)
    {
        if (_hoveredItemIndex == idx)
        {
            return;
        }

        _hoveredItemIndex = idx;
        // Rebuild the dropdown subtree so the row's Background config picks up the new
        // hover state. Cheap for typical dropdown sizes — popups usually carry under 50
        // rows, and we only run on hover transitions.
        RequestReconcile();
    }

    private void SelectAndClose(int actualIndex)
    {
        SetSelectedIndex(actualIndex);
        Close();
    }

    private void DrawChevron(Context ctx, GuiComponentBounds b)
    {
        // Centred inside the right-edge chevron strip, ~6 logical pixels wide and
        // ~4 px tall — matches the visual weight of the header text.
        const double chevronW = 8;
        const double chevronH = 5;

        double cx = b.Right - ChevronAreaWidth / 2.0 - chevronW / 2.0;
        double cy = b.Y + (b.Height - chevronH) / 2.0;

        ctx.NewPath();
        if (IsOpen)
        {
            // Up-pointing triangle.
            ctx.MoveTo(cx, cy + chevronH);
            ctx.LineTo(cx + chevronW, cy + chevronH);
            ctx.LineTo(cx + chevronW / 2, cy);
        }
        else
        {
            // Down-pointing triangle.
            ctx.MoveTo(cx, cy);
            ctx.LineTo(cx + chevronW, cy);
            ctx.LineTo(cx + chevronW / 2, cy + chevronH);
        }
        ctx.ClosePath();

        var c = Font.Color;
        ctx.SetSourceRGBA(c.R, c.G, c.B, c.A * (Enabled ? 1.0 : 0.45));
        ctx.Fill();
    }
}

/// <summary>
/// String-typed convenience over <see cref="GuiDropdown{T}"/> — most pickers operate on a
/// list of pre-localised labels, and writing the type argument every time adds noise.
/// All of the templating / extensibility hooks of the generic base remain available.
/// </summary>
public class GuiDropdown : GuiDropdown<string>;

/// <summary>
/// Internal popup chrome for <see cref="GuiDropdown{T}"/> — same recipe as
/// <see cref="GuiTooltipBackground"/> (rounded solid fill + outer stroke). Kept as a
/// distinct type so that future popup-only customisation (e.g. a drop shadow) does not
/// drift into the tooltip layer.
/// </summary>
internal sealed class GuiDropdownPopup : GuiTooltipBackground;
