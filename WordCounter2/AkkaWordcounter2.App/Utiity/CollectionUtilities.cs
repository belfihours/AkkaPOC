using System.Collections.Immutable;

namespace AkkaWordcounter2.App.Utiity;

public static class CollectionUtilities
{
    public static IImmutableDictionary<string, int> MergeWordCounts(
        IEnumerable<IDictionary<string, int>> counts)
    {
        var  mergedCounts = counts.Aggregate(ImmutableDictionary<string, int>.Empty,
            (accum, next) =>
            {
                foreach (var (word, count) in next)
                {
                    accum.SetItem(word, accum.GetValueOrDefault(word, 0) + count);
                }
                return accum;
            });
        return mergedCounts;
    }
}