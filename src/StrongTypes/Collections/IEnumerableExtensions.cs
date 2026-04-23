using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Returns the non-null values from <paramref name="source"/>, unwrapping each
    /// nullable into its underlying value.
    /// </summary>
    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source)
        where T : struct
    {
        return source.Where(item => item.HasValue).Select(item => item!.Value);
    }

    /// <summary>
    /// Returns the non-null references from <paramref name="source"/>.
    /// </summary>
    public static IEnumerable<T> ExceptNulls<T>(this IEnumerable<T?> source)
        where T : class
    {
        return source.Where(item => item is not null)!;
    }

    public static IEnumerable<T> Except<T>(this IEnumerable<T> source, params T[] excludedItems)
        => Enumerable.Except(source, excludedItems);
}
