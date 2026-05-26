using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiRenderHandle
{
    public ICoreClientAPI ClientApi { get; }

    public IGuiComponentSlot Slot { get; }

    /// <summary>Schedules reconciliation of <paramref name="renderFragment"/> and cascades to arrange + paint.</summary>
    public void RequestReconcile(GuiRenderFragment renderFragment);

    /// <summary>Requests a fresh arrange pass and cascades to paint.</summary>
    public void RequestArrange();

    /// <summary>Requests a repaint of the latest arranged tree.</summary>
    public void RequestPaint();

    /// <summary>Compatibility alias for <see cref="RequestPaint"/>.</summary>
    public void RequestRender();

    /// <summary>
    /// Looks up an unnamed cascading value of type <typeparamref name="T"/> from the
    /// nearest ancestor <see cref="CascadingValue{T}"/> with no name. The returned value
    /// reflects the provider's live state at call time. Returns <c>false</c> when no
    /// matching provider exists.
    /// </summary>
    public bool TryGetCascadingValue<T>(out T value);

    /// <summary>
    /// Looks up a cascading value of type <typeparamref name="T"/> with the given
    /// <paramref name="name"/>. A <c>null</c> name matches only unnamed providers; names
    /// are compared with ordinal equality. Returns <c>false</c> when no matching provider exists.
    /// </summary>
    public bool TryGetCascadingValue<T>(string? name, out T value);
}
