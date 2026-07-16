#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An interval where both endpoints are optional. The invariant <c>Start &lt;= End</c> holds whenever both endpoints are present; either or both may be <c>null</c>. Endpoints are inclusive by default; an endpoint created with <c>startInclusive: false</c> / <c>endInclusive: false</c> is excluded from membership. Equal endpoints form a single-value interval and require both endpoints inclusive.</summary>
/// <remarks>The deconstructor enables pattern matching over the four nullability cases via <c>(null, null)</c>, <c>(null, { } end)</c>, <c>({ } start, null)</c>, <c>({ } start, { } end)</c>.</remarks>
[JsonConverter(typeof(IntervalJsonConverterFactory))]
public readonly struct Interval<T> : IEquatable<Interval<T>>
    where T : struct, IComparable<T>
{
    // Inverted so default(Interval<T>) is the valid unbounded interval (-∞, +∞).
    private readonly bool _startExclusive;
    private readonly bool _endExclusive;

    private Interval(T? start, T? end)
    {
        // EF materializes through this ctor, so this guard validates the interval on read.
        if (start is { } s && end is { } e && s.CompareTo(e) > 0) throw IntervalHelpers.Reversed($"Interval<{typeof(T).Name}>", s, e);
        Start = start;
        End = end;
    }

    public T? Start { get; private init; }
    public T? End { get; private init; }

    // An unbounded endpoint has no inclusivity; normalize to the default so equal intervals compare equal.
    public bool StartInclusive { get => !_startExclusive; private init => _startExclusive = Start.HasValue && !value; }
    public bool EndInclusive { get => !_endExclusive; private init => _endExclusive = End.HasValue && !value; }

    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: both endpoints present with <paramref name="start"/> greater than <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static Interval<T>? TryCreate(T? start, T? end, bool startInclusive = true, bool endInclusive = true) =>
        IntervalHelpers.IsValidOrder(start, end, startInclusive, endInclusive)
            ? new Interval<T>(start, end) { StartInclusive = startInclusive, EndInclusive = endInclusive }
            : null;

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException">Both endpoints are present with <paramref name="start"/> greater than <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static Interval<T> Create(T? start, T? end, bool startInclusive = true, bool endInclusive = true) =>
        TryCreate(start, end, startInclusive, endInclusive)
            ?? throw new ArgumentException(
                $"Interval<{typeof(T).Name}> requires start <= end when both endpoints are present, with equal endpoints only when both are inclusive.",
                nameof(start));

    public void Deconstruct(out T? start, out T? end) => (start, end) = (Start, End);

    [Pure]
    public bool Contains(T value) => IntervalHelpers.Contains(Start, End, StartInclusive, EndInclusive, value);

    /// <summary>Whether this interval and <paramref name="other"/> share at least one value. Intervals that touch at a shared endpoint overlap only when both touching bounds are inclusive.</summary>
    [Pure]
    public bool Overlaps(Interval<T> other) => IntervalHelpers.Overlaps(this, other);

    /// <summary>The intersection of this interval and <paramref name="other"/>, or <c>null</c> when they are disjoint.</summary>
    [Pure]
    public Interval<T>? GetOverlap(Interval<T> other) => IntervalHelpers.GetOverlap(this, other);

    [Pure]
    public bool Equals(Interval<T> other) =>
        EqualityComparer<T?>.Default.Equals(Start, other.Start)
        && EqualityComparer<T?>.Default.Equals(End, other.End)
        && StartInclusive == other.StartInclusive
        && EndInclusive == other.EndInclusive;

    [Pure]
    public override bool Equals(object? obj) => obj is Interval<T> other && Equals(other);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(Start, End, StartInclusive, EndInclusive);

    public static bool operator ==(Interval<T> left, Interval<T> right) => left.Equals(right);
    public static bool operator !=(Interval<T> left, Interval<T> right) => !left.Equals(right);

    /// <summary>Narrows to <see cref="FiniteInterval{T}"/>, or <c>null</c> when either endpoint is unbounded.</summary>
    [Pure]
    public FiniteInterval<T>? AsFinite() =>
        Start is { } start && End is { } end ? FiniteInterval<T>.TryCreate(start, end, StartInclusive, EndInclusive) : null;

    /// <summary>Narrows to <see cref="IntervalFrom{T}"/>, or <c>null</c> when the lower endpoint is unbounded.</summary>
    [Pure]
    public IntervalFrom<T>? AsFrom() =>
        Start is { } start ? IntervalFrom<T>.TryCreate(start, End, StartInclusive, EndInclusive) : null;

    /// <summary>Narrows to <see cref="IntervalUntil{T}"/>, or <c>null</c> when the upper endpoint is unbounded.</summary>
    [Pure]
    public IntervalUntil<T>? AsUntil() =>
        End is { } end ? IntervalUntil<T>.TryCreate(Start, end, StartInclusive, EndInclusive) : null;

    /// <summary>Narrows to <see cref="FiniteInterval{T}"/>.</summary>
    /// <exception cref="InvalidOperationException">Either endpoint is unbounded.</exception>
    [Pure]
    public FiniteInterval<T> ToFinite() =>
        AsFinite()
            ?? throw new InvalidOperationException($"Interval<{typeof(T).Name}> cannot narrow to FiniteInterval; an endpoint is unbounded.");

    /// <summary>Narrows to <see cref="IntervalFrom{T}"/>.</summary>
    /// <exception cref="InvalidOperationException">The lower endpoint is unbounded.</exception>
    [Pure]
    public IntervalFrom<T> ToFrom() =>
        AsFrom()
            ?? throw new InvalidOperationException($"Interval<{typeof(T).Name}> cannot narrow to IntervalFrom; start is unbounded.");

    /// <summary>Narrows to <see cref="IntervalUntil{T}"/>.</summary>
    /// <exception cref="InvalidOperationException">The upper endpoint is unbounded.</exception>
    [Pure]
    public IntervalUntil<T> ToUntil() =>
        AsUntil()
            ?? throw new InvalidOperationException($"Interval<{typeof(T).Name}> cannot narrow to IntervalUntil; end is unbounded.");

    [Pure]
    public override string ToString() => IntervalHelpers.Format(Start, End, StartInclusive, EndInclusive);
}

