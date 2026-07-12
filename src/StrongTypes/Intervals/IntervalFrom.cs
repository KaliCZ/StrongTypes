#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval whose lower endpoint is required and whose upper endpoint is optional. The invariant <c>Start &lt;= End</c> holds whenever <c>End</c> is present. Endpoints are inclusive by default; an endpoint created with <c>startInclusive: false</c> / <c>endInclusive: false</c> is excluded from membership. Equal endpoints form a single-value interval and require both endpoints inclusive.</summary>
/// <typeparam name="T">The endpoint type.</typeparam>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct IntervalFrom<T> : IEquatable<IntervalFrom<T>>
    where T : struct, IComparable<T>
{
    // Inverted so default(IntervalFrom<T>) is the valid interval [default, +∞).
    private readonly bool _startExclusive;
    private readonly bool _endExclusive;

    private IntervalFrom(T start, T? end)
    {
        // EF materializes through this ctor, so this guard validates the interval on read.
        if (end is { } e && start.CompareTo(e) > 0) throw IntervalHelpers.Reversed($"IntervalFrom<{typeof(T).Name}>", start, e);
        Start = start;
        End = end;
    }

    public T Start { get; private init; }
    public T? End { get; private init; }

    public bool StartInclusive { get => !_startExclusive; private init => _startExclusive = !value; }

    // An unbounded endpoint has no inclusivity; normalize to the default so equal intervals compare equal.
    public bool EndInclusive { get => !_endExclusive; private init => _endExclusive = End.HasValue && !value; }

    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: <paramref name="start"/> greater than a present <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static IntervalFrom<T>? TryCreate(T start, T? end, bool startInclusive = true, bool endInclusive = true) =>
        IntervalHelpers.IsValidOrder(start, end, startInclusive, endInclusive)
            ? new IntervalFrom<T>(start, end) { StartInclusive = startInclusive, EndInclusive = endInclusive }
            : null;

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than a present <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static IntervalFrom<T> Create(T start, T? end, bool startInclusive = true, bool endInclusive = true) =>
        TryCreate(start, end, startInclusive, endInclusive)
            ?? throw new ArgumentException(
                $"IntervalFrom<{typeof(T).Name}> requires start <= end when end is present, with equal endpoints only when both are inclusive.",
                nameof(start));

    public void Deconstruct(out T start, out T? end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) => IntervalHelpers.Contains(Start, End, StartInclusive, EndInclusive, value);

    /// <summary>Whether this interval and <paramref name="other"/> share at least one value. Intervals that touch at a shared endpoint overlap only when both touching bounds are inclusive.</summary>
    [Pure]
    public bool Overlaps(Interval<T> other) => IntervalHelpers.Overlaps(this, other);

    /// <summary>The intersection of this interval and <paramref name="other"/>, or <c>null</c> when they are disjoint.</summary>
    [Pure]
    public IntervalFrom<T>? GetOverlap(Interval<T> other) => IntervalHelpers.GetOverlap(this, other)?.AsFrom();

    [Pure]
    public bool Equals(IntervalFrom<T> other) =>
        EqualityComparer<T>.Default.Equals(Start, other.Start)
        && EqualityComparer<T?>.Default.Equals(End, other.End)
        && StartInclusive == other.StartInclusive
        && EndInclusive == other.EndInclusive;

    [Pure]
    public override bool Equals(object? obj) => obj is IntervalFrom<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End, StartInclusive, EndInclusive);

    public static bool operator ==(IntervalFrom<T> left, IntervalFrom<T> right) => left.Equals(right);
    public static bool operator !=(IntervalFrom<T> left, IntervalFrom<T> right) => !left.Equals(right);

    /// <summary>Widens to <see cref="Interval{T}"/> by relaxing the lower bound to optional. Always succeeds.</summary>
    public static implicit operator Interval<T>(IntervalFrom<T> value) =>
        Interval<T>.Create(value.Start, value.End, value.StartInclusive, value.EndInclusive);

    /// <summary>Narrows to <see cref="FiniteInterval{T}"/>, or <c>null</c> when the upper endpoint is unbounded.</summary>
    [Pure]
    public FiniteInterval<T>? AsFinite() =>
        End is { } end ? FiniteInterval<T>.TryCreate(Start, end, StartInclusive, EndInclusive) : null;

    /// <summary>Narrows to <see cref="FiniteInterval{T}"/>.</summary>
    /// <exception cref="InvalidOperationException">The upper endpoint is unbounded.</exception>
    [Pure]
    public FiniteInterval<T> ToFinite() =>
        AsFinite()
            ?? throw new InvalidOperationException($"IntervalFrom<{typeof(T).Name}> cannot narrow to FiniteInterval; end is unbounded.");

    [Pure]
    public override string ToString() => IntervalHelpers.Format(Start, End, StartInclusive, EndInclusive);
}

/// <summary>Factory helpers for <see cref="IntervalFrom{T}"/> with inferred type arguments.</summary>
public static class IntervalFrom
{
    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: <paramref name="start"/> greater than a present <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static IntervalFrom<T>? TryCreate<T>(T start, T? end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => IntervalFrom<T>.TryCreate(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than a present <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static IntervalFrom<T> Create<T>(T start, T? end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => IntervalFrom<T>.Create(start, end, startInclusive, endInclusive);
}
