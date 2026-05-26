using BitzArt.UI.Tweaks.Config;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// Cascading value published by <see cref="ModConfigDialog"/> at the root of its render
/// tree. Carries the live mod-config DTO and a debounced save trigger so any descendant
/// page can mutate the config in place and request a persisted write without each page
/// holding its own reference.
/// </summary>
internal sealed class ModConfigContext
{
    public ModConfigContext(UiTweaksModConfig config, Action saveConfig)
    {
        Config = config;
        SaveConfig = saveConfig;
    }

    /// <summary>The live config instance shared by every page.</summary>
    public UiTweaksModConfig Config { get; }

    /// <summary>
    /// Schedules a debounced write of <see cref="Config"/> to disk. Cheap to call on every
    /// edit — successive calls within the debounce window collapse into a single write.
    /// </summary>
    public Action SaveConfig { get; }
}
