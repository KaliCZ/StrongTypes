using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Drops <c>null</c> entries and unwraps the remaining values.</summary>
    /// <typeparam name="T">The underlying value type.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>The non-null values from <paramref name="source"/> as a <typeparamref name="T"/> sequence.</returns>
    [Pure]
    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source)
        where T : struct
    {
        return source.Where(item => item.HasValue).Select(item => item!.Value);
    }

    /// <summary>Drops <c>null</c> references from the sequence.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <returns>The non-null references from <paramref name="source"/>.</returns>
    [Pure]
    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source)
        where T : class
    {
        return source.Where(item => item is not null)!;
    }

    /// <summary>Returns <paramref name="source"/> with <paramref name="excludedItems"/> removed.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to filter.</param>
    /// <param name="excludedItems">The values to exclude.</param>
    [Pure]
    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] excludedItems)
        => Enumerable.Except(source, excludedItems);
}
