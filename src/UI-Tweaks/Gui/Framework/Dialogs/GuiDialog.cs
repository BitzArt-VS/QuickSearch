using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public abstract class GuiDialog : GuiComponent, IGuiDialog, IDisposable
{
    // ClientApi is guaranteed non-null for dialogs: Attach is called in the GuiDialog
    // constructor before any consumer code runs, so the nullable base property is always set.
    protected new ICoreClientAPI ClientApi => base.ClientApi!;
    protected bool IsDisposed { get; private set; }
    public bool IsOpen { get; private set; }

    /// <summary>
    /// Whether this dialog currently holds keyboard focus. Only the focused dialog receives
    /// keyboard events and is drawn on top of other dialogs at the same render rank.
    /// </summary>
    public bool IsFocused { get; private set; }

    private readonly DialogRenderer _renderer;

    public virtual double RenderOrder => 0.2;

    /// <summary>
    /// Horizontal offset in logical (unscaled) pixels from the screen-centred default position.
    /// Mutated by drag interactions (see <see cref="Move"/>) and read by the renderer each
    /// frame; setting it directly snaps the dialog to a new position.
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// Vertical offset in logical (unscaled) pixels from the screen-centred default position.
    /// Mutated by drag interactions (see <see cref="Move"/>) and read by the renderer each
    /// frame; setting it directly snaps the dialog to a new position.
    /// </summary>
    public double OffsetY { get; set; }

    double IGuiDialog.OffsetX => OffsetX;
    double IGuiDialog.OffsetY => OffsetY;

    /// <summary>
    /// Adds a delta (logical pixels) to the dialog's screen-position offset. Intended as the
    /// drag-handler target for <see cref="GuiDialogTitleBar.OnDrag"/>: pass <c>this.Move</c> as
    /// the title bar's <c>onDrag</c> callback to make the title bar drag the dialog around.
    /// </summary>
    public void Move(double deltaX, double deltaY)
    {
        OffsetX += deltaX;
        OffsetY += deltaY;
    }

    /// <summary>
    /// When true, the user can drag the dialog's bottom and right edges (and the SE corner)
    /// to resize it. The cursor switches to a directional resize sprite while hovering a
    /// grab zone. <see cref="MinWidth"/>/<see cref="MinHeight"/>/<see cref="MaxWidth"/>/
    /// <see cref="MaxHeight"/> bound the size; they have no effect when this is false.
    /// <para>
    /// Top and left edges are intentionally non-resizable: the dialog is rendered centred
    /// on the screen with title-bar drag controlling the offset, so resizing from the
    /// bottom-right keeps the gesture predictable (the un-dragged edges are pinned by the
    /// existing centre+offset positioning, no compensation needed).
    /// </para>
    /// </summary>
    public bool IsResizable
    {
        get => _isResizable;
        set
        {
            if (_isResizable == value) return;
            _isResizable = value;
            // Lazy cursor registration: only pay the Cairo+temp-file cost when at least
            // one resizable dialog is constructed in this session. Idempotent.
            if (value)
            {
                GuiResizeCursors.EnsureLoaded(ClientApi);
            }
        }
    }
    private bool _isResizable = false;

    /// <summary>Minimum logical-pixel width enforced while resizing. Default 200.</summary>
    public int MinWidth { get; set; } = 200;
    /// <summary>Minimum logical-pixel height enforced while resizing. Default 100.</summary>
    public int MinHeight { get; set; } = 100;
    /// <summary>Maximum logical-pixel width enforced while resizing. Default 2000.</summary>
    public int MaxWidth { get; set; } = 2000;
    /// <summary>Maximum logical-pixel height enforced while resizing. Default 1500.</summary>
    public int MaxHeight { get; set; } = 1500;

    /// <summary>
    /// Thickness of the inward edge grab zone, in logical pixels. The corner zone is the
    /// square where two edge bands overlap. Tuned to be wide enough for comfortable
    /// grabbing without overlapping inner content.
    /// </summary>
    private const double ResizeEdgeThickness = 6.0;

    // Active resize state. None when not resizing. _resizeStart* snapshot the dialog's
    // logical size + offset at MouseDown so per-frame updates compute against a stable
    // baseline (avoiding drift from successive clamped deltas).
    private GuiResizeEdge _resizeEdge = GuiResizeEdge.None;
    private int _resizeStartMouseX;
    private int _resizeStartMouseY;
    private double _resizeStartW;
    private double _resizeStartH;
    private double _resizeStartOffsetX;
    private double _resizeStartOffsetY;
    // Physical-pixel left/top edge of the dialog at resize start. Used by UpdateResize
    // to keep the pinned edge at an exact integer pixel despite the texture width
    // oscillating between two rounded values as the logical size crosses half-integers.
    private int _resizeAnchorLeft;
    private int _resizeAnchorTop;

    protected GuiDialog(ICoreClientAPI clientApi)
    {
        LayoutParameters.Width = 400;
        LayoutParameters.Height = 300;
        _renderer = new DialogRenderer(clientApi, this, GetType().Name);
        Attach(_renderer.Handle, clientApi);
    }

    public void Open()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        // TryOpen drives vanilla focus management, which calls Focus() on the interceptor
        // and propagates here via OnFocus(). It also adds the interceptor to game.OpenedGuis,
        // which is what GuiManager.OnRenderFrameGUI iterates — our render is then driven
        // from the interceptor's OnRenderGUI override, so this dialog shares the vanilla
        // dialog z-stack instead of painting from a separate Ortho renderer slot.
        _renderer.TryOpen();
        StateHasChanged();
        OnOpened();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        // Suppress any active tooltip — otherwise its surface would flash on next open
        // until the user moves the cursor off and back onto the trigger.
        _renderer.HideTooltip();
        // Clear focus on close so a re-opened dialog starts in a fresh state and the
        // caret blink loop stops accumulating ticks against a node that may be pruned.
        _renderer.SetFocusedNode(null);
        _renderer.TryClose();
        OnClosed();
    }

    /// <summary>
    /// Requests keyboard focus for this dialog. Other open dialogs lose focus and this
    /// dialog is brought to the front of its render rank.
    /// </summary>
    public void RequestFocus()
    {
        if (!IsOpen || IsDisposed) return;
        _renderer.RequestFocus();
    }

    protected virtual void OnOpened() { }
    protected virtual void OnClosed() { }

    /// <summary>
    /// Override to react to focus changes. Vanilla <c>RequestFocus</c> already moves the
    /// focused dialog to the front of its <c>DrawOrder</c> rank inside <c>OpenedGuis</c>,
    /// so the renderer needs no extra work to draw on top of same-rank vanilla dialogs.
    /// </summary>
    protected virtual void OnFocusChanged(bool focused) { }

    void IGuiDialog.OnFocus()
    {
        if (IsFocused) return;
        IsFocused = true;
        OnFocusChanged(true);
    }

    void IGuiDialog.OnUnFocus()
    {
        if (!IsFocused) return;
        IsFocused = false;
        OnFocusChanged(false);
    }

    void IGuiDialog.OnKeyDown(KeyEvent args) => OnKeyDown(args);
    protected virtual void OnKeyDown(KeyEvent args) { }

    void IGuiDialog.OnKeyPress(KeyEvent args) => OnKeyPress(args);
    protected virtual void OnKeyPress(KeyEvent args) { }

    void IGuiDialog.OnKeyUp(KeyEvent args) => OnKeyUp(args);
    protected virtual void OnKeyUp(KeyEvent args) { }

    /// <inheritdoc/>
    public override void OnMouseDown(GuiMouseEventArgs args)
    {
        if (!IsResizable) return;
        if (_resizeEdge != GuiResizeEdge.None) return;

        var edge = HitTestResizeEdge(args.ScreenX, args.ScreenY);
        if (edge == GuiResizeEdge.None) return;

        BeginResize(edge, args.ScreenX, args.ScreenY);
        // Preserve any focused component through the gesture: re-claiming the currently
        // focused node sets _focusClaimedThisDispatch = true before the dispatcher's
        // automatic blur check runs.
        var currentFocused = _renderer.FocusedNode;
        if (currentFocused is not null) _renderer.SetFocusedNode(currentFocused);
    }

    /// <inheritdoc/>
    public override void OnMouseUp(GuiMouseEventArgs args)
    {
        if (_resizeEdge == GuiResizeEdge.None) return;
        EndResize();
        StateHasChanged();
    }

    /// <inheritdoc/>
    public override void OnMouseMove(GuiMouseEventArgs args)
    {
        if (_resizeEdge != GuiResizeEdge.None)
        {
            UpdateResize(args.ScreenX, args.ScreenY);
            return;
        }

        if (!IsResizable) return;
        var edge = HitTestResizeEdge(args.ScreenX, args.ScreenY);
        _renderer.SetMouseOverCursor(CursorForEdge(edge));
    }

    /// <inheritdoc/>
    public override void OnMouseLeave(GuiMouseEventArgs args)
    {
        if (_resizeEdge != GuiResizeEdge.None) return;
        _renderer.SetMouseOverCursor(null);
    }

    private GuiResizeEdge HitTestResizeEdge(int physX, int physY)
    {
        if (!_renderer.TryToLogical(physX, physY, out double lx, out double ly))
            return GuiResizeEdge.None;

        double w = LayoutParameters.Width!.Value;
        double h = LayoutParameters.Height!.Value;
        const double t = ResizeEdgeThickness;

        var edge = GuiResizeEdge.None;
        if (lx > w - t) edge |= GuiResizeEdge.East;
        if (ly > h - t) edge |= GuiResizeEdge.South;
        return edge;
    }

    /// <summary>
    /// Selects the cursor sprite for an active or hovered resize edge combination.
    /// Returns <c>null</c> when <paramref name="edge"/> is <see cref="GuiResizeEdge.None"/>.
    /// </summary>
    private static string? CursorForEdge(GuiResizeEdge edge) => edge switch
    {
        GuiResizeEdge.East => GuiResizeCursors.Horizontal,
        GuiResizeEdge.South => GuiResizeCursors.Vertical,
        GuiResizeEdge.East | GuiResizeEdge.South => GuiResizeCursors.DiagonalNwSe,
        _ => null,
    };

    private void BeginResize(GuiResizeEdge edge, int physX, int physY)
    {
        float scale = Vintagestory.API.Config.RuntimeEnv.GUIScale;
        _resizeEdge = edge;
        _resizeStartMouseX = physX;
        _resizeStartMouseY = physY;
        _resizeStartW = LayoutParameters.Width!.Value;
        _resizeStartH = LayoutParameters.Height!.Value;
        _resizeStartOffsetX = OffsetX;
        _resizeStartOffsetY = OffsetY;
        // Snapshot the physical left/top edge so UpdateResize can anchor against an exact
        // integer pixel. Without this, OffsetX derived from the fractional logical width
        // causes posX to oscillate ±1 px every time the texture width rounds differently.
        int physW = (int)Math.Round(_resizeStartW * scale);
        int physH = (int)Math.Round(_resizeStartH * scale);
        _resizeAnchorLeft = (int)((ClientApi.Render.FrameWidth - physW) / 2.0 + OffsetX * scale);
        _resizeAnchorTop = (int)((ClientApi.Render.FrameHeight - physH) / 2.0 + OffsetY * scale);
        // Pin the cursor for the gesture's duration so it stays correct even when the
        // pointer wanders outside the dialog mid-drag (vanilla GuiManager reads
        // MouseOverCursor unconditionally per frame, no hover gate).
        _renderer.SetMouseOverCursor(CursorForEdge(edge));
    }

    private void EndResize()
    {
        _resizeEdge = GuiResizeEdge.None;
        // Reset the cursor so other dialogs / world cursor take over once we release.
        _renderer.SetMouseOverCursor(null);
    }

    /// <summary>
    /// Recomputes <see cref="GuiComponent.LayoutParameters"/>.Width/Height and
    /// <see cref="OffsetX"/>/<see cref="OffsetY"/> from the cursor delta against the
    /// snapshot taken at <see cref="BeginResize"/>. Only South/East edges are supported
    /// (see <see cref="IsResizable"/> remarks). The offset is derived from the snapshotted
    /// physical anchor edge rather than the fractional logical delta, so the pinned edge
    /// stays at an exact integer pixel throughout the gesture — no shiver.
    /// Min/max bounds clamp the size; the dragged edge tracks the cursor up to the limit
    /// and then stops cleanly.
    /// </summary>
    private void UpdateResize(int physX, int physY)
    {
        float scale = Vintagestory.API.Config.RuntimeEnv.GUIScale;
        double dxLogical = (physX - _resizeStartMouseX) / scale;
        double dyLogical = (physY - _resizeStartMouseY) / scale;

        double newW = _resizeStartW;
        double newH = _resizeStartH;
        double newOffX = _resizeStartOffsetX;
        double newOffY = _resizeStartOffsetY;

        if ((_resizeEdge & GuiResizeEdge.East) != 0)
        {
            newW = Math.Clamp(_resizeStartW + dxLogical, MinWidth, MaxWidth);
            // Derive OffsetX from the snapshotted physical left anchor so the left edge
            // never oscillates: posX = (FrameWidth - physNewW) / 2 + OffsetX * scale
            // = _resizeAnchorLeft when OffsetX = (_resizeAnchorLeft + physNewW/2 - FrameWidth/2) / scale.
            double physNewW = Math.Round(newW * scale);
            newOffX = (_resizeAnchorLeft + physNewW / 2.0 - ClientApi.Render.FrameWidth / 2.0) / scale;
        }
        if ((_resizeEdge & GuiResizeEdge.South) != 0)
        {
            newH = Math.Clamp(_resizeStartH + dyLogical, MinHeight, MaxHeight);
            double physNewH = Math.Round(newH * scale);
            newOffY = (_resizeAnchorTop + physNewH / 2.0 - ClientApi.Render.FrameHeight / 2.0) / scale;
        }

        // Mutating LayoutParameters.Width/Height is observed by DialogRenderer's per-frame
        // size-change check, which recreates the Cairo surface and forces a full layout
        // pass + redraw. Cheap enough to do every move event (200+ Hz) for the size range
        // we expect.
        LayoutParameters.Width = newW;
        LayoutParameters.Height = newH;
        OffsetX = newOffX;
        OffsetY = newOffY;
    }

    bool IGuiDialog.OnEscapePressed()
    {
        // When a component is focused, blur it instead of closing — mirrors typical UI
        // behaviour where Escape first cancels the active input, then closes the dialog
        // on a second press. Components that need to consume Escape themselves can mark
        // the event Handled in their OnKeyDown hook before this fallback runs.
        if (_renderer.FocusedNode is not null)
        {
            _renderer.SetFocusedNode(null);
            return true;
        }
        Close();
        return true;
    }

    public virtual void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        Close();

        _renderer.Dispose();

        IsDisposed = true;

        GC.SuppressFinalize(this);
    }
}
