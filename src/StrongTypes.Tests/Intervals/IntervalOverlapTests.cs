using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class IntervalOverlapTests
{
    [Property]
    public void Overlaps_IsSymmetric(Interval<int> a, Interval<int> b) =>
        Assert.Equal(a.Overlaps(b), b.Overlaps(a));

    [Property]
    public void Overlaps_AgreesWithGetOverlap(Interval<int> a, Interval<int> b) =>
        Assert.Equal(a.GetOverlap(b) is not null, a.Overlaps(b));

    [Property]
    public void GetOverlap_ContainsExactlyTheValuesInBoth(Interval<int> a, Interval<int> b, int value)
    {
        var overlap = a.GetOverlap(b);
        Assert.Equal(a.Contains(value) && b.Contains(value), overlap is { } o && o.Contains(value));
    }

    [Property]
    public void FiniteReceiver_MatchesTheWidenedComputation(FiniteInterval<int> finite, Interval<int> other)
    {
        Interval<int> widened = finite;
        Assert.Equal(widened.Overlaps(other), finite.Overlaps(other));
        Assert.Equal(widened.GetOverlap(other), finite.GetOverlap(other) is { } o ? (Interval<int>?)o : null);
    }

    [Property]
    public void FromReceiver_MatchesTheWidenedComputation(IntervalFrom<int> from, Interval<int> other)
    {
        Interval<int> widened = from;
        Assert.Equal(widened.Overlaps(other), from.Overlaps(other));
        Assert.Equal(widened.GetOverlap(other), from.GetOverlap(other) is { } o ? (Interval<int>?)o : null);
    }

    [Property]
    public void UntilReceiver_MatchesTheWidenedComputation(IntervalUntil<int> until, Interval<int> other)
    {
        Interval<int> widened = until;
        Assert.Equal(widened.Overlaps(other), until.Overlaps(other));
        Assert.Equal(widened.GetOverlap(other), until.GetOverlap(other) is { } o ? (Interval<int>?)o : null);
    }

    [Fact]
    public void TouchingIntervals_OverlapInAPoint_OnlyWhenBothTouchingBoundsAreInclusive()
    {
        var left = FiniteInterval.Create(0, 5);
        var right = FiniteInterval.Create(5, 9);
        Assert.True(left.Overlaps(right));
        Assert.Equal(FiniteInterval.Create(5, 5), left.GetOverlap(right));

        var exclusiveEnd = FiniteInterval.Create(0, 5, endInclusive: false);
        Assert.False(exclusiveEnd.Overlaps(right));
        Assert.Null(exclusiveEnd.GetOverlap(right));

        var exclusiveStart = FiniteInterval.Create(5, 9, startInclusive: false);
        Assert.False(left.Overlaps(exclusiveStart));
        Assert.Null(left.GetOverlap(exclusiveStart));
    }

    [Fact]
    public void CrossingIntervalsOverlapOnTheSharedRange()
    {
        var left = FiniteInterval.Create(0, 6);
        var right = FiniteInterval.Create(5, 9);
        Assert.True(left.Overlaps(right));
        Assert.Equal(FiniteInterval.Create(5, 6), left.GetOverlap(right));
        Assert.Equal(
            FiniteInterval.Create(5, 6, endInclusive: false),
            FiniteInterval.Create(0, 6, endInclusive: false).GetOverlap(right));
    }

    [Fact]
    public void PointIntervalOverlapsExactlyWhereItLies()
    {
        var point = FiniteInterval.Create(5, 5);
        Assert.True(point.Overlaps(FiniteInterval.Create(0, 10)));
        Assert.Equal(point, point.GetOverlap(FiniteInterval.Create(0, 10)));
        Assert.False(point.Overlaps(FiniteInterval.Create(6, 10)));
        Assert.False(point.Overlaps(FiniteInterval.Create(5, 10, startInclusive: false)));
    }

    [Fact]
    public void DisjointIntervalsDoNotOverlap()
    {
        Assert.False(FiniteInterval.Create(0, 4).Overlaps(FiniteInterval.Create(5, 9)));
        Assert.Null(FiniteInterval.Create(0, 4).GetOverlap(FiniteInterval.Create(5, 9)));
    }

    [Fact]
    public void UnboundedIntervalOverlapsEverything()
    {
        var unbounded = Interval.Create<int>(null, null);
        Assert.True(unbounded.Overlaps(FiniteInterval.Create(1, 2)));
        Assert.Equal(Interval.Create(1, 2), unbounded.GetOverlap(FiniteInterval.Create(1, 2)));
    }

    [Fact]
    public void GetOverlap_KeepsTheReceiversBoundedEndpoints()
    {
        var from = IntervalFrom.Create(3, null);
        var until = IntervalUntil.Create(null, 8);

        IntervalFrom<int>? fromOverlap = from.GetOverlap(until);
        IntervalUntil<int>? untilOverlap = until.GetOverlap(from);

        Assert.Equal(3, fromOverlap!.Value.Start);
        Assert.Equal(8, fromOverlap.Value.End);
        Assert.Equal(3, untilOverlap!.Value.Start);
        Assert.Equal(8, untilOverlap.Value.End);
    }
}
