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
    /// <c>null</c> when the sequence is null or empty. The <c>As</c> prefix follows the
    /// codebase convention — <c>AsX</c> is the nullable form, <c>ToX</c> throws on failure.
    /// </summary>
    public static NonEmptyEnumerable<T>? AsNonEmpty<T>(this IEnumerable<T>? source)
        => NonEmptyEnumerable.TryCreateRange(source);

    /// <summary>
    /// Wraps <paramref name="source"/> as a <see cref="NonEmptyEnumerable{T}"/>, throwing
    /// <see cref="ArgumentException"/> when the sequence is null or empty.
    /// </summary>
    public static NonEmptyEnumerable<T> ToNonEmpty<T>(this IEnumerable<T>? source)
        => NonEmptyEnumerable.CreateRange(source);

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
        if (source is NonEmptyEnumerable<T> concrete)
        {
            // Span indexer elides the interface virtual-dispatch and repeated bounds checks.
            var src = concrete.AsSpan();
            for (var i = 0; i < src.Length; i++) buffer[i] = selector(src[i]);
        }
        else
        {
            for (var i = 0; i < source.Count; i++) buffer[i] = selector(source[i]);
        }
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
        if (source is NonEmptyEnumerable<T> concrete)
        {
            var src = concrete.AsSpan();
            for (var i = 0; i < src.Length; i++) buffer[i] = selector(src[i], i);
        }
        else
        {
            for (var i = 0; i < source.Count; i++) buffer[i] = selector(source[i], i);
        }
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

        // Hand-rolled because LINQ's Concat doesn't accept a span — converting would
        // cost an extra array allocation to save nothing.
        var buffer = new T[source.Count + items.Length];
        if (source is NonEmptyEnumerable<T> concrete)
            concrete.AsSpan().CopyTo(buffer);
        else
            for (var i = 0; i < source.Count; i++) buffer[i] = source[i];
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Concatenates an additional sequence onto a non-empty sequence.
    /// </summary>
    /// <remarks>
    /// Delegates to <see cref="Enumerable.Concat{TSource}"/> — now that
    /// <see cref="NonEmptyEnumerable{T}"/> implements <see cref="ICollection{T}"/>, LINQ's
    /// <c>Concat2Iterator.ToArray</c> pre-sizes and runs a Memmove-backed copy for both halves.
    /// </remarks>
    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);

        return NonEmptyEnumerable<T>.FromValidatedArray(Enumerable.Concat(source, items).ToArray());
    }
}
