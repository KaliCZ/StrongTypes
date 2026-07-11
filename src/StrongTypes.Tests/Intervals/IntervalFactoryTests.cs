using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class IntervalFactoryTests
{
    [Property]
    public void FiniteInterval_HelperMatchesGenericFactory(int start, int end)
    {
        Assert.Equal(FiniteInterval<int>.TryCreate(start, end), FiniteInterval.TryCreate(start, end));
        if (start > end)
        {
            Assert.Throws<ArgumentException>(() => FiniteInterval.Create(start, end));
        }
        else
        {
            Assert.Equal(FiniteInterval<int>.Create(start, end), FiniteInterval.Create(start, end));
        }
    }

    [Property]
    public void IntervalFrom_HelperMatchesGenericFactory(int start, int? end)
    {
        Assert.Equal(IntervalFrom<int>.TryCreate(start, end), IntervalFrom.TryCreate(start, end));
        if (end.HasValue && start > end.Value)
        {
            Assert.Throws<ArgumentException>(() => IntervalFrom.Create(start, end));
        }
        else
        {
            Assert.Equal(IntervalFrom<int>.Create(start, end), IntervalFrom.Create(start, end));
        }
    }

    [Property]
    public void IntervalUntil_HelperMatchesGenericFactory(int? start, int end)
    {
        Assert.Equal(IntervalUntil<int>.TryCreate(start, end), IntervalUntil.TryCreate(start, end));
        if (start.HasValue && start.Value > end)
        {
            Assert.Throws<ArgumentException>(() => IntervalUntil.Create(start, end));
        }
        else
        {
            Assert.Equal(IntervalUntil<int>.Create(start, end), IntervalUntil.Create(start, end));
        }
    }

    [Property]
    public void Interval_HelperMatchesGenericFactory(int? start, int? end)
    {
        Assert.Equal(Interval<int>.TryCreate(start, end), Interval.TryCreate(start, end));
        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            Assert.Throws<ArgumentException>(() => Interval.Create(start, end));
        }
        else
        {
            Assert.Equal(Interval<int>.Create(start, end), Interval.Create(start, end));
        }
    }

    [Fact]
    public void NullLiteralsInferFromTheBoundedEndpoint()
    {
        Assert.Equal(IntervalFrom<int>.Create(5, null), IntervalFrom.Create(5, null));
        Assert.Equal(IntervalUntil<int>.Create(null, 10), IntervalUntil.Create(null, 10));
        Assert.Equal(Interval<int>.Create(null, null), Interval.Create<int>(null, null));
    }

    [Fact]
    public void PlainValuesInferOnTheIntervalCompanion()
    {
        Assert.Equal(Interval<int>.Create(1, 10), Interval.Create(1, 10));
        Assert.Equal(Interval<int>.Create(1, null), Interval.Create(1, null));
        Assert.Equal(Interval<int>.Create(null, 10), Interval.Create(null, 10));
        Assert.Equal(Interval<int>.TryCreate(10, 1), Interval.TryCreate(10, 1));
    }
}
