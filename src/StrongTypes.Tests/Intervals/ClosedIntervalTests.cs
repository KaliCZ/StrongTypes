using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ClosedIntervalTests
{
    [Property]
    public void TryCreate_StartGreaterThanEnd_ReturnsNull(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Null(ClosedInterval<int>.TryCreate(larger, smaller));
    }

    [Property]
    public void TryCreate_StartLessOrEqualEnd_Wraps(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        var interval = ClosedInterval<int>.TryCreate(smaller, larger);
        Assert.NotNull(interval);
        Assert.Equal(smaller, interval!.Value.Start);
        Assert.Equal(larger, interval.Value.End);
    }

    [Property]
    public void TryCreate_DegenerateAccepted(int v)
    {
        var interval = ClosedInterval<int>.TryCreate(v, v);
        Assert.NotNull(interval);
        Assert.Equal(v, interval!.Value.Start);
        Assert.Equal(v, interval.Value.End);
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => ClosedInterval<int>.Create(larger, smaller));
    }

    [Property]
    public void Deconstruct_RoundTrips(ClosedInterval<int> interval)
    {
        var (start, end) = interval;
        Assert.Equal(interval.Start, start);
        Assert.Equal(interval.End, end);
    }

    [Property]
    public void Equality_SameContent(ClosedInterval<int> interval)
    {
        var copy = ClosedInterval<int>.Create(interval.Start, interval.End);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
        Assert.True(interval == copy);
        Assert.False(interval != copy);
    }

    [Property]
    public void Contains_StartAndEnd(ClosedInterval<int> interval)
    {
        Assert.True(interval.Contains(interval.Start));
        Assert.True(interval.Contains(interval.End));
    }
}
