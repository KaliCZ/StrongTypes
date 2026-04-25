#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval whose upper endpoint is required and whose lower endpoint is optional. The invariant <c>Start &lt;= End</c> holds whenever <c>Start</c> is present.</summary>
/// <typeparam name="T">The endpoint type.</typeparam>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct IntervalUntil<T> : IEquatable<IntervalUntil<T>>
    where T : struct, IComparable<T>
{
    private IntervalUntil(T? start, T end)
    {
        Start = start;
        End = end;
    }

    public T? Start { get; }
    public T End { get; }

    /// <summary>Wraps the pair, or returns <c>null</c> when <paramref name="start"/> is present and is greater than <paramref name="end"/>.</summary>
    [Pure]
    public static IntervalUntil<T>? TryCreate(T? start, T end)
    {
        if (start.HasValue && start.Value.CompareTo(end) > 0)
        {
            return null;
        }
        return new IntervalUntil<T>(start, end);
    }

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is present and is greater than <paramref name="end"/>.</exception>
    [Pure]
    public static IntervalUntil<T> Create(T? start, T end) =>
        TryCreate(start, end)
            ?? throw new ArgumentException($"IntervalUntil<{typeof(T).Name}> requires start <= end when start is present.", nameof(start));

    public void Deconstruct(out T? start, out T end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) =>
        (!Start.HasValue || Start.Value.CompareTo(value) <= 0)
        && value.CompareTo(End) <= 0;

    [Pure]
    public bool Equals(IntervalUntil<T> other) =>
        EqualityComparer<T?>.Default.Equals(Start, other.Start)
        && EqualityComparer<T>.Default.Equals(End, other.End);

    [Pure]
    public override bool Equals(object? obj) => obj is IntervalUntil<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(IntervalUntil<T> left, IntervalUntil<T> right) => left.Equals(right);
    public static bool operator !=(IntervalUntil<T> left, IntervalUntil<T> right) => !left.Equals(right);

    [Pure]
    public override string ToString() =>
        Start.HasValue ? $"[{Start.Value}, {End}]" : $"(-∞, {End}]";
}
