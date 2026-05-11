using Cairo;
using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class GuiContentSurface : IDisposable
{
    private readonly ICoreClientAPI _clientApi;
    private ImageSurface? _surface;
    private Context? _context;
    private LoadedTexture _texture;
    private int _physicalWidth;
    private int _physicalHeight;

    internal GuiRenderTreeBuilder Builder { get; }
    internal IGuiRenderHandle Handle { get; }

    internal int PhysicalWidth => _physicalWidth;
    internal int PhysicalHeight => _physicalHeight;

    internal GuiContentSurface(ICoreClientAPI clientApi, IGuiComponentTreeRenderer renderer)
    {
        _clientApi = clientApi;
        _texture = new LoadedTexture(clientApi);
        Builder = new GuiRenderTreeBuilder(renderer);
        Handle = new RenderHandle(renderer, Builder, parentBuilder: null);
    }

    internal bool EnsureSize(int physW, int physH)
    {
        if (_surface is not null && physW == _physicalWidth && physH == _physicalHeight) return false;

        _context?.Dispose();
        _surface?.Dispose();
        _surface = new ImageSurface(Format.Argb32, physW, physH);
        _context = new Context(_surface);
        _physicalWidth = physW;
        _physicalHeight = physH;
        return true;
    }

    internal void DrawContents(GuiComponentBounds bounds, GuiDirection direction, float scale)
    {
        _context!.IdentityMatrix();
        _context.Operator = Operator.Source;
        _context.SetSourceRGBA(0, 0, 0, 0);
        _context.Paint();
        _context.Operator = Operator.Over;
        _context.Scale(scale, scale);
        Builder.Render(_context, bounds, direction);
        _surface!.Flush();
        _clientApi.Gui.LoadOrUpdateCairoTexture(_surface, true, ref _texture);
    }

    internal void Blit(double posX, double posY)
    {
        if (_texture.TextureId == 0) return;
        _clientApi.Render.Render2DTexturePremultipliedAlpha(
            _texture.TextureId, posX, posY, _physicalWidth, _physicalHeight);
    }

    public void Dispose()
    {
        Builder.Dispose();
        _texture.Dispose();
        _context?.Dispose();
        _surface?.Dispose();
    }
}
