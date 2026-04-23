using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NegativeExtensionsTests
{
    // ── Sum ─────────────────────────────────────────────────────────────

    [Property]
    public void Sum_MatchesUnderlyingSum_OrThrowsOnOverflow(Negative<int>[] values)
    {
        if (values.Length == 0) return; // empty is covered by a dedicated fact

        // Widen to long so we can detect overflow without repeating the
        // implementation's checked arithmetic.
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
    public void Unwrap_ReturnsUnderlyingValue(Negative<int> n)
    {
        Assert.Equal(n.Value, n.Unwrap());
    }

    // ── Min / Max ───────────────────────────────────────────────────────

    [Property]
    public void Min_MatchesUnderlyingMin(Negative<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(n => n.Value).Min(), values.Min().Value);
    }

    [Property]
    public void Max_MatchesUnderlyingMax(Negative<int>[] values)
    {
        if (values.Length == 0) return;
        Assert.Equal(values.Select(n => n.Value).Max(), values.Max().Value);
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
