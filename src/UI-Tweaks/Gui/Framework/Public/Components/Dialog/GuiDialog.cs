using Vintagestory.API.Client;
using Vintagestory.API.Config;

namespace BitzArt.UI.Tweaks.Gui;

public abstract class GuiDialog : GuiComponent, IGuiDialog, IGuiResizable
{
    // ClientApi is guaranteed non-null after the dialog is attached by the dialog host.
    protected new ICoreClientAPI ClientApi => base.ClientApi!;

    /// <summary>
    /// Whether this dialog currently holds keyboard focus. Only the focused dialog receives
    /// keyboard events, and vanilla focus handling brings it in front of other Cairo dialogs.
    /// </summary>
    public bool IsFocused { get; private set; }

    private IGuiDialogRuntime? _runtime;

    /// <summary>
    /// Horizontal offset in logical (unscaled) pixels from the screen-centred default position.
    /// Mutated by drag interactions (see <see cref="Move"/>) and read by the renderer each
    /// frame; setting it directly snaps the dialog to a new position.
    /// </summary>
    public double OffsetX { get; set; }

    /// <summary>
    /// Vertical offset in logical (unscaled) pixels from the screen-centred default position.
    /// Mutated by drag interactions (see <see cref="Move"/>) and read by the renderer each
    /// frame; setting it directly snaps the dialog to a new position.
    /// </summary>
    public double OffsetY { get; set; }

    double IGuiDialog.OffsetX => OffsetX;
    double IGuiDialog.OffsetY => OffsetY;

    /// <summary>
    /// Adds a delta (logical pixels) to the dialog's screen-position offset. Intended as the
    /// drag-handler target for <see cref="GuiDialogTitleBar.OnDrag"/>: pass <c>this.Move</c> as
    /// the title bar's <c>onDrag</c> callback to make the title bar drag the dialog around.
    /// </summary>
    public void Move(double deltaX, double deltaY)
    {
        OffsetX += deltaX;
        OffsetY += deltaY;
    }

    /// <summary>
    /// When true, the user can drag this dialog's supported resize edges. By default,
    /// <see cref="SupportedResizeEdges"/> enables the bottom edge, right edge, and SE corner.
    /// The cursor switches to a directional resize sprite while hovering a grab zone.
    /// <see cref="MinWidth"/>/<see cref="MinHeight"/>/<see cref="MaxWidth"/>/
    /// <see cref="MaxHeight"/> bound the size; they have no effect when this is false.
    /// </summary>
    public bool IsResizable
    {
        get => _isResizable;
        set
        {
            if (_isResizable == value)
            {
                return;
            }
            _isResizable = value;
        }
    }
    private bool _isResizable = false;

    /// <summary>
    /// The dialog edges that can be resized while <see cref="IsResizable"/> is true.
    /// </summary>
    public GuiResizeEdge SupportedResizeEdges => IsResizable ? ResizeEdges : GuiResizeEdge.None;

    /// <summary>
    /// The resize edges supported by this dialog when resize is enabled.
    /// </summary>
    protected virtual GuiResizeEdge ResizeEdges => GuiResizeEdge.Right | GuiResizeEdge.Bottom;

    /// <summary>Minimum logical-pixel width enforced while resizing. Default 200.</summary>
    public int MinWidth { get; set; } = 200;
    /// <summary>Minimum logical-pixel height enforced while resizing. Default 100.</summary>
    public int MinHeight { get; set; } = 100;
    /// <summary>Maximum logical-pixel width enforced while resizing. Default 2000.</summary>
    public int MaxWidth { get; set; } = 2000;
    /// <summary>Maximum logical-pixel height enforced while resizing. Default 1500.</summary>
    public int MaxHeight { get; set; } = 1500;

    protected GuiDialog()
    {
    }

    void IGuiDialog.AttachDialogRuntime(IGuiDialogRuntime runtime)
    {
        _runtime = runtime;
    }

    private IGuiDialogRuntime Runtime => _runtime
        ?? throw new InvalidOperationException("Dialog is not attached to a dialog host.");

    /// <summary>
    /// Requests keyboard focus for this dialog. Other open dialogs lose focus and vanilla
    /// focus handling brings this dialog to the front.
    /// </summary>
    public void RequestFocus()
    {
        Runtime.RequestFocus();
    }

    protected void RequestClose()
    {
        Runtime.RequestClose();
    }

    protected override void ConfigureSlot(IGuiSlotBuilder builder)
    {
        base.ConfigureSlot(builder);
        builder.ConfigureLayout(layoutParameters =>
        {
            layoutParameters.Width = 400;
            layoutParameters.Height = 300;
        });
        builder
            // Keep the root slot focusable/interactive when clicking empty dialog chrome.
            // Resize gestures are wired automatically by the framework through IGuiResizable.
            .OnMouseDown(static _ => { })
            .OnFocusChanged(HandleDialogFocusChanged);
    }

    public virtual void Resize(GuiComponentBounds bounds)
    {
        double previousWidth = LayoutParameters.Width.Value;
        double previousHeight = LayoutParameters.Height.Value;

        double newWidth = Math.Clamp(bounds.Width, MinWidth, MaxWidth);
        double newHeight = Math.Clamp(bounds.Height, MinHeight, MaxHeight);

        LayoutParameters.Width = newWidth;
        LayoutParameters.Height = newHeight;

        ApplyScreenBounds(bounds.X, bounds.Y, newWidth, newHeight);

        OnResizeUpdated(newWidth != previousWidth || newHeight != previousHeight);
    }

    protected virtual void OnResizeUpdated(bool sizeChanged)
    {
        if (sizeChanged)
        {
            RequestArrange();
        }
    }

    private void HandleDialogFocusChanged(bool focused)
    {
        IsFocused = focused;
    }

    private void ApplyScreenBounds(double x, double y, double width, double height)
    {
        float scale = RuntimeEnv.GUIScale;
        double physicalWidth = Math.Round(width * scale);
        double physicalHeight = Math.Round(height * scale);

        OffsetX = (x * scale + physicalWidth / 2.0 - ClientApi.Render.FrameWidth / 2.0) / scale;
        OffsetY = (y * scale + physicalHeight / 2.0 - ClientApi.Render.FrameHeight / 2.0) / scale;
    }
}
