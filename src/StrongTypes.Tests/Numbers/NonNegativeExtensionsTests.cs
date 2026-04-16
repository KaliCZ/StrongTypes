#nullable enable

using System;
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
}
