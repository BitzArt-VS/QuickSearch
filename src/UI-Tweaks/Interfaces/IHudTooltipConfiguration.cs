using System.Collections.Generic;
using System.ComponentModel;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

public interface IHudTooltipConfiguration : INotifyPropertyChanged
{
    public string ComponentName { get; }

    public bool Enable { get; }

    public EnumDialogArea Area { get; }

    public double Height { get; }
    public double Width { get; }
    public IComponentOffset Offset { get; }
    public IComponentPadding Padding { get; }
    public double DrawOrder { get; }
    public bool CenterText { get; }

    public bool HasBackground { get; }
    public double BackgroundOpacity { get; }
    public double BackgroundCornerRadius { get; }

    public string Format { get; }
    public ICollection<string>? ExtraElements { get; }

    public double FontSize { get; }
}
