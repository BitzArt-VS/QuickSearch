using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

/// <summary>
/// A service that handles searching for in-game items.
/// </summary>
internal partial class QuickSearchService : IDisposable
{
    private bool _isDisposed;
    private ICoreClientAPI _clientApi;

    private List<ItemStack>? _items;
    private ItemIndex? _index;

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

            _index = new(_clientApi, _items);
        });
    }

    public IEnumerable<QuickSearchResultItem> Search(string query)
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

    /// <summary>
    /// A reverse index for quickly searching items by their names. <br />
    /// <see href="https://en.wikipedia.org/wiki/Inverted_index"/>
    /// </summary>
    internal class ItemIndex
    {
        private readonly ICoreClientAPI _clientApi;
        private readonly Dictionary<string, WordEntry> _words;

        private record WordEntry(string Word, List<ItemEntry> Items);
        private record ItemEntry(ItemStack Item, string Name, QuickSearchResultItem ResultItem);

        public ItemIndex(ICoreClientAPI clientApi, List<ItemStack> items)
        {
            _clientApi = clientApi;
            _words = [];

            foreach (var item in items)
            {
                try
                {
                    var name = item.GetName();
                    var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    var itemEntry = new ItemEntry(item, name, new(item, clientApi));

                    foreach (var word in words)
                    {
                        if (!_words.TryGetValue(word, out var wordEntry))
                        {
                            wordEntry = new(word, []);
                            _words.Add(word, wordEntry);
                        }
                        wordEntry.Items.Add(itemEntry);
                    }
                }
                catch (Exception ex)
                {
                    _clientApi.Logger.Error($"Failed to add item '{item.Collectible.Code}' to quick search index.");
                    _clientApi.Logger.Error(ex);
                }
            }
        }

        public IEnumerable<QuickSearchResultItem> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return [];
            }

            var queryWords = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var wordCount = queryWords.Length;

            if (wordCount == 0)
            {
                return [];
            }

            // Find matches for each query word
            List<HashSet<ItemEntry>> queryWordMatches = [];
            foreach (var queryWord in queryWords)
            {
                var matches = _words
                    .Values.Where(w => w.Word.Contains(queryWord, StringComparison.OrdinalIgnoreCase))
                    .SelectMany(w => w.Items)
                    .ToHashSet();

                // If any query word has no matches,
                // no items can match the entire query
                if (matches.Count == 0)
                {
                    return [];
                }

                queryWordMatches.Add(matches);
            }

            // Intersect the sets of matches for each query word
            // to find items that match all query words
            return queryWordMatches
                .Skip(1)
                .Aggregate(new HashSet<ItemEntry>(queryWordMatches.First()), (acc, set) =>
                {
                    acc.IntersectWith(set);
                    return acc;
                })
                .OrderBy(x => x.Name.Length)
                .Select(entry => entry.ResultItem);
        }
    }
}
