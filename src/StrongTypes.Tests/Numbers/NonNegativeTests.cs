#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class NonNegativeTests
{
    [Property]
    public void TryCreate_IntReturnsNonNullIffNonNegative(int value)
    {
        var result = NonNegative<int>.TryCreate(value);
        Assert.Equal(value >= 0, result is not null);
        if (result is { } nonNegative)
        {
            Assert.Equal(value, nonNegative.Value);
        }
    }

    [Property]
    public void TryCreate_LongReturnsNonNullIffNonNegative(long value)
    {
        var result = NonNegative<long>.TryCreate(value);
        Assert.Equal(value >= 0L, result is not null);
        if (result is { } nonNegative)
        {
            Assert.Equal(value, nonNegative.Value);
        }
    }

    [Property]
    public void TryCreate_DecimalReturnsNonNullIffNonNegative(decimal value)
    {
        var result = NonNegative<decimal>.TryCreate(value);
        Assert.Equal(value >= 0m, result is not null);
        if (result is { } nonNegative)
        {
            Assert.Equal(value, nonNegative.Value);
        }
    }

    [Property]
    public void Create_ThrowsIffNegative(int value)
    {
        if (value >= 0)
        {
            Assert.Equal(value, NonNegative<int>.Create(value).Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => NonNegative<int>.Create(value));
        }
    }

    [Property]
    public void ImplicitConversion_RecoversUnderlyingValue(int value)
    {
        if (NonNegative<int>.TryCreate(value) is not { } nonNegative)
        {
            return;
        }

        int recovered = nonNegative;
        Assert.Equal(value, recovered);
    }

    [Property]
    public void Equality_MatchesUnderlyingValue(int a, int b)
    {
        var na = NonNegative<int>.TryCreate(a);
        var nb = NonNegative<int>.TryCreate(b);
        if (na is null || nb is null)
        {
            return;
        }

        Assert.Equal(a == b, na.Value == nb.Value);
    }

    [Property]
    public void Comparison_MatchesUnderlyingValue(int a, int b)
    {
        var na = NonNegative<int>.TryCreate(a);
        var nb = NonNegative<int>.TryCreate(b);
        if (na is null || nb is null)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(na.Value.CompareTo(nb.Value)));
    }

    [Fact]
    public void TryCreate_ZeroSucceeds()
    {
        Assert.NotNull(NonNegative<int>.TryCreate(0));
        Assert.Equal(0, NonNegative<int>.Create(0).Value);
    }

    [Fact]
    public void Create_NegativeThrows()
    {
        Assert.Throws<ArgumentException>(() => NonNegative<int>.Create(-1));
    }
}
