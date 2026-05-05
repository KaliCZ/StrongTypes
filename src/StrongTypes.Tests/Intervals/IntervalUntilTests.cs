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
        var interval = IntervalUntil<int>.TryCreate(null, e);
        Assert.NotNull(interval);
        Assert.Null(interval!.Value.Start);
        Assert.Equal(e, interval.Value.End);
    }

    [Property]
    public void TryCreate_StartLessOrEqualEnd_Wraps(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        var interval = IntervalUntil<int>.TryCreate(smaller, larger);
        Assert.NotNull(interval);
        Assert.Equal(smaller, interval!.Value.Start);
        Assert.Equal(larger, interval.Value.End);
    }

    [Property]
    public void TryCreate_StartGreaterThanEnd_ReturnsNull(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Null(IntervalUntil<int>.TryCreate(larger, smaller));
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => IntervalUntil<int>.Create(larger, smaller));
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
        var copy = IntervalUntil<int>.Create(interval.Start, interval.End);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
    }
}
