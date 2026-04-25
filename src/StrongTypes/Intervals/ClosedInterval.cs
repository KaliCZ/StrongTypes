#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval bounded on both sides. <c>Start</c> and <c>End</c> are non-nullable and the invariant <c>Start &lt;= End</c> always holds.</summary>
/// <typeparam name="T">The endpoint type.</typeparam>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct ClosedInterval<T> : IEquatable<ClosedInterval<T>>
    where T : struct, IComparable<T>
{
    private ClosedInterval(T start, T end)
    {
        Start = start;
        End = end;
    }

    public T Start { get; }
    public T End { get; }

    /// <summary>Wraps the pair, or returns <c>null</c> when <paramref name="start"/> is greater than <paramref name="end"/>.</summary>
    [Pure]
    public static ClosedInterval<T>? TryCreate(T start, T end) =>
        start.CompareTo(end) <= 0 ? new ClosedInterval<T>(start, end) : null;

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than <paramref name="end"/>.</exception>
    [Pure]
    public static ClosedInterval<T> Create(T start, T end) =>
        TryCreate(start, end)
            ?? throw new ArgumentException($"ClosedInterval<{typeof(T).Name}> requires start <= end.", nameof(start));

    public void Deconstruct(out T start, out T end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) =>
        Start.CompareTo(value) <= 0 && value.CompareTo(End) <= 0;

    [Pure]
    public bool Equals(ClosedInterval<T> other) =>
        EqualityComparer<T>.Default.Equals(Start, other.Start)
        && EqualityComparer<T>.Default.Equals(End, other.End);

    [Pure]
    public override bool Equals(object? obj) => obj is ClosedInterval<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(ClosedInterval<T> left, ClosedInterval<T> right) => left.Equals(right);
    public static bool operator !=(ClosedInterval<T> left, ClosedInterval<T> right) => !left.Equals(right);

    [Pure]
    public override string ToString() => $"[{Start}, {End}]";
}
