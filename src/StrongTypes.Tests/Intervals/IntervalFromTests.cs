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
        var interval = IntervalFrom<int>.TryCreate(s, null);
        Assert.NotNull(interval);
        Assert.Equal(s, interval!.Value.Start);
        Assert.Null(interval.Value.End);
    }

    [Property]
    public void TryCreate_StartLessOrEqualEnd_Wraps(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        var interval = IntervalFrom<int>.TryCreate(smaller, larger);
        Assert.NotNull(interval);
        Assert.Equal(smaller, interval!.Value.Start);
        Assert.Equal(larger, interval.Value.End);
    }

    [Property]
    public void TryCreate_StartGreaterThanEnd_ReturnsNull(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Null(IntervalFrom<int>.TryCreate(larger, smaller));
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => IntervalFrom<int>.Create(larger, smaller));
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
        var copy = IntervalFrom<int>.Create(interval.Start, interval.End);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
    }
}
