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
    public void TryCreate_ShortReturnsNonNullIffNonNegative(short value)
    {
        var result = NonNegative<short>.TryCreate(value);
        Assert.Equal(value >= 0, result is not null);
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
        Assert.Equal(a == b, na.Value.Equals(nb.Value));
        Assert.Equal(a == b, na.Value.Equals((object)nb.Value));
        Assert.False(na.Value.Equals((object?)null));
        Assert.False(na.Value.Equals("not a non-negative"));
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
        Assert.Equal(a < b, na.Value < nb.Value);
        Assert.Equal(a <= b, na.Value <= nb.Value);
        Assert.Equal(a > b, na.Value > nb.Value);
        Assert.Equal(a >= b, na.Value >= nb.Value);
    }

    [Property]
    public void CrossTypeEquality_MatchesUnderlyingValue(int value)
    {
        if (NonNegative<int>.TryCreate(value) is not { } nonNegative)
        {
            return;
        }

        Assert.True(nonNegative.Equals(value));
        Assert.True(nonNegative == value);
        Assert.True(value == nonNegative);
        Assert.False(nonNegative != value);
    }

    [Property]
    public void CrossTypeComparison_MatchesUnderlyingValue(int a, int b)
    {
        if (NonNegative<int>.TryCreate(a) is not { } nonNegative)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(nonNegative.CompareTo(b)));
        Assert.Equal(a < b, nonNegative < b);
        Assert.Equal(a <= b, nonNegative <= b);
        Assert.Equal(a > b, nonNegative > b);
        Assert.Equal(a >= b, nonNegative >= b);
        Assert.Equal(b < a, b < nonNegative);
        Assert.Equal(b <= a, b <= nonNegative);
        Assert.Equal(b > a, b > nonNegative);
        Assert.Equal(b >= a, b >= nonNegative);
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

    // ── Generated members: branch coverage ──────────────────────────────

    [Fact]
    public void ExplicitOperator_FromUnderlying_Wraps()
    {
        var n = (NonNegative<int>)5;
        Assert.Equal(5, n.Value);
    }

    [Fact]
    public void ExplicitOperator_FromUnderlying_ThrowsOnInvariantViolation()
    {
        Assert.Throws<ArgumentException>(() => (NonNegative<int>)(-1));
    }

    [Fact]
    public void Equals_BoxedUnderlying_MatchesValue()
    {
        var n = NonNegative<int>.Create(7);
        Assert.True(n.Equals((object)7));
        Assert.False(n.Equals((object)8));
    }

    [Fact]
    public void IComparable_CompareTo_Null_Returns1()
    {
        System.IComparable n = NonNegative<int>.Create(7);
        Assert.Equal(1, n.CompareTo(null));
    }

    [Fact]
    public void IComparable_CompareTo_BoxedSelf_MatchesUnderlying()
    {
        System.IComparable a = NonNegative<int>.Create(3);
        Assert.Equal(0, a.CompareTo(NonNegative<int>.Create(3)));
        Assert.True(a.CompareTo(NonNegative<int>.Create(5)) < 0);
        Assert.True(a.CompareTo(NonNegative<int>.Create(1)) > 0);
    }

    [Fact]
    public void IComparable_CompareTo_BoxedUnderlying_MatchesUnderlying()
    {
        System.IComparable a = NonNegative<int>.Create(3);
        Assert.Equal(0, a.CompareTo(3));
        Assert.True(a.CompareTo(5) < 0);
        Assert.True(a.CompareTo(1) > 0);
    }

    [Fact]
    public void IComparable_CompareTo_UnrelatedType_Throws()
    {
        System.IComparable n = NonNegative<int>.Create(3);
        Assert.Throws<ArgumentException>(() => n.CompareTo("not a number"));
    }
}
