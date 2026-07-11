using System;
using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class IntervalDateExtensionsTests
{
    private static readonly DateTime BaseMoment = new(2020, 1, 1);
    private static readonly DateOnly BaseDay = new(2020, 1, 1);

    [Property]
    public void DateTimeIntervals_ContainADay_WhenTheyCoverAnInstantOfIt(int startSeed, int lengthSeed, int daySeed)
    {
        var start = BaseMoment.AddMinutes(startSeed % 200_000);
        var end = start.AddMinutes(Math.Abs(lengthSeed % 200_000));
        var day = BaseDay.AddDays(daySeed % 150);
        var dayStart = day.ToDateTime(TimeOnly.MinValue);
        var nextDayStart = day.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var laterStart = start > dayStart ? start : dayStart;

        Assert.Equal(laterStart < nextDayStart && laterStart <= end, FiniteInterval.Create(start, end).Contains(day));
        Assert.Equal(start < nextDayStart, IntervalFrom.Create(start, null).Contains(day));
        Assert.Equal(dayStart <= end, IntervalUntil.Create(null, end).Contains(day));
        Assert.Equal(laterStart < nextDayStart && laterStart <= end, Interval.Create(start, end).Contains(day));

        if (start != end)
        {
            var earlierEnd = end < nextDayStart ? end : nextDayStart;
            Assert.Equal(laterStart < earlierEnd, FiniteInterval.Create(start, end, endInclusive: false).Contains(day));
        }
    }

    [Property]
    public void DateIntervals_ContainAMoment_ByItsCalendarDay(int startSeed, int lengthSeed, int minuteSeed)
    {
        var startDay = BaseDay.AddDays(startSeed % 300);
        var endDay = startDay.AddDays(Math.Abs(lengthSeed % 300));
        var moment = BaseMoment.AddMinutes(minuteSeed % 1_000_000);
        var momentDay = DateOnly.FromDateTime(moment);

        var finite = FiniteInterval.Create(startDay, endDay);
        var from = IntervalFrom.Create(startDay, endDay);
        var until = IntervalUntil.Create(startDay, endDay);
        var any = Interval.Create(startDay, endDay);
        Assert.Equal(finite.Contains(momentDay), finite.Contains(moment));
        Assert.Equal(from.Contains(momentDay), from.Contains(moment));
        Assert.Equal(until.Contains(momentDay), until.Contains(moment));
        Assert.Equal(any.Contains(momentDay), any.Contains(moment));
    }

    [Fact]
    public void AMidnightEnd_CountsItsDayOnlyWhenInclusive()
    {
        var inclusive = FiniteInterval.Create(new DateTime(2020, 1, 1, 8, 0, 0), new DateTime(2020, 1, 3, 0, 0, 0));
        Assert.True(inclusive.Contains(new DateOnly(2020, 1, 1)));    // covered from 08:00
        Assert.True(inclusive.Contains(new DateOnly(2020, 1, 2)));
        Assert.True(inclusive.Contains(new DateOnly(2020, 1, 3)));    // the midnight instant itself
        Assert.False(inclusive.Contains(new DateOnly(2019, 12, 31)));

        var exclusive = FiniteInterval.Create(
            new DateTime(2020, 1, 1, 8, 0, 0), new DateTime(2020, 1, 3, 0, 0, 0), endInclusive: false);
        Assert.True(exclusive.Contains(new DateOnly(2020, 1, 2)));
        Assert.False(exclusive.Contains(new DateOnly(2020, 1, 3)));   // stops just short of Jan 3
    }

    [Fact]
    public void TheEndDay_ContainsAMoment_OnlyWhenInclusive()
    {
        var inclusive = FiniteInterval.Create(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 5));
        Assert.True(inclusive.Contains(new DateTime(2020, 1, 5, 0, 0, 0)));
        Assert.True(inclusive.Contains(new DateTime(2020, 1, 5, 23, 59, 0)));
        Assert.False(inclusive.Contains(new DateTime(2020, 1, 6, 0, 0, 0)));

        var exclusive = FiniteInterval.Create(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 5), endInclusive: false);
        Assert.True(exclusive.Contains(new DateTime(2020, 1, 4, 23, 59, 0)));
        Assert.False(exclusive.Contains(new DateTime(2020, 1, 5, 0, 0, 0)));
    }

    [Property]
    public void ToDateInterval_ContainsExactlyTheDaysTheIntervalCovers(int startSeed, int lengthSeed, int daySeed)
    {
        var start = BaseMoment.AddMinutes(startSeed % 200_000);
        var end = start.AddMinutes(Math.Abs(lengthSeed % 200_000));
        var day = BaseDay.AddDays(daySeed % 150);

        var finite = FiniteInterval.Create(start, end);
        Assert.Equal(finite.Contains(day), finite.ToDateInterval().Contains(day));
        var from = IntervalFrom.Create(start, null);
        Assert.Equal(from.Contains(day), from.ToDateInterval().Contains(day));
        var until = IntervalUntil.Create(null, end);
        Assert.Equal(until.Contains(day), until.ToDateInterval().Contains(day));
        var any = Interval.Create(start, end);
        Assert.Equal(any.Contains(day), any.ToDateInterval().Contains(day));

        if (start != end)
        {
            var halfOpen = FiniteInterval.Create(start, end, endInclusive: false);
            Assert.Equal(halfOpen.Contains(day), halfOpen.ToDateInterval().Contains(day));
        }
    }

    [Fact]
    public void ToDateInterval_ReturnsTheCoveredDays()
    {
        var stay = FiniteInterval.Create(new DateTime(2020, 1, 1, 14, 0, 0), new DateTime(2020, 1, 4, 10, 0, 0));
        Assert.Equal(FiniteInterval.Create(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 4)), stay.ToDateInterval());

        var inclusiveMidnightEnd = FiniteInterval.Create(new DateTime(2020, 1, 1, 8, 0, 0), new DateTime(2020, 1, 3, 0, 0, 0));
        Assert.Equal(FiniteInterval.Create(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 3)), inclusiveMidnightEnd.ToDateInterval());

        var exclusiveMidnightEnd = FiniteInterval.Create(
            new DateTime(2020, 1, 1, 8, 0, 0), new DateTime(2020, 1, 3, 0, 0, 0), endInclusive: false);
        Assert.Equal(FiniteInterval.Create(new DateOnly(2020, 1, 1), new DateOnly(2020, 1, 2)), exclusiveMidnightEnd.ToDateInterval());

        var point = FiniteInterval.Create(new DateTime(2020, 1, 2, 10, 0, 0), new DateTime(2020, 1, 2, 10, 0, 0));
        Assert.Equal(FiniteInterval.Create(new DateOnly(2020, 1, 2), new DateOnly(2020, 1, 2)), point.ToDateInterval());
        Assert.Equal(1, point.ToDateInterval().Days());
    }

    [Theory]
    [InlineData("2020-01-01", "2020-01-01", true, true, 1)]
    [InlineData("2020-01-01", "2020-01-02", true, true, 2)]
    [InlineData("2020-01-01", "2020-01-02", true, false, 1)]
    [InlineData("2020-01-01", "2020-01-02", false, true, 1)]
    [InlineData("2020-01-01", "2020-01-02", false, false, 0)]
    [InlineData("2020-01-01", "2020-12-31", true, true, 366)]   // leap year
    public void Days_CountsTheContainedDays(string start, string end, bool startInclusive, bool endInclusive, int expected)
    {
        var interval = FiniteInterval.Create(
            DateOnly.Parse(start, CultureInfo.InvariantCulture),
            DateOnly.Parse(end, CultureInfo.InvariantCulture),
            startInclusive,
            endInclusive);
        Assert.Equal(expected, interval.Days());
    }
}
