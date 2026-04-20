using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Config;

public abstract record TooltipOptions : IHudTooltipConfiguration, INotifyPropertyChanged
{
    [JsonIgnore]
    public abstract string ComponentName { get; }

    [JsonProperty("enable", Order = 01)]
    public virtual bool Enable { get; set; } = true;

    [JsonProperty("dialogArea", Order = 21)]
    public virtual string DialogArea { get; set; } = EnumDialogArea.CenterMiddle.ToString();

    [JsonProperty("height", Order = 22)]
    public virtual double Height { get; set; } = 25;

    [JsonProperty("width", Order = 23)]
    public virtual double Width { get; set; } = 200;

    [JsonProperty("centerText", Order = 24)]
    public virtual bool CenterText { get; set; } = false;

    [JsonProperty("offset", Order = 24)]
    public virtual ComponentOffset Offset { get; set; } = new();

    [JsonProperty("padding", Order = 25)]
    public virtual ComponentPadding Padding { get; set; } = new(0, 9);

    [JsonProperty("drawOrder", Order = 26)]
    public virtual double DrawOrder { get; set; } = 0.1;

    [JsonProperty("hasBackground", Order = 31)]
    public virtual bool HasBackground { get; set; } = false;

    [JsonProperty("backgroundOpacity", Order = 32)]
    public virtual double BackgroundOpacity { get; set; } = 0.5;

    [JsonProperty("backgroundCornerRadius", Order = 33)]
    public virtual double BackgroundCornerRadius { get; set; } = 16.0;

    [JsonProperty("format", Order = 41)]
    public virtual string Format { get; set; } = string.Empty;

    [JsonProperty("extraElements", Order = 42, NullValueHandling = NullValueHandling.Ignore, ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public virtual ICollection<string>? ExtraElements { get; set; } = null;

    [JsonProperty("fontSize", Order = 51)]
    public virtual double FontSize { get; set; } = GuiStyle.DetailFontSize;

    IComponentOffset IHudTooltipConfiguration.Offset => Offset;
    IComponentPadding IHudTooltipConfiguration.Padding => Padding;
    EnumDialogArea IHudTooltipConfiguration.Area => Enum.Parse<EnumDialogArea>(DialogArea, ignoreCase: true);

    public event PropertyChangedEventHandler? PropertyChanged;

    public virtual void NotifyPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
