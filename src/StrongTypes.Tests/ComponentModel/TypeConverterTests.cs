using System;
using System.ComponentModel;
using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class TypeConverterTests
{
    public static TheoryData<Type, string> ConvertibleTypes => new()
    {
        { typeof(NonEmptyString), "hello" },
        { typeof(Email), "ops@example.com" },
        { typeof(Digit), "7" },
        { typeof(Positive<int>), "42" },
        { typeof(NonNegative<int>), "0" },
        { typeof(Negative<int>), "-42" },
        { typeof(NonPositive<long>), "0" },
    };

    /// <summary>Anything reaching a strong type through <see cref="TypeDescriptor"/> — configuration binding, WPF/WinForms bindings, designers — starts here.</summary>
    [Theory]
    [MemberData(nameof(ConvertibleTypes))]
    public void TypeDescriptor_ResolvesAConverterThatAcceptsStrings(Type type, string wire)
    {
        var converter = TypeDescriptor.GetConverter(type);

        Assert.True(converter.CanConvertFrom(typeof(string)));
        Assert.True(converter.CanConvertTo(typeof(string)));

        var converted = converter.ConvertFromInvariantString(wire);

        Assert.NotNull(converted);
        Assert.IsType(type, converted);
        Assert.Equal(wire, converter.ConvertToInvariantString(converted));
    }

    [Theory]
    [MemberData(nameof(ConvertibleTypes))]
    public void Converter_RejectsNonStringSources(Type type, string wire)
    {
        _ = wire;
        var converter = TypeDescriptor.GetConverter(type);

        Assert.False(converter.CanConvertFrom(typeof(Guid)));
        Assert.Throws<NotSupportedException>(() => converter.ConvertFrom(Guid.NewGuid()));
    }

    /// <summary>A broken invariant surfaces as the wrapper's own <see cref="ArgumentException"/>, not a generic conversion failure — that message is what the caller reports.</summary>
    [Theory]
    [InlineData(typeof(Positive<int>), "-5")]
    [InlineData(typeof(NonNegative<int>), "-1")]
    [InlineData(typeof(NonEmptyString), "   ")]
    [InlineData(typeof(Email), "not-an-email")]
    [InlineData(typeof(Digit), "42")]
    public void Converter_InvariantBreach_ThrowsArgumentException(Type type, string wire) =>
        Assert.Throws<ArgumentException>(() => TypeDescriptor.GetConverter(type).ConvertFromInvariantString(wire));

    [Fact]
    public void Converter_MalformedNumber_ThrowsFormatException() =>
        Assert.Throws<FormatException>(() => TypeDescriptor.GetConverter(typeof(Positive<int>)).ConvertFromInvariantString("not-a-number"));

    // ── Culture ─────────────────────────────────────────────────────────

    [Fact]
    public void Converter_ParsesInTheSuppliedCulture()
    {
        var converter = TypeDescriptor.GetConverter(typeof(Positive<decimal>));
        var german = CultureInfo.GetCultureInfo("de-DE");

        Assert.Equal(1234.5m, ((Positive<decimal>)converter.ConvertFrom(null, german, "1234,5")!).Value);
        Assert.Equal(1234.5m, ((Positive<decimal>)converter.ConvertFrom(null, CultureInfo.InvariantCulture, "1234.5")!).Value);
    }

    /// <summary><c>ConvertTo</c> must format in the same culture <c>ConvertFrom</c> parses in, or the pair does not round-trip.</summary>
    [Theory]
    [InlineData("de-DE")]
    [InlineData("en-US")]
    [InlineData("")]
    public void Converter_RoundTripsThroughOneCulture(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        var converter = TypeDescriptor.GetConverter(typeof(Positive<decimal>));
        var original = Positive<decimal>.Create(1234.5m);

        var text = converter.ConvertTo(null, culture, original, typeof(string));
        var roundTripped = converter.ConvertFrom(null, culture, text!);

        Assert.Equal(original, roundTripped);
    }

    [Property]
    public void Converter_RoundTripsEveryPositiveInt(int value)
    {
        if (value <= 0) return;

        var converter = TypeDescriptor.GetConverter(typeof(Positive<int>));
        var text = converter.ConvertToInvariantString(Positive<int>.Create(value));

        Assert.Equal(Positive<int>.Create(value), converter.ConvertFromInvariantString(text!));
    }

    // ── StrongTypeConverter's own contract ──────────────────────────────

    [Fact]
    public void StrongTypeConverter_NonParsableType_Throws() =>
        Assert.Throws<ArgumentException>(() => new StrongTypeConverter(typeof(object)));

    [Fact]
    public void StrongTypeConverter_NullType_Throws() =>
        Assert.Throws<ArgumentNullException>(() => new StrongTypeConverter(null!));
}
