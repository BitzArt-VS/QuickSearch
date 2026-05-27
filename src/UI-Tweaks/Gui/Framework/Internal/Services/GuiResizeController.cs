namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiResizeController
{
    private const double EdgeThickness = 6.0;

    private readonly Func<IGuiNode?> _getRootNode;
    private readonly Action<string?> _setMouseOverCursor;

    private bool _isResizing;
    private bool _useScreenBounds;
    private ResizeRegion _activeRegion;
    private GuiResizeEdge _activeEdge;
    private GuiComponentBounds _startBounds;
    private GuiPoint _startPointer;

    internal bool IsResizing => _isResizing;

    internal GuiResizeController(Func<IGuiNode?> getRootNode, Action<string?> setMouseOverCursor)
    {
        _getRootNode = getRootNode;
        _setMouseOverCursor = setMouseOverCursor;
    }

    internal bool TryBegin(IReadOnlyList<ResizeRegion> regions, GuiMouseEventArgs args)
    {
        if (_isResizing)
        {
            return false;
        }

        if (!TryHit(regions, args.Position.X, args.Position.Y, out var region, out var edge))
        {
            return false;
        }

        _isResizing = true;
        _activeRegion = region;
        _activeEdge = edge;
        _useScreenBounds = ReferenceEquals(region.Token, _getRootNode());
        _startBounds = _useScreenBounds ? ToScreenBounds(region.Bounds, args) : region.Bounds;
        _startPointer = _useScreenBounds ? args.AbsolutePosition : args.Position;

        _setMouseOverCursor.Invoke(CursorForEdge(edge));
        return true;
    }

    internal void Update(GuiMouseEventArgs args)
    {
        if (!_isResizing)
        {
            return;
        }

        var pointer = _useScreenBounds ? args.AbsolutePosition : args.Position;
        var delta = new GuiPoint(
            pointer.X - _startPointer.X,
            pointer.Y - _startPointer.Y);

        _activeRegion.Target.Resize(CreateRequestedBounds(delta));
    }

    internal void End()
    {
        if (!_isResizing)
        {
            return;
        }

        _isResizing = false;
        _activeRegion = default;
        _activeEdge = GuiResizeEdge.None;
        _useScreenBounds = false;
        _setMouseOverCursor.Invoke(null);
    }

    internal bool UpdateHover(IReadOnlyList<ResizeRegion> regions, GuiMouseEventArgs args)
    {
        if (_isResizing)
        {
            return true;
        }

        if (!TryHit(regions, args.Position.X, args.Position.Y, out _, out var edge))
        {
            _setMouseOverCursor.Invoke(null);
            return false;
        }

        _setMouseOverCursor.Invoke(CursorForEdge(edge));
        return true;
    }

    private bool TryHit(
        IReadOnlyList<ResizeRegion> regions,
        double x,
        double y,
        out ResizeRegion region,
        out GuiResizeEdge edge)
    {
        for (int i = regions.Count - 1; i >= 0; i--)
        {
            region = regions[i];
            edge = HitTest(region, x, y);
            if (edge != GuiResizeEdge.None)
            {
                return true;
            }
        }

        region = default;
        edge = GuiResizeEdge.None;
        return false;
    }

    private static GuiResizeEdge HitTest(ResizeRegion region, double x, double y)
    {
        if (!region.Contains(x, y))
        {
            return GuiResizeEdge.None;
        }

        var supported = region.Target.SupportedResizeEdges;
        if (supported == GuiResizeEdge.None)
        {
            return GuiResizeEdge.None;
        }

        double localX = x - region.Bounds.X;
        double localY = y - region.Bounds.Y;
        double width = region.Bounds.Width;
        double height = region.Bounds.Height;

        var edge = GuiResizeEdge.None;

        if ((supported & GuiResizeEdge.Left) != 0 && localX < EdgeThickness)
        {
            edge |= GuiResizeEdge.Left;
        }
        else if ((supported & GuiResizeEdge.Right) != 0 && localX > width - EdgeThickness)
        {
            edge |= GuiResizeEdge.Right;
        }

        if ((supported & GuiResizeEdge.Top) != 0 && localY < EdgeThickness)
        {
            edge |= GuiResizeEdge.Top;
        }
        else if ((supported & GuiResizeEdge.Bottom) != 0 && localY > height - EdgeThickness)
        {
            edge |= GuiResizeEdge.Bottom;
        }

        return edge;
    }

    private GuiComponentBounds CreateRequestedBounds(GuiPoint delta)
    {
        double left = _startBounds.X;
        double top = _startBounds.Y;
        double right = _startBounds.Right;
        double bottom = _startBounds.Bottom;

        if ((_activeEdge & GuiResizeEdge.Left) != 0)
        {
            left += delta.X;
        }
        else if ((_activeEdge & GuiResizeEdge.Right) != 0)
        {
            right += delta.X;
        }

        if ((_activeEdge & GuiResizeEdge.Top) != 0)
        {
            top += delta.Y;
        }
        else if ((_activeEdge & GuiResizeEdge.Bottom) != 0)
        {
            bottom += delta.Y;
        }

        return new GuiComponentBounds(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Abs(right - left),
            Math.Abs(bottom - top));
    }

    private static GuiComponentBounds ToScreenBounds(GuiComponentBounds surfaceBounds, GuiMouseEventArgs args)
    {
        double surfaceScreenX = args.AbsolutePosition.X - args.Position.X;
        double surfaceScreenY = args.AbsolutePosition.Y - args.Position.Y;

        return new GuiComponentBounds(
            surfaceScreenX + surfaceBounds.X,
            surfaceScreenY + surfaceBounds.Y,
            surfaceBounds.Width,
            surfaceBounds.Height);
    }

    private static string? CursorForEdge(GuiResizeEdge edge)
    {
        if (edge is GuiResizeEdge.Left or GuiResizeEdge.Right)
        {
            return GuiResizeCursors.Horizontal;
        }

        if (edge is GuiResizeEdge.Top or GuiResizeEdge.Bottom)
        {
            return GuiResizeCursors.Vertical;
        }

        if (edge == (GuiResizeEdge.Left | GuiResizeEdge.Top)
            || edge == (GuiResizeEdge.Right | GuiResizeEdge.Bottom))
        {
            return GuiResizeCursors.DiagonalNwSe;
        }

        if (edge == (GuiResizeEdge.Right | GuiResizeEdge.Top)
            || edge == (GuiResizeEdge.Left | GuiResizeEdge.Bottom))
        {
            return GuiResizeCursors.DiagonalNeSw;
        }

        return null;
    }
}
