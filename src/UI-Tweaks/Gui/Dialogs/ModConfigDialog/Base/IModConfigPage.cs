using BitzArt.UI.Tweaks.Gui;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// Marker interface for pages hosted inside <see cref="ModConfigDialog"/>.
/// Implementing types must declare a static <see cref="PageName"/> used by the nav
/// list as the button label — no runtime instance is needed to retrieve the name.
/// </summary>
public interface IModConfigPage : IGuiComponent
{
    /// <summary>
    /// The display name shown in the nav list for this page.
    /// Declared as <c>static abstract</c> so it can be read at registration time
    /// (in <c>NavEntry&lt;T&gt;</c>) without constructing a page instance.
    /// </summary>
    static abstract string PageName { get; }
}
