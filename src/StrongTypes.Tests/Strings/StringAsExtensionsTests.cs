using System;
using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class StringAsExtensionsTests
{
    private enum Color { Red, Green, Blue }

    [Flags]
    private enum Permission { None = 0, Read = 1, Write = 2, Execute = 4 }

    // ------- Happy-path round-trips via the invariant culture -------

    [Property]
    public void AsByte_RoundTrips(byte value)
    {
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).AsByte(CultureInfo.InvariantCulture));
    }

    [Property]
    public void AsShort_RoundTrips(short value)
    {
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).AsShort(CultureInfo.InvariantCulture));
    }

    [Property]
    public void AsInt_RoundTrips(int value)
    {
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).AsInt(CultureInfo.InvariantCulture));
    }

    [Property]
    public void AsLong_RoundTrips(long value)
    {
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).AsLong(CultureInfo.InvariantCulture));
    }

    [Property]
    public void AsDecimal_RoundTrips(decimal value)
    {
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).AsDecimal(CultureInfo.InvariantCulture));
    }

    [Property]
    public void AsBool_RoundTrips(bool value)
    {
        Assert.Equal(value, value.ToString().AsBool());
    }

    [Property]
    public void AsGuid_RoundTrips(Guid value)
    {
        Assert.Equal(value, value.ToString().AsGuid());
    }

    // ------- Failure cases: null / whitespace / garbage all return null -------

    public static TheoryData<string?> BadNumericInputs()
    {
        var data = new TheoryData<string?>();
        data.Add((string?)null);
        data.Add(string.Empty);
        data.Add("   ");
        data.Add("ASDF");
        data.Add("1.2.3");
        return data;
    }

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void AsByte_ReturnsNull_OnBadInput(string? s) => Assert.Null(s.AsByte());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void AsInt_ReturnsNull_OnBadInput(string? s) => Assert.Null(s.AsInt());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void AsLong_ReturnsNull_OnBadInput(string? s) => Assert.Null(s.AsLong());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void AsDecimal_ReturnsNull_OnBadInput(string? s) => Assert.Null(s.AsDecimal());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void AsBool_ReturnsNull_OnBadInput(string? s) => Assert.Null(s.AsBool());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void AsGuid_ReturnsNull_OnBadInput(string? s) => Assert.Null(s.AsGuid());

    [Fact]
    public void AsByte_ReturnsNull_OnOverflow() => Assert.Null("258".AsByte());

    [Fact]
    public void AsShort_ReturnsNull_OnOverflow() => Assert.Null("32768".AsShort());

    [Fact]
    public void AsInt_ReturnsNull_OnOverflow() => Assert.Null("2147483648".AsInt());

    [Fact]
    public void AsDateTime_ParsesISO8601() =>
        Assert.Equal(new DateTime(2022, 1, 13, 16, 25, 35), "2022-01-13T16:25:35".AsDateTime());

    [Fact]
    public void AsTimeSpan_Parses() =>
        Assert.Equal(new TimeSpan(1, 12, 24, 2), "1.12:24:02".AsTimeSpan());

    // ------- AsEnum -------

    [Fact]
    public void AsEnum_ParsesDefinedNames()
    {
        Assert.Equal(Color.Red, "Red".AsEnum<Color>());
        Assert.Equal(Color.Green, "Green".AsEnum<Color>());
    }

    [Fact]
    public void AsEnum_IsCaseSensitiveByDefault()
    {
        Assert.Null("red".AsEnum<Color>());
        Assert.Equal(Color.Red, "red".AsEnum<Color>(ignoreCase: true));
    }

    [Fact]
    public void AsEnum_AcceptsCommaSeparatedFlagCombinations() =>
        // AsEnum now just delegates to Enum.TryParse, which accepts
        // comma-separated flag names for [Flags] enums.
        Assert.Equal(Permission.Read | Permission.Write, "Read, Write".AsEnum<Permission>());

    [Fact]
    public void AsEnum_RejectsUndefinedValues() =>
        Assert.Null("Purple".AsEnum<Color>());

    [Theory, InlineData(null), InlineData(""), InlineData("   ")]
    public void AsEnum_ReturnsNull_OnEmptyInput(string? s) => Assert.Null(s.AsEnum<Color>());

    // ------- Throwing To* variants: happy path -------

    [Property]
    public void ToByte_RoundTrips(byte value) =>
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).ToByte(CultureInfo.InvariantCulture));

    [Property]
    public void ToShort_RoundTrips(short value) =>
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).ToShort(CultureInfo.InvariantCulture));

    [Property]
    public void ToInt_RoundTrips(int value) =>
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).ToInt(CultureInfo.InvariantCulture));

    [Property]
    public void ToLong_RoundTrips(long value) =>
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).ToLong(CultureInfo.InvariantCulture));

    [Property]
    public void ToDecimal_RoundTrips(decimal value) =>
        Assert.Equal(value, value.ToString(CultureInfo.InvariantCulture).ToDecimal(CultureInfo.InvariantCulture));

    [Property]
    public void ToBool_RoundTrips(bool value) =>
        Assert.Equal(value, value.ToString().ToBool());

    [Property]
    public void ToGuid_RoundTrips(Guid value) =>
        Assert.Equal(value, value.ToString().ToGuid());

    [Fact]
    public void ToNonEmpty_RoundTrips()
    {
        NonEmptyString s = "hello".ToNonEmpty();
        Assert.Equal("hello", s.Value);
    }

    [Fact]
    public void ToDateTime_ParsesISO8601() =>
        Assert.Equal(new DateTime(2022, 1, 13, 16, 25, 35), "2022-01-13T16:25:35".ToDateTime());

    [Fact]
    public void ToTimeSpan_Parses() =>
        Assert.Equal(new TimeSpan(1, 12, 24, 2), "1.12:24:02".ToTimeSpan());

    [Fact]
    public void ToEnum_ParsesDefinedName() =>
        Assert.Equal(Color.Red, "Red".ToEnum<Color>());

    [Fact]
    public void ToEnum_IgnoreCase() =>
        Assert.Equal(Color.Red, "red".ToEnum<Color>(ignoreCase: true));

    // ------- Throwing To* variants: failure paths -------

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void ToInt_Throws_OnBadInput(string? s) => Assert.ThrowsAny<Exception>(() => s.ToInt());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void ToLong_Throws_OnBadInput(string? s) => Assert.ThrowsAny<Exception>(() => s.ToLong());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void ToDecimal_Throws_OnBadInput(string? s) => Assert.ThrowsAny<Exception>(() => s.ToDecimal());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void ToBool_Throws_OnBadInput(string? s) => Assert.ThrowsAny<Exception>(() => s.ToBool());

    [Theory, MemberData(nameof(BadNumericInputs))]
    public void ToGuid_Throws_OnBadInput(string? s) => Assert.ThrowsAny<Exception>(() => s.ToGuid());

    [Fact]
    public void ToByte_Throws_OnOverflow() => Assert.Throws<OverflowException>(() => "258".ToByte());

    [Fact]
    public void ToInt_Throws_OnOverflow() => Assert.Throws<OverflowException>(() => "2147483648".ToInt());

    [Fact]
    public void ToInt_Throws_OnFormatError() => Assert.Throws<FormatException>(() => "ASDF".ToInt());

    [Fact]
    public void ToEnum_Throws_OnUndefinedValue() =>
        Assert.Throws<ArgumentException>(() => "Purple".ToEnum<Color>());

    [Theory, InlineData(null), InlineData(""), InlineData("   ")]
    public void ToNonEmpty_Throws_OnEmptyInput(string? s) =>
        Assert.Throws<ArgumentException>(() => s.ToNonEmpty());
}
