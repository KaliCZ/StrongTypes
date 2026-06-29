#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval whose lower endpoint is required and whose upper endpoint is optional. The invariant <c>Start &lt;= End</c> holds whenever <c>End</c> is present.</summary>
/// <typeparam name="T">The endpoint type.</typeparam>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct IntervalFrom<T> : IEquatable<IntervalFrom<T>>
    where T : struct, IComparable<T>
{
    private IntervalFrom(T start, T? end)
    {
        Start = start;
        End = end;
    }

    public T Start { get; }
    public T? End { get; }

    /// <summary>Wraps the pair, or returns <c>null</c> when <paramref name="end"/> is present and <paramref name="start"/> is greater than it.</summary>
    [Pure]
    public static IntervalFrom<T>? TryCreate(T start, T? end)
    {
        if (end.HasValue && start.CompareTo(end.Value) > 0)
        {
            return null;
        }
        return new IntervalFrom<T>(start, end);
    }

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="end"/> is present and <paramref name="start"/> is greater than it.</exception>
    [Pure]
    public static IntervalFrom<T> Create(T start, T? end) =>
        TryCreate(start, end)
            ?? throw new ArgumentException($"IntervalFrom<{typeof(T).Name}> requires start <= end when end is present.", nameof(start));

    public void Deconstruct(out T start, out T? end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) =>
        Start.CompareTo(value) <= 0
        && (!End.HasValue || value.CompareTo(End.Value) <= 0);

    [Pure]
    public bool Equals(IntervalFrom<T> other) =>
        EqualityComparer<T>.Default.Equals(Start, other.Start)
        && EqualityComparer<T?>.Default.Equals(End, other.End);

    [Pure]
    public override bool Equals(object? obj) => obj is IntervalFrom<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static bool operator ==(IntervalFrom<T> left, IntervalFrom<T> right) => left.Equals(right);
    public static bool operator !=(IntervalFrom<T> left, IntervalFrom<T> right) => !left.Equals(right);

    /// <summary>Widens to <see cref="Interval{T}"/> by relaxing the lower bound to optional. Always succeeds.</summary>
    public static implicit operator Interval<T>(IntervalFrom<T> value) => Interval<T>.Create(value.Start, value.End);

    /// <summary>Narrows to <see cref="ClosedInterval{T}"/>, or <c>null</c> when the upper endpoint is open.</summary>
    [Pure]
    public ClosedInterval<T>? AsClosed() => End.HasValue ? ClosedInterval<T>.TryCreate(Start, End.Value) : null;

    /// <summary>Narrows to <see cref="ClosedInterval{T}"/>.</summary>
    /// <exception cref="InvalidOperationException">The upper endpoint is open.</exception>
    [Pure]
    public ClosedInterval<T> ToClosed() =>
        AsClosed()
            ?? throw new InvalidOperationException($"IntervalFrom<{typeof(T).Name}> cannot narrow to ClosedInterval; end is open.");

    [Pure]
    public override string ToString() =>
        End.HasValue ? $"[{Start}, {End.Value}]" : $"[{Start}, +∞)";
}
