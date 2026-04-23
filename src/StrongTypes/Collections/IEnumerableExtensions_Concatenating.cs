using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params T[] items)
        => Enumerable.Concat(first, items);

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params IEnumerable<T>[] others)
        => Enumerable.Concat(first, others.SelectMany(o => o));
}
