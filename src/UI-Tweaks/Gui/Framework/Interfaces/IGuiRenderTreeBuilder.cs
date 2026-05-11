namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiRenderTreeBuilder
{
    /// <summary>
    /// Declares a node at the next position. The <paramref name="key"/> uniquely identifies
    /// this slot within its parent's subtree; the builder tracks the instance across rebuilds
    /// under <c>(Type, key)</c>.
    /// </summary>
    internal IGuiComponentBuilder<T> AddComponent<T>(int key)
        where T : IGuiNode, new();

    /// <summary>
    /// Pushes a cascading value for the <paramref name="content"/> fragment at any nesting
    /// depth. Purely logical — no component is created, no slot is allocated, and the layout
    /// tree is unaffected. Inner scopes shadow outer scopes with the same <c>(Type, Name)</c> key.
    /// </summary>
    internal void PushCascadeScope<T>(T value, string? name, GuiRenderFragment content);
}
