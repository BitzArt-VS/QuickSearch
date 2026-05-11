using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class DialogScreenProjection
{
    private readonly ICoreClientAPI _clientApi;
    private readonly IGuiDialog _dialog;

    internal DialogScreenProjection(ICoreClientAPI clientApi, IGuiDialog dialog)
    {
        _clientApi = clientApi;
        _dialog = dialog;
    }

    internal bool TryToLogical(int x, int y, out double logicalX, out double logicalY)
    {
        var (posX, posY, physW, physH, scale) = ResolveScreenRect();
        logicalX = (x - posX) / scale;
        logicalY = (y - posY) / scale;
        return x >= posX && x < posX + physW && y >= posY && y < posY + physH;
    }

    internal bool Contains(int x, int y)
    {
        var (posX, posY, physW, physH, _) = ResolveScreenRect();
        return x >= posX && x < posX + physW && y >= posY && y < posY + physH;
    }

    internal (int posX, int posY) GetScreenOrigin()
    {
        var (posX, posY, _, _, _) = ResolveScreenRect();
        return (posX, posY);
    }

    private (int posX, int posY, double physW, double physH, float scale) ResolveScreenRect()
    {
        float scale = RuntimeEnv.GUIScale;
        double physW = _dialog.LayoutParameters.Width!.Value * scale;
        double physH = _dialog.LayoutParameters.Height!.Value * scale;
        var (posX, posY) = ComputeScreenOrigin(physW, physH, scale);
        return (posX, posY, physW, physH, scale);
    }

    private (int posX, int posY) ComputeScreenOrigin(double physW, double physH, float scale)
    {
        // Truncate (not round) to avoid a one-pixel stair-step oscillation when the
        // floating-point position crosses an x.5 boundary during drag.
        int posX = (int)((_clientApi.Render.FrameWidth - physW) / 2.0 + _dialog.OffsetX * scale);
        int posY = (int)((_clientApi.Render.FrameHeight - physH) / 2.0 + _dialog.OffsetY * scale);
        return (posX, posY);
    }
}
