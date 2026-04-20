#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// Factory helpers for <see cref="NonEmptyEnumerable{T}"/>.
/// <para>
/// Use <see cref="Of{T}(T, ReadOnlySpan{T})"/> (or its tail-sequence overload) when the
/// non-empty shape is known statically — they construct without validation.
/// Use <see cref="TryCreate{T}(IEnumerable{T})"/> (nullable) or <see cref="Create{T}(IEnumerable{T})"/>
/// (throwing) when wrapping an existing sequence whose emptiness must be checked.
/// </para>
/// </summary>
public static class NonEmptyEnumerable
{
    /// <summary>
    /// Creates a non-empty enumerable from a head element and zero or more tail elements.
    /// </summary>
    /// <remarks>
    /// Named <c>Of</c> rather than <c>Create</c> so that passing a single array — e.g.
    /// <c>NonEmptyEnumerable.Create(someArray)</c> — unambiguously reaches the sequence-validating
    /// <see cref="Create{T}(IEnumerable{T})"/> instead of silently wrapping the array as a
    /// single-element <c>NonEmptyEnumerable&lt;T[]&gt;</c>.
    /// </remarks>
    public static NonEmptyEnumerable<T> Of<T>(T head, params ReadOnlySpan<T> tail)
    {
        var buffer = new T[tail.Length + 1];
        buffer[0] = head;
        tail.CopyTo(buffer.AsSpan(1));
        return NonEmptyEnumerable<T>.FromValidatedArray(buffer);
    }

    /// <summary>
    /// Creates a non-empty enumerable from a head element and a tail sequence.
    /// </summary>
    public static NonEmptyEnumerable<T> Of<T>(T head, IEnumerable<T> tail)
    {
        ArgumentNullException.ThrowIfNull(tail);
        return NonEmptyEnumerable<T>.FromValidatedArray([head, .. tail]);
    }

    /// <summary>
    /// Returns a <see cref="NonEmptyEnumerable{T}"/> wrapping <paramref name="values"/>, or
    /// <c>null</c> if <paramref name="values"/> is null or empty. When <paramref name="values"/>
    /// is already a <see cref="NonEmptyEnumerable{T}"/>, it is returned as-is — callers may
    /// rely on this for idempotent wrapping.
    /// </summary>
    public static NonEmptyEnumerable<T>? TryCreate<T>(IEnumerable<T>? values) =>
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
    public static NonEmptyEnumerable<T> Create<T>(IEnumerable<T>? values)
        => TryCreate(values)
           ?? throw new ArgumentException("You cannot create NonEmptyEnumerable from a null or empty sequence.", nameof(values));
}

/// <summary>
/// A read-only list of <typeparamref name="T"/> guaranteed to contain at least one element.
/// Construct via <see cref="NonEmptyEnumerable.Of{T}(T, ReadOnlySpan{T})"/> or its overloads.
/// </summary>
[JsonConverter(typeof(NonEmptyEnumerableJsonConverterFactory))]
[DebuggerTypeProxy(typeof(NonEmptyEnumerableDebugView<>))]
[DebuggerDisplay("Count = {Count}")]
public sealed class NonEmptyEnumerable<T> : INonEmptyEnumerable<T>, IEquatable<NonEmptyEnumerable<T>>
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

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    /// <summary>
    /// Returns the underlying buffer as a <see cref="ReadOnlySpan{T}"/>. Useful for
    /// allocation-free iteration; the span must not outlive the enumerable.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => _values;

    #region Equality

    public bool Equals(NonEmptyEnumerable<T>? other)
        => other is not null && _values.AsSpan().SequenceEqual(other._values, EqualityComparer<T>.Default);

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
