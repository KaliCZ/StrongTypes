#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonNegativeExtensionsTests
{
    [Property]
    public void Sum_MatchesUnderlyingSum_OrThrowsOnOverflow(NonNegative<int>[] values)
    {
        long expected = values.Sum(n => (long)n.Value);
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
    public void Sum_EmptyReturnsZero()
    {
        Assert.Equal(0, Array.Empty<NonNegative<int>>().Sum().Value);
    }

    [Fact]
    public void Sum_ThrowsOverflowException_OnIntOverflow()
    {
        var values = new[] { NonNegative<int>.Create(int.MaxValue), NonNegative<int>.Create(1) };
        Assert.Throws<OverflowException>(() => values.Sum());
    }

    [Fact]
    public void Sum_Decimal_Works()
    {
        var values = new[] { NonNegative<decimal>.Create(0m), NonNegative<decimal>.Create(2.25m) };
        Assert.Equal(2.25m, values.Sum().Value);
    }

    [Fact]
    public void Sum_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NonNegative<int>>)null!).Sum());
    }

    // ── Unwrap ──────────────────────────────────────────────────────────

    [Property]
    public void Unwrap_ReturnsUnderlyingValue(NonNegative<int> n)
    {
        Assert.Equal(n.Value, n.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Property]
    public void Min_MatchesUnderlyingMin(NonNegative<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(n => n.Value).Min(), values.Min().Value);
    }

    [Property]
    public void Max_MatchesUnderlyingMax(NonNegative<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(n => n.Value).Max(), values.Max().Value);
    }

    [Fact]
    public void Min_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<NonNegative<int>>().Min());
    }

    [Fact]
    public void Max_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<NonNegative<int>>().Max());
    }

    [Fact]
    public void Min_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NonNegative<int>>)null!).Min());
    }

    [Fact]
    public void Max_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NonNegative<int>>)null!).Max());
    }
}
