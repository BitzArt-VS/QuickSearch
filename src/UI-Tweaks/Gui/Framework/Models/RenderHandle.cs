using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Per-component render handle. Schedules a scoped rebuild of a specific subtree
/// rather than always rebuilding from the root, and exposes ambient services (cascading
/// values, client API) accessible from inside <see cref="IGuiComponent"/> hooks.
/// </summary>
/// <remarks>
/// <paramref name="parentBuilder"/> is the builder in which the owning component was
/// declared as a slot — the source of cascading values that this component consumes.
/// It is <c>null</c> only for the root dialog component (no parent declared it). The
/// reference is stable for the lifetime of the component instance, but the chain it
/// exposes (<c>parentBuilder.CascadeChain</c>) is refreshed by ancestor reconciles, so
/// lookups always see the latest chain without rewiring this handle.
/// </remarks>
internal sealed class RenderHandle(
    IGuiComponentTreeRenderer renderer,
    GuiRenderTreeBuilder childBuilder,
    GuiRenderTreeBuilder? parentBuilder) : IGuiRenderHandle
{
    private readonly IGuiComponentTreeRenderer _renderer = renderer;
    private readonly GuiRenderTreeBuilder _childBuilder = childBuilder;
    private readonly GuiRenderTreeBuilder? _parentBuilder = parentBuilder;

    public ICoreClientAPI ClientApi => _renderer.ClientApi;

    public void StateHasChanged(GuiRenderFragment renderFragment)
        => _renderer.Schedule(renderFragment, _childBuilder);

    public bool TryGetCascadingValue<T>(out T value)
        => TryGetCascadingValue(name: null, out value);

    public bool TryGetCascadingValue<T>(string? name, out T value)
    {
        // Read the parent builder's chain on every call — it is the live reference the
        // grand-parent reconcile updates. _parentBuilder is null only for the root dialog
        // (no ancestor providers possible), so the short-circuit returns "not found".
        var chain = _parentBuilder?.CascadeChain;
        if (chain is null)
        {
            value = default!;
            return false;
        }
        return chain.TryGet(name, out value);
    }
}
