#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval where both endpoints are optional. The invariant <c>Start &lt;= End</c> holds whenever both endpoints are present; either or both may be <c>null</c>.</summary>
/// <typeparam name="T">The endpoint type.</typeparam>
/// <remarks>The deconstructor enables pattern matching over the four nullability cases via <c>(null, null)</c>, <c>(null, { } end)</c>, <c>({ } start, null)</c>, <c>({ } start, { } end)</c>.</remarks>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct Interval<T> : IEquatable<Interval<T>>
    where T : struct, IComparable<T>
{
    private Interval(T? start, T? end)
    {
        Start = start;
        End = end;
    }

    public T? Start { get; }
    public T? End { get; }

    /// <summary>Wraps the pair, or returns <c>null</c> when both endpoints are present and <paramref name="start"/> is greater than <paramref name="end"/>.</summary>
    [Pure]
    public static Interval<T>? TryCreate(T? start, T? end)
    {
        if (start.HasValue && end.HasValue && start.Value.CompareTo(end.Value) > 0)
        {
            return null;
        }
        return new Interval<T>(start, end);
    }

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException">Both endpoints are present and <paramref name="start"/> is greater than <paramref name="end"/>.</exception>
    [Pure]
    public static Interval<T> Create(T? start, T? end) =>
        TryCreate(start, end)
            ?? throw new ArgumentException($"Interval<{typeof(T).Name}> requires start <= end when both endpoints are present.", nameof(start));

    public void Deconstruct(out T? start, out T? end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) =>
        (!Start.HasValue || Start.Value.CompareTo(value) <= 0)
        && (!End.HasValue || value.CompareTo(End.Value) <= 0);

    [Pure]
    public bool Equals(Interval<T> other) =>
        EqualityComparer<T?>.Default.Equals(Start, other.Start)
        && EqualityComparer<T?>.Default.Equals(End, other.End);

    [Pure]
    public override bool Equals(object? obj) => obj is Interval<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(Interval<T> left, Interval<T> right) => left.Equals(right);
    public static bool operator !=(Interval<T> left, Interval<T> right) => !left.Equals(right);

    [Pure]
    public override string ToString() => (Start, End) switch
    {
        (null, null) => "(-∞, +∞)",
        (null, { } e) => $"(-∞, {e}]",
        ({ } s, null) => $"[{s}, +∞)",
        ({ } s, { } e) => $"[{s}, {e}]",
    };
}
