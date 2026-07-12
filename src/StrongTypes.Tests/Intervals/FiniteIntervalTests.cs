using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class FiniteIntervalTests
{
    [Property]
    public void TryCreate_StartGreaterThanEnd_ReturnsNull(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Null(FiniteInterval.TryCreate(larger, smaller));
    }

    [Property]
    public void TryCreate_StartLessOrEqualEnd_Wraps(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        var interval = FiniteInterval.TryCreate(smaller, larger);
        Assert.NotNull(interval);
        Assert.Equal(smaller, interval!.Value.Start);
        Assert.Equal(larger, interval.Value.End);
    }

    [Property]
    public void TryCreate_EqualEndpoints_FormASingleValueIntervalOnlyWhenBothBoundsAreInclusive(
        int v, bool startInclusive, bool endInclusive)
    {
        var interval = FiniteInterval.TryCreate(v, v, startInclusive, endInclusive);
        Assert.Equal(startInclusive && endInclusive, interval is not null);
        if (interval is { } point)
        {
            Assert.True(point.Contains(v));
        }
    }

    [Property]
    public void Create_Throws_WhenStartGreaterThanEnd(int a, int b)
    {
        var (smaller, larger) = a <= b ? (a, b) : (b, a);
        if (smaller == larger) return;
        Assert.Throws<ArgumentException>(() => FiniteInterval.Create(larger, smaller));
    }

    [Property]
    public void Deconstruct_RoundTrips(FiniteInterval<int> interval)
    {
        var (start, end) = interval;
        Assert.Equal(interval.Start, start);
        Assert.Equal(interval.End, end);
    }

    [Property]
    public void Equality_SameContent(FiniteInterval<int> interval)
    {
        var copy = FiniteInterval.Create(interval.Start, interval.End, interval.StartInclusive, interval.EndInclusive);
        Assert.Equal(interval, copy);
        Assert.Equal(interval.GetHashCode(), copy.GetHashCode());
        Assert.True(interval == copy);
        Assert.False(interval != copy);
    }

    [Fact]
    public void Equality_DistinguishesBoundInclusivity()
    {
        Assert.NotEqual(FiniteInterval.Create(1, 10), FiniteInterval.Create(1, 10, endInclusive: false));
        Assert.NotEqual(FiniteInterval.Create(1, 10), FiniteInterval.Create(1, 10, startInclusive: false));
    }

    [Property]
    public void Contains_HonorsBoundInclusivity(FiniteInterval<int> interval)
    {
        Assert.Equal(interval.StartInclusive, interval.Contains(interval.Start));
        Assert.Equal(interval.EndInclusive, interval.Contains(interval.End));
    }
}
