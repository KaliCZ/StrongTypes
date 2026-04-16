#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NegativeTests
{
    [Property]
    public void TryCreate_IntReturnsNonNullIffNegative(int value)
    {
        var result = Negative<int>.TryCreate(value);
        Assert.Equal(value < 0, result is not null);
        if (result is { } negative)
        {
            Assert.Equal(value, negative.Value);
        }
    }

    [Property]
    public void TryCreate_LongReturnsNonNullIffNegative(long value)
    {
        var result = Negative<long>.TryCreate(value);
        Assert.Equal(value < 0L, result is not null);
        if (result is { } negative)
        {
            Assert.Equal(value, negative.Value);
        }
    }

    [Property]
    public void TryCreate_DecimalReturnsNonNullIffNegative(decimal value)
    {
        var result = Negative<decimal>.TryCreate(value);
        Assert.Equal(value < 0m, result is not null);
        if (result is { } negative)
        {
            Assert.Equal(value, negative.Value);
        }
    }

    [Property]
    public void TryCreate_ShortReturnsNonNullIffNegative(short value)
    {
        var result = Negative<short>.TryCreate(value);
        Assert.Equal(value < 0, result is not null);
        if (result is { } negative)
        {
            Assert.Equal(value, negative.Value);
        }
    }

    [Property]
    public void Create_ThrowsIffNonNegative(int value)
    {
        if (value < 0)
        {
            Assert.Equal(value, Negative<int>.Create(value).Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Negative<int>.Create(value));
        }
    }

    [Property]
    public void ImplicitConversion_RecoversUnderlyingValue(int value)
    {
        if (Negative<int>.TryCreate(value) is not { } negative)
        {
            return;
        }

        int recovered = negative;
        Assert.Equal(value, recovered);
    }

    [Property]
    public void Equality_MatchesUnderlyingValue(int a, int b)
    {
        var na = Negative<int>.TryCreate(a);
        var nb = Negative<int>.TryCreate(b);
        if (na is null || nb is null)
        {
            return;
        }

        Assert.Equal(a == b, na.Value == nb.Value);
        Assert.Equal(a == b, na.Value.Equals(nb.Value));
    }

    [Property]
    public void Comparison_MatchesUnderlyingValue(int a, int b)
    {
        var na = Negative<int>.TryCreate(a);
        var nb = Negative<int>.TryCreate(b);
        if (na is null || nb is null)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(na.Value.CompareTo(nb.Value)));
        Assert.Equal(a < b, na.Value < nb.Value);
        Assert.Equal(a <= b, na.Value <= nb.Value);
        Assert.Equal(a > b, na.Value > nb.Value);
        Assert.Equal(a >= b, na.Value >= nb.Value);
    }

    [Property]
    public void CrossTypeEquality_MatchesUnderlyingValue(int value)
    {
        if (Negative<int>.TryCreate(value) is not { } negative)
        {
            return;
        }

        Assert.True(negative.Equals(value));
        Assert.True(negative == value);
        Assert.True(value == negative);
        Assert.False(negative != value);
    }

    [Property]
    public void CrossTypeComparison_MatchesUnderlyingValue(int a, int b)
    {
        if (Negative<int>.TryCreate(a) is not { } negative)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(negative.CompareTo(b)));
        Assert.Equal(a < b, negative < b);
        Assert.Equal(a <= b, negative <= b);
        Assert.Equal(a > b, negative > b);
        Assert.Equal(a >= b, negative >= b);
        Assert.Equal(b < a, b < negative);
        Assert.Equal(b <= a, b <= negative);
        Assert.Equal(b > a, b > negative);
        Assert.Equal(b >= a, b >= negative);
    }

    [Fact]
    public void Create_ZeroThrows()
    {
        Assert.Throws<ArgumentException>(() => Negative<int>.Create(0));
    }

    [Fact]
    public void TryCreate_ZeroReturnsNull()
    {
        Assert.Null(Negative<int>.TryCreate(0));
    }

    [Fact]
    public void Default_RepresentsNegativeOne()
    {
        Assert.Equal(-1, default(Negative<int>).Value);
        Assert.Equal(-1L, default(Negative<long>).Value);
        Assert.Equal(-1m, default(Negative<decimal>).Value);
        Assert.Equal((short)-1, default(Negative<short>).Value);
    }

    [Fact]
    public void Default_EqualsCreateNegativeOne()
    {
        Assert.Equal(Negative<int>.Create(-1), default(Negative<int>));
        Assert.True(default(Negative<int>) == Negative<int>.Create(-1));
        Assert.Equal(Negative<int>.Create(-1).GetHashCode(), default(Negative<int>).GetHashCode());
    }

    [Fact]
    public void RoundTrips_AtMinValue()
    {
        Assert.Equal(int.MinValue, Negative<int>.Create(int.MinValue).Value);
        Assert.Equal(long.MinValue, Negative<long>.Create(long.MinValue).Value);
    }
}
