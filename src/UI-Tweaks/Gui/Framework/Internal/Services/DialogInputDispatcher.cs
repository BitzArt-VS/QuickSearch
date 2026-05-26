using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogInputDispatcher
{
    internal delegate bool CoordinateConverter(int x, int y, out double logicalX, out double logicalY);

    // Shared
    private readonly CoordinateConverter _convertToLogical;
    private readonly TooltipHost _tooltipHost;

    // Interactive regions
    private readonly List<InteractiveRegion> _interactiveRegions = [];

    // Keyboard / focus
    private readonly List<KeyboardRegion> _keyboardRegions = [];

    // Mouse capture state
    private object? _capturedToken;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseUp;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseClick;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseMove;
    private EnumMouseButton _capturedButton;

    // Hover state
    private object? _hoveredToken;
    private GuiCallback<GuiMouseEventArgs> _hoveredOnMouseLeave;

    // Focus tracking within a mouse dispatch
    private bool _focusClaimedThisDispatch;

    internal IGuiNode? FocusedNode { get; private set; }
    private bool IsCapturing => _capturedToken is not null;

    internal DialogInputDispatcher(
        CoordinateConverter convertToLogical,
        TooltipHost tooltipHost)
    {
        _convertToLogical = convertToLogical;
        _tooltipHost = tooltipHost;
    }

    internal void ClearArrangedRegions()
    {
        _interactiveRegions.Clear();
        _keyboardRegions.Clear();
    }

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

    internal void AddInteractiveRegion(in InteractiveRegion region) => _interactiveRegions.Add(region);
    internal void AddKeyboardRegion(in KeyboardRegion region) => _keyboardRegions.Add(region);

    internal bool DispatchMouseWheel(int physicalX, int physicalY, float delta)
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

    internal bool DispatchMouseDown(MouseEvent args)
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

    internal bool DispatchMouseUp(MouseEvent args)
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

    internal bool DispatchMouseMove(MouseEvent args)
    {
        if (_capturedToken is not null)
        {
            return DispatchMouseMoveToCapture(args);
        }

        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        int regionIndex = RefreshHover(args.X, args.Y, logicalX, logicalY, args.Button);
        return regionIndex >= 0;
    }

    internal bool DispatchKeyDown(KeyEvent args) => DispatchKey(GuiKeyEventKind.Down, args);
    internal bool DispatchKeyUp(KeyEvent args) => DispatchKey(GuiKeyEventKind.Up, args);
    internal bool DispatchKeyPress(KeyEvent args) => DispatchKey(GuiKeyEventKind.Press, args);

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
