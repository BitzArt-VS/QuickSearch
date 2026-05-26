namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiComponentBuilder<T> : IGuiRenderTreeBuilder, IGuiSlotBuilder
    where T : IGuiNode
{
    internal IGuiComponentBuilder<T> AddConfigurationAction(Action<T> action);
}
