using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class IntervalUntilTests
{
    [Property]
    public void TryCreate_StartNull_Wraps(int e)
    {
        var interval = IntervalUntil.TryCreate(null, e);
        Assert.NotNull(interval);
        Assert.Null(interval!.Value.Start);
        Assert.Equal(e, interval.Value.End);
    }

    [Property]
    public void TryCreate_StartLessOrEqualEnd_Wraps(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        var interval = IntervalUntil.TryCreate(smaller, larger);
        Assert.NotNull(interval);
        Assert.Equal(smaller, interval!.Value.Start);
        Assert.Equal(larger, interval.Value.End);
    }

    [Property]
    public void TryCreate_StartGreaterThanEnd_ReturnsNull(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Null(IntervalUntil.TryCreate(larger, smaller));
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => IntervalUntil.Create(larger, smaller));
    }

    [Property]
    public void Deconstruct_RoundTrips(IntervalUntil<int> interval)
    {
        var (start, end) = interval;
        Assert.Equal(interval.Start, start);
        Assert.Equal(interval.End, end);
    }

    [Property]
    public void Equality_SameContent(IntervalUntil<int> interval)
    {
        var copy = IntervalUntil.Create(interval.Start, interval.End, interval.StartInclusive, interval.EndInclusive);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Contains_HonorsBoundInclusivity()
    {
        var openStart = IntervalUntil.Create(null, 10);
        Assert.True(openStart.Contains(10));
        Assert.True(openStart.Contains(int.MinValue));
        Assert.False(openStart.Contains(11));

        var bounded = IntervalUntil.Create(0, 10);
        Assert.True(bounded.Contains(0));
        Assert.False(bounded.Contains(-1));

        var exclusive = IntervalUntil.Create(0, 10, startInclusive: false, endInclusive: false);
        Assert.False(exclusive.Contains(0));
        Assert.True(exclusive.Contains(1));
        Assert.False(exclusive.Contains(10));
    }

    [Fact]
    public void UnboundedStartNormalizesItsBoundFlag()
    {
        var interval = IntervalUntil.Create((int?)null, 10, startInclusive: false);
        Assert.True(interval.StartInclusive);
        Assert.Equal(IntervalUntil.Create((int?)null, 10), interval);
    }

    [Fact]
    public void ToString_RendersUnboundedLowerBound()
    {
        Assert.Equal("(-∞, 10]", IntervalUntil.Create(null, 10).ToString());
        Assert.Equal("[1, 10]", IntervalUntil.Create(1, 10).ToString());
        Assert.Equal("(1, 10)", IntervalUntil.Create(1, 10, startInclusive: false, endInclusive: false).ToString());
    }
}
