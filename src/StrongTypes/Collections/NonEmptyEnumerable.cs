#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// Factory helpers for <see cref="NonEmptyEnumerable{T}"/>.
/// <para>
/// Use <see cref="Create{T}(ReadOnlySpan{T})"/> for variadic construction
/// (<c>Create(1, 2, 3)</c>) or the collection-expression form
/// (<c>NonEmptyEnumerable&lt;int&gt; list = [1, 2, 3];</c>).
/// Use <see cref="TryCreateRange{T}(IEnumerable{T})"/> (nullable) or
/// <see cref="CreateRange{T}(IEnumerable{T})"/> (throwing) when wrapping a runtime sequence
/// (<see cref="List{T}"/>, a LINQ query, etc.) whose emptiness must be checked.
/// </para>
/// </summary>
public static class NonEmptyEnumerable
{
    /// <summary>
    /// Returns a <see cref="NonEmptyEnumerable{T}"/> wrapping <paramref name="values"/>, or
    /// <c>null</c> if <paramref name="values"/> is null or empty. When <paramref name="values"/>
    /// is already a <see cref="NonEmptyEnumerable{T}"/>, it is returned as-is — callers may
    /// rely on this for idempotent wrapping.
    /// </summary>
    public static NonEmptyEnumerable<T>? TryCreateRange<T>(IEnumerable<T>? values) =>
        values switch
        {
            null => null,
            NonEmptyEnumerable<T> already => already,
            T[] { Length: 0 } => null,
            T[] array => NonEmptyEnumerable<T>.FromValidatedArray([.. array]),
            List<T> { Count: 0 } => null,
            List<T> list => NonEmptyEnumerable<T>.FromValidatedArray(CollectionsMarshal.AsSpan(list).ToArray()),
            IReadOnlyCollection<T> { Count: 0 } => null,
            // Fallback: materialize once via ToArray — the resulting array is already a
            // private copy, so hand it straight to the constructor without a second copy.
            _ => values.ToArray() is { Length: > 0 } arr ? NonEmptyEnumerable<T>.FromValidatedArray(arr) : null
        };

    /// <summary>
    /// Returns a <see cref="NonEmptyEnumerable{T}"/> wrapping <paramref name="values"/>.
    /// Throws <see cref="ArgumentException"/> if <paramref name="values"/> is null or empty.
    /// </summary>
    public static NonEmptyEnumerable<T> CreateRange<T>(IEnumerable<T>? values)
        => TryCreateRange(values)
           ?? throw new ArgumentException("You cannot create NonEmptyEnumerable from a null or empty sequence.", nameof(values));

    /// <summary>
    /// Returns a <see cref="NonEmptyEnumerable{T}"/> wrapping <paramref name="values"/>.
    /// Throws <see cref="ArgumentException"/> if <paramref name="values"/> is empty.
    /// </summary>
    /// <remarks>
    /// This overload is the <see cref="System.Runtime.CompilerServices.CollectionBuilderAttribute"/>
    /// target for <see cref="NonEmptyEnumerable{T}"/>, enabling collection-expression syntax
    /// (<c>NonEmptyEnumerable&lt;int&gt; list = [1, 2, 3];</c>). An empty collection expression
    /// (<c>[]</c>) throws at runtime — the compiler has no way to reject it statically.
    /// <para>
    /// Named differently from <see cref="CreateRange{T}(IEnumerable{T})"/> so array arguments
    /// resolve unambiguously: <c>new int[] {1,2,3}</c> is implicitly convertible to both
    /// <see cref="ReadOnlySpan{T}"/> and <see cref="IEnumerable{T}"/>, and the pair would
    /// otherwise be indistinguishable at the call site. Follows the BCL precedent of
    /// <c>ImmutableArray.Create(params ROS&lt;T&gt;)</c> + <c>ImmutableArray.CreateRange(IEnumerable&lt;T&gt;)</c>.
    /// </para>
    /// </remarks>
    public static NonEmptyEnumerable<T> Create<T>(params ReadOnlySpan<T> values)
    {
        if (values.IsEmpty)
            throw new ArgumentException("You cannot create NonEmptyEnumerable from an empty sequence.", nameof(values));
        return NonEmptyEnumerable<T>.FromValidatedArray(values.ToArray());
    }
}

