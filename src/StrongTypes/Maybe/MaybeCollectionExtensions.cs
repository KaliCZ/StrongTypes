#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

/// <summary>Extensions bridging <see cref="IEnumerable{T}"/> and <see cref="Maybe{T}"/>.</summary>
public static class MaybeCollectionExtensions
{
    #region IEnumerable<T> → Maybe<T>

    /// <summary>Returns the first element satisfying <paramref name="predicate"/>, or <c>None</c> when no element matches.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    /// <param name="predicate">Tested against each element.</param>
    public static Maybe<T> SafeFirst<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
        => source.Where(predicate).SafeFirst();

    /// <summary>Returns the first element of <paramref name="source"/>, or <c>None</c> when the sequence is empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    public static Maybe<T> SafeFirst<T>(this IEnumerable<T> source) where T : notnull
    {
        if (source is IReadOnlyList<T> list)
            return list.Count == 0 ? default : Maybe<T>.Some(list[0]);

        using var enumerator = source.GetEnumerator();
        return enumerator.MoveNext() ? Maybe<T>.Some(enumerator.Current) : default;
    }

    /// <summary>Returns the last element satisfying <paramref name="predicate"/>, or <c>None</c> when no element matches.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    /// <param name="predicate">Tested against each element.</param>
    public static Maybe<T> SafeLast<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
        => source.Where(predicate).SafeLast();

    /// <summary>Returns the last element of <paramref name="source"/>, or <c>None</c> when the sequence is empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    public static Maybe<T> SafeLast<T>(this IEnumerable<T> source) where T : notnull
    {
        if (source is IReadOnlyList<T> list)
            return list.Count == 0 ? default : Maybe<T>.Some(list[list.Count - 1]);

        return source.Reverse().SafeFirst();
    }

    /// <summary>Returns the single element satisfying <paramref name="predicate"/>, or <c>None</c> when zero or more than one match.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    /// <param name="predicate">Tested against each element.</param>
    public static Maybe<T> SafeSingle<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        where T : notnull
        => source.Where(predicate).SafeSingle();

    /// <summary>Returns the single element of <paramref name="source"/>, or <c>None</c> when the sequence has zero or more than one element.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    public static Maybe<T> SafeSingle<T>(this IEnumerable<T> source) where T : notnull
    {
        using var enumerator = source.GetEnumerator();
        if (!enumerator.MoveNext()) return default;
        var candidate = enumerator.Current;
        return enumerator.MoveNext() ? default : Maybe<T>.Some(candidate);
    }

    /// <summary>Projects each element with <paramref name="selector"/> and returns the maximum, or <c>None</c> when the sequence is empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TValue">The projected value type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    /// <param name="selector">Projects each element.</param>
    public static Maybe<TValue> SafeMax<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)
        where TValue : notnull
        => source.Select(selector).SafeMax();

    /// <summary>Returns the maximum element, or <c>None</c> when the sequence is empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
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

    /// <summary>Projects each element with <paramref name="selector"/> and returns the minimum, or <c>None</c> when the sequence is empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <typeparam name="TValue">The projected value type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
    /// <param name="selector">Projects each element.</param>
    public static Maybe<TValue> SafeMin<T, TValue>(this IEnumerable<T> source, Func<T, TValue> selector)
        where TValue : notnull
        => source.Select(selector).SafeMin();

    /// <summary>Returns the minimum element, or <c>None</c> when the sequence is empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to scan.</param>
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

    /// <summary>Extracts the underlying values from every populated <see cref="Maybe{T}"/>, dropping empties.</summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    /// <param name="source">The sequence of <see cref="Maybe{T}"/>.</param>
    public static IEnumerable<T> Values<T>(this IEnumerable<Maybe<T>> source) where T : notnull
        => source.Where(m => m.HasValue).Select(m => m.InternalValue);

    #endregion
}
