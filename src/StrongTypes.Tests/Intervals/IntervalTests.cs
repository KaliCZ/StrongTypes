using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class IntervalTests
{
    [Fact]
    public void TryCreate_BothNull_Wraps()
    {
        var interval = Interval<int>.TryCreate(null, null);
        Assert.NotNull(interval);
        Assert.Null(interval!.Value.Start);
        Assert.Null(interval.Value.End);
    }

    [Property]
    public void TryCreate_StartOnly_Wraps(int s)
    {
        var interval = Interval<int>.TryCreate(s, null);
        Assert.NotNull(interval);
        Assert.Equal(s, interval!.Value.Start);
        Assert.Null(interval.Value.End);
    }

    [Property]
    public void TryCreate_EndOnly_Wraps(int e)
    {
        var interval = Interval<int>.TryCreate(null, e);
        Assert.NotNull(interval);
        Assert.Null(interval!.Value.Start);
        Assert.Equal(e, interval.Value.End);
    }

    [Property]
    public void TryCreate_BothPresent_Validated(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;

        Assert.NotNull(Interval<int>.TryCreate(smaller, larger));
        Assert.Null(Interval<int>.TryCreate(larger, smaller));
    }

    [Property]
    public void Deconstruct_RoundTripsAllFourPatterns(Interval<int> interval)
    {
        var (start, end) = interval;
        Assert.Equal(interval.Start, start);
        Assert.Equal(interval.End, end);

        var label = interval switch
        {
            (null, null) => "unbounded",
            (null, _) => "upTo",
            (_, null) => "from",
            _ => "closed"
        };

        var expected = (interval.Start, interval.End) switch
        {
            (null, null) => "unbounded",
            (null, _) => "upTo",
            (_, null) => "from",
            _ => "closed"
        };
        Assert.Equal(expected, label);
    }

    [Property]
    public void Equality_SameContent(Interval<int> interval)
    {
        var copy = Interval<int>.Create(interval.Start, interval.End);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
        Assert.True(interval == copy);
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => Interval<int>.Create(larger, smaller));
    }
}
