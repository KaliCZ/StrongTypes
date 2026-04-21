using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params T[] items)
    {
        return Enumerable.Concat(first, items);
    }

    public static IEnumerable<T> Concat<T>(this IEnumerable<T> first, params IEnumerable<T>[] others)
    {
        return Enumerable.Concat(first, others.Flatten());
    }

    public static IEnumerable<T> SafeConcat<T>(this IEnumerable<T> first, params T[] items)
    {
        return first is null
            ? items
            : Enumerable.Concat(first, items);
    }

    public static IEnumerable<T> SafeConcat<T>(this IEnumerable<T> first, params IEnumerable<T>[] others)
    {
        var othersResult = others is null
            ? Array.Empty<T>()
            : others.SelectMany(o => o ?? Enumerable.Empty<T>());

        return first is null
            ? othersResult
            : Enumerable.Concat(first, othersResult);
    }

    public static INonEmptyEnumerable<T> Concat<T>(this T e, params IEnumerable<T>[] others)
    {
        return NonEmptyEnumerable.CreateRange(others.Flatten().Prepend(e));
    }
}