using Cairo;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiNode
{
    public GuiRenderFragment RenderFragment { get; }

    public void Attach(IGuiRenderHandle renderHandle, ICoreClientAPI clientApi);

    public void OnInitialized() { }

    public void OnParametersSet() { }

    /// <summary>
    /// Called each frame to draw this node within the given bounds. Save and restore
    /// Cairo context state around any transforms.
    /// </summary>
    public void Render(Context context, GuiComponentBounds bounds) { }

    /// <summary>
    /// Called after all children have rendered, before the next sibling slot draws.
    /// An overlay can still be obscured by a later sibling that overlaps the same area.
    /// Save and restore Cairo context state around any transforms.
    /// </summary>
    public void RenderOverlay(Context context, GuiComponentBounds bounds) { }

    /// <summary>
    /// Called when a key is pressed while this node is focused. Mark
    /// <see cref="GuiKeyEventArgs.Handled"/> to suppress framework defaults (e.g. Escape
    /// closing the dialog). Slot-level <c>OnKeyDown</c> handlers attached via the builder
    /// also fire and are independent of this hook.
    /// </summary>
    public void OnKeyDown(GuiKeyEventArgs args) { }

    /// <summary>
    /// Called when a key is released. Always fires on the node that received the matching
    /// <see cref="OnKeyDown"/> — focus does not change between the two halves of a keystroke.
    /// </summary>
    public void OnKeyUp(GuiKeyEventArgs args) { }

    /// <summary>
    /// Called after <see cref="OnKeyDown"/> when the OS produced a printable character.
    /// Prefer this over <see cref="OnKeyDown"/> for text input — it handles modifiers,
    /// dead keys, and IME composition correctly.
    /// </summary>
    public void OnKeyPress(GuiKeyEventArgs args) { }

    /// <summary>
    /// Called when the primary mouse button is pressed inside this node's bounds.
    /// Slot-level <c>OnMouseDown</c> handlers attached via the builder also fire and are
    /// independent of this hook.
    /// </summary>
    public void OnMouseDown(GuiMouseEventArgs args) { }

    /// <summary>
    /// Called when the mouse button is released after a prior <c>OnMouseDown</c> on this
    /// node, regardless of where the cursor is at release (implicit mouse capture).
    /// </summary>
    public void OnMouseUp(GuiMouseEventArgs args) { }

    /// <summary>
    /// Called when both press and release land inside this node's bounds (a complete click).
    /// Fires after <see cref="OnMouseUp"/>.
    /// </summary>
    public void OnMouseClick(GuiMouseEventArgs args) { }

    /// <summary>
    /// Called on mouse movement while this node has capture (between
    /// <see cref="OnMouseDown"/> and <see cref="OnMouseUp"/>) and also on uncaptured
    /// hover movement while the cursor is inside this node's bounds. Check
    /// <see cref="GuiMouseEventArgs.Button"/> to distinguish drag from hover.
    /// </summary>
    public void OnMouseMove(GuiMouseEventArgs args) { }

    /// <summary>
    /// Called once when the uncaptured cursor first enters this node's bounds.
    /// </summary>
    public void OnMouseEnter(GuiMouseEventArgs args) { }

    /// <summary>
    /// Called once when the cursor leaves this node's bounds after a prior
    /// <see cref="OnMouseEnter"/>. Always paired with Enter.
    /// </summary>
    public void OnMouseLeave(GuiMouseEventArgs args) { }
}
