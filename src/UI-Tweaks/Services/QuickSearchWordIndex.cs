using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace BitzArt.UI.Tweaks;

internal class QuickSearchWordIndex
{
    private readonly Dictionary<string, WordEntry> _words;

    private record WordEntry(string Word, List<ItemEntry> Items);
    private record ItemEntry(ItemStack Item, string Name);

    public QuickSearchWordIndex(List<ItemStack> items)
    {
        _words = [];

        foreach (var item in items)
        {
            var name = item.GetName();
            var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            var itemEntry = new ItemEntry(item, name);

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
    }

    public IEnumerable<ItemStack> Search(string query)
    {
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
            .Select(entry => entry.Item);
    }
}
