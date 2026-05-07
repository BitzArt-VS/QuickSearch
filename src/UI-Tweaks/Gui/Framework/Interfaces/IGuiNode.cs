using Cairo;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// The minimum framework contract for any participant in the render tree. A node is
/// reconciled, attached to a render handle, and given lifecycle / paint hooks — but does
/// <b>not</b> participate in the layout system on its own. The layout pass treats nodes
/// that do not also implement <see cref="IGuiComponent"/> as <i>layout-transparent</i>:
/// no <see cref="GuiComponentLayoutParameters"/> are consulted, the node's own
/// <see cref="RenderFragment"/> children flow at the parent's declaration site, and the
/// node's <see cref="Render"/> / <see cref="RenderOverlay"/> hooks receive bounds spanning
/// the union of those inner children along the parent's flow axis.
/// <para>
/// Use this base for cross-cutting wrapper components (tooltips, focus tracking, debug
/// overlays, portals) that decorate or observe their content without affecting layout.
/// For ordinary visible components that occupy space, implement <see cref="IGuiComponent"/>
/// instead — it extends this contract with the layout properties.
/// </para>
/// </summary>
public interface IGuiNode
{
    /// <summary>
    /// The node's render fragment — a persistent delegate object that describes this
    /// node's subtree. The reconciler stores and invokes this directly; its object identity
    /// is stable for the lifetime of the node instance.
    /// </summary>
    public GuiRenderFragment RenderFragment { get; }

    /// <summary>
    /// Called by the framework to attach the node to a render handle and provide access
    /// to the client API.
    /// </summary>
    public void Attach(IGuiRenderHandle renderHandle, ICoreClientAPI clientApi);

    /// <summary>
    /// Called once after the node is first mounted and its initial parameters have been set.
    /// </summary>
    public void OnInitialized() { }

    /// <summary>
    /// Called every time after the node's parameters have been set or updated.
    /// </summary>
    public void OnParametersSet() { }

    /// <summary>
    /// Called by the renderer each frame to draw this node within the given bounds.
    /// For <see cref="IGuiComponent"/> the bounds are computed by the layout pass; for
    /// pure <see cref="IGuiNode"/> implementations the bounds span the union of the
    /// node's inner children along the parent's flow axis.
    /// The Cairo context is already set up; save and restore state around any transforms.
    /// </summary>
    public void Render(Context context, GuiComponentBounds bounds) { }

    /// <summary>
    /// Called by the renderer each frame after this node's own children have been
    /// rendered, but before any later sibling slot begins drawing. Use this for overlays
    /// that must appear on top of children (borders, glows, etc.).
    /// Note: because each slot draws its full subtree (background → children → overlay) before
    /// the next sibling starts, an overlay can still be obscured by a later overlapping sibling.
    /// The Cairo context is already set up; save and restore state around any transforms.
    /// Default: no-op.
    /// </summary>
    public void RenderOverlay(Context context, GuiComponentBounds bounds) { }

    /// <summary>
    /// Called by the framework when a key is pressed while this node is focused
    /// (see <see cref="FocusManager"/>). Mark <see cref="GuiKeyEventArgs.Handled"/> to
    /// suppress framework defaults (e.g. Escape closing the dialog). Default: no-op.
    /// <para>
    /// Slot-level <c>OnKeyDown</c> handlers attached via the builder fire alongside this
    /// hook — they are independent registration sites and both run for the focused slot.
    /// </para>
    /// </summary>
    public void OnKeyDown(GuiKeyEventArgs args) { }

    /// <summary>
    /// Called by the framework when a key is released while this node is focused. Always
    /// fires on the node that received the matching <see cref="OnKeyDown"/> — focus does
    /// not change between the two halves of a single keystroke. Default: no-op.
    /// </summary>
    public void OnKeyUp(GuiKeyEventArgs args) { }

    /// <summary>
    /// Called by the framework after <see cref="OnKeyDown"/> when the OS produced a printable
    /// character for the keystroke. <see cref="GuiKeyEventArgs.KeyChar"/> carries the
    /// character. Use this for text input rather than <see cref="OnKeyDown"/>: it correctly
    /// handles modifiers, dead keys, and IME composition. Default: no-op.
    /// </summary>
    public void OnKeyPress(GuiKeyEventArgs args) { }
}
