#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NonPositiveExtensionsTests
{
    // ── Sum ─────────────────────────────────────────────────────────────

    [Property]
    public void Sum_MatchesUnderlyingSum_WhenNoOverflow(int[] values)
    {
        var nonPositives = values
            .Where(v => v <= 0 && v > -1_000_000)
            .Select(v => NonPositive<int>.Create(v))
            .ToArray();

        var expected = nonPositives.Sum(n => n.Value);
        var actual = nonPositives.Sum();
        Assert.Equal(expected, actual.Value);
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
    public void Unwrap_ReturnsUnderlyingValue(int value)
    {
        if (NonPositive<int>.TryCreate(value) is not { } n) return;
        Assert.Equal(value, n.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Fact]
    public void Min_ReturnsSmallest()
    {
        var values = new[] { NonPositive<int>.Create(-7), NonPositive<int>.Create(0), NonPositive<int>.Create(-5) };
        Assert.Equal(-7, values.Min().Value);
    }

    [Fact]
    public void Max_ReturnsLargest()
    {
        var values = new[] { NonPositive<int>.Create(-7), NonPositive<int>.Create(0), NonPositive<int>.Create(-5) };
        Assert.Equal(0, values.Max().Value);
    }

    [Fact]
    public void Min_SingleElement_ReturnsThatElement()
    {
        Assert.Equal(-4, new[] { NonPositive<int>.Create(-4) }.Min().Value);
    }

    [Fact]
    public void Max_SingleElement_ReturnsThatElement()
    {
        Assert.Equal(-4, new[] { NonPositive<int>.Create(-4) }.Max().Value);
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
