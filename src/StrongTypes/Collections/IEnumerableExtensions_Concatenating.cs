#nullable enable

using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>Appends <paramref name="items"/> to <paramref name="first"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="first">The starting sequence.</param>
    /// <param name="items">Elements appended after <paramref name="first"/>.</param>
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params T[] items)
        => Enumerable.Concat(first, items);

    /// <summary>Appends each of <paramref name="others"/> to <paramref name="first"/> in order.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="first">The starting sequence.</param>
    /// <param name="others">Sequences concatenated after <paramref name="first"/>.</param>
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params IEnumerable<T>[] others)
        => Enumerable.Concat(first, others.SelectMany(o => o));
}
