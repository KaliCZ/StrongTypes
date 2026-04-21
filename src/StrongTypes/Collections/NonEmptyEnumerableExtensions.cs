#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

    /// <summary>
    /// Maps every element and returns a <see cref="NonEmptyEnumerable{TResult}"/>.
    /// </summary>
    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this NonEmptyEnumerable<T> source,
        Func<T, TResult> selector)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        // Span indexer elides the interface virtual-dispatch and repeated bounds checks.
        var buffer = new TResult[source.Count];
        var src = source.AsSpan();
        for (var i = 0; i < src.Length; i++) buffer[i] = selector(src[i]);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Maps every element and returns a <see cref="NonEmptyEnumerable{TResult}"/>.
    /// </summary>
    public static NonEmptyEnumerable<TResult> Select<T, TResult>(
        this INonEmptyEnumerable<T> source,
        Func<T, TResult> selector)
    {
        // Route interface-typed callers with a concrete backing to the span fast path.
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Select(selector);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);

        var buffer = new TResult[source.Count];
        for (var i = 0; i < source.Count; i++) buffer[i] = selector(source[i]);
        return NonEmptyEnumerable<TResult>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Maps every element with its index and returns a <see cref="NonEmptyEnumerable{TResult}"/>.
    /// </summary>
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

    /// <summary>
    /// Maps every element with its index and returns a <see cref="NonEmptyEnumerable{TResult}"/>.
    /// </summary>
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

    /// <summary>
    /// Projects each element to a non-empty sequence and concatenates them into a single
    /// <see cref="NonEmptyEnumerable{TResult}"/>.
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
    /// Returns the distinct elements of <paramref name="source"/>.
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
    public static NonEmptyEnumerable<T> Concat<T>(this NonEmptyEnumerable<T> source, params ReadOnlySpan<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Hand-rolled because LINQ's Concat doesn't accept a span — converting would
        // cost an extra array allocation to save nothing.
        var buffer = new T[source.Count + items.Length];
        source.AsSpan().CopyTo(buffer);
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Concatenates additional items onto a non-empty sequence.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, params ReadOnlySpan<T> items)
    {
        if (source is NonEmptyEnumerable<T> concrete) return concrete.Concat(items);
        ArgumentNullException.ThrowIfNull(source);

        var buffer = new T[source.Count + items.Length];
        for (var i = 0; i < source.Count; i++) buffer[i] = source[i];
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Concatenates an additional sequence onto a non-empty sequence.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);

        return NonEmptyEnumerable<T>.FromValidatedArray(Enumerable.Concat(source, items).ToArray());
    }

    /// <summary>
    /// Flattens a non-empty sequence of non-empty sequences into a single
    /// <see cref="NonEmptyEnumerable{T}"/>.
    /// </summary>
    public static NonEmptyEnumerable<T> Flatten<T>(this INonEmptyEnumerable<INonEmptyEnumerable<T>> source)
        => source.SelectMany(inner => inner);

    /// <summary>
    /// Produces a <see cref="NonEmptyEnumerable{T}"/> whose first element is
    /// <paramref name="head"/> and whose remaining elements are the concatenation of
    /// <paramref name="tails"/> in order. <c>null</c> entries in <paramref name="tails"/>
    /// are treated as empty. The non-empty invariant holds unconditionally because
    /// <paramref name="head"/> is always present.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this T head, params IEnumerable<T>[] tails)
    {
        ArgumentNullException.ThrowIfNull(tails);

        // Fast path: if every non-null tail can report its count cheaply, allocate the
        // output buffer exactly once and copy each tail in place.
        var total = 1;
        var allKnown = true;
        foreach (var tail in tails)
        {
            if (tail is null) continue;
            if (tail.TryGetNonEnumeratedCount(out var c)) total += c;
            else { allKnown = false; break; }
        }

        if (allKnown)
        {
            var buffer = new T[total];
            buffer[0] = head;
            var offset = 1;
            foreach (var tail in tails)
            {
                if (tail is null) continue;
                offset += CopyInto(tail, buffer, offset);
            }
            return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
        }

        // Fallback: List<T> growth. AddRange reaches Buffer.Memmove for ICollection<T>
        // tails; the final ToArray produces the owned buffer the invariant requires.
        var list = new List<T> { head };
        foreach (var tail in tails)
            if (tail is not null) list.AddRange(tail);
        return NonEmptyEnumerable<T>.FromValidatedArray(list.ToArray());
    }

    // Copies source into buffer starting at offset and returns the number of elements
    // written. Dispatches to the fastest available path for the source's runtime type —
    // span copy for array/NonEmptyEnumerable/List, ICollection.CopyTo for everything else
    // shape-aware, foreach as a last resort.
    private static int CopyInto<T>(IEnumerable<T> source, T[] buffer, int offset)
    {
        switch (source)
        {
            case T[] array:
                array.AsSpan().CopyTo(buffer.AsSpan(offset));
                return array.Length;
            case NonEmptyEnumerable<T> ne:
                ne.AsSpan().CopyTo(buffer.AsSpan(offset));
                return ne.Count;
            case List<T> list:
                CollectionsMarshal.AsSpan(list).CopyTo(buffer.AsSpan(offset));
                return list.Count;
            case ICollection<T> coll:
                coll.CopyTo(buffer, offset);
                return coll.Count;
            default:
                // Only reachable from the List<T> fallback path — TryGetNonEnumeratedCount
                // can succeed for IReadOnlyCollection<T>-without-ICollection<T>, but those
                // shapes are vanishingly rare and the enumerator path is correct either way.
                var i = 0;
                foreach (var item in source) { buffer[offset + i] = item; i++; }
                return i;
        }
    }
}
