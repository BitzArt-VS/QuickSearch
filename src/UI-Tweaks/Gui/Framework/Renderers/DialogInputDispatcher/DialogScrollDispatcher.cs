using System.Collections.Generic;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogScrollDispatcher
{
    private readonly record struct ScrollRegion(GuiComponentBounds Bounds, GuiContainer Container);

    private readonly List<ScrollRegion> _regions = [];

    internal void Add(GuiComponentBounds bounds, GuiContainer container) => _regions.Add(new ScrollRegion(bounds, container));
    internal void Clear() => _regions.Clear();

    internal bool Dispatch(double logicalX, double logicalY, float delta)
    {
        for (int i = _regions.Count - 1; i >= 0; i--)
        {
            var region = _regions[i];
            if (logicalX >= region.Bounds.X && logicalX < region.Bounds.X + region.Bounds.Width
             && logicalY >= region.Bounds.Y && logicalY < region.Bounds.Y + region.Bounds.Height)
            {
                region.Container.HandleMouseWheel(delta);
                return true;
            }
        }
        return false;
    }
}
