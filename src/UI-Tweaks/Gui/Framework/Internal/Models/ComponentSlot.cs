using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal sealed class ComponentSlot(
    GuiSurfaceRenderer renderer,
    IGuiNode instance,
    GuiRenderTreeBuilder childBuilder,
    GuiRenderTreeBuilder.RenderTreeFrame frame)
    : IGuiComponentSlot, IGuiRenderHandle
{
    private readonly GuiSurfaceRenderer _renderer = renderer;

    public readonly IGuiNode Instance = instance;
    public readonly GuiRenderTreeBuilder ChildBuilder = childBuilder;

    // The frame is stored here so AddComponent<T> can retrieve and reset it on subsequent
    // rebuilds rather than allocating a new instance. Safe to cast back to RenderTreeFrame<T>
    // since the slot key includes the type — the frame type always matches.
    public readonly GuiRenderTreeBuilder.RenderTreeFrame Frame = frame;

    public bool HasArrangedBounds;
    public bool IsScrollable;
    public GuiComponentBounds Bounds;
    public GuiComponentBounds ScrollClipBounds;

    public GuiCallback<GuiMouseEventArgs> OnMouseDown;
    public GuiCallback<GuiMouseEventArgs> OnMouseUp;
    public GuiCallback<GuiMouseEventArgs> OnMouseClick;
    public GuiCallback<GuiMouseEventArgs> OnMouseMove;
    public GuiCallback<GuiMouseEventArgs> OnMouseEnter;
    public GuiCallback<GuiMouseEventArgs> OnMouseLeave;

    public GuiCallback<GuiKeyEventArgs> OnKeyDown;
    public GuiCallback<GuiKeyEventArgs> OnKeyUp;
    public GuiCallback<GuiKeyEventArgs> OnKeyPress;
    public GuiCallback<bool> OnFocusChanged;

    public ICoreClientAPI ClientApi => _renderer.ClientApi;
    public IGuiComponentSlot Slot => this;
    public IGuiNode Node => Instance;
    public IReadOnlyList<IGuiComponentSlot> Children => ChildBuilder.ComponentSlots;

    public bool HasMouseHandlers =>
        OnMouseDown.HasHandler || OnMouseUp.HasHandler
        || OnMouseClick.HasHandler || OnMouseMove.HasHandler
        || OnMouseEnter.HasHandler || OnMouseLeave.HasHandler;

    public bool HasKeyboardRegionHandlers =>
        OnKeyDown.HasHandler || OnKeyUp.HasHandler || OnKeyPress.HasHandler || OnFocusChanged.HasHandler;

    public void RequestReconcile(GuiRenderFragment renderFragment)
        => _renderer.Schedule(renderFragment, ChildBuilder);

    public void RequestArrange()
        => _renderer.RequestArrange();

    public void RequestPaint()
        => _renderer.RequestPaint();

    public void RequestRender()
        => RequestPaint();

    public bool TryGetCascadingValue<T>(out T value)
        => TryGetCascadingValue(name: null, out value);

    public bool TryGetCascadingValue<T>(string? name, out T value)
    {
        var chain = ChildBuilder.InheritedCascadeChain;
        if (chain is null)
        {
            value = default!;
            return false;
        }

        return chain.TryGet(name, out value);
    }

    public void SetLayoutTransparentBounds(GuiComponentBounds bounds)
    {
        HasArrangedBounds = true;
        IsScrollable = false;
        Bounds = bounds;
    }

    public void SetComponentBounds(GuiComponentBounds bounds)
    {
        HasArrangedBounds = true;
        IsScrollable = false;
        Bounds = bounds;
        ScrollClipBounds = default;
    }

    public void SetScrollableBounds(GuiComponentBounds scrollClipBounds)
    {
        IsScrollable = true;
        ScrollClipBounds = scrollClipBounds;
    }
}
