using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

internal class VanillaGuiDialog(ICoreClientAPI clientApi) : Vintagestory.API.Client.GuiDialog(clientApi)
{
    protected bool IsDisposed = false;
    protected ICoreClientAPI ClientApi = clientApi;

    public override string? ToggleKeyCombinationCode => null;

    public override void Dispose()
    {
        if (IsDisposed)
        {
            return;
        }

        base.Dispose();

        ClientApi = null!;
        IsDisposed = true;
    }

    public bool TryOpenOnKeyPress()
    {
        ignoreNextKeyPress = true;

        return TryOpen();
    }

    public override bool TryOpen()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (IsOpened())
        {
            return false;
        }

        return base.TryOpen();
    }

    protected void InvokeAsync(Action action)
    {
        ClientApi.Event.EnqueueMainThreadTask(action, string.Empty);
    }
}
