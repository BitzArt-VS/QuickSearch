using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

internal class QuickSearchService : IDisposable
{
    private bool _isDisposed;
    private ICoreClientAPI _clientApi;

    private List<ItemStack>? _items;
    private QuickSearchWordIndex? _index;

    public QuickSearchService(ICoreClientAPI clientApi)
    {
        _isDisposed = false;
        _clientApi = clientApi;

        clientApi.Event.LevelFinalize += OnLevelFinalize;
    }

    private void OnLevelFinalize()
    {
        Task.Run(() =>
        {
            _items = [.. _clientApi.World.Collectibles
                .SelectMany(x => x.GetHandBookStacks(_clientApi) ?? [])
                .OrderBy(x => x.GetName().Length)];

            _index = new(_items);
        });
    }

    public IEnumerable<ItemStack> Search(string query)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_index is null)
        {
            return [];
        }

        return _index.Search(query);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _items = null;
        _index = null;

        _clientApi.Event.LevelFinalize -= OnLevelFinalize;
        _clientApi = null!;

        _isDisposed = true;
    }
}
