using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal interface IGuiComponentTreeRenderer : IRenderer
{
    public IGuiRenderHandle Handle { get; }

    public ICoreClientAPI ClientApi { get; }

    /// <summary>
    /// Schedules a rebuild of the given fragment. Deduplicated by reference — the same
    /// fragment will not be enqueued twice.
    /// </summary>
    public void Schedule(GuiRenderFragment fragment, GuiRenderTreeBuilder builder);

    /// <summary>
    /// Removes a previously scheduled fragment from the rebuild queue. Called by a parent
    /// builder when it is rebuilding this fragment's subtree itself. No-op when not queued.
    /// </summary>
    public void Cancel(GuiRenderFragment fragment);

    /// <summary>
    /// Records an interactive region. Regions are recorded in render order; hit-testing
    /// walks in reverse.
    /// </summary>
    public void AddInteractiveRegion(in InteractiveRegion region);

    /// <summary>
    /// Records a mouse-wheel target. The wheel dispatcher forwards to the topmost
    /// scrollable area under the cursor.
    /// </summary>
    public void AddScrollRegion(GuiComponentBounds bounds, GuiContainer container);

    /// <summary>
    /// Records a keyboard region. Events are dispatched to the entry whose
    /// <see cref="KeyboardRegion.Token"/> matches the currently focused node —
    /// there is no spatial routing.
    /// </summary>
    public void AddKeyboardRegion(in KeyboardRegion region);
}
