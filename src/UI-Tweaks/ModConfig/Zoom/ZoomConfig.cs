using Newtonsoft.Json;
using System.ComponentModel;

namespace BitzArt.UI.Tweaks.Config;

public record ZoomConfig : INotifyPropertyChanged
{
    [JsonProperty("enable", Order = 1)]
    public bool Enable { get; set; } = false;

    [JsonProperty("strength", Order = 2)]
    public int Strength { get; set; } = 2;

    [JsonProperty("speed", Order = 3)]
    public int Speed { get; set; } = 2;

    [JsonProperty("vignetteStrength", Order = 4)]
    public int VignetteStrength { get; set; } = 5;

    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
