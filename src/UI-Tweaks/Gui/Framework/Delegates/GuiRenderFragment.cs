namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A render-tree-building delegate — declares a subtree of nodes via <paramref name="builder"/>.
/// The fragment instance is stable across rebuilds: the framework invokes the same delegate
/// each pass, and the builder reconciles its declarations against the live slot map.
/// </summary>
public delegate void GuiRenderFragment(IGuiRenderTreeBuilder builder);

/// <summary>
/// Parameterised counterpart to <see cref="GuiRenderFragment"/> — declares a subtree
/// driven by a single <typeparamref name="T"/> argument. Used by templated components
/// (e.g. <see cref="GuiDropdown{T}"/>) to let callers describe how a single item renders.
/// <para>
/// <b>Shape choice.</b> Blazor models its templated render fragments as
/// <c>delegate RenderFragment RenderFragment&lt;T&gt;(T value)</c> — a curried form that
/// returns a fragment factory. We use the flatter
/// <c>delegate void(IGuiRenderTreeBuilder, T)</c> instead: invoking the template requires
/// just a single virtual call with no intermediate fragment object, so a list of
/// <i>N</i> items pays zero allocations per item per rebuild. From the call site DX is
/// identical — the lambda body looks the same as the curried form.
/// </para>
/// </summary>
public delegate void GuiRenderFragment<in T>(IGuiRenderTreeBuilder builder, T item);
