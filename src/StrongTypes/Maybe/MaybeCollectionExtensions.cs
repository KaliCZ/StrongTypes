using System;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

/// <summary>
/// Extensions over <see cref="Maybe{T}"/> that interact with collections and vice-versa:
/// pulling a single <see cref="Maybe{T}"/> out of an <see cref="IEnumerable{T}"/>, and
/// flattening a sequence of <see cref="Maybe{T}"/> down to its populated values.
/// </summary>
public static class MaybeCollectionExtensions
{
    #region IEnumerable<T> → Maybe<T>

    /// <summary>
    /// Returns the first element satisfying <paramref name="predicate"/>, or
    /// <see cref="Maybe{T}.None"/> if no element matches.
    /// </summary>
    [Pure]
    public static Maybe<T> SafeFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
        => source.Where(predicate).SafeFirst();

    /// <summary>
    /// Returns the first element of <paramref name="source"/>, or
    /// <see cref="Maybe{T}.None"/> if the sequence is empty.
    /// </summary>
    [Pure]
    public static Maybe<T> SafeFirst<T>(this IEnumerable<T> source) where T : notnull
    {
        if (source is IReadOnlyList<T> list)
            return list.Count == 0 ? default : Maybe<T>.Some(list[0]);

        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() ? Maybe<T>.Some(enumerator.Current) : default;
    }

    [Pure]
    public static Maybe<T> SafeLast<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
        => source.Where(predicate).SafeLast();

    [Pure]
    public static Maybe<T> SafeLast<T>(this IEnumerable<T> source) where T : notnull
    {
        if (source is IReadOnlyList<T> list)
            return list.Count == 0 ? default : Maybe<T>.Some(list[list.Count - 1]);

        return source.Reverse().SafeFirst();
    }

    [Pure]
    public static Maybe<T> SafeSingle<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
        => source.Where(predicate).SafeSingle();

    /// <summary>
    /// Returns the single element of <paramref name="source"/> if it contains exactly
    /// one; <see cref="Maybe{T}.None"/> for zero OR more than one.
    /// </summary>
    [Pure]
    public static Maybe<T> SafeSingle<T>(this IEnumerable<T> source) where T : notnull
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        var candidate = enumerator.Current;
        return enumerator.MoveNext() ? default : Maybe<T>.Some(candidate);
    }

    [Pure]
    public static Maybe<TValue> SafeMax<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)
        where TValue : notnull
        => source.Select(selector).SafeMax();

    [Pure]
    public static Maybe<T> SafeMax<T>(this IEnumerable<T> source) where T : notnull
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        var max = enumerator.Current;
        var comparer = Comparer<T>.Default;
        while (enumerator.MoveNext())
        {
            if (comparer.Compare(enumerator.Current, max) > 0) max = enumerator.Current;
        }
        return Maybe<T>.Some(max);
    }

    [Pure]
    public static Maybe<TValue> SafeMin<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)
        where TValue : notnull
        => source.Select(selector).SafeMin();

    [Pure]
    public static Maybe<T> SafeMin<T>(this IEnumerable<T> source) where T : notnull
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        var min = enumerator.Current;
        var comparer = Comparer<T>.Default;
        while (enumerator.MoveNext())
        {
            if (comparer.Compare(enumerator.Current, min) < 0) min = enumerator.Current;
        }
        return Maybe<T>.Some(min);
    }

    #endregion

    #region Values (flatten IEnumerable<Maybe<T>> to its populated values)

    /// <summary>
    /// Extracts the underlying values from every populated <see cref="Maybe{T}"/>,
    /// dropping empties.
    /// </summary>
    [Pure]
    public static IEnumerable<T> Values<T>(this IEnumerable<Maybe<T>> source) where T : notnull
        => source.Where(m => m.HasValue).Select(m => m.InternalValue);

    #endregion
}
