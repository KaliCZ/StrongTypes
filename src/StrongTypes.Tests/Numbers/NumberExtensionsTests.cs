#nullable enable

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
}
