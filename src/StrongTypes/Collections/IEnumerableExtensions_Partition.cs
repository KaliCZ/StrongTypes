using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Splits <paramref name="source"/> by <paramref name="predicate"/>, preserving relative order within each partition.</summary>
    [Pure]
    public static (IReadOnlyList<T> Passing, IReadOnlyList<T> Violating) Partition<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        var capacity = source is ICollection<T> c ? c.Count : 0;
        var passing = new List<T>(capacity);
        var violating = new List<T>(capacity);

        foreach (var value in source)
        {
            if (predicate(value)) passing.Add(value);
            else violating.Add(value);
        }

        return (passing, violating);
    }
}
