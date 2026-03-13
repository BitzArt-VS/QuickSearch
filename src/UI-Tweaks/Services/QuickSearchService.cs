using System;
using System.Threading.Tasks;
using Vintagestory.API.Client;

namespace BitzArt.UI.Tweaks;

internal class QuickSearchService(ICoreClientAPI clientApi) : IDisposable
{
    private bool _isDisposed = false;
    private readonly QuickSearchDialog _dialog = new(clientApi);

    public void Initialize()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        RegisterQuickSearchHotKey();
    }

    private void RegisterQuickSearchHotKey()
    {
        clientApi.Input.AddHotKey(ModHotKeys.QuickSearch, (keys) =>
        {
            ObjectDisposedException.ThrowIf(_isDisposed, this);
            _dialog.TryOpen();

            return true;
        });
    }

    private Task OpenAsync()
    {
        
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _dialog.Dispose();

        _isDisposed = true;
    }
}
