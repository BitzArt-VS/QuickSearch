using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogRenderer : IGuiComponentTreeRenderer, IDisposable
{
    private readonly ICoreClientAPI _clientApi;
    private readonly IGuiDialog _dialog;
    private bool _isDisposed;
    private bool _surfaceDirty = true;
    private bool _isFocused;

    private readonly CairoDialogInputInterceptor _inputInterceptor;

    private readonly GuiContentSurface _contentSurface;
    private float _currentScale;
    private double _currentLogicalWidth;
    private double _currentLogicalHeight;

    private readonly ScopedRebuildQueue _rebuildQueue = new();
    private readonly DialogInputDispatcher _inputDispatcher;

    private readonly DialogScreenProjection _screenProjection;

    private readonly TooltipRenderer _tooltipRenderer;
    private readonly TooltipHost _tooltipHost;
    private readonly OverlayRenderer _overlayRenderer;
    private readonly OverlayHost _overlayHost;
    private readonly GuiCursorHost _cursorHost = new();
    private string? _dialogOverrideCursor;

    private readonly FocusManager _focusManager;
    private readonly FloatingLayerRenderer[] _floatingLayers;

    internal GuiCursorHost CursorHost => _cursorHost;
    internal IGuiNode? FocusedNode => _inputDispatcher.FocusedNode;
    internal bool CaretBlinkOn => _inputDispatcher.CaretBlinkOn;

    public IGuiRenderHandle Handle => _contentSurface.Handle;

    public ICoreClientAPI ClientApi => _clientApi;

    public double RenderOrder => _dialog.RenderOrder;
    public int RenderRange => int.MaxValue;

    internal DialogRenderer(ICoreClientAPI clientApi, IGuiDialog dialog, string name)
    {
        _clientApi = clientApi;
        _dialog = dialog;

        if (dialog.LayoutParameters.Width is null || dialog.LayoutParameters.Height is null)
            throw new ArgumentException("Dialog must have fixed width and height for rendering.", nameof(dialog));

        _currentScale = RuntimeEnv.GUIScale;
        _currentLogicalWidth = dialog.LayoutParameters.Width!.Value;
        _currentLogicalHeight = dialog.LayoutParameters.Height!.Value;

        _contentSurface = new GuiContentSurface(clientApi, this);
        _contentSurface.EnsureSize(
            (int)Math.Round(_currentLogicalWidth * _currentScale),
            (int)Math.Round(_currentLogicalHeight * _currentScale));

        _tooltipRenderer = new TooltipRenderer(clientApi);
        _tooltipHost = new TooltipHost(_tooltipRenderer);
        _overlayRenderer = new OverlayRenderer(this, clientApi);
        _overlayHost = new OverlayHost(_overlayRenderer);
        _floatingLayers = [_overlayRenderer, _tooltipRenderer];
        _focusManager = new FocusManager(this);

        _screenProjection = new DialogScreenProjection(clientApi, dialog);
        _inputDispatcher = new DialogInputDispatcher(_screenProjection.TryToLogical, _tooltipHost, () => _surfaceDirty = true);

        _contentSurface.Builder.CascadeChain = BuildRootCascadeChain();
        _tooltipRenderer.SetCascadeChain(_contentSurface.Builder.CascadeChain);
        _overlayRenderer.SetCascadeChain(_contentSurface.Builder.CascadeChain);

        _inputInterceptor = new CairoDialogInputInterceptor(clientApi, this);
        clientApi.Gui.RegisterDialog(_inputInterceptor);
    }

    private CascadingValueChain BuildRootCascadeChain()
    {
        var chain = new CascadingValueChain(parent: null, valueType: typeof(TooltipHost), name: null, value: _tooltipHost);
        chain = new CascadingValueChain(parent: chain, valueType: typeof(FocusManager), name: null, value: _focusManager);
        chain = new CascadingValueChain(parent: chain, valueType: typeof(OverlayHost), name: null, value: _overlayHost);
        chain = new CascadingValueChain(parent: chain, valueType: typeof(GuiCursorHost), name: null, value: _cursorHost);
        return chain;
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (_isDisposed) return;

        if (_inputDispatcher.Tick(deltaTime)) _surfaceDirty = true;
        if (_rebuildQueue.Drain()) _surfaceDirty = true;
        if (RecreateScaledSurfaceIfNeeded()) _surfaceDirty = true;
        if (_surfaceDirty) ExecuteRenderWalk();

        BlitMainTexture();

        for (int i = 0; i < _floatingLayers.Length; i++)
            _floatingLayers[i].Render();
    }

    private bool RecreateScaledSurfaceIfNeeded()
    {
        float scale = RuntimeEnv.GUIScale;
        double logicalWidth = _dialog.LayoutParameters.Width!.Value;
        double logicalHeight = _dialog.LayoutParameters.Height!.Value;

        if (scale == _currentScale && logicalWidth == _currentLogicalWidth && logicalHeight == _currentLogicalHeight)
            return false;

        _currentScale = scale;
        _currentLogicalWidth = logicalWidth;
        _currentLogicalHeight = logicalHeight;
        _contentSurface.EnsureSize(
            (int)Math.Round(logicalWidth * scale),
            (int)Math.Round(logicalHeight * scale));
        return true;
    }

    private void ExecuteRenderWalk()
    {
        _inputDispatcher.ClearPerFrameRegions();
        _tooltipHost.ResetFrame();

        for (int i = 0; i < _floatingLayers.Length; i++)
            _floatingLayers[i].OnFrameStart();

        // Register the dialog as the lowest-z-order background region so any click inside
        // the dialog bounds that misses all component regions hits the dialog itself. Added
        // first (index 0) so the reverse-order hit-test always prefers components over it.
        _inputDispatcher.AddInteractiveRegion(new InteractiveRegion(
            new GuiComponentBounds(0, 0, _currentLogicalWidth, _currentLogicalHeight),
            _dialog,
            onMouseDown: default,
            onMouseUp: default,
            onMouseClick: default,
            onMouseMove: default,
            onMouseEnter: default,
            onMouseLeave: default,
            virtualTarget: (IGuiNode)_dialog));

        var bounds = new GuiComponentBounds(0, 0, _currentLogicalWidth, _currentLogicalHeight);
        _contentSurface.DrawContents(bounds, _dialog.LayoutParameters.Direction, _currentScale);
        _surfaceDirty = false;

        for (int i = 0; i < _floatingLayers.Length; i++)
            _floatingLayers[i].RunWalk();

        _inputDispatcher.RefreshHoverIfNotCapturing(_clientApi.Input.MouseX, _clientApi.Input.MouseY);
    }

    private void BlitMainTexture()
    {
        var (posX, posY) = _screenProjection.GetScreenOrigin();
        _contentSurface.Blit(posX, posY);
    }

    public void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder)
    {
        if (_isDisposed) return;
        _rebuildQueue.Schedule(fragment, builder);
    }

    public void Cancel(GuiRenderFragment fragment) => _rebuildQueue.Cancel(fragment);

    public void AddInteractiveRegion(in InteractiveRegion region) => _inputDispatcher.AddInteractiveRegion(region);
    public void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container) => _inputDispatcher.AddScrollRegion(bounds, container);
    public void AddKeyboardRegion(in KeyboardRegion region) => _inputDispatcher.AddKeyboardRegion(region);

    // --- Lifecycle ---

    internal void TryOpen() => _inputInterceptor.TryOpen();
    internal void TryClose() => _inputInterceptor.TryClose();

    internal void RequestFocus() => _clientApi.Gui.RequestFocus(_inputInterceptor);

    internal void SetMouseOverCursor(string? cursor)
    {
        _dialogOverrideCursor = cursor;
        // Set immediately on the interceptor so the cursor is correct even when the
        // mouse is stationary (e.g. holding down at the start of a resize gesture).
        _inputInterceptor.MouseOverCursor = cursor;
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

    // --- Full event handlers (called directly by the interceptor) ---

    internal void OnMouseDown(MouseEvent args)
    {
        if (args.Handled) return;
        bool hit = _inputDispatcher.DispatchMouseDown(args);
        if (hit)
        {
            RequestFocus();
            args.Handled = true;
        }
    }

    internal void OnMouseUp(MouseEvent args)
    {
        if (args.Handled) return;
        _inputDispatcher.DispatchMouseUp(args);
        if (ContainsScreenPoint(args.X, args.Y) || ContainsOverlayScreenPoint(args.X, args.Y))
        {
            args.Handled = true;
        }
    }

    internal void OnMouseMove(MouseEvent args)
    {
        if (args.Handled) return;
        bool dispatched = _inputDispatcher.DispatchMouseMove(args);
        // Resize cursor (set via SetMouseOverCursor during dispatch) takes priority over
        // any component hover cursor. Fall back to the hover cursor when not resizing.
        _inputInterceptor.MouseOverCursor = _dialogOverrideCursor ?? _cursorHost.HoverCursor;
        if (dispatched || ContainsScreenPoint(args.X, args.Y) || ContainsOverlayScreenPoint(args.X, args.Y))
        {
            args.Handled = true;
        }
    }

    internal void OnMouseWheel(MouseWheelEventArgs args)
    {
        if (args.IsHandled) return;
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
        if (args.Handled) return;
        _dialog.OnKeyDown(args);
        if (args.Handled) return;
        _inputDispatcher.DispatchKeyDown(args);
    }

    internal void OnKeyUp(KeyEvent args)
    {
        if (args.Handled) return;
        _dialog.OnKeyUp(args);
        if (args.Handled) return;
        _inputDispatcher.DispatchKeyUp(args);
    }

    internal void OnKeyPress(KeyEvent args)
    {
        if (args.Handled) return;
        _dialog.OnKeyPress(args);
        if (args.Handled) return;
        _inputDispatcher.DispatchKeyPress(args);
    }

    // --- Geometry helpers (used by GuiDialog for resize hit-testing and overlay checks) ---

    public bool ContainsScreenPoint(int x, int y) => _screenProjection.Contains(x, y);
    public bool ContainsOverlayScreenPoint(int x, int y) => _overlayRenderer.ContainsScreenPoint(x, y);

    internal (int posX, int posY) GetScreenOrigin() => _screenProjection.GetScreenOrigin();

    public bool TryToLogical(int x, int y, out double logicalX, out double logicalY) =>
        _screenProjection.TryToLogical(x, y, out logicalX, out logicalY);

    internal void SetFocusedNode(IGuiNode? node) => _inputDispatcher.SetFocusedNode(node);

    internal void HideTooltip() => _tooltipHost.Hide();

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _clientApi.UnregisterDialog(_inputInterceptor);
        _inputInterceptor.Dispose();
        for (int i = 0; i < _floatingLayers.Length; i++)
            _floatingLayers[i].Dispose();
        _contentSurface.Dispose();
    }
}
