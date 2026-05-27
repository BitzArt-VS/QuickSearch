using System.Diagnostics.CodeAnalysis;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks.Gui;

public sealed class GuiDialogHostInstance : IDisposable
{
    private readonly ICoreClientAPI _clientApi;
    private readonly Dictionary<Type, DialogRenderer> _renderers = [];
    private bool _isDisposed;

    public GuiDialogHostInstance(ICoreClientAPI clientApi)
    {
        _clientApi = clientApi;
    }

    public bool Toggle<TDialog>(Action<TDialog>? configure = null)
        where TDialog : class, IGuiDialog, new()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_renderers.ContainsKey(typeof(TDialog)))
        {
            return Close<TDialog>();
        }

        Open(configure);
        return true;
    }

    public TDialog Open<TDialog>(Action<TDialog>? configure = null)
        where TDialog : class, IGuiDialog, new()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_renderers.TryGetValue(typeof(TDialog), out var renderer))
        {
            var typedRenderer = (DialogRenderer<TDialog>)renderer;
            if (configure is not null)
            {
                // Already-open reconfiguration is intentionally in-place. Exceptions from
                // configuration/reconciliation/validation propagate and may leave this
                // dialog partially updated, so treat them as programmer errors.
                typedRenderer.ReconcileDialog(configure);
            }
            return typedRenderer.Dialog;
        }

        var newRenderer = new DialogRenderer<TDialog>(_clientApi, configure, () => Close<TDialog>());
        _renderers[typeof(TDialog)] = newRenderer;
        return newRenderer.Dialog;
    }

    public bool Close<TDialog>()
        where TDialog : class, IGuiDialog, new()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (!_renderers.TryGetValue(typeof(TDialog), out var renderer))
        {
            return false;
        }

        renderer.Dispose();
        _renderers.Remove(typeof(TDialog));

        return true;
    }

    public bool IsOpen<TDialog>()
        where TDialog : class, IGuiDialog, new()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        return _renderers.ContainsKey(typeof(TDialog));
    }

    public bool TryGet<TDialog>([NotNullWhen(true)] out TDialog? dialog)
        where TDialog : class, IGuiDialog, new()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_renderers.TryGetValue(typeof(TDialog), out var renderer))
        {
            dialog = ((DialogRenderer<TDialog>)renderer).Dialog;
            return true;
        }

        dialog = null;
        return false;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        foreach (var renderer in _renderers.Values)
        {
            renderer.Dispose();
        }

        _renderers.Clear();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }
}
