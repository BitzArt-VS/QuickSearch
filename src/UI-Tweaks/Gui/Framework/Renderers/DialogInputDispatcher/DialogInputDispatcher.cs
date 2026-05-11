using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogInputDispatcher
{
    private readonly List<InteractiveRegion> _interactiveRegions = [];
    private readonly DialogScrollDispatcher _scrollDispatcher = new();
    private readonly DialogKeyDispatcher _keyDispatcher = new();
    private readonly DialogMouseDispatcher _mouseDispatcher;
    private readonly DialogMouseDispatcher.CoordinateConverter _convertToLogical;
    private readonly System.Action _markDirty;

    internal IGuiNode? FocusedNode => _keyDispatcher.FocusedNode;
    internal bool CaretBlinkOn => _keyDispatcher.CaretBlinkOn;

    internal DialogInputDispatcher(
        DialogMouseDispatcher.CoordinateConverter convertToLogical,
        TooltipHost tooltipHost,
        System.Action markDirty)
    {
        _convertToLogical = convertToLogical;
        _markDirty = markDirty;
        _mouseDispatcher = new DialogMouseDispatcher(
            _interactiveRegions, convertToLogical, tooltipHost, markDirty, SetFocusedNode);
    }

    internal void SetFocusedNode(IGuiNode? node)
    {
        _mouseDispatcher.OnFocusClaimed();
        if (_keyDispatcher.SetFocusedNode(node)) _markDirty();
    }

    internal bool Tick(float deltaTime) => _keyDispatcher.Tick(deltaTime);

    internal void ClearPerFrameRegions()
    {
        _interactiveRegions.Clear();
        _scrollDispatcher.Clear();
        _keyDispatcher.ClearKeyboardRegions();
    }

    internal void RefreshHoverIfNotCapturing(int physicalX, int physicalY)
    {
        if (_mouseDispatcher.IsCapturing) return;
        _convertToLogical(physicalX, physicalY, out double logicalX, out double logicalY);
        _mouseDispatcher.RefreshHover(logicalX, logicalY, physicalX, physicalY, EnumMouseButton.None, 0);
    }

    internal void AddInteractiveRegion(in InteractiveRegion region) => _interactiveRegions.Add(region);
    internal void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container) => _scrollDispatcher.Add(bounds, container);
    internal void AddKeyboardRegion(in KeyboardRegion region) => _keyDispatcher.AddKeyboardRegion(region);

    internal bool DispatchMouseWheel(int physicalX, int physicalY, float delta)
    {
        _convertToLogical(physicalX, physicalY, out double logicalX, out double logicalY);
        if (!_scrollDispatcher.Dispatch(logicalX, logicalY, delta)) return false;
        _markDirty();
        return true;
    }

    internal bool DispatchMouseDown(MouseEvent args) => _mouseDispatcher.DispatchMouseDown(args);
    internal bool DispatchMouseUp(MouseEvent args) => _mouseDispatcher.DispatchMouseUp(args);
    internal bool DispatchMouseMove(MouseEvent args) => _mouseDispatcher.DispatchMouseMove(args);

    internal bool DispatchKeyDown(KeyEvent args)
    {
        if (!_keyDispatcher.DispatchKeyDown(args)) return false;
        _markDirty();
        return true;
    }

    internal bool DispatchKeyUp(KeyEvent args)
    {
        if (!_keyDispatcher.DispatchKeyUp(args)) return false;
        _markDirty();
        return true;
    }

    internal bool DispatchKeyPress(KeyEvent args)
    {
        if (!_keyDispatcher.DispatchKeyPress(args)) return false;
        _markDirty();
        return true;
    }
}
