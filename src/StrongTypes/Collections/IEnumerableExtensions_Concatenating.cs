using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    [Pure]
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params T[] items)
        => Enumerable.Concat(first, items);

    [Pure]
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params IEnumerable<T>[] others)
        => Enumerable.Concat(first, others.SelectMany(o => o));
}
