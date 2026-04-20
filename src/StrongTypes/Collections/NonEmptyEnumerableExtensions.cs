#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

/// <summary>
/// Extension methods that produce or consume <see cref="NonEmptyEnumerable{T}"/>.
/// Kept deliberately small — anything that does not require the non-empty invariant
/// (projection, filtering, aggregation) is available via LINQ.
/// </summary>
public static class NonEmptyEnumerableExtensions
{
    /// <summary>
    /// Wraps <paramref name="source"/> as a <see cref="NonEmptyEnumerable{T}"/>, or returns
    /// <c>null</c> when the sequence is null or empty.
    /// </summary>
    public static NonEmptyEnumerable<T>? TryAsNonEmpty<T>(this IEnumerable<T>? source)
        => NonEmptyEnumerable.TryCreate(source);

    /// <summary>
    /// Maps every element and returns a <see cref="NonEmptyEnumerable{TResult}"/> — the
    /// non-empty invariant carries through the projection.
    /// </summary>
    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        for (var i = 0; i < source.Count; i++)
            buffer[i] = selector(source[i]);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Maps every element with its index and returns a <see cref="NonEmptyEnumerable{TResult}"/>.
    /// </summary>
    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        for (var i = 0; i < source.Count; i++)
            buffer[i] = selector(source[i], i);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Projects each element to a non-empty sequence and concatenates them. The result is
    /// itself non-empty because the first projected sequence contributes at least one element.
    /// </summary>
    public static NonEmptyEnumerable<TResult> SelectMany<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, INonEmptyEnumerable<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var flattened = Enumerable.SelectMany(source, item => (IEnumerable<TResult>)selector(item)).ToArray();
        return NonEmptyEnumerable<TResult>.FromValidatedArray(flattened);
    }

    /// <summary>
    /// Returns the distinct elements. The head is always present, so the result is non-empty.
    /// </summary>
    public static NonEmptyEnumerable<T> Distinct<T>(this INonEmptyEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var distinct = Enumerable.Distinct(source).ToArray();
        return NonEmptyEnumerable<T>.FromValidatedArray(distinct);
    }

    /// <summary>
    /// Concatenates additional items onto a non-empty sequence.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, params ReadOnlySpan<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new T[source.Count + items.Length];
        for (var i = 0; i < source.Count; i++) buffer[i] = source[i];
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Concatenates additional sequences onto a non-empty sequence.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);

        return NonEmptyEnumerable<T>.FromValidatedArray([.. source, .. items]);
    }
}
