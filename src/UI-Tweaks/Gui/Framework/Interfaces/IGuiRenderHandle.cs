using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiRenderHandle
{
    public ICoreClientAPI ClientApi { get; }

    public void StateHasChanged(GuiRenderFragment renderFragment);

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
