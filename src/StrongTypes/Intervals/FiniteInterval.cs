#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval bounded on both sides. <c>Start</c> and <c>End</c> are non-nullable and the invariant <c>Start &lt;= End</c> always holds. Endpoints are inclusive by default; an endpoint created with <c>startInclusive: false</c> / <c>endInclusive: false</c> is excluded from membership. Equal endpoints form a single-value interval and require both endpoints inclusive.</summary>
/// <typeparam name="T">The endpoint type.</typeparam>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct FiniteInterval<T> : IEquatable<FiniteInterval<T>>
    where T : struct, IComparable<T>
{
    // Inverted so default(FiniteInterval<T>) is the valid single-value interval [default, default].
    private readonly bool _startExclusive;
    private readonly bool _endExclusive;

    private FiniteInterval(T start, T end)
    {
        // EF materializes through this ctor, so this guard validates the interval on read.
        if (start.CompareTo(end) > 0) throw IntervalHelpers.Reversed($"FiniteInterval<{typeof(T).Name}>", start, end);
        Start = start;
        End = end;
    }

    public T Start { get; private init; }
    public T End { get; private init; }

    public bool StartInclusive { get => !_startExclusive; private init => _startExclusive = !value; }
    public bool EndInclusive { get => !_endExclusive; private init => _endExclusive = !value; }

    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: <paramref name="start"/> greater than <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static FiniteInterval<T>? TryCreate(T start, T end, bool startInclusive = true, bool endInclusive = true) =>
        IntervalHelpers.IsValidOrder<T>(start, end, startInclusive, endInclusive)
            ? new FiniteInterval<T>(start, end) { StartInclusive = startInclusive, EndInclusive = endInclusive }
            : null;

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static FiniteInterval<T> Create(T start, T end, bool startInclusive = true, bool endInclusive = true) =>
        TryCreate(start, end, startInclusive, endInclusive)
            ?? throw new ArgumentException(
                $"FiniteInterval<{typeof(T).Name}> requires start <= end, with equal endpoints only when both are inclusive.",
                nameof(start));

    public void Deconstruct(out T start, out T end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) => IntervalHelpers.Contains(Start, End, StartInclusive, EndInclusive, value);

    /// <summary>Whether this interval and <paramref name="other"/> share at least one value. Intervals that touch at a shared endpoint overlap only when both touching bounds are inclusive.</summary>
    [Pure]
    public bool Overlaps(Interval<T> other) => IntervalHelpers.Overlaps(this, other);

    /// <summary>The intersection of this interval and <paramref name="other"/>, or <c>null</c> when they are disjoint.</summary>
    [Pure]
    public FiniteInterval<T>? GetOverlap(Interval<T> other) => IntervalHelpers.GetOverlap(this, other)?.AsFinite();

    [Pure]
    public bool Equals(FiniteInterval<T> other) =>
        EqualityComparer<T>.Default.Equals(Start, other.Start)
        && EqualityComparer<T>.Default.Equals(End, other.End)
        && StartInclusive == other.StartInclusive
        && EndInclusive == other.EndInclusive;

    [Pure]
    public override bool Equals(object? obj) => obj is FiniteInterval<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End, StartInclusive, EndInclusive);

    public static bool operator ==(FiniteInterval<T> left, FiniteInterval<T> right) => left.Equals(right);
    public static bool operator !=(FiniteInterval<T> left, FiniteInterval<T> right) => !left.Equals(right);

    /// <summary>Widens to <see cref="IntervalFrom{T}"/> by relaxing the upper bound to optional. Always succeeds.</summary>
    public static implicit operator IntervalFrom<T>(FiniteInterval<T> value) =>
        IntervalFrom<T>.Create(value.Start, value.End, value.StartInclusive, value.EndInclusive);

    /// <summary>Widens to <see cref="IntervalUntil{T}"/> by relaxing the lower bound to optional. Always succeeds.</summary>
    public static implicit operator IntervalUntil<T>(FiniteInterval<T> value) =>
        IntervalUntil<T>.Create(value.Start, value.End, value.StartInclusive, value.EndInclusive);

    /// <summary>Widens to <see cref="Interval{T}"/> by relaxing both bounds to optional. Always succeeds.</summary>
    public static implicit operator Interval<T>(FiniteInterval<T> value) =>
        Interval<T>.Create(value.Start, value.End, value.StartInclusive, value.EndInclusive);

    [Pure]
    public override string ToString() => IntervalHelpers.Format<T>(Start, End, StartInclusive, EndInclusive);
}

/// <summary>Factory helpers for <see cref="FiniteInterval{T}"/> with inferred type arguments.</summary>
public static class FiniteInterval
{
    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: <paramref name="start"/> greater than <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static FiniteInterval<T>? TryCreate<T>(T start, T end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => FiniteInterval<T>.TryCreate(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static FiniteInterval<T> Create<T>(T start, T end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => FiniteInterval<T>.Create(start, end, startInclusive, endInclusive);
}
