using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogMouseDispatcher
{
    internal delegate bool CoordinateConverter(int x, int y, out double logicalX, out double logicalY);

    private readonly List<InteractiveRegion> _regions;
    private readonly CoordinateConverter _convertToLogical;
    private readonly TooltipHost _tooltipHost;
    private readonly Action _markDirty;
    private readonly Action<IGuiNode?> _setFocusedNode;

    private object? _capturedToken;
    private IGuiNode? _capturedVirtualTarget;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseUp;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseClick;
    private GuiCallback<GuiMouseEventArgs> _capturedOnMouseMove;
    private EnumMouseButton _capturedButton;

    private object? _hoveredToken;
    private IGuiNode? _hoveredVirtualTarget;
    private GuiCallback<GuiMouseEventArgs> _hoveredOnMouseLeave;

    private bool _focusClaimedThisDispatch;

    internal bool IsCapturing => _capturedToken is not null;

    internal DialogMouseDispatcher(
        List<InteractiveRegion> regions,
        CoordinateConverter convertToLogical,
        TooltipHost tooltipHost,
        Action markDirty,
        Action<IGuiNode?> setFocusedNode)
    {
        _regions = regions;
        _convertToLogical = convertToLogical;
        _tooltipHost = tooltipHost;
        _markDirty = markDirty;
        _setFocusedNode = setFocusedNode;
    }

    internal void OnFocusClaimed() => _focusClaimedThisDispatch = true;

    internal bool DispatchMouseDown(MouseEvent args)
    {
        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        _tooltipHost.Hide();

        int regionIndex = HitTest(logicalX, logicalY);
        if (regionIndex < 0) return false;

        var region = _regions[regionIndex];
        var mouseArgs = new GuiMouseEventArgs(logicalX, logicalY, args.X, args.Y, args.Button, args.Modifiers);

        CaptureRegion(region, args.Button);
        _markDirty();

        _focusClaimedThisDispatch = false;
        region.OnMouseDown.Invoke(mouseArgs);
        region.VirtualTarget?.OnMouseDown(mouseArgs);
        if (!_focusClaimedThisDispatch) _setFocusedNode(null);

        return true;
    }

    internal bool DispatchMouseUp(MouseEvent args)
    {
        if (_capturedToken is null) return false;
        if (args.Button != _capturedButton) return false;

        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        bool insideCapture = IsCursorInsideCapturedRegion(logicalX, logicalY);
        var mouseArgs = new GuiMouseEventArgs(logicalX, logicalY, args.X, args.Y, args.Button, args.Modifiers);

        var onMouseUp = _capturedOnMouseUp;
        var onMouseClick = _capturedOnMouseClick;
        var capturedVirtualTarget = _capturedVirtualTarget;
        _capturedToken = null;
        _capturedVirtualTarget = null;
        _capturedOnMouseUp = default;
        _capturedOnMouseClick = default;
        _capturedOnMouseMove = default;

        onMouseUp.Invoke(mouseArgs);
        capturedVirtualTarget?.OnMouseUp(mouseArgs);
        if (insideCapture)
        {
            onMouseClick.Invoke(mouseArgs);
            capturedVirtualTarget?.OnMouseClick(mouseArgs);
        }

        _markDirty();
        return true;
    }

    internal bool DispatchMouseMove(MouseEvent args)
    {
        if (_capturedToken is not null)
            return DispatchMouseMoveToCapture(args);

        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        int regionIndex = RefreshHover(logicalX, logicalY, args.X, args.Y, args.Button, args.Modifiers);
        return regionIndex >= 0;
    }

    internal int RefreshHover(double logicalX, double logicalY, int physicalX, int physicalY, EnumMouseButton button, int modifiers)
    {
        int regionIndex = HitTest(logicalX, logicalY);
        object? newToken = regionIndex >= 0 ? _regions[regionIndex].Token : null;
        IGuiNode? newVirtualTarget = regionIndex >= 0 ? _regions[regionIndex].VirtualTarget : null;

        var mouseArgs = new GuiMouseEventArgs(logicalX, logicalY, physicalX, physicalY, button, modifiers);

        if (newToken != _hoveredToken)
        {
            LeaveHoveredRegion(mouseArgs);
            EnterHoveredRegion(newToken, newVirtualTarget, mouseArgs);
        }
        else
        {
            _hoveredVirtualTarget?.OnMouseMove(mouseArgs);
        }

        _tooltipHost.UpdateHover(logicalX, logicalY);
        return regionIndex;
    }

    private bool DispatchMouseMoveToCapture(MouseEvent args)
    {
        if (!_capturedOnMouseMove.HasHandler && _capturedVirtualTarget is null) return true;
        _convertToLogical(args.X, args.Y, out double logicalX, out double logicalY);
        var mouseArgs = new GuiMouseEventArgs(logicalX, logicalY, args.X, args.Y, args.Button, args.Modifiers);
        _capturedOnMouseMove.Invoke(mouseArgs);
        _capturedVirtualTarget?.OnMouseMove(mouseArgs);
        _markDirty();
        return true;
    }

    private int HitTest(double logicalX, double logicalY)
    {
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            if (_regions[i].Contains(logicalX, logicalY)) return i;
        }
        return -1;
    }

    private void CaptureRegion(InteractiveRegion region, EnumMouseButton button)
    {
        _capturedToken = region.Token;
        _capturedVirtualTarget = region.VirtualTarget;
        _capturedOnMouseUp = region.OnMouseUp;
        _capturedOnMouseClick = region.OnMouseClick;
        _capturedOnMouseMove = region.OnMouseMove;
        _capturedButton = button;
    }

    private bool IsCursorInsideCapturedRegion(double logicalX, double logicalY)
    {
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            if (_regions[i].Token != _capturedToken) continue;
            return _regions[i].Contains(logicalX, logicalY);
        }
        return false;
    }

    private void LeaveHoveredRegion(GuiMouseEventArgs mouseArgs)
    {
        if (_hoveredToken is null) return;
        if (_hoveredOnMouseLeave.HasHandler)
        {
            _hoveredOnMouseLeave.Invoke(mouseArgs);
            _markDirty();
        }
        _hoveredVirtualTarget?.OnMouseLeave(mouseArgs);
        _hoveredToken = null;
        _hoveredVirtualTarget = null;
        _hoveredOnMouseLeave = default;
    }

    private void EnterHoveredRegion(object? newToken, IGuiNode? newVirtualTarget, GuiMouseEventArgs mouseArgs)
    {
        if (newToken is null) return;
        _hoveredToken = newToken;
        _hoveredVirtualTarget = newVirtualTarget;

        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            if (_regions[i].Token != newToken) continue;
            _hoveredOnMouseLeave = _regions[i].OnMouseLeave;
            if (_regions[i].OnMouseEnter.HasHandler)
            {
                _regions[i].OnMouseEnter.Invoke(mouseArgs);
                _markDirty();
            }
            break;
        }

        newVirtualTarget?.OnMouseEnter(mouseArgs);
    }
}
