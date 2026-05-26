using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public abstract class GuiDialog : GuiComponent, IGuiDialog, IDisposable
{
    // ClientApi is guaranteed non-null after the dialog is attached by the dialog host.
    protected new ICoreClientAPI ClientApi => base.ClientApi!;
    protected bool IsDisposed { get; private set; }

    /// <summary>
    /// Whether this dialog currently holds keyboard focus. Only the focused dialog receives
    /// keyboard events, and vanilla focus handling brings it in front of other Cairo dialogs.
    /// </summary>
    public bool IsFocused { get; private set; }

    private GuiDialogRuntime? _runtime;

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
            if (_isResizable == value)
            {
                return;
            }

            _isResizable = value;
            EnsureResizeCursors();
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
    private double _resizeStartMouseLogicalX;
    private double _resizeStartMouseLogicalY;
    private double _resizeStartW;
    private double _resizeStartH;
    private double _resizeStartOffsetX;
    private double _resizeStartOffsetY;
    // Physical-pixel left/top edge of the dialog at resize start. Used by UpdateResize
    // to keep the pinned edge at an exact integer pixel despite the texture width
    // oscillating between two rounded values as the logical size crosses half-integers.
    private int _resizeAnchorLeft;
    private int _resizeAnchorTop;

    protected GuiDialog()
    {
    }

    internal void AttachRuntime(GuiDialogRuntime runtime)
    {
        _runtime = runtime;
        EnsureResizeCursors();
    }

    private GuiDialogRuntime Runtime => _runtime
        ?? throw new InvalidOperationException("Dialog is not attached to a dialog host.");

    private void EnsureResizeCursors()
    {
        if (!_isResizable || base.ClientApi is not ICoreClientAPI clientApi)
        {
            return;
        }

        // Lazy cursor registration: only pay the Cairo+temp-file cost when at least
        // one resizable dialog is constructed in this session. Idempotent.
        GuiResizeCursors.EnsureLoaded(clientApi);
    }

    /// <summary>
    /// Requests keyboard focus for this dialog. Other open dialogs lose focus and vanilla
    /// focus handling brings this dialog to the front.
    /// </summary>
    public void RequestFocus()
    {
        if (IsDisposed)
        {
            return;
        }

        Runtime.RequestFocus();
    }

    protected void RequestClose()
    {
        if (IsDisposed)
        {
            return;
        }

        Runtime.RequestClose();
    }

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        base.ConfigureSlot(builder);
        IGuiDialog dialog = this;
        builder.ConfigureLayout(layoutParameters =>
        {
            layoutParameters.Width = 400;
            layoutParameters.Height = 300;
        });
        builder
            .OnMouseDown(dialog.OnMouseDown)
            .OnMouseUp(dialog.OnMouseUp)
            .OnMouseMove(dialog.OnMouseMove)
            .OnMouseLeave(dialog.OnMouseLeave);
    }

    protected virtual void OnResizeUpdated(bool sizeChanged)
    {
        if (sizeChanged)
        {
            RequestArrange();
        }
    }

    /// <summary>
    /// Override to react to focus changes. Vanilla <c>RequestFocus</c> already moves the
    /// focused dialog to the front of the opened dialog list, so the renderer needs no
    /// extra dialog-level ordering API.
    /// </summary>
    protected virtual void OnFocusChanged(bool focused) { }

    void IGuiDialog.OnFocus()
    {
        if (IsFocused)
        {
            return;
        }

        IsFocused = true;
        OnFocusChanged(true);
    }

    void IGuiDialog.OnUnFocus()
    {
        if (!IsFocused)
        {
            return;
        }

        IsFocused = false;
        OnFocusChanged(false);
    }

    void IGuiDialog.OnKeyDown(KeyEvent args) => OnKeyDown(args);
    protected virtual void OnKeyDown(KeyEvent args) { }

    void IGuiDialog.OnKeyPress(KeyEvent args) => OnKeyPress(args);
    protected virtual void OnKeyPress(KeyEvent args) { }

    void IGuiDialog.OnKeyUp(KeyEvent args) => OnKeyUp(args);
    protected virtual void OnKeyUp(KeyEvent args) { }

    void IGuiDialog.OnMouseDown(GuiMouseEventArgs args)
    {
        if (!IsResizable)
        {
            return;
        }

        if (_resizeEdge != GuiResizeEdge.None)
        {
            return;
        }

        var edge = HitTestResizeEdge(args.Position.X, args.Position.Y);
        if (edge == GuiResizeEdge.None)
        {
            return;
        }

        BeginResize(edge, args.Position.X, args.Position.Y);
        // Preserve any focused component through the gesture: re-claiming the currently
        // focused node sets _focusClaimedThisDispatch = true before the dispatcher's
        // automatic blur check runs.
        var currentFocused = Runtime.FocusedNode;
        if (currentFocused is not null)
        {
            Runtime.SetFocusedNode(currentFocused);
        }
    }

    void IGuiDialog.OnMouseUp(GuiMouseEventArgs args)
    {
        if (_resizeEdge == GuiResizeEdge.None)
        {
            return;
        }

        EndResize();
        RequestPaint();
    }

    void IGuiDialog.OnMouseMove(GuiMouseEventArgs args)
    {
        if (_resizeEdge != GuiResizeEdge.None)
        {
            UpdateResize(args.Position.X, args.Position.Y);
            return;
        }

        if (!IsResizable)
        {
            return;
        }

        var edge = HitTestResizeEdge(args.Position.X, args.Position.Y);
        Runtime.SetMouseOverCursor(CursorForEdge(edge));
    }

    void IGuiDialog.OnMouseLeave(GuiMouseEventArgs args)
    {
        if (_resizeEdge != GuiResizeEdge.None)
        {
            return;
        }

        Runtime.SetMouseOverCursor(null);
    }

    private GuiResizeEdge HitTestResizeEdge(double lx, double ly)
    {
        double w = LayoutParameters.Width.Value;
        double h = LayoutParameters.Height.Value;
        const double t = ResizeEdgeThickness;

        var edge = GuiResizeEdge.None;
        if (lx > w - t)
        {
            edge |= GuiResizeEdge.Right;
        }

        if (ly > h - t)
        {
            edge |= GuiResizeEdge.Bottom;
        }

        return edge;
    }

    /// <summary>
    /// Selects the cursor sprite for an active or hovered resize edge combination.
    /// Returns <c>null</c> when <paramref name="edge"/> is <see cref="GuiResizeEdge.None"/>.
    /// </summary>
    private static string? CursorForEdge(GuiResizeEdge edge) => edge switch
    {
        GuiResizeEdge.Right => GuiResizeCursors.Horizontal,
        GuiResizeEdge.Bottom => GuiResizeCursors.Vertical,
        GuiResizeEdge.Right | GuiResizeEdge.Bottom => GuiResizeCursors.DiagonalNwSe,
        _ => null,
    };

    private void BeginResize(GuiResizeEdge edge, double logicalX, double logicalY)
    {
        float scale = Vintagestory.API.Config.RuntimeEnv.GUIScale;
        _resizeEdge = edge;
        _resizeStartMouseLogicalX = logicalX;
        _resizeStartMouseLogicalY = logicalY;
        _resizeStartW = LayoutParameters.Width.Value;
        _resizeStartH = LayoutParameters.Height.Value;
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
        Runtime.SetMouseOverCursor(CursorForEdge(edge));
    }

    private void EndResize()
    {
        _resizeEdge = GuiResizeEdge.None;
        // Reset the cursor so other dialogs / world cursor take over once we release.
        Runtime.SetMouseOverCursor(null);
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
    private void UpdateResize(double logicalX, double logicalY)
    {
        float scale = Vintagestory.API.Config.RuntimeEnv.GUIScale;
        double dxLogical = logicalX - _resizeStartMouseLogicalX;
        double dyLogical = logicalY - _resizeStartMouseLogicalY;

        double newW = _resizeStartW;
        double newH = _resizeStartH;
        double newOffX = _resizeStartOffsetX;
        double newOffY = _resizeStartOffsetY;

        if ((_resizeEdge & GuiResizeEdge.Right) != 0)
        {
            newW = Math.Clamp(_resizeStartW + dxLogical, MinWidth, MaxWidth);
            // Derive OffsetX from the snapshotted physical left anchor so the left edge
            // never oscillates: posX = (FrameWidth - physNewW) / 2 + OffsetX * scale
            // = _resizeAnchorLeft when OffsetX = (_resizeAnchorLeft + physNewW/2 - FrameWidth/2) / scale.
            double physNewW = Math.Round(newW * scale);
            newOffX = (_resizeAnchorLeft + physNewW / 2.0 - ClientApi.Render.FrameWidth / 2.0) / scale;
        }
        if ((_resizeEdge & GuiResizeEdge.Bottom) != 0)
        {
            newH = Math.Clamp(_resizeStartH + dyLogical, MinHeight, MaxHeight);
            double physNewH = Math.Round(newH * scale);
            newOffY = (_resizeAnchorTop + physNewH / 2.0 - ClientApi.Render.FrameHeight / 2.0) / scale;
        }

        double previousWidth = LayoutParameters.Width.Value;
        double previousHeight = LayoutParameters.Height.Value;

        LayoutParameters.Width = newW;
        LayoutParameters.Height = newH;
        OffsetX = newOffX;
        OffsetY = newOffY;

        OnResizeUpdated(newW != previousWidth || newH != previousHeight);
    }

    bool IGuiDialog.OnEscapePressed()
    {
        // When a component is focused, blur it instead of closing — mirrors typical UI
        // behaviour where Escape first cancels the active input, then closes the dialog
        // on a second press. Components that need to consume Escape themselves can mark
        // the event Handled in their builder.OnKeyDown handler before this fallback runs.
        if (Runtime.FocusedNode is not null)
        {
            Runtime.SetFocusedNode(null);
            return true;
        }
        RequestClose();
        return true;
    }

    public virtual void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        IsDisposed = true;

        GC.SuppressFinalize(this);
    }
}
