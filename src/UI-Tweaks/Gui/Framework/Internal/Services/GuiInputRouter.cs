using Vintagestory.API.Client;
using Vintagestory.API.Config;
using EnumMouseButton = Vintagestory.API.Common.EnumMouseButton;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiInputRouter
{
    internal delegate bool CoordinateConverter(int x, int y, out double logicalX, out double logicalY);

    private readonly ICoreClientAPI _clientApi;
    private readonly CoordinateConverter _convertToLogical;
    private readonly TooltipHost _tooltipHost;
    private readonly GuiCursorHost _cursorHost;
    private readonly Func<int, int, bool> _containsSurfacePoint;
    private readonly Func<int, int, bool> _containsOverlayPoint;
    private readonly Action _requestHostFocus;
    private readonly Action<string?> _setHostMouseCursor;
    private readonly Action _onRootFocus;
    private readonly Action _onRootUnFocus;
    private readonly Func<bool> _onRootEscapePressed;
    private readonly Action<KeyEvent> _onRootKeyDown;
    private readonly Action<KeyEvent> _onRootKeyUp;
    private readonly Action<KeyEvent> _onRootKeyPress;

    private readonly List<InteractiveRegion> _interactiveRegions = [];
    private readonly List<KeyboardRegion> _keyboardRegions = [];

    private object? _capturedToken;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseUp;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseClick;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseMove;
    private EnumMouseButton _capturedButton;

    private object? _hoveredToken;
    private GuiCallback<GuiMouseEventArgs> _hoveredOnMouseLeave;

    private bool _focusClaimedThisDispatch;
    private bool _isFocused;
    private string? _overrideCursor;

    internal IGuiNode? FocusedNode { get; private set; }
    private bool IsCapturing => _capturedToken is not null;

    internal GuiInputRouter(
        ICoreClientAPI clientApi,
        CoordinateConverter convertToLogical,
        TooltipHost tooltipHost,
        GuiCursorHost cursorHost,
        Func<int, int, bool> containsSurfacePoint,
        Func<int, int, bool> containsOverlayPoint,
        Action requestHostFocus,
        Action<string?> setHostMouseCursor,
        Action onRootFocus,
        Action onRootUnFocus,
        Func<bool> onRootEscapePressed,
        Action<KeyEvent> onRootKeyDown,
        Action<KeyEvent> onRootKeyUp,
        Action<KeyEvent> onRootKeyPress)
    {
        _clientApi = clientApi;
        _convertToLogical = convertToLogical;
        _tooltipHost = tooltipHost;
        _cursorHost = cursorHost;
        _containsSurfacePoint = containsSurfacePoint;
        _containsOverlayPoint = containsOverlayPoint;
        _requestHostFocus = requestHostFocus;
        _setHostMouseCursor = setHostMouseCursor;
        _onRootFocus = onRootFocus;
        _onRootUnFocus = onRootUnFocus;
        _onRootEscapePressed = onRootEscapePressed;
        _onRootKeyDown = onRootKeyDown;
        _onRootKeyUp = onRootKeyUp;
        _onRootKeyPress = onRootKeyPress;
    }

    internal void RequestFocus() => _requestHostFocus.Invoke();

    internal void SetMouseOverCursor(string? cursor)
    {
        _overrideCursor = cursor;
        // Set immediately on the host so the cursor is correct even when the
        // mouse is stationary (e.g. holding down at the start of a resize gesture).
        _setHostMouseCursor.Invoke(cursor);
    }

    internal void ClearArrangedRegions()
    {
        _interactiveRegions.Clear();
        _keyboardRegions.Clear();
    }

    internal void AddInteractiveRegion(in InteractiveRegion region) => _interactiveRegions.Add(region);
    internal void AddKeyboardRegion(in KeyboardRegion region) => _keyboardRegions.Add(region);

    internal void SetFocusedNode(IGuiNode? node)
    {
        _focusClaimedThisDispatch = true;
        if (ReferenceEquals(FocusedNode, node))
        {
            return;
        }

        var previousNode = FocusedNode;
        FocusedNode = node;
        DispatchFocusChanged(previousNode, focused: false);
        DispatchFocusChanged(node, focused: true);
    }

    internal void RefreshHoverIfNotCapturing(int physicalX, int physicalY)
    {
        if (IsCapturing)
        {
            return;
        }

        _convertToLogical(physicalX, physicalY, out double logicalX, out double logicalY);
        RefreshHover(physicalX, physicalY, logicalX, logicalY, EnumMouseButton.None);
    }

    internal void OnFocus()
    {
        _isFocused = true;
        _onRootFocus.Invoke();
    }

    internal void OnUnFocus()
    {
        _isFocused = false;
        _onRootUnFocus.Invoke();
    }

    internal bool OnEscapePressed() => _onRootEscapePressed.Invoke();

    internal void OnMouseDown(MouseEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        bool hit = DispatchMouseDown(args);
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

        DispatchMouseUp(args);
        if (_containsSurfacePoint(args.X, args.Y) || _containsOverlayPoint(args.X, args.Y))
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

        bool dispatched = DispatchMouseMove(args);
        // Override cursor (e.g. dialog resize) takes priority over any component hover cursor.
        _setHostMouseCursor.Invoke(_overrideCursor ?? _cursorHost.HoverCursor);
        if (dispatched || _containsSurfacePoint(args.X, args.Y) || _containsOverlayPoint(args.X, args.Y))
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

        int mouseX = _clientApi.Input.MouseX;
        int mouseY = _clientApi.Input.MouseY;

        if (_isFocused && (_containsSurfacePoint(mouseX, mouseY) || _containsOverlayPoint(mouseX, mouseY)))
        {
            DispatchMouseWheel(mouseX, mouseY, args.deltaPrecise);
            args.SetHandled(true);
        }
    }

    internal void OnKeyDown(KeyEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _onRootKeyDown.Invoke(args);
        if (args.Handled)
        {
            return;
        }

        DispatchKeyDown(args);
    }

    internal void OnKeyUp(KeyEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _onRootKeyUp.Invoke(args);
        if (args.Handled)
        {
            return;
        }

        DispatchKeyUp(args);
    }

    internal void OnKeyPress(KeyEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _onRootKeyPress.Invoke(args);
        if (args.Handled)
        {
            return;
        }

        DispatchKeyPress(args);
    }

    private bool DispatchMouseWheel(int physicalX, int physicalY, float delta)
    {
        _convertToLogical(physicalX, physicalY, out double logicalX, out double logicalY);

        for (int i = _interactiveRegions.Count - 1; i >= 0; i--)
        {
            var region = _interactiveRegions[i];
            if (!region.OnMouseWheel.HasHandler)
            {
                continue;
            }

            if (!region.Contains(logicalX, logicalY))
            {
                continue;
            }

            region.OnMouseWheel.Invoke(MakeMouseArgs(physicalX, physicalY, logicalX, logicalY, EnumMouseButton.None) with { WheelDelta = delta });
            return true;
        }
        return false;
    }

    private bool DispatchMouseDown(MouseEvent args)
    {
        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        _tooltipHost.Hide();

        int regionIndex = HitTest(logicalX, logicalY);
        if (regionIndex < 0)
        {
            return false;
        }

        var region = _interactiveRegions[regionIndex];
        var mouseArgs = MakeMouseArgs(args.X, args.Y, logicalX, logicalY, args.Button);

        CaptureRegion(region, args.Button);

        _focusClaimedThisDispatch = false;
        region.OnMouseDown.Invoke(mouseArgs);
        if (!_focusClaimedThisDispatch)
        {
            SetFocusedNode(null);
        }

        return true;
    }

    private bool DispatchMouseUp(MouseEvent args)
    {
        if (_capturedToken is null)
        {
            return false;
        }

        if (args.Button != _capturedButton)
        {
            return false;
        }

        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        bool insideCapture = IsCursorInsideCapturedRegion(logicalX, logicalY);
        var mouseArgs = MakeMouseArgs(args.X, args.Y, logicalX, logicalY, args.Button);

        var onMouseUp = _capturedOnMouseUp;
        var onMouseClick = _capturedOnMouseClick;
        _capturedToken = null;
        _capturedOnMouseUp = default;
        _capturedOnMouseClick = default;
        _capturedOnMouseMove = default;

        onMouseUp.Invoke(mouseArgs);
        if (insideCapture)
        {
            onMouseClick.Invoke(mouseArgs);
        }

        return true;
    }

    private bool DispatchMouseMove(MouseEvent args)
    {
        if (_capturedToken is not null)
        {
            return DispatchMouseMoveToCapture(args);
        }

        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        int regionIndex = RefreshHover(args.X, args.Y, logicalX, logicalY, args.Button);
        return regionIndex >= 0;
    }

    private bool DispatchKeyDown(KeyEvent args) => DispatchKey(GuiKeyEventKind.Down, args);
    private bool DispatchKeyUp(KeyEvent args) => DispatchKey(GuiKeyEventKind.Up, args);
    private bool DispatchKeyPress(KeyEvent args) => DispatchKey(GuiKeyEventKind.Press, args);

    private int RefreshHover(int physicalX, int physicalY, double logicalX, double logicalY, EnumMouseButton button)
    {
        int regionIndex = HitTest(logicalX, logicalY);
        object? newToken = regionIndex >= 0 ? _interactiveRegions[regionIndex].Token : null;

        var mouseArgs = MakeMouseArgs(physicalX, physicalY, logicalX, logicalY, button);

        if (newToken != _hoveredToken)
        {
            LeaveHoveredRegion(mouseArgs);
            EnterHoveredRegion(newToken, mouseArgs);
        }
        else
        {
            DispatchMouseMoveToHoveredRegion(newToken, mouseArgs);
        }

        _tooltipHost.UpdateHover(logicalX, logicalY);
        return regionIndex;
    }

    private bool DispatchMouseMoveToCapture(MouseEvent args)
    {
        if (!_capturedOnMouseMove.HasHandler)
        {
            return true;
        }

        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        var mouseArgs = MakeMouseArgs(args.X, args.Y, logicalX, logicalY, args.Button);
        _capturedOnMouseMove.Invoke(mouseArgs);
        return true;
    }

    private static GuiMouseEventArgs MakeMouseArgs(int physicalX, int physicalY, double logicalX, double logicalY, EnumMouseButton button)
        => new(new(logicalX, logicalY), new(physicalX / RuntimeEnv.GUIScale, physicalY / RuntimeEnv.GUIScale), button);

    private int HitTest(double logicalX, double logicalY)
    {
        for (int i = _interactiveRegions.Count - 1; i >= 0; i--)
        {
            if (!_interactiveRegions[i].HasClickHandlers)
            {
                continue;
            }

            if (_interactiveRegions[i].Contains(logicalX, logicalY))
            {
                return i;
            }
        }
        return -1;
    }

    private void CaptureRegion(InteractiveRegion region, EnumMouseButton button)
    {
        _capturedToken = region.Token;
        _capturedOnMouseUp = region.OnMouseUp;
        _capturedOnMouseClick = region.OnMouseClick;
        _capturedOnMouseMove = region.OnMouseMove;
        _capturedButton = button;
    }

    private bool IsCursorInsideCapturedRegion(double logicalX, double logicalY)
    {
        for (int i = _interactiveRegions.Count - 1; i >= 0; i--)
        {
            if (_interactiveRegions[i].Token != _capturedToken)
            {
                continue;
            }

            return _interactiveRegions[i].Contains(logicalX, logicalY);
        }
        return false;
    }

    private void LeaveHoveredRegion(GuiMouseEventArgs mouseArgs)
    {
        if (_hoveredToken is null)
        {
            return;
        }

        if (_hoveredOnMouseLeave.HasHandler)
        {
            _hoveredOnMouseLeave.Invoke(mouseArgs);
        }
        _hoveredToken = null;
        _hoveredOnMouseLeave = default;
    }

    private void EnterHoveredRegion(object? newToken, GuiMouseEventArgs mouseArgs)
    {
        if (newToken is null)
        {
            return;
        }

        _hoveredToken = newToken;

        for (int i = _interactiveRegions.Count - 1; i >= 0; i--)
        {
            if (_interactiveRegions[i].Token != newToken)
            {
                continue;
            }

            _hoveredOnMouseLeave = _interactiveRegions[i].OnMouseLeave;
            if (_interactiveRegions[i].OnMouseEnter.HasHandler)
            {
                _interactiveRegions[i].OnMouseEnter.Invoke(mouseArgs);
            }
            break;
        }
    }

    private void DispatchMouseMoveToHoveredRegion(object? token, GuiMouseEventArgs mouseArgs)
    {
        if (token is null)
        {
            return;
        }

        for (int i = _interactiveRegions.Count - 1; i >= 0; i--)
        {
            if (_interactiveRegions[i].Token != token)
            {
                continue;
            }

            _interactiveRegions[i].OnMouseMove.Invoke(mouseArgs);
            return;
        }
    }

    private bool DispatchKey(GuiKeyEventKind kind, KeyEvent args)
    {
        if (FocusedNode is null)
        {
            return false;
        }

        var keyArgs = new GuiKeyEventArgs(args);

        for (int i = 0; i < _keyboardRegions.Count; i++)
        {
            if (!ReferenceEquals(_keyboardRegions[i].Token, FocusedNode))
            {
                continue;
            }

            _keyboardRegions[i].Dispatch(kind, keyArgs);
            break;
        }

        return true;
    }

    private void DispatchFocusChanged(IGuiNode? node, bool focused)
    {
        if (node is null)
        {
            return;
        }

        for (int i = 0; i < _keyboardRegions.Count; i++)
        {
            if (!ReferenceEquals(_keyboardRegions[i].Token, node))
            {
                continue;
            }

            _keyboardRegions[i].OnFocusChanged.Invoke(focused);
            return;
        }
    }
}
