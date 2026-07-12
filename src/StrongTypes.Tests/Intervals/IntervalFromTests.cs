using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class IntervalFromTests
{
    [Property]
    public void TryCreate_EndNull_Wraps(int s)
    {
        var interval = IntervalFrom.TryCreate(s, null);
        Assert.NotNull(interval);
        Assert.Equal(s, interval!.Value.Start);
        Assert.Null(interval.Value.End);
    }

    [Property]
    public void TryCreate_StartLessOrEqualEnd_Wraps(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        var interval = IntervalFrom.TryCreate(smaller, larger);
        Assert.NotNull(interval);
        Assert.Equal(smaller, interval!.Value.Start);
        Assert.Equal(larger, interval.Value.End);
    }

    [Property]
    public void TryCreate_StartGreaterThanEnd_ReturnsNull(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Null(IntervalFrom.TryCreate(larger, smaller));
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => IntervalFrom.Create(larger, smaller));
    }

    [Property]
    public void Deconstruct_RoundTrips(IntervalFrom<int> interval)
    {
        var (start, end) = interval;
        Assert.Equal(interval.Start, start);
        Assert.Equal(interval.End, end);
    }

    [Property]
    public void Equality_SameContent(IntervalFrom<int> interval)
    {
        var copy = IntervalFrom.Create(interval.Start, interval.End, interval.StartInclusive, interval.EndInclusive);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Contains_HonorsBoundInclusivity()
    {
        var openEnded = IntervalFrom.Create(0, null);
        Assert.True(openEnded.Contains(0));
        Assert.True(openEnded.Contains(int.MaxValue));
        Assert.False(openEnded.Contains(-1));

        var bounded = IntervalFrom.Create(0, 10);
        Assert.True(bounded.Contains(10));

        var exclusive = IntervalFrom.Create(0, 10, startInclusive: false, endInclusive: false);
        Assert.False(exclusive.Contains(0));
        Assert.True(exclusive.Contains(9));
        Assert.False(exclusive.Contains(10));
    }

    [Fact]
    public void UnboundedEndNormalizesItsBoundFlag()
    {
        var interval = IntervalFrom.Create(1, (int?)null, endInclusive: false);
        Assert.True(interval.EndInclusive);
        Assert.Equal(IntervalFrom.Create(1, (int?)null), interval);
    }

    [Fact]
    public void ToString_RendersUnboundedUpperBound()
    {
        Assert.Equal("[1, +∞)", IntervalFrom.Create(1, null).ToString());
        Assert.Equal("[1, 10]", IntervalFrom.Create(1, 10).ToString());
        Assert.Equal("(1, 10)", IntervalFrom.Create(1, 10, startInclusive: false, endInclusive: false).ToString());
    }
}
