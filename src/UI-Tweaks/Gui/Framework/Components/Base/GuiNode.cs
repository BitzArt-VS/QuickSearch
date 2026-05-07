using System;
using Cairo;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Default base-class implementation of <see cref="IGuiNode"/>. Wires up the render handle,
/// render fragment, lifecycle hooks, cascading-value resolution, and an empty
/// <see cref="BuildRenderTree"/> hook for declaring children.
/// <para>
/// Layout-participating components inherit from <see cref="GuiComponent"/> (which extends
/// this base with <see cref="GuiComponent.LayoutParameters"/> and
/// <see cref="GuiComponent.Measure"/>). Pure layout-transparent wrappers — tooltips,
/// focus trackers, debug overlays — inherit from this base directly.
/// </para>
/// </summary>
public abstract class GuiNode : IGuiNode
{
    public GuiRenderFragment RenderFragment { get; }

    protected IGuiRenderHandle? RenderHandle { get; private set; }
    protected ICoreClientAPI? ClientApi { get; private set; }

    protected GuiNode()
    {
        RenderFragment = builder => BuildRenderTree(builder);
    }

    public void Attach(IGuiRenderHandle renderHandle, ICoreClientAPI clientApi)
    {
        RenderHandle = renderHandle;
        ClientApi = clientApi;
    }

    /// <summary>
    /// Notifies the framework that this node's state has changed and requests a re-render.
    /// </summary>
    /// <remarks>Call this method to trigger a UI update when the node's state changes outside of the
    /// normal rendering flow, such as in response to an event or asynchronous operation.</remarks>
    /// <exception cref="InvalidOperationException">Thrown if the node is not attached to a render handle.</exception>
    protected void StateHasChanged()
    {
        if (RenderHandle is null)
        {
            throw new InvalidOperationException("Cannot call StateHasChanged on a node that is not attached to a render handle.");
        }

        RenderHandle.StateHasChanged(RenderFragment);
    }

    /// <summary>
    /// Returns the cascading value of type <typeparamref name="T"/> published by the
    /// nearest ancestor <see cref="CascadingValue{T}"/> with no <c>Name</c>, or
    /// <c>default</c> when no matching provider exists.
    /// </summary>
    /// <remarks>
    /// Mirrors Blazor's <c>[CascadingParameter]</c> resolution. Typically called from
    /// <see cref="OnParametersSet"/> to snapshot the value into a field; it is also safe
    /// to call from <see cref="Render"/> / <see cref="RenderOverlay"/> for nodes that
    /// prefer to read the latest value directly each frame.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the node is not yet attached to a render handle (i.e. before
    /// <see cref="Attach"/> has run).
    /// </exception>
    protected T? GetCascadingValue<T>()
        => GetCascadingValue<T>(name: null);

    /// <inheritdoc cref="GetCascadingValue{T}()"/>
    /// <param name="name">
    /// Discriminator matching <see cref="CascadingValue{T}.Name"/>. <c>null</c> matches
    /// only providers with no name; non-null names compare ordinally.
    /// </param>
    protected T? GetCascadingValue<T>(string? name)
    {
        if (RenderHandle is null)
        {
            throw new InvalidOperationException("Cannot resolve a cascading value before the node is attached to a render handle.");
        }

        return RenderHandle.TryGetCascadingValue<T>(name, out var value) ? value : default;
    }

    /// <summary>
    /// Attempts to resolve an unnamed cascading value of type <typeparamref name="T"/>.
    /// Returns <c>false</c> when no matching provider exists, leaving <paramref name="value"/>
    /// at <c>default</c> — use this overload when "no provider" must be distinguished
    /// from "provider published <c>default(T)</c>".
    /// </summary>
    protected bool TryGetCascadingValue<T>(out T value)
        => TryGetCascadingValue(name: null, out value);

    /// <inheritdoc cref="TryGetCascadingValue{T}(out T)"/>
    /// <param name="name">Discriminator matching <see cref="CascadingValue{T}.Name"/>.</param>
    protected bool TryGetCascadingValue<T>(string? name, out T value)
    {
        if (RenderHandle is null)
        {
            throw new InvalidOperationException("Cannot resolve a cascading value before the node is attached to a render handle.");
        }

        return RenderHandle.TryGetCascadingValue(name, out value);
    }

    /// <inheritdoc/>
    public virtual void OnInitialized() { }

    /// <inheritdoc/>
    public virtual void OnParametersSet() { }

    /// <summary>
    /// Override this method to build the render tree for this node using the provided builder.
    /// </summary>
    /// <param name="builder">Builder for constructing internal render tree of this node.</param>
    protected virtual void BuildRenderTree(IGuiRenderTreeBuilder builder) { }

    /// <summary>
    /// Override this method to perform custom rendering using the provided Cairo context and bounds.
    /// </summary>
    /// <param name="context">Cairo context for drawing operations.</param>
    /// <param name="bounds">Node bounds defining the area available for rendering.</param>
    public virtual void Render(Context context, GuiComponentBounds bounds) { }

    /// <summary>
    /// Override this method to render overlays on top of this node's children.
    /// Called after this node's own children have been rendered, but before any
    /// later sibling slot begins drawing.
    /// </summary>
    /// <param name="context">Cairo context for drawing operations.</param>
    /// <param name="bounds">Node bounds defining the area available for rendering.</param>
    public virtual void RenderOverlay(Context context, GuiComponentBounds bounds) { }

    /// <inheritdoc/>
    public virtual void OnKeyDown(GuiKeyEventArgs args) { }

    /// <inheritdoc/>
    public virtual void OnKeyUp(GuiKeyEventArgs args) { }

    /// <inheritdoc/>
    public virtual void OnKeyPress(GuiKeyEventArgs args) { }
}
