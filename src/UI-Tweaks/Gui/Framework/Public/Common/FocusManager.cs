namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Per-surface focus controller. Published at the GUI root as a cascading value so any
/// <see cref="IGuiNode"/> in the subtree can request focus and react to focus changes.
/// <para>
/// Focus is tracked by reference identity: the focused token is an <see cref="IGuiNode"/>
/// instance, the same object the framework stores as <c>slot.Instance</c>. Slot-level
/// keyboard handlers registered via the builder fire when the focused slot's instance
/// matches the current <see cref="FocusedNode"/>.
/// </para>
/// <para>
/// The framework clears focus automatically when:
/// <list type="bullet">
///   <item>A mouse-down dispatch occurs and no handler in the dispatch chain calls
///   <see cref="RequestFocus"/> — clicking outside any focusable region blurs.</item>
///   <item>The owning GUI surface is closed.</item>
/// </list>
/// Components that want focus call <see cref="RequestFocus"/> from their own
/// <c>OnMouseDown</c> handler. Components that no longer want focus (or want to forward
/// it elsewhere) call <see cref="Blur"/> or <see cref="RequestFocus"/> with a different
/// node.
/// </para>
/// </summary>
public sealed class FocusManager
{
    private readonly GuiInputRouter _inputRouter;

    internal FocusManager(GuiInputRouter inputRouter) => _inputRouter = inputRouter;

    /// <summary>The currently focused node, or <c>null</c> when nothing is focused.</summary>
    public IGuiNode? FocusedNode => _inputRouter.FocusedNode;

    /// <summary>True when <paramref name="node"/> is the currently focused node (reference equality).</summary>
    public bool IsFocused(IGuiNode? node) => node is not null && ReferenceEquals(_inputRouter.FocusedNode, node);

    /// <summary>
    /// Sets focus to <paramref name="node"/>. No-op when the node is already focused.
    /// Triggers slot-level <c>OnFocusChanged</c> handlers on the previously focused and newly
    /// focused nodes, in that order.
    /// </summary>
    public void RequestFocus(IGuiNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        _inputRouter.SetFocusedNode(node);
    }

    /// <summary>Clears focus. No-op when nothing is currently focused.</summary>
    public void Blur() => _inputRouter.SetFocusedNode(null);

}
