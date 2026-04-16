#nullable enable

using System;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class PositiveExtensionsTests
{
    [Property]
    public void Sum_MatchesUnderlyingSum_WhenNoOverflow(int[] values)
    {
        // Filter to strictly-positive and cap magnitude so no overflow.
        var positives = values
            .Where(v => v > 0 && v < 1_000_000)
            .Select(v => Positive<int>.Create(v))
            .ToArray();
        if (positives.Length == 0)
        {
            return;
        }

        var expected = positives.Sum(p => p.Value);
        var actual = positives.Sum();
        Assert.Equal(expected, actual.Value);
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
}
