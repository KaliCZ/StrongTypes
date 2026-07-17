using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

/// <summary>Without <see cref="IConvertible"/>, <see cref="RangeAttribute"/>'s numeric constructors hit an <see cref="InvalidCastException"/> in <c>Convert</c> that the attribute swallows — every value, in range or not, silently reports as invalid — so these pin the <c>Convert</c> bridge and the resulting validation behavior.</summary>
[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NumericConvertibleTests
{
    [Property]
    public void ConvertToInt32_MatchesTheUnderlyingValue(Positive<int> value) =>
        Assert.Equal(value.Value, Convert.ToInt32(value));

    [Property]
    public void ConvertToDouble_MatchesTheUnderlyingValue(Positive<int> value) =>
        Assert.Equal(value.Value, Convert.ToDouble(value));

    [Property]
    public void ChangeType_ConvertsToTheUnderlyingType(Positive<int> value) =>
        Assert.Equal(value.Value, Convert.ChangeType(value, typeof(int)));

    [Fact]
    public void EveryWrapper_IsConvertible()
    {
        Assert.Equal(42, Convert.ToInt32(Positive<int>.Create(42)));
        Assert.Equal(0, Convert.ToInt32(NonNegative<int>.Create(0)));
        Assert.Equal(-42, Convert.ToInt32(Negative<int>.Create(-42)));
        Assert.Equal(-42, Convert.ToInt32(NonPositive<int>.Create(-42)));
    }

    [Fact]
    public void ConvertToString_HonoursTheProvider() =>
        Assert.Equal("1234,5", Convert.ToString(Positive<decimal>.Create(1234.5m), CultureInfo.GetCultureInfo("de-DE")));

    [Fact]
    public void GetTypeCode_IsObject() =>
        Assert.Equal(TypeCode.Object, ((IConvertible)Positive<int>.Create(1)).GetTypeCode());

    // ── RangeAttribute ──────────────────────────────────────────────────

    private sealed class IntRangeModel
    {
        [Range(1, 100)] public Positive<int>? Quantity { get; set; }
    }

    private sealed class DoubleRangeModel
    {
        [Range(0.5, 2.5)] public Positive<double>? Factor { get; set; }
    }

    [Property]
    public void RangeAttribute_ValidatesTheUnderlyingValue(Positive<int> value)
    {
        var model = new IntRangeModel { Quantity = value };
        Assert.Equal(value.Value <= 100, Validate(model));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(50, true)]
    [InlineData(100, true)]
    [InlineData(101, false)]
    [InlineData(150, false)]
    public void RangeAttribute_IntBounds_AreHonoured(int value, bool expected) =>
        Assert.Equal(expected, Validate(new IntRangeModel { Quantity = Positive<int>.Create(value) }));

    [Theory]
    [InlineData(0.5, true)]
    [InlineData(1.5, true)]
    [InlineData(2.5, true)]
    [InlineData(0.4, false)]
    [InlineData(2.6, false)]
    public void RangeAttribute_DoubleBounds_AreHonoured(double value, bool expected) =>
        Assert.Equal(expected, Validate(new DoubleRangeModel { Factor = Positive<double>.Create(value) }));

    private static bool Validate(object model) =>
        Validator.TryValidateObject(model, new ValidationContext(model), null, validateAllProperties: true);
}
