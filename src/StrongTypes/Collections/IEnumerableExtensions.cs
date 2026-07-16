using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Drops <c>null</c> entries and unwraps the remaining values.</summary>
    [Pure]
    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source)
        where T : struct
    {
        return source.Where(item => item.HasValue).Select(item => item!.Value);
    }

    /// <summary>Drops <c>null</c> references from the sequence.</summary>
    [Pure]
    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source)
        where T : class
    {
        return source.Where(item => item is not null)!;
    }

    [Pure]
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] excludedItems)
        => Enumerable.Except(source, excludedItems);
}
