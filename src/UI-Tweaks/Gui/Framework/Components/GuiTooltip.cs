using System;
using Cairo;

namespace BitzArt.UI.Tweaks.Gui;

/// <summary>
/// A wrapper component that attaches a tooltip to its child content. The wrapped child is
/// declared via <see cref="Content"/> and laid out normally; <see cref="TooltipContent"/>
/// declares the render tree shown in the floating tooltip surface when the cursor hovers
/// anywhere over this component's allocated bounds.
/// <para>
/// <b>Layout-transparent</b>: <see cref="GuiTooltip"/> implements only <see cref="IGuiNode"/>
/// (not <see cref="IGuiComponent"/>), so the layout pass treats it as invisible. Its
/// <see cref="Content"/> children flow at the wrapper's declaration site as if they had
/// been declared directly there. The wrapper's bounds — used for hover hit-testing — are
/// derived from the union of those children's allocated rectangles along the parent's
/// flow axis.
/// </para>
/// <para>
/// Tooltip layout is computed and drawn by a separate <see cref="TooltipRenderer"/> on
/// its own Cairo surface, so the tooltip is free to extend beyond this component's
/// parent bounds — and even beyond the dialog's own surface — without being clipped.
/// </para>
/// <para>
/// The tooltip wrapping <see cref="GuiTooltipBackground"/> is added automatically with
/// vanilla styling. Pass <see cref="ConfigureBackground"/> to retune (colour, border,
/// radius, padding); for full custom chrome, declare your own panel inside
/// <see cref="TooltipContent"/> and rely on the wrapper's transparency.
/// </para>
/// </summary>
public sealed class GuiTooltip : GuiNode
{
    /// <summary>The wrapped content — laid out at the wrapper's declaration site.</summary>
    public GuiRenderFragment? Content { get; set; }

    /// <summary>The render fragment shown inside the floating tooltip surface.</summary>
    public GuiRenderFragment? TooltipContent { get; set; }

    /// <summary>
    /// Optional configuration applied to the automatic <see cref="GuiTooltipBackground"/>
    /// wrapper around <see cref="TooltipContent"/>. Use to override fill / border / radius
    /// / panel padding while keeping the rest of the vanilla defaults.
    /// </summary>
    public Action<GuiTooltipBackground>? ConfigureBackground { get; set; }

    private TooltipHost? _host;

    protected override void BuildRenderTree(IGuiRenderTreeBuilder builder)
    {
        // Inline wrapped content. Because GuiTooltip is layout-transparent (IGuiNode only),
        // the renderer splices these slots into the parent's flow rather than allocating
        // space for the wrapper itself.
        Content?.Invoke(builder);
    }

    public override void OnParametersSet()
    {
        // Snapshot the host once per pass — the dialog publishes it at the root and never
        // swaps the reference, but a fresh lookup keeps us correct if that ever changes.
        _host = GetCascadingValue<TooltipHost>();
    }

    public override void Render(Context context, GuiComponentBounds bounds)
    {
        // Register the trigger region with the host. The host's region table is reset at
        // the start of every render walk, so a single AddRegion call here is enough — no
        // teardown needed when this component is removed (its slot stops appearing in
        // the walk, and the next mouse-move clears the active tooltip).
        if (_host is null || TooltipContent is null) return;
        _host.AddRegion(this, bounds, TooltipContent, ConfigureBackground);
    }
}
