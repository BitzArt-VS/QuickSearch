namespace BitzArt.UI.Tweaks.Gui;

public interface IGuiComponent : IGuiNode, IGuiMeasurable
{
    public GuiComponentLayoutParameters LayoutParameters { get; }
}
