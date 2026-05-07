namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiRenderTreeBuilder
{
    /// <summary>
    /// Declares a node at the next stacked position.
    /// The <paramref name="key"/> uniquely identifies this slot within its parent's subtree;
    /// the builder tracks the instance across rebuilds under <c>(Type, key)</c>.
    /// Returns a fluent <see cref="IGuiComponentBuilder{T}"/> for chaining configuration.
    /// Constrained to <see cref="IGuiNode"/> — both layout-participating components
    /// (<see cref="IGuiComponent"/>) and pure layout-transparent decorators are accepted.
    /// </summary>
    internal IGuiComponentBuilder<T> AddComponent<T>(int key)
        where T : IGuiNode, new();
    /// <summary>
    /// Pushes a cascading value of type <typeparamref name="T"/> for the duration of the
    /// <paramref name="content"/> fragment. All component slots declared anywhere inside
    /// <paramref name="content"/> (at any nesting depth) can resolve the value via
    /// <see cref="IGuiRenderHandle.TryGetCascadingValue{T}"/> /
    /// <see cref="GuiComponent.GetCascadingValue{T}()"/>.
    /// Inner scopes shadow outer scopes with the same <c>(Type, Name)</c> key.
    /// This is a purely logical operation — no component is created, no slot is allocated,
    /// and the layout tree is unaffected.
    /// </summary>
    internal void PushCascadeScope<T>(T value, string? name, GuiRenderFragment content);}
