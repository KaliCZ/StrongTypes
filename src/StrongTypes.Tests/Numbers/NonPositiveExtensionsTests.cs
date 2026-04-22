#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonPositiveExtensionsTests
{
    // ── Sum ─────────────────────────────────────────────────────────────

    [Property]
    public void Sum_MatchesUnderlyingSum_OrThrowsOnOverflow(NonPositive<int>[] values)
    {
        long expected = values.Sum(n => (long)n.Value);
        if (expected < int.MinValue)
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
        Assert.Equal(0, Array.Empty<NonPositive<int>>().Sum().Value);
    }

    [Fact]
    public void Sum_ThrowsOverflowException_OnIntUnderflow()
    {
        var values = new[] { NonPositive<int>.Create(int.MinValue), NonPositive<int>.Create(-1) };
        Assert.Throws<OverflowException>(() => values.Sum());
    }

    [Fact]
    public void Sum_Decimal_Works()
    {
        var values = new[] { NonPositive<decimal>.Create(0m), NonPositive<decimal>.Create(-2.25m) };
        Assert.Equal(-2.25m, values.Sum().Value);
    }

    [Fact]
    public void Sum_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NonPositive<int>>)null!).Sum());
    }

    // ── Unwrap ──────────────────────────────────────────────────────────

    [Property]
    public void Unwrap_ReturnsUnderlyingValue(NonPositive<int> n)
    {
        Assert.Equal(n.Value, n.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Property]
    public void Min_MatchesUnderlyingMin(NonPositive<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(n => n.Value).Min(), values.Min().Value);
    }

    [Property]
    public void Max_MatchesUnderlyingMax(NonPositive<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(n => n.Value).Max(), values.Max().Value);
    }

    [Fact]
    public void Min_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<NonPositive<int>>().Min());
    }

    [Fact]
    public void Max_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<NonPositive<int>>().Max());
    }

    [Fact]
    public void Min_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NonPositive<int>>)null!).Min());
    }

    [Fact]
    public void Max_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<NonPositive<int>>)null!).Max());
    }
}
