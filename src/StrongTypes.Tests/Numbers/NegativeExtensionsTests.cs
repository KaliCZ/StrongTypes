#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NegativeExtensionsTests
{
    // ── Sum ─────────────────────────────────────────────────────────────

    [Property]
    public void Sum_MatchesUnderlyingSum_WhenNoOverflow(int[] values)
    {
        var negatives = values
            .Where(v => v < 0 && v > -1_000_000)
            .Select(v => Negative<int>.Create(v))
            .ToArray();
        if (negatives.Length == 0) return;

        var expected = negatives.Sum(n => n.Value);
        var actual = negatives.Sum();
        Assert.Equal(expected, actual.Value);
    }

    [Fact]
    public void Sum_EmptyThrows_BecauseZeroIsNotNegative()
    {
        Assert.Throws<ArgumentException>(() => Array.Empty<Negative<int>>().Sum());
    }

    [Fact]
    public void Sum_SingleElementReturnsThatElement()
    {
        var only = Negative<int>.Create(-42);
        Assert.Equal(-42, new[] { only }.Sum().Value);
    }

    [Fact]
    public void Sum_ThrowsOverflowException_OnIntUnderflow()
    {
        var values = new[] { Negative<int>.Create(int.MinValue), Negative<int>.Create(-1) };
        Assert.Throws<OverflowException>(() => values.Sum());
    }

    [Fact]
    public void Sum_Decimal_Works()
    {
        var values = new[] { Negative<decimal>.Create(-1.5m), Negative<decimal>.Create(-2.25m) };
        Assert.Equal(-3.75m, values.Sum().Value);
    }

    [Fact]
    public void Sum_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Negative<int>>)null!).Sum());
    }

    // ── Unwrap ──────────────────────────────────────────────────────────

    [Property]
    public void Unwrap_ReturnsUnderlyingValue(int value)
    {
        if (Negative<int>.TryCreate(value) is not { } n) return;
        Assert.Equal(value, n.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Fact]
    public void Min_ReturnsSmallest()
    {
        var values = new[] { Negative<int>.Create(-7), Negative<int>.Create(-2), Negative<int>.Create(-5) };
        Assert.Equal(-7, values.Min().Value);
    }

    [Fact]
    public void Max_ReturnsLargest()
    {
        var values = new[] { Negative<int>.Create(-7), Negative<int>.Create(-2), Negative<int>.Create(-5) };
        Assert.Equal(-2, values.Max().Value);
    }

    [Fact]
    public void Min_SingleElement_ReturnsThatElement()
    {
        Assert.Equal(-4, new[] { Negative<int>.Create(-4) }.Min().Value);
    }

    [Fact]
    public void Max_SingleElement_ReturnsThatElement()
    {
        Assert.Equal(-4, new[] { Negative<int>.Create(-4) }.Max().Value);
    }

    [Fact]
    public void Min_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<Negative<int>>().Min());
    }

    [Fact]
    public void Max_Empty_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Array.Empty<Negative<int>>().Max());
    }

    [Fact]
    public void Min_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Negative<int>>)null!).Min());
    }

    [Fact]
    public void Max_NullSource_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ((IEnumerable<Negative<int>>)null!).Max());
    }
}
