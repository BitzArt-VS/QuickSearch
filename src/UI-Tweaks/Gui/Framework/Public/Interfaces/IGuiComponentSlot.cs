namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// Read-only view of a node's mounted position in the GUI component tree.
/// </summary>
public interface IGuiComponentSlot
{
    public IGuiNode Node { get; }

    public IReadOnlyList<IGuiComponentSlot> Children { get; }
}
