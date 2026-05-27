using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public static class GuiDialogHost
{
    public static GuiDialogHostInstance Instance
    {
        get => field ?? throw new InvalidOperationException($"{nameof(GuiDialogHost)} has not been initialized.");
        private set => field = value;
    }

    internal static void Initialize(ICoreClientAPI clientApi) => Instance = new(clientApi);

    public static bool Toggle<TDialog>(Action<TDialog>? configure = null)
        where TDialog : class, IGuiDialog, new()
        => Instance.Toggle(configure);

    public static TDialog Open<TDialog>(Action<TDialog>? configure = null)
        where TDialog : class, IGuiDialog, new()
        => Instance.Open(configure);

    public static bool Close<TDialog>()
        where TDialog : class, IGuiDialog, new()
        => Instance.Close<TDialog>();

    public static bool IsOpen<TDialog>()
        where TDialog : class, IGuiDialog, new()
        => Instance.IsOpen<TDialog>();

    public static bool TryGet<TDialog>([NotNullWhen(true)] out TDialog? dialog)
        where TDialog : class, IGuiDialog, new()
        => Instance.TryGet(out dialog);
}
