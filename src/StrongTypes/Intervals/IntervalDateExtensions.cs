#nullable enable
using System;
using System.Diagnostics.Contracts;

namespace StrongTypes;

/// <summary>Bridges <see cref="DateTime"/> and <see cref="DateOnly"/> values and the intervals over them.</summary>
public static class IntervalDateExtensions
{
    /// <summary>Whether the interval covers any instant of the calendar day <paramref name="day"/>.</summary>
    [Pure]
    public static bool Contains(this FiniteInterval<DateTime> interval, DateOnly day) => CoversDay(interval, day);

    /// <summary>Whether the interval covers any instant of the calendar day <paramref name="day"/>.</summary>
    [Pure]
    public static bool Contains(this IntervalFrom<DateTime> interval, DateOnly day) => CoversDay(interval, day);

    /// <summary>Whether the interval covers any instant of the calendar day <paramref name="day"/>.</summary>
    [Pure]
    public static bool Contains(this IntervalUntil<DateTime> interval, DateOnly day) => CoversDay(interval, day);

    /// <summary>Whether the interval covers any instant of the calendar day <paramref name="day"/>.</summary>
    [Pure]
    public static bool Contains(this Interval<DateTime> interval, DateOnly day) => CoversDay(interval, day);

    /// <summary>Whether the interval contains the calendar day of <paramref name="moment"/>.</summary>
    [Pure]
    public static bool Contains(this FiniteInterval<DateOnly> interval, DateTime moment) => interval.Contains(moment.ToDateOnly());

    /// <summary>Whether the interval contains the calendar day of <paramref name="moment"/>.</summary>
    [Pure]
    public static bool Contains(this IntervalFrom<DateOnly> interval, DateTime moment) => interval.Contains(moment.ToDateOnly());

    /// <summary>Whether the interval contains the calendar day of <paramref name="moment"/>.</summary>
    [Pure]
    public static bool Contains(this IntervalUntil<DateOnly> interval, DateTime moment) => interval.Contains(moment.ToDateOnly());

    /// <summary>Whether the interval contains the calendar day of <paramref name="moment"/>.</summary>
    [Pure]
    public static bool Contains(this Interval<DateOnly> interval, DateTime moment) => interval.Contains(moment.ToDateOnly());

    /// <summary>The calendar days the interval covers, both endpoints inclusive: the result contains a day exactly when this interval covers some instant of it.</summary>
    [Pure]
    public static FiniteInterval<DateOnly> ToDateInterval(this FiniteInterval<DateTime> interval) =>
        FiniteInterval.Create(interval.Start.ToDateOnly(), CoveredEndDay(interval.End, interval.EndInclusive)!.Value);

    /// <summary>The calendar days the interval covers, bounded endpoints inclusive: the result contains a day exactly when this interval covers some instant of it. An unbounded endpoint stays unbounded.</summary>
    [Pure]
    public static IntervalFrom<DateOnly> ToDateInterval(this IntervalFrom<DateTime> interval) =>
        IntervalFrom.Create(interval.Start.ToDateOnly(), CoveredEndDay(interval.End, interval.EndInclusive));

    /// <summary>The calendar days the interval covers, bounded endpoints inclusive: the result contains a day exactly when this interval covers some instant of it. An unbounded endpoint stays unbounded.</summary>
    [Pure]
    public static IntervalUntil<DateOnly> ToDateInterval(this IntervalUntil<DateTime> interval) =>
        IntervalUntil.Create(interval.Start?.ToDateOnly(), CoveredEndDay(interval.End, interval.EndInclusive)!.Value);

    /// <summary>The calendar days the interval covers, bounded endpoints inclusive: the result contains a day exactly when this interval covers some instant of it. An unbounded endpoint stays unbounded.</summary>
    [Pure]
    public static Interval<DateOnly> ToDateInterval(this Interval<DateTime> interval) =>
        Interval.Create(interval.Start?.ToDateOnly(), CoveredEndDay(interval.End, interval.EndInclusive));

    /// <summary>The number of calendar days the interval contains; an excluded endpoint day is not counted.</summary>
    [Pure]
    public static int Days(this FiniteInterval<DateOnly> interval) =>
        interval.End.DayNumber - interval.Start.DayNumber + 1 - (interval.StartInclusive ? 0 : 1) - (interval.EndInclusive ? 0 : 1);

    /// <summary>The calendar day of <paramref name="moment"/>, dropping its time of day.</summary>
    [Pure]
    public static DateOnly ToDateOnly(this DateTime moment) => DateOnly.FromDateTime(moment);

    /// <summary>The instants of the calendar day <paramref name="day"/> as the half-open interval [midnight, next midnight); the last representable day is capped at <see cref="DateTime.MaxValue"/>.</summary>
    [Pure]
    public static FiniteInterval<DateTime> ToTimeInterval(this DateOnly day) =>
        day == DateOnly.MaxValue
            ? FiniteInterval.Create(day.ToDateTime(TimeOnly.MinValue), DateTime.MaxValue)
            : FiniteInterval.Create(day.ToDateTime(TimeOnly.MinValue), day.AddDays(1).ToDateTime(TimeOnly.MinValue), endInclusive: false);

    private static bool CoversDay(Interval<DateTime> interval, DateOnly day) => interval.Overlaps(day.ToTimeInterval());

    // An exclusive end at exactly midnight stops short of its own calendar day.
    private static DateOnly? CoveredEndDay(DateTime? end, bool endInclusive)
    {
        if (end is not { } e)
        {
            return null;
        }
        var day = e.ToDateOnly();
        return !endInclusive && e.TimeOfDay == TimeSpan.Zero && day != DateOnly.MinValue ? day.AddDays(-1) : day;
    }
}
