using System;
using System.Text.Json;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public readonly struct PageSizeBounds : IBounds<int>
{
    public static int Min => 1;
    public static int Max => 100;
}

public readonly struct SignedTinyBounds : IBounds<int>
{
    public static int Min => -3;
    public static int Max => 3;
}

public readonly struct SingletonBounds : IBounds<int>
{
    public static int Min => 7;
    public static int Max => 7;
}

public class BoundedIntTests
{
    private static bool IsInPageRange(int value) =>
        value >= PageSizeBounds.Min && value <= PageSizeBounds.Max;

    private static bool IsInSignedRange(int value) =>
        value >= SignedTinyBounds.Min && value <= SignedTinyBounds.Max;

    [Property]
    public void TryCreate_NonNullIffWithinRange(int value)
    {
        var result = BoundedInt<PageSizeBounds>.TryCreate(value);
        Assert.Equal(IsInPageRange(value), result is not null);
        if (result is { } bounded)
        {
            Assert.Equal(value, bounded.Value);
        }
    }

    [Property]
    public void TryCreate_NegativeRange_NonNullIffWithinRange(int value)
    {
        var result = BoundedInt<SignedTinyBounds>.TryCreate(value);
        Assert.Equal(IsInSignedRange(value), result is not null);
        if (result is { } bounded)
        {
            Assert.Equal(value, bounded.Value);
        }
    }

    [Property]
    public void Create_ThrowsIffOutsideRange(int value)
    {
        if (IsInPageRange(value))
        {
            Assert.Equal(value, BoundedInt<PageSizeBounds>.Create(value).Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => BoundedInt<PageSizeBounds>.Create(value));
        }
    }

    [Property]
    public void ImplicitConversion_RecoversUnderlyingValue(int value)
    {
        if (BoundedInt<PageSizeBounds>.TryCreate(value) is not { } bounded)
        {
            return;
        }

        int recovered = bounded;
        Assert.Equal(value, recovered);
    }

    [Property]
    public void Equality_MatchesUnderlyingValue(int a, int b)
    {
        var ba = BoundedInt<PageSizeBounds>.TryCreate(a);
        var bb = BoundedInt<PageSizeBounds>.TryCreate(b);
        if (ba is null || bb is null)
        {
            return;
        }

        Assert.Equal(a == b, ba.Value == bb.Value);
        Assert.Equal(a == b, ba.Value.Equals(bb.Value));
        Assert.Equal(a == b, ba.Value.Equals((object)bb.Value));
        Assert.False(ba.Value.Equals((object?)null));
        Assert.False(ba.Value.Equals("not a bounded"));
    }

