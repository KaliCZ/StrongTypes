#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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

        var buffer = new T[source.Count + items.Length];
        CopyNonEmptyInto(source, buffer, 0);
        items.CopyTo(buffer.AsSpan(source.Count));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Concatenates an additional sequence onto a non-empty sequence. Known array-backed
    /// or <see cref="ICollection{T}"/>-shaped inputs take a vectorized copy path; other
    /// enumerables fall back to a single-pass enumeration.
    /// </summary>
    public static NonEmptyEnumerable<T> Concat<T>(this INonEmptyEnumerable<T> source, IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(items);

        if (TryGetCount(items, out var itemCount))
        {
            var buffer = new T[source.Count + itemCount];
            CopyNonEmptyInto(source, buffer, 0);
            CopyEnumerableInto(items, buffer, source.Count);
            return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
        }

        // Unknown count: let the collection-expression lowering manage the growable buffer.
        return NonEmptyEnumerable<T>.FromValidatedArray([.. source, .. items]);
    }

    // ─── Fast-path copy helpers ──────────────────────────────────────────

    private static bool TryGetCount<T>(IEnumerable<T> source, out int count)
    {
        switch (source)
        {
            case ICollection<T> c: count = c.Count; return true;
            case IReadOnlyCollection<T> r: count = r.Count; return true;
            default: count = 0; return false;
        }
    }

    private static void CopyNonEmptyInto<T>(INonEmptyEnumerable<T> source, T[] destination, int offset)
    {
        if (source is NonEmptyEnumerable<T> concrete)
        {
            concrete.AsSpan().CopyTo(destination.AsSpan(offset));
            return;
        }
        for (var i = 0; i < source.Count; i++) destination[offset + i] = source[i];
    }

    // Takes the fastest available copy path for each known-shape source:
    //   • NonEmptyEnumerable<T>, T[], List<T>, ArraySegment<T> → Memmove-backed span copy
    //   • ICollection<T> (HashSet, LinkedList, …)             → CopyTo(T[], int)
    //   • IReadOnlyList<T>                                    → indexer loop, no allocation
    //   • Anything else                                       → single-pass enumeration
    private static void CopyEnumerableInto<T>(IEnumerable<T> source, T[] destination, int offset)
    {
        switch (source)
        {
            case NonEmptyEnumerable<T> nel:
                nel.AsSpan().CopyTo(destination.AsSpan(offset));
                return;
            case T[] array:
                array.AsSpan().CopyTo(destination.AsSpan(offset));
                return;
            case List<T> list:
                CollectionsMarshal.AsSpan(list).CopyTo(destination.AsSpan(offset));
                return;
            case ArraySegment<T> seg:
                seg.AsSpan().CopyTo(destination.AsSpan(offset));
                return;
            case ICollection<T> coll:
                coll.CopyTo(destination, offset);
                return;
            case IReadOnlyList<T> rol:
                for (var i = 0; i < rol.Count; i++) destination[offset + i] = rol[i];
                return;
            default:
                var n = 0;
                foreach (var item in source) destination[offset + n++] = item;
                return;
        }
    }
}
