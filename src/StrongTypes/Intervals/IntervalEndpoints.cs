#nullable enable
using System;

namespace StrongTypes;

// A null endpoint is unbounded; a bounded endpoint is inclusive or exclusive per its flag.
internal static class IntervalEndpoints
{
    public static bool IsValidOrder<T>(T? start, T? end, bool startInclusive, bool endInclusive) where T : struct, IComparable<T>
    {
        if (start is not { } s || end is not { } e)
        {
            return true;
        }
        var comparison = s.CompareTo(e);
        return comparison < 0 || (comparison == 0 && startInclusive && endInclusive);
    }

    public static bool Contains<T>(T? start, T? end, bool startInclusive, bool endInclusive, T value) where T : struct, IComparable<T>
    {
        if (start is { } s && (startInclusive ? s.CompareTo(value) > 0 : s.CompareTo(value) >= 0))
        {
            return false;
        }
        if (end is { } e && (endInclusive ? value.CompareTo(e) > 0 : value.CompareTo(e) >= 0))
        {
            return false;
        }
        return true;
    }

    public static bool Overlaps<T>(Interval<T> left, Interval<T> right) where T : struct, IComparable<T> =>
        GetOverlap(left, right) is not null;

    public static Interval<T>? GetOverlap<T>(Interval<T> left, Interval<T> right) where T : struct, IComparable<T>
    {
        var (start, startInclusive) = LaterStart(
            (left.Start, left.StartInclusive), (right.Start, right.StartInclusive));
        var (end, endInclusive) = EarlierEnd(
            (left.End, left.EndInclusive), (right.End, right.EndInclusive));
        return Interval<T>.TryCreate(start, end, startInclusive, endInclusive);
    }

    public static string Format<T>(T? start, T? end, bool startInclusive, bool endInclusive) where T : struct
    {
        var startPart = start is { } s ? $"{(startInclusive ? '[' : '(')}{s}" : "(-∞";
        var endPart = end is { } e ? $"{e}{(endInclusive ? ']' : ')')}" : "+∞)";
        return $"{startPart}, {endPart}";
    }

    // On a tie the more restrictive bound wins: the shared value stays in only when both sides keep it.
    private static (T? Value, bool Inclusive) LaterStart<T>((T? Value, bool Inclusive) left, (T? Value, bool Inclusive) right)
        where T : struct, IComparable<T>
    {
        if (left.Value is not { } l)
        {
            return right;
        }
        if (right.Value is not { } r)
        {
            return left;
        }
        var comparison = l.CompareTo(r);
        if (comparison != 0)
        {
            return comparison > 0 ? left : right;
        }
        return (l, left.Inclusive && right.Inclusive);
    }

    private static (T? Value, bool Inclusive) EarlierEnd<T>((T? Value, bool Inclusive) left, (T? Value, bool Inclusive) right)
        where T : struct, IComparable<T>
    {
        if (left.Value is not { } l)
        {
            return right;
        }
        if (right.Value is not { } r)
        {
            return left;
        }
        var comparison = l.CompareTo(r);
        if (comparison != 0)
        {
            return comparison < 0 ? left : right;
        }
        return (l, left.Inclusive && right.Inclusive);
    }
}
