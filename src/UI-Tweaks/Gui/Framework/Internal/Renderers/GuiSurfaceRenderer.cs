using Cairo;
using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

internal abstract class GuiSurfaceRenderer : IDisposable
{
    protected readonly ICoreClientAPI _clientApi;
    private ImageSurface? _surface;
    private Context? _context;
    private LoadedTexture _texture;
    private int _physicalWidth;
    private int _physicalHeight;
    protected float _currentScale;
    protected bool _reconcileRequested;
    protected bool _arrangeRequested;
    protected bool _paintRequested;

    public ICoreClientAPI ClientApi => _clientApi;

    protected int PhysicalWidth => _physicalWidth;
    protected int PhysicalHeight => _physicalHeight;
    protected GuiRenderTreeBuilder Builder { get; }
    protected bool HasPendingSurfaceUpdate => _reconcileRequested || _arrangeRequested || _paintRequested;

    protected GuiSurfaceRenderer(ICoreClientAPI clientApi)
    {
        _clientApi = clientApi;
        _texture = new LoadedTexture(clientApi);
        _currentScale = RuntimeEnv.GUIScale;
        Builder = new GuiRenderTreeBuilder(this);
    }

    protected void RequestReconcile()
    {
        _reconcileRequested = true;
        RequestArrange();
    }

    public void RequestArrange()
    {
        _arrangeRequested = true;
        RequestPaint();
    }

    public void RequestPaint() => _paintRequested = true;

    public void RequestRender() => RequestPaint();

    public virtual void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder) => RequestReconcile();
    public virtual void Cancel(GuiRenderFragment fragment) { }

    public virtual void AddInteractiveRegion(in InteractiveRegion region) { }
    public virtual void AddResizeRegion(in ResizeRegion region) { }
    public virtual void AddKeyboardRegion(in KeyboardRegion region) { }

    public virtual bool ContainsScreenPoint(int x, int y) => false;

    internal void SetCascadeChain(CascadingValueChain? chain) => Builder.CascadeChain = chain;

    protected void EnsureSurfaceSize(int physW, int physH)
    {
        if (_surface is not null && physW == _physicalWidth && physH == _physicalHeight)
        {
            return;
        }

        _context?.Dispose();
        _surface?.Dispose();
        _surface = new ImageSurface(Format.Argb32, physW, physH);
        _context = new Context(_surface);
        _physicalWidth = physW;
        _physicalHeight = physH;
    }

    protected void DrawSurfaceContents(GuiComponentBounds bounds, GuiDirection direction, float scale, bool arrange)
    {
        DrawSurfaceContents(bounds, scale, arrange, context =>
        {
            if (arrange)
            {
                Builder.Render(context, bounds, direction);
            }
            else
            {
                Builder.Paint(context);
            }
        });
    }

    private void DrawSurfaceContents(GuiComponentBounds bounds, float scale, bool arrange, Action<Context> draw)
    {
        _context!.IdentityMatrix();
        _context.Operator = Operator.Source;
        _context.SetSourceRGBA(0, 0, 0, 0);
        _context.Paint();
        _context.Operator = Operator.Over;
        _context.Scale(scale, scale);
        draw(_context);
        _surface!.Flush();
        _clientApi.Gui.LoadOrUpdateCairoTexture(_surface, true, ref _texture);
        _currentScale = scale;
        ClearInvalidationRequests(arrange);
    }

    protected void ClearInvalidationRequests(bool arranged)
    {
        if (arranged)
        {
            _reconcileRequested = false;
            _arrangeRequested = false;
        }
        _paintRequested = false;
    }

    protected void BlitAt(double posX, double posY)
    {
        if (_texture.TextureId == 0)
        {
            return;
        }

        _clientApi.Render.Render2DTexturePremultipliedAlpha(
            _texture.TextureId, posX, posY, _physicalWidth, _physicalHeight);
    }

    public virtual void Dispose()
    {
        Builder.Dispose();
        _texture.Dispose();
        _context?.Dispose();
        _surface?.Dispose();
    }
}
