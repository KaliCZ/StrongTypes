using System;
using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class PositiveTests
{
    [Property]
    public void TryCreate_IntReturnsNonNullIffPositive(int value)
    {
        var result = Positive<int>.TryCreate(value);
        Assert.Equal(value > 0, result is not null);
        if (result is { } positive)
        {
            Assert.Equal(value, positive.Value);
        }
    }

    [Property]
    public void TryCreate_LongReturnsNonNullIffPositive(long value)
    {
        var result = Positive<long>.TryCreate(value);
        Assert.Equal(value > 0L, result is not null);
        if (result is { } positive)
        {
            Assert.Equal(value, positive.Value);
        }
    }

    [Property]
    public void TryCreate_DecimalReturnsNonNullIffPositive(decimal value)
    {
        var result = Positive<decimal>.TryCreate(value);
        Assert.Equal(value > 0m, result is not null);
        if (result is { } positive)
        {
            Assert.Equal(value, positive.Value);
        }
    }

    [Property]
    public void TryCreate_ShortReturnsNonNullIffPositive(short value)
    {
        var result = Positive<short>.TryCreate(value);
        Assert.Equal(value > 0, result is not null);
        if (result is { } positive)
        {
            Assert.Equal(value, positive.Value);
        }
    }

    [Property]
    public void Create_ThrowsIffNonPositive(int value)
    {
        if (value > 0)
        {
            Assert.Equal(value, Positive<int>.Create(value).Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Positive<int>.Create(value));
        }
    }

    [Property]
    public void ImplicitConversion_RecoversUnderlyingValue(int value)
    {
        if (Positive<int>.TryCreate(value) is not { } positive)
        {
            return;
        }

        int recovered = positive;
        Assert.Equal(value, recovered);
    }

    [Property]
    public void Equality_MatchesUnderlyingValue(int a, int b)
    {
        var pa = Positive<int>.TryCreate(a);
        var pb = Positive<int>.TryCreate(b);
        if (pa is null || pb is null)
        {
            return;
        }

        Assert.Equal(a == b, pa.Value == pb.Value);
        Assert.Equal(a == b, pa.Value.Equals(pb.Value));
        Assert.Equal(a == b, pa.Value.Equals((object)pb.Value));
        Assert.False(pa.Value.Equals((object?)null));
        Assert.False(pa.Value.Equals("not a positive"));
    }

    [Property]
    public void Comparison_MatchesUnderlyingValue(int a, int b)
    {
        var pa = Positive<int>.TryCreate(a);
        var pb = Positive<int>.TryCreate(b);
        if (pa is null || pb is null)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(pa.Value.CompareTo(pb.Value)));
        Assert.Equal(a < b, pa.Value < pb.Value);
        Assert.Equal(a <= b, pa.Value <= pb.Value);
        Assert.Equal(a > b, pa.Value > pb.Value);
        Assert.Equal(a >= b, pa.Value >= pb.Value);
    }

    [Property]
    public void CrossTypeEquality_MatchesUnderlyingValue(int value)
    {
        if (Positive<int>.TryCreate(value) is not { } positive)
        {
            return;
        }

        Assert.True(positive.Equals(value));
        Assert.True(positive == value);
        Assert.True(value == positive);
        Assert.False(positive != value);
    }

    [Property]
    public void CrossTypeComparison_MatchesUnderlyingValue(int a, int b)
    {
        if (Positive<int>.TryCreate(a) is not { } positive)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(positive.CompareTo(b)));
        Assert.Equal(a < b, positive < b);
        Assert.Equal(a <= b, positive <= b);
        Assert.Equal(a > b, positive > b);
        Assert.Equal(a >= b, positive >= b);
        Assert.Equal(b < a, b < positive);
        Assert.Equal(b <= a, b <= positive);
        Assert.Equal(b > a, b > positive);
        Assert.Equal(b >= a, b >= positive);
    }

    [Fact]
    public void Create_ZeroThrows()
    {
        Assert.Throws<ArgumentException>(() => Positive<int>.Create(0));
    }

    [Fact]
    public void TryCreate_ZeroReturnsNull()
    {
        Assert.Null(Positive<int>.TryCreate(0));
    }

    [Fact]
    public void Default_RepresentsOne()
    {
        Assert.Equal(1, default(Positive<int>).Value);
        Assert.Equal(1L, default(Positive<long>).Value);
        Assert.Equal(1m, default(Positive<decimal>).Value);
        Assert.Equal((short)1, default(Positive<short>).Value);
    }

    [Fact]
    public void Default_EqualsCreateOne()
    {
        Assert.Equal(Positive<int>.Create(1), default(Positive<int>));
        Assert.True(default(Positive<int>) == Positive<int>.Create(1));
        Assert.Equal(Positive<int>.Create(1).GetHashCode(), default(Positive<int>).GetHashCode());
    }

    [Fact]
    public void RoundTrips_AtMaxValue()
    {
        Assert.Equal(int.MaxValue, Positive<int>.Create(int.MaxValue).Value);
        Assert.Equal(long.MaxValue, Positive<long>.Create(long.MaxValue).Value);
    }

    // ── Generated members: branch coverage ──────────────────────────────

    [Fact]
    public void ExplicitOperator_FromUnderlying_Wraps()
    {
        var positive = (Positive<int>)5;
        Assert.Equal(5, positive.Value);
    }

    [Fact]
    public void ExplicitOperator_FromUnderlying_ThrowsOnInvariantViolation()
    {
        Assert.Throws<ArgumentException>(() => (Positive<int>)0);
    }

    [Fact]
    public void Equals_BoxedUnderlying_MatchesValue()
    {
        var positive = Positive<int>.Create(7);
        Assert.True(positive.Equals((object)7));
        Assert.False(positive.Equals((object)8));
    }

    [Fact]
    public void IComparable_CompareTo_Null_Returns1()
    {
        System.IComparable positive = Positive<int>.Create(7);
        Assert.Equal(1, positive.CompareTo(null));
    }

    [Fact]
    public void IComparable_CompareTo_BoxedSelf_MatchesUnderlying()
    {
        System.IComparable a = Positive<int>.Create(3);
        Assert.Equal(0, a.CompareTo(Positive<int>.Create(3)));
        Assert.True(a.CompareTo(Positive<int>.Create(5)) < 0);
        Assert.True(a.CompareTo(Positive<int>.Create(1)) > 0);
    }

    [Fact]
    public void IComparable_CompareTo_BoxedUnderlying_MatchesUnderlying()
    {
        System.IComparable a = Positive<int>.Create(3);
        Assert.Equal(0, a.CompareTo(3));
        Assert.True(a.CompareTo(5) < 0);
        Assert.True(a.CompareTo(1) > 0);
    }

    [Fact]
    public void IComparable_CompareTo_UnrelatedType_Throws()
    {
        System.IComparable positive = Positive<int>.Create(3);
        Assert.Throws<ArgumentException>(() => positive.CompareTo("not a number"));
    }

    // ── IParsable ───────────────────────────────────────────────────────

    [Property]
    public void TryParse_ValidPositive_ReturnsTrueAndWrapsValue(int value)
    {
        if (value <= 0) return;
        var s = value.ToString(CultureInfo.InvariantCulture);
        Assert.True(Positive<int>.TryParse(s, CultureInfo.InvariantCulture, out var parsed));
        Assert.Equal(value, parsed.Value);
    }

    [Property]
    public void TryParse_NonPositive_ReturnsFalse(int value)
    {
        if (value > 0) return;
        var s = value.ToString(CultureInfo.InvariantCulture);
        Assert.False(Positive<int>.TryParse(s, CultureInfo.InvariantCulture, out var parsed));
        Assert.Equal(default, parsed);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-number")]
    [InlineData("3.14")]
    public void TryParse_NotANumber_ReturnsFalse(string? input)
    {
        Assert.False(Positive<int>.TryParse(input, CultureInfo.InvariantCulture, out var parsed));
        Assert.Equal(default, parsed);
    }

    [Fact]
    public void Parse_ValidPositive_WrapsValue() =>
        Assert.Equal(42, Positive<int>.Parse("42", CultureInfo.InvariantCulture).Value);

    [Fact]
    public void Parse_NonPositive_Throws() =>
        Assert.Throws<ArgumentException>(() => Positive<int>.Parse("0", CultureInfo.InvariantCulture));

    [Fact]
    public void Parse_NotANumber_Throws() =>
        Assert.Throws<FormatException>(() => Positive<int>.Parse("abc", CultureInfo.InvariantCulture));
}