/// <summary>
/// A read-only list of <typeparamref name="T"/> guaranteed to contain at least one element.
/// Construct via <see cref="NonEmptyEnumerable.Create{T}(ReadOnlySpan{T})"/>,
/// <see cref="NonEmptyEnumerable.CreateRange{T}(IEnumerable{T})"/>, or a collection expression
/// (<c>NonEmptyEnumerable&lt;int&gt; list = [1, 2, 3];</c>).
/// </summary>
[JsonConverter(typeof(NonEmptyEnumerableJsonConverterFactory))]
[CollectionBuilder(typeof(NonEmptyEnumerable), nameof(NonEmptyEnumerable.Create))]
[DebuggerTypeProxy(typeof(NonEmptyEnumerableDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public sealed class NonEmptyEnumerable<T> : INonEmptyEnumerable<T>, ICollection<T>, IEquatable<NonEmptyEnumerable<T>>
{
    // Owned buffer — factories copy inputs, callers can never mutate us from outside.
    private readonly T[] _values;

    private NonEmptyEnumerable(T[] values)
    {
        _values = values;
    }

    /// <summary>
    /// Wraps a caller-owned array that is already known to be non-empty and safe from
    /// outside mutation. Internal so every public entry point goes through a validated
    /// factory that does its own copy.
    /// </summary>
    internal static NonEmptyEnumerable<T> FromValidatedArray(T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        // Release-safe — the non-empty invariant is load-bearing and all callers live in
        // this assembly; a silent bug would surface as IndexOutOfRangeException on Head.
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);
        return new NonEmptyEnumerable<T>(values);
    }

    public T Head => _values[0];

    // `field` caches the first materialization — prior implementation allocated a fresh
    // ArraySegment (boxed via IReadOnlyList) on every access.
    public IReadOnlyList<T> Tail => field ??= new ArraySegment<T>(_values, 1, _values.Length - 1);

    public int Count => _values.Length;

    public T this[int index] => _values[index];

    /// <summary>
    /// Returns a struct enumerator so <c>foreach (var x in nonEmpty)</c> on the concrete type
    /// is allocation-free. Calls via <see cref="IEnumerable{T}"/> still box through the
    /// explicit interface impls — unavoidable for the interface contract.
    /// </summary>
    public Enumerator GetEnumerator() => new(_values);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns the underlying buffer as a <see cref="ReadOnlySpan{T}"/>. Useful for
    /// allocation-free iteration; the span must not outlive the enumerable.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => _values;

    #region ICollection<T> — read-only implementation

    // ICollection<T> is implemented primarily so LINQ and System.Text.Json recognize this
    // type for their fast paths (ToArray pre-sizing, Concat count-aware copying, etc.).
    // The mutator methods are standard read-only throws — same pattern ReadOnlyCollection<T>,
    // ImmutableArray<T>, and Array use for this purpose.

    bool ICollection<T>.IsReadOnly => true;

    public bool Contains(T item) => Array.IndexOf(_values, item) >= 0;

    public void CopyTo(T[] array, int arrayIndex)
        => _values.AsSpan().CopyTo(array.AsSpan(arrayIndex));

    void ICollection<T>.Add(T item) => throw new NotSupportedException("NonEmptyEnumerable is read-only.");
    void ICollection<T>.Clear() => throw new NotSupportedException("NonEmptyEnumerable is read-only.");
    bool ICollection<T>.Remove(T item) => throw new NotSupportedException("NonEmptyEnumerable is read-only.");

    #endregion

    public struct Enumerator : IEnumerator<T>
    {
        private readonly T[] _values;
        private int _index;

        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        public readonly T Current => _values[_index];
        readonly object? IEnumerator.Current => Current;

        public bool MoveNext() => ++_index < _values.Length;

        public void Reset() => _index = -1;

        public readonly void Dispose() { }
    }

    #region Equality

    public bool Equals(NonEmptyEnumerable<T>? other)
        // No explicit comparer — the parameterless SequenceEqual takes the SIMD-vectorized path
        // for bitwise-equatable T (primitives, enums, Guid, unmanaged structs) and still falls
        // back to EqualityComparer<T>.Default.Equals for everything else.
        => other is not null && _values.AsSpan().SequenceEqual(other._values);

    public override bool Equals(object? obj) => obj is NonEmptyEnumerable<T> other && Equals(other);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var value in _values) hash.Add(value);
        return hash.ToHashCode();
    }

    #endregion

    public override string ToString() => $"[{string.Join(", ", _values)}]";
}

internal sealed class NonEmptyEnumerableDebugView<T>(NonEmptyEnumerable<T> source)
{
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public T[] Items => [.. source];
}
