using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NumberExtensionsTests
{
    [Fact]
    public void Divide_Int_ReturnsQuotient()
    {
        Assert.Equal(0.5m, 1.Divide(2));
        Assert.Equal(1.5m, 3.Divide(2));
    }

    [Fact]
    public void Divide_Int_ReturnsNull_WhenDivisorIsZero()
    {
        Assert.Null(1.Divide(0));
        Assert.Null(3489.Divide(0));
    }

    [Fact]
    public void Divide_Decimal_ReturnsQuotient()
    {
        Assert.Equal(0.5m, 1m.Divide(2));
        Assert.Equal(1.5m, 3m.Divide(2));
    }

    [Fact]
    public void Divide_Decimal_ReturnsNull_WhenDivisorIsZero()
    {
        Assert.Null(1m.Divide(0));
        Assert.Null(3489m.Divide(0));
    }

    [Property]
    public void Divide_Int_MatchesRawDivision_WhenNonZero(int a, decimal b)
    {
        if (b == 0)
        {
            return;
        }
        Assert.Equal(a / b, a.Divide(b));
    }

    [Property]
    public void Divide_Decimal_MatchesRawDivision_WhenNonZero(decimal a, decimal b)
    {
        if (b == 0)
        {
            return;
        }
        Assert.Equal(a / b, a.Divide(b));
    }

    [Fact]
    public void SafeDivide_Int_ReturnsQuotient()
    {
        Assert.Equal(0.5m, 1.SafeDivide(2));
        Assert.Equal(1.5m, 3.SafeDivide(2));
    }

    [Fact]
    public void SafeDivide_Int_ReturnsFallback_WhenDivisorIsZero()
    {
        Assert.Equal(14.33m, 1.SafeDivide(0, 14.33m));
        Assert.Equal(12.12m, 3489.SafeDivide(0, 12.12m));
        Assert.Equal(0m, 1.SafeDivide(0));
    }

    [Fact]
    public void SafeDivide_Decimal_ReturnsQuotient()
    {
        Assert.Equal(0.5m, 1m.SafeDivide(2));
        Assert.Equal(1.5m, 3m.SafeDivide(2));
    }

    [Fact]
    public void SafeDivide_Decimal_ReturnsFallback_WhenDivisorIsZero()
    {
        Assert.Equal(14.33m, 1m.SafeDivide(0, 14.33m));
        Assert.Equal(12.12m, 3489m.SafeDivide(0, 12.12m));
        Assert.Equal(0m, 1m.SafeDivide(0));
    }

    [Property]
    public void SafeDivide_Int_EqualsDivideOrFallback(int a, decimal b, decimal otherwise)
    {
        Assert.Equal(a.Divide(b) ?? otherwise, a.SafeDivide(b, otherwise));
    }

    [Property]
    public void SafeDivide_Decimal_EqualsDivideOrFallback(decimal a, decimal b, decimal otherwise)
    {
        Assert.Equal(a.Divide(b) ?? otherwise, a.SafeDivide(b, otherwise));
    }

    [Property]
    public void AsPositive_Int_ReturnsNonNullIffPositive(int value)
    {
        var result = value.AsPositive();
        Assert.Equal(value > 0, result is not null);
        if (result is { } p) Assert.Equal(value, p.Value);
    }

    [Property]
    public void AsPositive_Long_ReturnsNonNullIffPositive(long value)
    {
        var result = value.AsPositive();
        Assert.Equal(value > 0L, result is not null);
        if (result is { } p) Assert.Equal(value, p.Value);
    }

    [Property]
    public void AsPositive_Decimal_ReturnsNonNullIffPositive(decimal value)
    {
        var result = value.AsPositive();
        Assert.Equal(value > 0m, result is not null);
        if (result is { } p) Assert.Equal(value, p.Value);
    }

    [Property]
    public void AsPositive_Short_ReturnsNonNullIffPositive(short value)
    {
        var result = value.AsPositive();
        Assert.Equal(value > 0, result is not null);
        if (result is { } p) Assert.Equal(value, p.Value);
    }

    [Property]
    public void AsNonNegative_Int_ReturnsNonNullIffNonNegative(int value)
    {
        var result = value.AsNonNegative();
        Assert.Equal(value >= 0, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNonNegative_Long_ReturnsNonNullIffNonNegative(long value)
    {
        var result = value.AsNonNegative();
        Assert.Equal(value >= 0L, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNonNegative_Decimal_ReturnsNonNullIffNonNegative(decimal value)
    {
        var result = value.AsNonNegative();
        Assert.Equal(value >= 0m, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNegative_Int_ReturnsNonNullIffNegative(int value)
    {
        var result = value.AsNegative();
        Assert.Equal(value < 0, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNegative_Long_ReturnsNonNullIffNegative(long value)
    {
        var result = value.AsNegative();
        Assert.Equal(value < 0L, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNegative_Decimal_ReturnsNonNullIffNegative(decimal value)
    {
        var result = value.AsNegative();
        Assert.Equal(value < 0m, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNonPositive_Int_ReturnsNonNullIffNonPositive(int value)
    {
        var result = value.AsNonPositive();
        Assert.Equal(value <= 0, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNonPositive_Long_ReturnsNonNullIffNonPositive(long value)
    {
        var result = value.AsNonPositive();
        Assert.Equal(value <= 0L, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Property]
    public void AsNonPositive_Decimal_ReturnsNonNullIffNonPositive(decimal value)
    {
        var result = value.AsNonPositive();
        Assert.Equal(value <= 0m, result is not null);
        if (result is { } n) Assert.Equal(value, n.Value);
    }

    [Fact]
    public void AsPositive_Zero_ReturnsNull()
    {
        Assert.Null(0.AsPositive());
        Assert.Null(0m.AsPositive());
    }

    [Fact]
    public void AsNonNegative_Zero_ReturnsZero()
    {
        var wrapped = 0.AsNonNegative();
        Assert.NotNull(wrapped);
        Assert.Equal(0, wrapped.Value.Value);
    }

    [Fact]
    public void AsNegative_Zero_ReturnsNull()
    {
        Assert.Null(0.AsNegative());
        Assert.Null(0m.AsNegative());
    }

    [Fact]
    public void AsNonPositive_Zero_ReturnsZero()
    {
        var wrapped = 0.AsNonPositive();
        Assert.NotNull(wrapped);
        Assert.Equal(0, wrapped.Value.Value);
    }

    [Property]
    public void ToPositive_Int_MatchesCreate(int value)
    {
        if (value > 0)
        {
            Assert.Equal(value, value.ToPositive().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToPositive());
        }
    }

    [Property]
    public void ToPositive_Long_MatchesCreate(long value)
    {
        if (value > 0L)
        {
            Assert.Equal(value, value.ToPositive().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToPositive());
        }
    }

    [Property]
    public void ToPositive_Decimal_MatchesCreate(decimal value)
    {
        if (value > 0m)
        {
            Assert.Equal(value, value.ToPositive().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToPositive());
        }
    }

    [Property]
    public void ToNonNegative_Int_MatchesCreate(int value)
    {
        if (value >= 0)
        {
            Assert.Equal(value, value.ToNonNegative().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNonNegative());
        }
    }

    [Property]
    public void ToNonNegative_Decimal_MatchesCreate(decimal value)
    {
        if (value >= 0m)
        {
            Assert.Equal(value, value.ToNonNegative().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNonNegative());
        }
    }

    [Property]
    public void ToNegative_Int_MatchesCreate(int value)
    {
        if (value < 0)
        {
            Assert.Equal(value, value.ToNegative().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNegative());
        }
    }

    [Property]
    public void ToNegative_Long_MatchesCreate(long value)
    {
        if (value < 0L)
        {
            Assert.Equal(value, value.ToNegative().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNegative());
        }
    }

    [Property]
    public void ToNegative_Decimal_MatchesCreate(decimal value)
    {
        if (value < 0m)
        {
            Assert.Equal(value, value.ToNegative().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNegative());
        }
    }

    [Property]
    public void ToNonPositive_Int_MatchesCreate(int value)
    {
        if (value <= 0)
        {
            Assert.Equal(value, value.ToNonPositive().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNonPositive());
        }
    }

    [Property]
    public void ToNonPositive_Decimal_MatchesCreate(decimal value)
    {
        if (value <= 0m)
        {
            Assert.Equal(value, value.ToNonPositive().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToNonPositive());
        }
    }

    [Fact]
    public void ToPositive_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() => 0.ToPositive());
    }

    [Fact]
    public void ToNegative_Zero_Throws()
    {
        Assert.Throws<ArgumentException>(() => 0.ToNegative());
    }

    [Fact]
    public void ToNonNegative_Zero_ReturnsZero()
    {
        Assert.Equal(0, 0.ToNonNegative().Value);
    }

    [Fact]
    public void ToNonPositive_Zero_ReturnsZero()
    {
        Assert.Equal(0, 0.ToNonPositive().Value);
    }
}
