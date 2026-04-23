#nullable enable

using System;
using System.Collections.Generic;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Splits <paramref name="source"/> by <paramref name="predicate"/>, preserving relative order within each partition.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to partition.</param>
    /// <param name="predicate">Tested against each element.</param>
    /// <returns><c>Passing</c> holds items for which <paramref name="predicate"/> returned <c>true</c>; <c>Violating</c> holds the rest.</returns>
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
