using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Provides a component with the ability to request a re-render of the dialog and
/// access to the host environment (assets, render API, etc.).
/// </summary>
public interface IGuiRenderHandle
{
    /// <summary>
    /// The client API for the dialog this component belongs to. Use it to load assets,
    /// query input state, or call into the vanilla render API from within draw hooks.
    /// </summary>
    public ICoreClientAPI ClientApi { get; }

    /// <summary>
    /// Schedules a scoped rebuild for the subtree owned by the component that holds this handle.
    /// </summary>
    public void StateHasChanged(GuiRenderFragment renderFragment);

    /// <summary>
    /// Looks up an unnamed cascading value of type <typeparamref name="T"/> from the nearest
    /// ancestor <see cref="CascadingValue{T}"/> with no <see cref="CascadingValue{T}.Name"/>.
    /// Inner providers shadow outer providers with the same <c>(Type, Name)</c> key.
    /// Returns <c>false</c> when no matching provider exists in the ancestry.
    /// </summary>
    /// <remarks>
    /// The lookup reads live state — the returned value reflects the provider's current
    /// <c>Value</c> at call time. Typically called from
    /// <see cref="IGuiComponent.OnParametersSet"/> to snapshot the current value into
    /// component state.
    /// </remarks>
    public bool TryGetCascadingValue<T>(out T value);

    /// <summary>
    /// Looks up a cascading value of type <typeparamref name="T"/> with the given
    /// <paramref name="name"/> from the nearest matching ancestor <see cref="CascadingValue{T}"/>.
    /// A <c>null</c> <paramref name="name"/> matches only providers with no name set
    /// (and is equivalent to the parameterless overload). Names are compared with ordinal
    /// equality; <c>null</c> never matches a non-null provider name (Blazor parity).
    /// </summary>
    public bool TryGetCascadingValue<T>(string? name, out T value);
}