/// <summary>Factory helpers for <see cref="Interval{T}"/> with inferred type arguments. Only the all-<c>null</c> call needs an explicit type argument.</summary>
public static class Interval
{
    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: both endpoints present with <paramref name="start"/> greater than <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static Interval<T>? TryCreate<T>(T? start, T? end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.TryCreate(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: <paramref name="start"/> greater than a present <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static Interval<T>? TryCreate<T>(T start, T? end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.TryCreate(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: a present <paramref name="start"/> greater than <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static Interval<T>? TryCreate<T>(T? start, T end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.TryCreate(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair, or returns <c>null</c> when the pair describes a reversed or empty range: <paramref name="start"/> greater than <paramref name="end"/>, or equal endpoints with an exclusive bound.</summary>
    [Pure]
    public static Interval<T>? TryCreate<T>(T start, T end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.TryCreate(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException">Both endpoints are present with <paramref name="start"/> greater than <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static Interval<T> Create<T>(T? start, T? end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.Create(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than a present <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static Interval<T> Create<T>(T start, T? end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.Create(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException">A present <paramref name="start"/> is greater than <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static Interval<T> Create<T>(T? start, T end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.Create(start, end, startInclusive, endInclusive);

    /// <summary>Wraps the pair.</summary>
    /// <exception cref="ArgumentException"><paramref name="start"/> is greater than <paramref name="end"/>, or the endpoints are equal with an exclusive bound.</exception>
    [Pure]
    public static Interval<T> Create<T>(T start, T end, bool startInclusive = true, bool endInclusive = true)
        where T : struct, IComparable<T> => Interval<T>.Create(start, end, startInclusive, endInclusive);
}
