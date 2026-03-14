using System;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

internal class ModGuiDialog(ICoreClientAPI clientApi) : GuiDialog(clientApi)
{
    protected bool IsDisposed = false;
    public override string? ToggleKeyCombinationCode => null;

    protected ICoreClientAPI ClientApi = clientApi;

    protected void InvokeAsync(Action action)
    {
        ClientApi.Event.EnqueueMainThreadTask(action, string.Empty);
    }

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
}
