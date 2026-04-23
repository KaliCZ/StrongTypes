#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace StrongTypes;

/// <summary>Extension methods that produce or consume <see cref="NonEmptyEnumerable{T}"/>.</summary>
public static class NonEmptyEnumerableExtensions
{
    /// <summary>Wraps <paramref name="source"/> as a <see cref="NonEmptyEnumerable{T}"/>, or returns <c>null</c> when the sequence is null or empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to wrap.</param>
    public static NonEmptyEnumerable<T>? AsNonEmpty<T>(this IEnumerable<T>? source)
        => NonEmptyEnumerable.TryCreateRange(source);

    /// <summary>Wraps <paramref name="source"/> as a <see cref="NonEmptyEnumerable{T}"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to wrap.</param>
    /// <exception cref="ArgumentException"><paramref name="source"/> is <c>null</c> or empty.</exception>
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

    public static NonEmptyEnumerable<T> Concat<T>(this NonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);
        return NonEmptyEnumerable<T>.FromValidatedArray(Enumerable.Concat(source, items).ToArray());
    }

    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);
        return NonEmptyEnumerable<T>.FromValidatedArray(Enumerable.Concat(source, items).ToArray());
    }

    public static NonEmptyEnumerable<T> Flatten<T>(this INonEmptyEnumerable<INonEmptyEnumerable<T>> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source.SelectMany(inner => inner);
    }

    /// <summary>Prepends <paramref name="head"/> to the concatenation of <paramref name="tails"/> in order.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="head">The leading element.</param>
    /// <param name="tails">Sequences concatenated after <paramref name="head"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="tails"/> or any element of it is <c>null</c>.</exception>
    public static NonEmptyEnumerable<T> Concat<T>(this T head, params IEnumerable<T>[] tails)
    {
        ArgumentNullException.ThrowIfNull(tails);
        for (var i = 0; i < tails.Length; i++)
            if (tails[i] is null)
                throw new ArgumentNullException($"{nameof(tails)}[{i}]");
        return [head, ..tails.SelectMany(items => items)];
    }

    public static NonEmptyEnumerable<T> Prepend<T>(this INonEmptyEnumerable<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);
        return item.Concat(source);
    }

    public static NonEmptyEnumerable<T> Append<T>(this INonEmptyEnumerable<T> source, T item)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Concat(item);

        var buffer = new T[source.Count + 1];
        for (var i = 0; i < source.Count; i++) buffer[i] = source[i];
        buffer[source.Count] = item;
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<T> Reverse<T>(this NonEmptyEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        var buffer = source.AsSpan().ToArray();
        Array.Reverse(buffer);
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    public static NonEmptyEnumerable<T> Reverse<T>(this INonEmptyEnumerable<T> source)
    {
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Reverse();
        ArgumentNullException.ThrowIfNull(source);
        var buffer = new T[source.Count];
        for (var i = 0; i < source.Count; i++) buffer[source.Count - 1 - i] = source[i];
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>Takes the first <paramref name="count"/> elements, or the full source when <paramref name="count"/> exceeds <c>source.Count</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to take from.</param>
    /// <param name="count">The positive number of elements to take.</param>
    public static NonEmptyEnumerable<T> Take<T>(this INonEmptyEnumerable<T> source, Positive<int> count)
    {
        ArgumentNullException.ThrowIfNull(source);
        var n = Math.Min(count.Value, source.Count);
        if (source is NonEmptyEnumerable<T> concrete)
            return NonEmptyEnumerable<T>.FromValidatedArray(concrete.AsSpan()[..n].ToArray());

        var buffer = new T[n];
        for (var i = 0; i < n; i++) buffer[i] = source[i];
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>Takes the first <paramref name="count"/> elements, or the full source when <paramref name="count"/> exceeds <c>source.Count</c>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence to take from.</param>
    /// <param name="count">The number of elements to take.</param>
    /// <exception cref="ArgumentException"><paramref name="count"/> is not positive.</exception>
    public static NonEmptyEnumerable<T> Take<T>(this INonEmptyEnumerable<T> source, int count)
        => source.Take(count.ToPositive());

    // ── Aggregation (total functions — non-emptiness makes these non-throwing) ──

    public static T Max<T>(this INonEmptyEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Enumerable.Max(source)!;
    }

    public static T Min<T>(this INonEmptyEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return Enumerable.Min(source)!;
    }

    public static T MaxBy<T, TKey>(this INonEmptyEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        return Enumerable.MaxBy(source, keySelector)!;
    }

    public static T MinBy<T, TKey>(this INonEmptyEnumerable<T> source, Func<T, TKey> keySelector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(keySelector);
        return Enumerable.MinBy(source, keySelector)!;
    }

    public static T Last<T>(this INonEmptyEnumerable<T> source)
    {
        ArgumentNullException.ThrowIfNull(source);
        return source[source.Count - 1];
    }

    public static T Aggregate<T>(this INonEmptyEnumerable<T> source, Func<T, T, T> func)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(func);
        return Enumerable.Aggregate(source, func);
    }

    /// <summary>Returns the arithmetic mean of the sequence.</summary>
    /// <typeparam name="T">A numeric type.</typeparam>
    /// <param name="source">The sequence to average.</param>
    /// <exception cref="OverflowException">The running sum overflows <typeparamref name="T"/>.</exception>
    public static T Average<T>(this INonEmptyEnumerable<T> source) where T : INumber<T>
    {
        ArgumentNullException.ThrowIfNull(source);
        var sum = T.Zero;
        foreach (var value in source) sum = checked(sum + value);
        return sum / T.CreateChecked(source.Count);
    }
}
