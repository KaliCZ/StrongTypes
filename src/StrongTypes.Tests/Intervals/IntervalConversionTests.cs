using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class IntervalConversionTests
{
    [Property]
    public void FiniteInterval_widens_to_Interval(FiniteInterval<int> c)
    {
        Interval<int> i = c;
        Assert.Equal(c.Start, i.Start);
        Assert.Equal(c.End, i.End);
    }

    [Property]
    public void FiniteInterval_widens_to_IntervalFrom(FiniteInterval<int> c)
    {
        IntervalFrom<int> f = c;
        Assert.Equal(c.Start, f.Start);
        Assert.Equal(c.End, f.End);
    }

    [Property]
    public void FiniteInterval_widens_to_IntervalUntil(FiniteInterval<int> c)
    {
        IntervalUntil<int> u = c;
        Assert.Equal(c.Start, u.Start);
        Assert.Equal(c.End, u.End);
    }

    [Property]
    public void IntervalFrom_widens_to_Interval(IntervalFrom<int> f)
    {
        Interval<int> i = f;
        Assert.Equal(f.Start, i.Start);
        Assert.Equal(f.End, i.End);
    }

    [Property]
    public void IntervalUntil_widens_to_Interval(IntervalUntil<int> u)
    {
        Interval<int> i = u;
        Assert.Equal(u.Start, i.Start);
        Assert.Equal(u.End, i.End);
    }

    [Property]
    public void Widening_preserves_Contains(FiniteInterval<int> c, int value)
    {
        Interval<int> i = c;
        Assert.Equal(c.Contains(value), i.Contains(value));
    }

    [Property]
    public void AsFinite_roundtrips_a_widened_FiniteInterval(FiniteInterval<int> c)
    {
        Interval<int> widened = c;
        Assert.Equal(c, widened.AsFinite());
    }

    [Property]
    public void Interval_AsFinite_is_null_when_an_endpoint_is_open(Interval<int> i)
    {
        var closed = i.AsFinite();
        if (i.Start.HasValue && i.End.HasValue)
        {
            Assert.NotNull(closed);
            Assert.Equal(i.Start.Value, closed!.Value.Start);
            Assert.Equal(i.End.Value, closed.Value.End);
        }
        else
        {
            Assert.Null(closed);
        }
    }

    [Property]
    public void Interval_AsFrom_is_null_only_without_a_lower_bound(Interval<int> i)
    {
        var from = i.AsFrom();
        Assert.Equal(i.Start.HasValue, from.HasValue);
        if (from.HasValue)
        {
            Assert.Equal(i.Start!.Value, from.Value.Start);
            Assert.Equal(i.End, from.Value.End);
        }
    }

    [Property]
    public void Interval_AsUntil_is_null_only_without_an_upper_bound(Interval<int> i)
    {
        var until = i.AsUntil();
        Assert.Equal(i.End.HasValue, until.HasValue);
        if (until.HasValue)
        {
            Assert.Equal(i.Start, until.Value.Start);
            Assert.Equal(i.End!.Value, until.Value.End);
        }
    }

    [Property]
    public void IntervalFrom_AsFinite_is_null_only_when_open_ended(IntervalFrom<int> f)
    {
        var closed = f.AsFinite();
        Assert.Equal(f.End.HasValue, closed.HasValue);
        if (closed.HasValue)
        {
            Assert.Equal(f.Start, closed.Value.Start);
            Assert.Equal(f.End!.Value, closed.Value.End);
        }
    }

    [Property]
    public void IntervalUntil_AsFinite_is_null_only_when_open_started(IntervalUntil<int> u)
    {
        var closed = u.AsFinite();
        Assert.Equal(u.Start.HasValue, closed.HasValue);
        if (closed.HasValue)
        {
            Assert.Equal(u.Start!.Value, closed.Value.Start);
            Assert.Equal(u.End, closed.Value.End);
        }
    }

    [Property]
    public void Interval_ToFinite_matches_AsFinite_or_throws(Interval<int> i)
    {
        var closed = i.AsFinite();
        if (closed.HasValue)
            Assert.Equal(closed.Value, i.ToFinite());
        else
            Assert.Throws<System.InvalidOperationException>(() => i.ToFinite());
    }

    [Property]
    public void Interval_ToFrom_matches_AsFrom_or_throws(Interval<int> i)
    {
        var from = i.AsFrom();
        if (from.HasValue)
            Assert.Equal(from.Value, i.ToFrom());
        else
            Assert.Throws<System.InvalidOperationException>(() => i.ToFrom());
    }

    [Property]
    public void Interval_ToUntil_matches_AsUntil_or_throws(Interval<int> i)
    {
        var until = i.AsUntil();
        if (until.HasValue)
            Assert.Equal(until.Value, i.ToUntil());
        else
            Assert.Throws<System.InvalidOperationException>(() => i.ToUntil());
    }

    [Property]
    public void IntervalFrom_ToFinite_matches_AsFinite_or_throws(IntervalFrom<int> f)
    {
        var closed = f.AsFinite();
        if (closed.HasValue)
            Assert.Equal(closed.Value, f.ToFinite());
        else
            Assert.Throws<System.InvalidOperationException>(() => f.ToFinite());
    }

    [Property]
    public void IntervalUntil_ToFinite_matches_AsFinite_or_throws(IntervalUntil<int> u)
    {
        var closed = u.AsFinite();
        if (closed.HasValue)
            Assert.Equal(closed.Value, u.ToFinite());
        else
            Assert.Throws<System.InvalidOperationException>(() => u.ToFinite());
    }

    [Fact]
    public void Variants_widen_into_a_shared_Interval_collection()
    {
        var closed = FiniteInterval.Create(0, 10);
        var from = IntervalFrom.Create(5, null);
        var until = IntervalUntil.Create(null, 5);
        var open = Interval.Create<int>(null, null);

        Interval<int>[] all = [closed, from, until, open];

        Assert.Equal(4, all.Length);
        Assert.Equal(0, all[0].Start);
        Assert.Equal(10, all[0].End);
        Assert.Equal(5, all[1].Start);
        Assert.Null(all[1].End);
        Assert.Null(all[2].Start);
        Assert.Equal(5, all[2].End);
        Assert.Null(all[3].Start);
        Assert.Null(all[3].End);
    }
}
