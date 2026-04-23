using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Splits <paramref name="source"/> into two lists: items for which
    /// <paramref name="predicate"/> returns true (<c>Passing</c>) and items
    /// for which it returns false (<c>Violating</c>). Relative order is
    /// preserved within each partition.
    /// </summary>
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
