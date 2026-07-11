using System;
using System.Text.Json;
using Xunit;

namespace StrongTypes.Tests;

// The interval suite generates over int; this pins the generic surface on other endpoint types.
public class IntervalEndpointTypesTests
{
    [Fact]
    public void DateTimeEndpoints_ValidateOrdering()
    {
        var earlier = new DateTime(2026, 7, 1, 10, 0, 0);
        var later = new DateTime(2026, 7, 1, 15, 30, 0);

        Assert.Null(FiniteInterval.TryCreate(later, earlier));
        Assert.Throws<ArgumentException>(() => IntervalFrom.Create(later, earlier));

        var interval = FiniteInterval.Create(earlier, later);
        Assert.True(interval.Contains(earlier));
        Assert.True(interval.Contains(later));
        Assert.False(interval.Contains(later.AddTicks(1)));

        var halfOpen = FiniteInterval.Create(earlier, later, endInclusive: false);
        Assert.True(halfOpen.Contains(later.AddTicks(-1)));
        Assert.False(halfOpen.Contains(later));
    }

    [Fact]
    public void DateTimeIntervals_Overlap()
    {
        var noon = new DateTime(2026, 7, 1, 12, 0, 0);
        var morning = FiniteInterval.Create(new DateTime(2026, 7, 1, 8, 0, 0), noon, endInclusive: false);

        Assert.False(morning.Overlaps(IntervalFrom.Create(noon, null)));
        var fromEleven = IntervalFrom.Create(noon.AddHours(-1), null);
        Assert.True(morning.Overlaps(fromEleven));
        Assert.Equal(FiniteInterval.Create(noon.AddHours(-1), noon, endInclusive: false), morning.GetOverlap(fromEleven));
    }

    [Fact]
    public void DateTimeInterval_RoundTripsThroughJson()
    {
        var interval = Interval.Create(new DateTime(2026, 7, 1, 10, 30, 0), null);
        var roundTripped = JsonSerializer.Deserialize<Interval<DateTime>>(JsonSerializer.Serialize(interval));
        Assert.Equal(interval, roundTripped);
    }

    [Fact]
    public void DateOnlyEndpoints_ValidateAndDeconstruct()
    {
        var season = IntervalUntil.Create(null, new DateOnly(2026, 8, 31));
        var (start, end) = season;

        Assert.Null(start);
        Assert.Equal(new DateOnly(2026, 8, 31), end);
        Assert.Null(IntervalUntil.TryCreate(new DateOnly(2026, 9, 1), new DateOnly(2026, 8, 31)));
        Assert.True(season.Contains(DateOnly.MinValue));
        Assert.True(season.Contains(new DateOnly(2026, 8, 31)));
        Assert.False(season.Contains(new DateOnly(2026, 9, 1)));
    }

    [Fact]
    public void DateOnlyInterval_RoundTripsThroughJson()
    {
        var interval = FiniteInterval.Create(new DateOnly(2026, 6, 1), new DateOnly(2026, 8, 31));
        var roundTripped = JsonSerializer.Deserialize<FiniteInterval<DateOnly>>(JsonSerializer.Serialize(interval));
        Assert.Equal(interval, roundTripped);
    }

    [Fact]
    public void DecimalEndpoints_WidenAndNarrow()
    {
        var price = FiniteInterval.Create(10m, 20m);
        Interval<decimal> widened = price;

        Assert.True(widened.Contains(15m));
        Assert.Equal(price, widened.AsFinite());
        Assert.Equal("[10, 20]", price.ToString());
    }

    [Fact]
    public void TimeOnlyEndpoints_OverlapAcrossVariants()
    {
        var shift = FiniteInterval.Create(new TimeOnly(9, 0), new TimeOnly(17, 0));
        var afternoon = IntervalFrom.Create(new TimeOnly(13, 0), null);

        Assert.Equal(FiniteInterval.Create(new TimeOnly(13, 0), new TimeOnly(17, 0)), shift.GetOverlap(afternoon));
    }
}
