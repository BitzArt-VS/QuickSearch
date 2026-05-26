using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogRenderer<TDialog> : DialogRenderer
    where TDialog : GuiDialog, new()
{
    internal new TDialog Dialog => (TDialog)base.Dialog;

    internal DialogRenderer(
        ICoreClientAPI clientApi,
        Action<TDialog>? configure,
        Action requestClose)
        : base(clientApi)
    {
        try
        {
            InitializeDialog(configure, requestClose);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    internal void ReconcileDialog(Action<TDialog> configure)
    {
        ReconcileDialogSlot(configure);
    }
}

internal abstract class DialogRenderer : GuiSurfaceRenderer
{
    private IGuiDialog _dialog = null!;
    private bool _isDisposed;
    private bool _isFocused;
    private Action _requestClose = null!;

    private GuiElementAdapter _guiElementAdapter = null!;

    private double _currentLogicalWidth;
    private double _currentLogicalHeight;

    private readonly ScopedRebuildQueue _rebuildQueue = new();
    private DialogInputDispatcher _inputDispatcher = null!;

    private FloatingLayerRenderer _tooltipLayer = null!;
    private TooltipHost _tooltipHost = null!;
    private FloatingLayerRenderer _overlayLayer = null!;
    private OverlayHost _overlayHost = null!;
    private readonly GuiCursorHost _cursorHost = new();
    private string? _dialogOverrideCursor;

    private FocusManager _focusManager = null!;
    private FloatingLayerRenderer[] _floatingLayers = [];

    internal GuiCursorHost CursorHost => _cursorHost;
    internal IGuiNode? FocusedNode => _inputDispatcher.FocusedNode;
    internal IGuiDialog Dialog => _dialog;

    protected DialogRenderer(ICoreClientAPI clientApi)
        : base(clientApi)
    {
    }

    protected TDialog InitializeDialog<TDialog>(Action<TDialog>? configure, Action requestClose)
        where TDialog : GuiDialog, new()
    {
        _requestClose = requestClose;

        _tooltipLayer = new FloatingLayerRenderer(_clientApi);
        _overlayLayer = new FloatingLayerRenderer(_clientApi);
        _tooltipHost = new TooltipHost(_tooltipLayer);
        _overlayHost = new OverlayHost(_overlayLayer, this);
        _floatingLayers = [_overlayLayer, _tooltipLayer];
        _focusManager = new FocusManager(this);

        _inputDispatcher = new DialogInputDispatcher(TryToLogical, _tooltipHost);

        Builder.CascadeChain = BuildRootCascadeChain();
        _tooltipLayer.SetCascadeChain(Builder.CascadeChain);
        _overlayLayer.SetCascadeChain(Builder.CascadeChain);

        ReconcileDialogSlot(configure);
        var dialog = (TDialog)_dialog;

        _currentLogicalWidth = dialog.LayoutParameters.Width.Value;
        _currentLogicalHeight = dialog.LayoutParameters.Height.Value;

        EnsureSurfaceSize(
            (int)Math.Round(_currentLogicalWidth * _currentScale),
            (int)Math.Round(_currentLogicalHeight * _currentScale));

        _guiElementAdapter = new GuiElementAdapter(_clientApi, this);
        _clientApi.Gui.RegisterDialog(_guiElementAdapter);
        _guiElementAdapter.TryOpen();

        return dialog;
    }

    protected void ReconcileDialogSlot<TDialog>(Action<TDialog>? configure)
        where TDialog : GuiDialog, new()
    {
        Builder.Run(builder =>
        {
            builder.Add<TDialog>(0)
                .Configure(dialog =>
                {
                    if (!ReferenceEquals(_dialog, dialog))
                    {
                        _dialog = dialog;
                        dialog.AttachRuntime(new GuiDialogRuntime(this, _requestClose));
                    }

                    configure?.Invoke(dialog);
                });
        });
        ValidateRootSize();
        RequestArrange();
    }

    private void ValidateRootSize()
    {
        if (!_dialog.LayoutParameters.Width.IsFixed || !_dialog.LayoutParameters.Height.IsFixed)
        {
            throw new InvalidOperationException("Dialog must have fixed width and height for rendering.");
        }
    }

    private CascadingValueChain BuildRootCascadeChain()
    {
        var chain = new CascadingValueChain(parent: null, valueType: typeof(TooltipHost), name: null, value: _tooltipHost);
        chain = new CascadingValueChain(parent: chain, valueType: typeof(FocusManager), name: null, value: _focusManager);
        chain = new CascadingValueChain(parent: chain, valueType: typeof(OverlayHost), name: null, value: _overlayHost);
        chain = new CascadingValueChain(parent: chain, valueType: typeof(GuiCursorHost), name: null, value: _cursorHost);
        return chain;
    }

    internal void OnRenderFrame(float deltaTime)
    {
        if (_isDisposed)
        {
            return;
        }

        _inputDispatcher.FocusedNode?.OnFrame(deltaTime);
        if (_rebuildQueue.Drain())
        {
            RequestArrange();
        }
        RequestSurfaceUpdateForScaleOrSizeChanges();
        if (_arrangeRequested)
        {
            ExecuteArrangeWalk();
        }
        else if (_paintRequested)
        {
            ExecutePaintWalk();
        }

        var (posX, posY) = GetScreenOrigin();
        BlitAt(posX, posY);

        for (int i = 0; i < _floatingLayers.Length; i++)
        {
            _floatingLayers[i].Render();
        }
    }

    private void RequestSurfaceUpdateForScaleOrSizeChanges()
    {
        float scale = RuntimeEnv.GUIScale;
        double logicalWidth = _dialog.LayoutParameters.Width.Value;
        double logicalHeight = _dialog.LayoutParameters.Height.Value;

        if (scale == _currentScale && logicalWidth == _currentLogicalWidth && logicalHeight == _currentLogicalHeight)
        {
            return;
        }

        _currentScale = scale;
        _currentLogicalWidth = logicalWidth;
        _currentLogicalHeight = logicalHeight;
        EnsureSurfaceSize(
            (int)Math.Round(logicalWidth * scale),
            (int)Math.Round(logicalHeight * scale));

        RequestPaint();
    }

    private void ExecuteArrangeWalk()
    {
        _inputDispatcher.ClearArrangedRegions();
        _tooltipHost.ResetFrame();

        for (int i = 0; i < _floatingLayers.Length; i++)
        {
            _floatingLayers[i].OnFrameStart();
        }

        var bounds = new GuiComponentBounds(0, 0, _currentLogicalWidth, _currentLogicalHeight);
        DrawSurfaceContents(bounds, GuiDirection.Vertical, _currentScale, arrange: true);

        for (int i = 0; i < _floatingLayers.Length; i++)
        {
            _floatingLayers[i].RunWalk();
        }

        _inputDispatcher.RefreshHoverIfNotCapturing(_clientApi.Input.MouseX, _clientApi.Input.MouseY);
    }

    private void ExecutePaintWalk()
    {
        _tooltipHost.ResetFrame();

        var bounds = new GuiComponentBounds(0, 0, _currentLogicalWidth, _currentLogicalHeight);
        DrawSurfaceContents(bounds, GuiDirection.Vertical, _currentScale, arrange: false);

        _inputDispatcher.RefreshHoverIfNotCapturing(_clientApi.Input.MouseX, _clientApi.Input.MouseY);
    }

    public override void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder)
    {
        if (_isDisposed)
        {
            return;
        }

        _rebuildQueue.Schedule(fragment, builder);
        RequestReconcile();
    }

    public override void Cancel(GuiRenderFragment fragment) => _rebuildQueue.Cancel(fragment);

    public override void AddInteractiveRegion(in InteractiveRegion region) => _inputDispatcher.AddInteractiveRegion(region);
    public override void AddKeyboardRegion(in KeyboardRegion region) => _inputDispatcher.AddKeyboardRegion(region);

    // --- Lifecycle ---

    internal void RequestFocus() => _clientApi.Gui.RequestFocus(_guiElementAdapter);

    internal void SetMouseOverCursor(string? cursor)
    {
        _dialogOverrideCursor = cursor;
        // Set immediately on the adapter so the cursor is correct even when the
        // mouse is stationary (e.g. holding down at the start of a resize gesture).
        _guiElementAdapter.MouseOverCursor = cursor;
    }

    // --- Focus forwarding ---

    internal void OnFocus()
    {
        _isFocused = true;
        _dialog.OnFocus();
    }

    internal void OnUnFocus()
    {
        _isFocused = false;
        _dialog.OnUnFocus();
    }

    internal bool OnEscapePressed() => _dialog.OnEscapePressed();

    // --- Full event handlers (called directly by the adapter) ---

    internal void OnMouseDown(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        bool hit = _inputDispatcher.DispatchMouseDown(args);
        if (hit)
        {
            RequestFocus();
            args.Handled = true;
        }
    }

    internal void OnMouseUp(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _inputDispatcher.DispatchMouseUp(args);
        if (ContainsScreenPoint(args.X, args.Y) || ContainsOverlayScreenPoint(args.X, args.Y))
        {
            args.Handled = true;
        }
    }

    internal void OnMouseMove(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        bool dispatched = _inputDispatcher.DispatchMouseMove(args);
        // Resize cursor (set via SetMouseOverCursor during dispatch) takes priority over
        // any component hover cursor. Fall back to the hover cursor when not resizing.
        _guiElementAdapter.MouseOverCursor = _dialogOverrideCursor ?? _cursorHost.HoverCursor;
        if (dispatched || ContainsScreenPoint(args.X, args.Y) || ContainsOverlayScreenPoint(args.X, args.Y))
        {
            args.Handled = true;
        }
    }

    internal void OnMouseWheel(MouseWheelEventArgs args)
    {
        if (args.IsHandled)
        {
            return;
        }

        if (_isFocused
            && (ContainsScreenPoint(_clientApi.Input.MouseX, _clientApi.Input.MouseY)
                || ContainsOverlayScreenPoint(_clientApi.Input.MouseX, _clientApi.Input.MouseY)))
        {
            _inputDispatcher.DispatchMouseWheel(_clientApi.Input.MouseX, _clientApi.Input.MouseY, args.deltaPrecise);
            args.SetHandled(true);
        }
    }

    internal void OnKeyDown(KeyEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _dialog.OnKeyDown(args);
        if (args.Handled)
        {
            return;
        }

        _inputDispatcher.DispatchKeyDown(args);
    }

    internal void OnKeyUp(KeyEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _dialog.OnKeyUp(args);
        if (args.Handled)
        {
            return;
        }

        _inputDispatcher.DispatchKeyUp(args);
    }

    internal void OnKeyPress(KeyEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _dialog.OnKeyPress(args);
        if (args.Handled)
        {
            return;
        }

        _inputDispatcher.DispatchKeyPress(args);
    }

    // --- Geometry helpers (used by GuiDialog for resize hit-testing and overlay checks) ---

    public override bool ContainsScreenPoint(int x, int y)
    {
        var (positionX, positionY, physicalWidth, physicalHeight, _) = ResolveScreenRect();
        return x >= positionX && x < positionX + physicalWidth
            && y >= positionY && y < positionY + physicalHeight;
    }

    public bool ContainsOverlayScreenPoint(int x, int y) => _overlayLayer.ContainsScreenPoint(x, y);

    internal (int posX, int posY) GetScreenOrigin()
    {
        var (positionX, positionY, _, _, _) = ResolveScreenRect();
        return (positionX, positionY);
    }

    public bool TryToLogical(int x, int y, out double logicalX, out double logicalY)
    {
        var (positionX, positionY, physicalWidth, physicalHeight, scale) = ResolveScreenRect();
        logicalX = (x - positionX) / scale;
        logicalY = (y - positionY) / scale;
        return x >= positionX && x < positionX + physicalWidth
            && y >= positionY && y < positionY + physicalHeight;
    }

    private (int positionX, int positionY, double physicalWidth, double physicalHeight, float scale) ResolveScreenRect()
    {
        float scale = RuntimeEnv.GUIScale;
        double physicalWidth = Math.Round(_dialog.LayoutParameters.Width.Value * scale);
        double physicalHeight = Math.Round(_dialog.LayoutParameters.Height.Value * scale);
        var (positionX, positionY) = ComputeScreenOrigin(physicalWidth, physicalHeight, scale);
        return (positionX, positionY, physicalWidth, physicalHeight, scale);
    }

    private (int positionX, int positionY) ComputeScreenOrigin(double physicalWidth, double physicalHeight, float scale)
    {
        // The surface renderer rounds logical size to physical pixels before blitting.
        // Center against that same rounded rectangle so resize anchoring cannot alternate
        // between adjacent integer origins while the logical size crosses half-pixels.
        int positionX = (int)((_clientApi.Render.FrameWidth - physicalWidth) / 2.0 + _dialog.OffsetX * scale);
        int positionY = (int)((_clientApi.Render.FrameHeight - physicalHeight) / 2.0 + _dialog.OffsetY * scale);
        return (positionX, positionY);
    }

    internal void SetFocusedNode(IGuiNode? node) => _inputDispatcher.SetFocusedNode(node);

    public override void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _tooltipHost?.Hide();

        _inputDispatcher?.SetFocusedNode(null);
        if (_guiElementAdapter is not null)
        {
            _guiElementAdapter.TryClose();
            _clientApi.UnregisterDialog(_guiElementAdapter);
            _guiElementAdapter.Dispose();
        }

        for (int i = 0; i < _floatingLayers.Length; i++)
        {
            _floatingLayers[i].Dispose();
        }

        base.Dispose();
    }
}
