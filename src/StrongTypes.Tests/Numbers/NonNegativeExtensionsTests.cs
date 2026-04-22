#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NonNegativeExtensionsTests
{
    [Property]
    public void Sum_MatchesUnderlyingSum_WhenNoOverflow(int[] values)
    {
        var nonNegatives = values
            .Where(v => v >= 0 && v < 1_000_000)
            .Select(v => NonNegative<int>.Create(v))
            .ToArray();

        var expected = nonNegatives.Sum(n => n.Value);
        var actual = nonNegatives.Sum();
        Assert.Equal(expected, actual.Value);
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
    public void Unwrap_ReturnsUnderlyingValue(int value)
    {
        if (NonNegative<int>.TryCreate(value) is not { } n) return;
        Assert.Equal(value, n.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Fact]
    public void Min_ReturnsSmallest()
    {
        var values = new[] { NonNegative<int>.Create(7), NonNegative<int>.Create(0), NonNegative<int>.Create(5) };
        Assert.Equal(0, values.Min().Value);
    }

    [Fact]
    public void Max_ReturnsLargest()
    {
        var values = new[] { NonNegative<int>.Create(7), NonNegative<int>.Create(0), NonNegative<int>.Create(5) };
        Assert.Equal(7, values.Max().Value);
    }

    [Fact]
    public void Min_SingleElement_ReturnsThatElement()
    {
        Assert.Equal(4, new[] { NonNegative<int>.Create(4) }.Min().Value);
    }

    [Fact]
    public void Max_SingleElement_ReturnsThatElement()
    {
        Assert.Equal(4, new[] { NonNegative<int>.Create(4) }.Max().Value);
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
