using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class PositiveExtensionsTests
{
    [Property]
    public void Sum_MatchesUnderlyingSum_OrThrowsOnOverflow(Positive<int>[] values)
    {
        if (values.Length == 0) return; // empty is covered by a dedicated fact

        long expected = values.Sum(p => (long)p.Value);
        if (expected > int.MaxValue)
        {
            Assert.Throws<OverflowException>(() => values.Sum());
        }
        else
        {
            Assert.Equal((int)expected, values.Sum().Value);
        }
    }

    [Fact]
    public void Sum_EmptyThrows_BecauseZeroIsNotPositive()
    {
        Assert.Throws<ArgumentException>(() => Array.Empty<Positive<int>>().Sum());
    }

    [Fact]
    public void Sum_SingleElementReturnsThatElement()
    {
        var only = Positive<int>.Create(42);
        Assert.Equal(42, new[] { only }.Sum().Value);
    }

    [Fact]
    public void Sum_ThrowsOverflowException_OnIntOverflow()
    {
        var values = new[] { Positive<int>.Create(int.MaxValue), Positive<int>.Create(1) };
        Assert.Throws<OverflowException>(() => values.Sum());
    }

    [Fact]
    public void Sum_ThrowsOverflowException_OnLongOverflow()
    {
        var values = new[] { Positive<long>.Create(long.MaxValue), Positive<long>.Create(1L) };
        Assert.Throws<OverflowException>(() => values.Sum());
    }

    [Fact]
    public void Sum_Decimal_Works()
    {
        var values = new[] { Positive<decimal>.Create(1.5m), Positive<decimal>.Create(2.25m) };
        Assert.Equal(3.75m, values.Sum().Value);
    }

    [Fact]
    public void Sum_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Positive<int>>)null!).Sum());
    }

    // ── Unwrap ──────────────────────────────────────────────────────────

    [Property]
    public void Unwrap_ReturnsUnderlyingValue(Positive<int> p)
    {
        Assert.Equal(p.Value, p.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Property]
    public void Min_MatchesUnderlyingMin(Positive<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(p => p.Value).Min(), values.Min().Value);
    }

    [Property]
    public void Max_MatchesUnderlyingMax(Positive<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(p => p.Value).Max(), values.Max().Value);
    }

    [Fact]
    public void Min_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<Positive<int>>().Min());
    }

    [Fact]
    public void Max_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<Positive<int>>().Max());
    }

    [Fact]
    public void Min_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Positive<int>>)null!).Min());
    }

    [Fact]
    public void Max_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Positive<int>>)null!).Max());
    }
}
