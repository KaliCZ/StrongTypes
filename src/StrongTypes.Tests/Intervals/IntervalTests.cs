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
        var interval = Interval.TryCreate<int>(null, null);
        Assert.NotNull(interval);
        Assert.Null(interval!.Value.Start);
        Assert.Null(interval.Value.End);
    }

    [Property]
    public void TryCreate_StartOnly_Wraps(int s)
    {
        var interval = Interval.TryCreate(s, null);
        Assert.NotNull(interval);
        Assert.Equal(s, interval!.Value.Start);
        Assert.Null(interval.Value.End);
    }

    [Property]
    public void TryCreate_EndOnly_Wraps(int e)
    {
        var interval = Interval.TryCreate(null, e);
        Assert.NotNull(interval);
        Assert.Null(interval!.Value.Start);
        Assert.Equal(e, interval.Value.End);
    }

    [Property]
    public void TryCreate_BothPresent_Validated(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;

        Assert.NotNull(Interval.TryCreate(smaller, larger));
        Assert.Null(Interval.TryCreate(larger, smaller));
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
        var copy = Interval.Create(interval.Start, interval.End, interval.StartInclusive, interval.EndInclusive);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
        Assert.True(interval == copy);
    }

    [Fact]
    public void UnboundedEndpointsNormalizeTheirBoundFlag()
    {
        var from = Interval.Create(1, (int?)null, endInclusive: false);
        Assert.True(from.EndInclusive);
        Assert.Equal(Interval.Create(1, (int?)null), from);

        var until = Interval.Create((int?)null, 10, startInclusive: false);
        Assert.True(until.StartInclusive);
        Assert.Equal(Interval.Create((int?)null, 10), until);
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => Interval.Create(larger, smaller));
    }

    [Fact]
    public void Contains_RespectsBothBoundsAndTheirInclusivity()
    {
        var bounded = Interval.Create(0, 10);
        Assert.True(bounded.Contains(0));
        Assert.True(bounded.Contains(10));
        Assert.True(bounded.Contains(5));
        Assert.False(bounded.Contains(-1));
        Assert.False(bounded.Contains(11));

        var halfOpen = Interval.Create(0, 10, endInclusive: false);
        Assert.True(halfOpen.Contains(0));
        Assert.True(halfOpen.Contains(9));
        Assert.False(halfOpen.Contains(10));

        var open = Interval.Create(0, 10, startInclusive: false, endInclusive: false);
        Assert.False(open.Contains(0));
        Assert.True(open.Contains(1));
        Assert.False(open.Contains(10));
    }

    [Fact]
    public void Contains_UnboundedSidesAcceptEverythingWithinTheBounds()
    {
        Assert.True(Interval.Create<int>(null, null).Contains(int.MinValue));
        Assert.True(Interval.Create<int>(null, null).Contains(int.MaxValue));

        var from = Interval.Create(0, null);
        Assert.True(from.Contains(int.MaxValue));
        Assert.False(from.Contains(-1));

        var until = Interval.Create(null, 0);
        Assert.True(until.Contains(int.MinValue));
        Assert.True(until.Contains(0));
        Assert.False(until.Contains(1));
    }

    [Fact]
    public void ToString_RendersBoundsAndInclusivity()
    {
        Assert.Equal("(-∞, +∞)", Interval.Create<int>(null, null).ToString());
        Assert.Equal("(-∞, 10]", Interval.Create(null, 10).ToString());
        Assert.Equal("[1, +∞)", Interval.Create(1, null).ToString());
        Assert.Equal("[1, 10]", Interval.Create(1, 10).ToString());
        Assert.Equal("[1, 10)", Interval.Create(1, 10, endInclusive: false).ToString());
        Assert.Equal("(1, 10]", Interval.Create(1, 10, startInclusive: false).ToString());
        Assert.Equal("(1, 10)", Interval.Create(1, 10, startInclusive: false, endInclusive: false).ToString());
    }
}
