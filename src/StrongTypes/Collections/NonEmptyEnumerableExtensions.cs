#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

/// <summary>
/// Extension methods that produce or consume <see cref="NonEmptyEnumerable{T}"/>.
/// </summary>
public static class NonEmptyEnumerableExtensions
{
    /// <summary>
    /// Wraps <paramref name="source"/> as a <see cref="NonEmptyEnumerable{T}"/>, or returns
    /// <c>null</c> when the sequence is null or empty.
    /// </summary>
    public static NonEmptyEnumerable<T>? AsNonEmpty<T>(this IEnumerable<T>? source)
        => NonEmptyEnumerable.TryCreateRange(source);

    /// <summary>
    /// Wraps <paramref name="source"/> as a <see cref="NonEmptyEnumerable{T}"/>, throwing
    /// <see cref="ArgumentException"/> when the sequence is null or empty.
    /// </summary>
    public static NonEmptyEnumerable<T> ToNonEmpty<T>(this IEnumerable<T>? source)
        => NonEmptyEnumerable.CreateRange(source);

    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this NonEmptyEnumerable<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        var src = source.AsSpan();
        for (var i = 0; i < src.Length; i++) buffer[i] = selector(src[i]);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, TResult> selector)
    {
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Select(selector);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        for (var i = 0; i < source.Count; i++) buffer[i] = selector(source[i]);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this NonEmptyEnumerable<T> source,
        Func<T, int, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        var src = source.AsSpan();
        for (var i = 0; i < src.Length; i++) buffer[i] = selector(src[i], i);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, int, TResult> selector)
    {
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Select(selector);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        for (var i = 0; i < source.Count; i++) buffer[i] = selector(source[i], i);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<TResult> SelectMany<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, INonEmptyEnumerable<TResult>> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var flattened = Enumerable.SelectMany(source, item => (IEnumerable<TResult>)selector(item)).ToArray();
        return NonEmptyEnumerable<TResult>.FromValidatedArray(flattened);
    }

    public static NonEmptyEnumerable<T> Distinct<T>(this INonEmptyEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var distinct = Enumerable.Distinct(source).ToArray();
        return NonEmptyEnumerable<T>.FromValidatedArray(distinct);
    }

    public static NonEmptyEnumerable<T> Concat<T>(this NonEmptyEnumerable<T> source, params ReadOnlySpan<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);

        // LINQ's Concat can't take a span; materializing one would cost an extra copy.
        var buffer = new T[source.Count + items.Length];
        source.AsSpan().CopyTo(buffer);
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, params ReadOnlySpan<T> items)
    {
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Concat(items);
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new T[source.Count + items.Length];
        for (var i = 0; i < source.Count; i++) buffer[i] = source[i];
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);

        return NonEmptyEnumerable<T>.FromValidatedArray(Enumerable.Concat(source, items).ToArray());
    }

    public static NonEmptyEnumerable<T> Flatten<T>(this INonEmptyEnumerable<INonEmptyEnumerable<T>> source)
        => source.SelectMany(inner => inner);

    /// <summary>
    /// Prepends <paramref name="head"/> to the concatenation of <paramref name="tails"/>
    /// in order. Null entries in <paramref name="tails"/> are treated as empty.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this T head, params IEnumerable<T>[] tails)
    {
        ArgumentNullException.ThrowIfNull(tails);
        return [head, ..tails.SelectMany(items => items ?? [])];
    }
}