    [Property]
    public void Comparison_MatchesUnderlyingValue(int a, int b)
    {
        var ba = BoundedInt<PageSizeBounds>.TryCreate(a);
        var bb = BoundedInt<PageSizeBounds>.TryCreate(b);
        if (ba is null || bb is null)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(ba.Value.CompareTo(bb.Value)));
        Assert.Equal(a < b, ba.Value < bb.Value);
        Assert.Equal(a <= b, ba.Value <= bb.Value);
        Assert.Equal(a > b, ba.Value > bb.Value);
        Assert.Equal(a >= b, ba.Value >= bb.Value);
    }

    [Property]
    public void CrossTypeEquality_MatchesUnderlyingValue(int value)
    {
        if (BoundedInt<PageSizeBounds>.TryCreate(value) is not { } bounded)
        {
            return;
        }

        Assert.True(bounded.Equals(value));
        Assert.True(bounded == value);
        Assert.True(value == bounded);
        Assert.False(bounded != value);
    }

    [Property]
    public void CrossTypeComparison_MatchesUnderlyingValue(int a, int b)
    {
        if (BoundedInt<PageSizeBounds>.TryCreate(a) is not { } bounded)
        {
            return;
        }

        Assert.Equal(Math.Sign(a.CompareTo(b)), Math.Sign(bounded.CompareTo(b)));
        Assert.Equal(a < b, bounded < b);
        Assert.Equal(a <= b, bounded <= b);
        Assert.Equal(a > b, bounded > b);
        Assert.Equal(a >= b, bounded >= b);
        Assert.Equal(b < a, b < bounded);
        Assert.Equal(b <= a, b <= bounded);
        Assert.Equal(b > a, b > bounded);
        Assert.Equal(b >= a, b >= bounded);
    }

    // ── Boundary values ─────────────────────────────────────────────────

    [Fact]
    public void Min_IsAccepted()
    {
        Assert.Equal(PageSizeBounds.Min, BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Min).Value);
        Assert.Equal(SignedTinyBounds.Min, BoundedInt<SignedTinyBounds>.Create(SignedTinyBounds.Min).Value);
    }

    [Fact]
    public void Max_IsAccepted()
    {
        Assert.Equal(PageSizeBounds.Max, BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Max).Value);
        Assert.Equal(SignedTinyBounds.Max, BoundedInt<SignedTinyBounds>.Create(SignedTinyBounds.Max).Value);
    }

    [Fact]
    public void JustBelowMin_IsRejected()
    {
        Assert.Null(BoundedInt<PageSizeBounds>.TryCreate(PageSizeBounds.Min - 1));
        Assert.Throws<ArgumentException>(() => BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Min - 1));
    }

    [Fact]
    public void JustAboveMax_IsRejected()
    {
        Assert.Null(BoundedInt<PageSizeBounds>.TryCreate(PageSizeBounds.Max + 1));
        Assert.Throws<ArgumentException>(() => BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Max + 1));
    }

    [Fact]
    public void SingletonRange_OnlyAcceptsTheOneValue()
    {
        Assert.Equal(7, BoundedInt<SingletonBounds>.Create(7).Value);
        Assert.Null(BoundedInt<SingletonBounds>.TryCreate(6));
        Assert.Null(BoundedInt<SingletonBounds>.TryCreate(8));
    }

    // ── Default ─────────────────────────────────────────────────────────

    [Fact]
    public void Default_RepresentsMin_AndSatisfiesInvariant()
    {
        Assert.Equal(PageSizeBounds.Min, default(BoundedInt<PageSizeBounds>).Value);
        Assert.Equal(SignedTinyBounds.Min, default(BoundedInt<SignedTinyBounds>).Value);
    }

    [Fact]
    public void Default_EqualsCreateMin()
    {
        Assert.Equal(BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Min), default(BoundedInt<PageSizeBounds>));
        Assert.True(default(BoundedInt<PageSizeBounds>) == BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Min));
        Assert.Equal(
            BoundedInt<PageSizeBounds>.Create(PageSizeBounds.Min).GetHashCode(),
            default(BoundedInt<PageSizeBounds>).GetHashCode());
    }

    // ── Static Min/Max surface ──────────────────────────────────────────

    [Fact]
    public void StaticMinMax_ReflectBounds()
    {
        Assert.Equal(PageSizeBounds.Min, BoundedInt<PageSizeBounds>.Min);
        Assert.Equal(PageSizeBounds.Max, BoundedInt<PageSizeBounds>.Max);
    }

    // ── Generated members: branch coverage ──────────────────────────────

    [Fact]
    public void ExplicitOperator_FromUnderlying_Wraps()
    {
        var bounded = (BoundedInt<PageSizeBounds>)50;
        Assert.Equal(50, bounded.Value);
    }

    [Fact]
    public void ExplicitOperator_FromUnderlying_ThrowsOnInvariantViolation()
    {
        Assert.Throws<ArgumentException>(() => (BoundedInt<PageSizeBounds>)0);
        Assert.Throws<ArgumentException>(() => (BoundedInt<PageSizeBounds>)101);
    }

    [Fact]
    public void Create_ErrorMessage_MentionsBounds()
    {
        var ex = Assert.Throws<ArgumentException>(() => BoundedInt<PageSizeBounds>.Create(0));
        Assert.Contains("1", ex.Message);
        Assert.Contains("100", ex.Message);
    }

    [Fact]
    public void Equals_BoxedUnderlying_MatchesValue()
    {
        var bounded = BoundedInt<PageSizeBounds>.Create(50);
        Assert.True(bounded.Equals((object)50));
        Assert.False(bounded.Equals((object)51));
    }

    [Fact]
    public void IComparable_CompareTo_Null_Returns1()
    {
        System.IComparable bounded = BoundedInt<PageSizeBounds>.Create(50);
        Assert.Equal(1, bounded.CompareTo(null));
    }

    [Fact]
    public void IComparable_CompareTo_BoxedSelf_MatchesUnderlying()
    {
        System.IComparable a = BoundedInt<PageSizeBounds>.Create(30);
        Assert.Equal(0, a.CompareTo(BoundedInt<PageSizeBounds>.Create(30)));
        Assert.True(a.CompareTo(BoundedInt<PageSizeBounds>.Create(50)) < 0);
        Assert.True(a.CompareTo(BoundedInt<PageSizeBounds>.Create(10)) > 0);
    }

    [Fact]
    public void IComparable_CompareTo_BoxedUnderlying_MatchesUnderlying()
    {
        System.IComparable a = BoundedInt<PageSizeBounds>.Create(30);
        Assert.Equal(0, a.CompareTo(30));
        Assert.True(a.CompareTo(50) < 0);
        Assert.True(a.CompareTo(10) > 0);
    }

    [Fact]
    public void IComparable_CompareTo_UnrelatedType_Throws()
    {
        System.IComparable bounded = BoundedInt<PageSizeBounds>.Create(50);
        Assert.Throws<ArgumentException>(() => bounded.CompareTo("not a number"));
    }

    [Fact]
    public void ToString_ReturnsUnderlyingString()
    {
        var bounded = BoundedInt<PageSizeBounds>.Create(42);
        Assert.Equal("42", bounded.ToString());
    }

    // ── Extensions ──────────────────────────────────────────────────────

    [Property]
    public void AsBounded_NonNullIffWithinRange(int value)
    {
        var result = value.AsBounded<PageSizeBounds>();
        Assert.Equal(IsInPageRange(value), result is not null);
        if (result is { } bounded)
        {
            Assert.Equal(value, bounded.Value);
        }
    }

    [Property]
    public void ToBounded_ThrowsIffOutsideRange(int value)
    {
        if (IsInPageRange(value))
        {
            Assert.Equal(value, value.ToBounded<PageSizeBounds>().Value);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => value.ToBounded<PageSizeBounds>());
        }
    }

    // ── JSON ────────────────────────────────────────────────────────────

    [Property]
    public void Json_RoundTrips(int value)
    {
        if (BoundedInt<PageSizeBounds>.TryCreate(value) is not { } bounded)
        {
            return;
        }

        var json = JsonSerializer.Serialize(bounded);
        Assert.Equal(value.ToString(), json);
        Assert.Equal(bounded, JsonSerializer.Deserialize<BoundedInt<PageSizeBounds>>(json));
    }

    [Fact]
    public void Json_OutOfRange_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<BoundedInt<PageSizeBounds>>("0"));
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<BoundedInt<PageSizeBounds>>("101"));
    }
}
