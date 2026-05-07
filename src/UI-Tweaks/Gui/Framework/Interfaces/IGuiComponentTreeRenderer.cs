using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal interface IGuiComponentTreeRenderer : IRenderer
{
    public IGuiRenderHandle Handle { get; }

    /// <summary>
    /// The client API the owning dialog was constructed with. Forwarded to component
    /// render handles so draw hooks can load assets and call into the vanilla API.
    /// </summary>
    public ICoreClientAPI ClientApi { get; }

    /// <summary>
    /// Schedules a rebuild of the given fragment against its builder on the next render frame.
    /// The same fragment instance will not be enqueued twice (deduplicated by reference).
    /// </summary>
    public void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder);

    /// <summary>
    /// Removes a previously scheduled fragment from the pending or active rebuild queue.
    /// Called by a parent builder when it is about to rebuild this fragment's subtree as part
    /// of its own reconcile pass, making the separately-scheduled rebuild redundant.
    /// No-op when the fragment is not queued.
    /// </summary>
    public void Cancel(GuiRenderFragment fragment);

    /// <summary>
    /// Records an interactive region during the render walk. Called by
    /// <see cref="GuiRenderTreeBuilder.Render"/> for any slot whose frame has at least one
    /// mouse handler attached, after the slot's bounds have been resolved. Regions are
    /// recorded in render order; hit-testing walks the table in reverse.
    /// </summary>
    public void AddInteractiveRegion(in InteractiveRegion region);

    /// <summary>
    /// Records a mouse-wheel target during the render walk. Called by
    /// <see cref="GuiRenderTreeBuilder.Render"/> for every scrollable
    /// <see cref="GuiContainer"/> whose viewport intersects the cursor. The renderer's
    /// wheel dispatcher hit-tests this list (in reverse) to find the topmost scrollable
    /// area under the cursor and forwards the wheel delta to its
    /// <c>HandleMouseWheel</c> method.
    /// </summary>
    public void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container);

    /// <summary>
    /// Records a keyboard-region entry during the render walk. Called by
    /// <see cref="GuiRenderTreeBuilder.Render"/> for any slot whose frame has at least one
    /// keyboard handler attached. Keyboard events are dispatched to the entry whose
    /// <see cref="KeyboardRegion.Token"/> matches the currently focused
    /// <see cref="IGuiNode"/> (looked up via <see cref="FocusManager"/>); there is no
    /// spatial routing.
    /// </summary>
    public void AddKeyboardRegion(in KeyboardRegion region);
}
