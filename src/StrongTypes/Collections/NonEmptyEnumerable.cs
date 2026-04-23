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

/// <summary>Factory helpers for <see cref="NonEmptyEnumerable{T}"/>.</summary>
public static class NonEmptyEnumerable
{
    /// <summary>Wraps <paramref name="values"/> as a <see cref="NonEmptyEnumerable{T}"/>, or returns <c>null</c> when the sequence is null or empty.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The source sequence.</param>
    public static NonEmptyEnumerable<T>? TryCreateRange<T>(IEnumerable<T>? values) =>
        values switch
        {
            null => null,
            NonEmptyEnumerable<T> already => already,
            T[] { Length: 0 } => null,
            T[] array => NonEmptyEnumerable<T>.FromValidatedArray([.. array]),
            List<T> { Count: 0 } => null,
            List<T> list => NonEmptyEnumerable<T>.FromValidatedArray(CollectionsMarshal.AsSpan(list).ToArray()),
            IReadOnlyList<T> { Count: 0 } => null,
            IReadOnlyList<T> list => IndexerCopy(list),
            IReadOnlyCollection<T> { Count: 0 } => null,
            _ => values.ToArray() is { Length: > 0 } arr ? NonEmptyEnumerable<T>.FromValidatedArray(arr) : null
        };

    private static NonEmptyEnumerable<T> IndexerCopy<T>(IReadOnlyList<T> list)
    {
        var buffer = new T[list.Count];
        for (var i = 0; i < buffer.Length; i++) buffer[i] = list[i];
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>Wraps <paramref name="values"/> as a <see cref="NonEmptyEnumerable{T}"/>.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The source sequence.</param>
    /// <exception cref="ArgumentException"><paramref name="values"/> is <c>null</c> or empty.</exception>
    public static NonEmptyEnumerable<T> CreateRange<T>(IEnumerable<T>? values)
        => TryCreateRange(values)
           ?? throw new ArgumentException("You cannot create NonEmptyEnumerable from a null or empty sequence.", nameof(values));

    /// <summary>Creates a <see cref="NonEmptyEnumerable{T}"/> from the supplied elements. Also backs the collection-expression syntax (<c>NonEmptyEnumerable&lt;int&gt; list = [1, 2, 3];</c>).</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="values">The elements to wrap.</param>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty.</exception>
    public static NonEmptyEnumerable<T> Create<T>(params ReadOnlySpan<T> values)
    {
        if (values.IsEmpty)
            throw new ArgumentException("You cannot create NonEmptyEnumerable from an empty sequence.", nameof(values));
        return NonEmptyEnumerable<T>.FromValidatedArray(values.ToArray());
    }
}

/// <summary>A read-only list of <typeparamref name="T"/> guaranteed to contain at least one element.</summary>
/// <typeparam name="T">The element type.</typeparam>
/// <remarks>Construct via <see cref="NonEmptyEnumerable.Create{T}(ReadOnlySpan{T})"/>, <see cref="NonEmptyEnumerable.CreateRange{T}(IEnumerable{T})"/>, or a collection expression (<c>NonEmptyEnumerable&lt;int&gt; list = [1, 2, 3];</c>).</remarks>
[JsonConverter(typeof(NonEmptyEnumerableJsonConverterFactory))]
[CollectionBuilder(typeof(NonEmptyEnumerable), nameof(NonEmptyEnumerable.Create))]
[DebuggerTypeProxy(typeof(NonEmptyEnumerableDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public sealed class NonEmptyEnumerable<T> : INonEmptyEnumerable<T>, ICollection<T>, IEquatable<NonEmptyEnumerable<T>>
{
    internal readonly T[] _values;

    private NonEmptyEnumerable(T[] values)
    {
        _values = values;
    }

    internal static NonEmptyEnumerable<T> FromValidatedArray(T[] values)
    {
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfZero(values.Length);
        return new NonEmptyEnumerable<T>(values);
    }

    public T Head => _values[0];

    public IReadOnlyList<T> Tail => field ??= new ArraySegment<T>(_values, 1, _values.Length - 1);

    public int Count => _values.Length;

    public T this[int index] => _values[index];

    public Enumerator GetEnumerator() => new(_values);

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>Returns the elements as a <see cref="ReadOnlySpan{T}"/>.</summary>
    /// <remarks>The span must not outlive the enumerable.</remarks>
    public ReadOnlySpan<T> AsSpan() => _values;

    #region ICollection<T> — read-only implementation

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
