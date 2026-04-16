#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NonPositiveTests
{
    [Property]
    public void TryCreate_IntReturnsNonNullIffNonPositive(int value)
    {
        var result = NonPositive<int>.TryCreate(value);
        Assert.Equal(value <= 0, result is not null);
        if (result is { } nonPositive)
        {
            Assert.Equal(value, nonPositive.Value);
        }
    }

    [Property]
    public void TryCreate_LongReturnsNonNullIffNonPositive(long value)
    {
        var result = NonPositive<long>.TryCreate(value);
        Assert.Equal(value <= 0L, result is not null);
        if (result is { } nonPositive)
        {
            Assert.Equal(value, nonPositive.Value);
        }
    }

    [Property]
    public void TryCreate_DecimalReturnsNonNullIffNonPositive(decimal value)
    {
        var result = NonPositive<decimal>.TryCreate(value);
        Assert.Equal(value <= 0m, result is not null);
        if (result is { } nonPositive)
        {
            Assert.Equal(value, nonPositive.Value);
        }
    }

    [Property]
    public void TryCreate_ShortReturnsNonNullIffNonPositive(short value)
    {
        var result = NonPositive<short>.TryCreate(value);
        Assert.Equal(value <= 0, result is not null);
        if (result is { } nonPositive)
        {
            Assert.Equal(value, nonPositive.Value);
        }
    }

    [Property]
    public void Create_ThrowsIffPositive(int value)
    {
        if (value <= 0)
        {
            Assert.Equal(value, NonPositive<int>.Create(value).Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => NonPositive<int>.Create(value));
        }
    }

    [Property]
    public void ImplicitConversion_RecoversUnderlyingValue(int value)
    {
        if (NonPositive<int>.TryCreate(value) is not { } nonPositive)
        {
            return;
        }

        int recovered = nonPositive;
        Assert.Equal(value, recovered);
    }

    [Property]
    public void Equality_MatchesUnderlyingValue(int a, int b)
    {
        var na = NonPositive<int>.TryCreate(a);
        var nb = NonPositive<int>.TryCreate(b);
        if (na is null || nb is null)
        {
            return;
        }

        Assert.Equal(a == b, na.Value == nb.Value);
    }

    [Property]
    public void Comparison_MatchesUnderlyingValue(int a, int b)
    {
        var na = NonPositive<int>.TryCreate(a);
        var nb = NonPositive<int>.TryCreate(b);
        if (na is null || nb is null)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(na.Value.CompareTo(nb.Value)));
    }

    [Property]
    public void CrossTypeEquality_MatchesUnderlyingValue(int value)
    {
        if (NonPositive<int>.TryCreate(value) is not { } nonPositive)
        {
            return;
        }

        Assert.True(nonPositive.Equals(value));
        Assert.True(nonPositive == value);
        Assert.True(value == nonPositive);
        Assert.False(nonPositive != value);
    }

    [Property]
    public void CrossTypeComparison_MatchesUnderlyingValue(int a, int b)
    {
        if (NonPositive<int>.TryCreate(a) is not { } nonPositive)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(nonPositive.CompareTo(b)));
        Assert.Equal(a < b, nonPositive < b);
        Assert.Equal(a <= b, nonPositive <= b);
        Assert.Equal(a > b, nonPositive > b);
        Assert.Equal(a >= b, nonPositive >= b);
        Assert.Equal(b < a, b < nonPositive);
        Assert.Equal(b <= a, b <= nonPositive);
        Assert.Equal(b > a, b > nonPositive);
        Assert.Equal(b >= a, b >= nonPositive);
    }

    [Fact]
    public void TryCreate_ZeroSucceeds()
    {
        Assert.NotNull(NonPositive<int>.TryCreate(0));
        Assert.Equal(0, NonPositive<int>.Create(0).Value);
    }

    [Fact]
    public void Create_PositiveThrows()
    {
        Assert.Throws<ArgumentException>(() => NonPositive<int>.Create(1));
    }
}
