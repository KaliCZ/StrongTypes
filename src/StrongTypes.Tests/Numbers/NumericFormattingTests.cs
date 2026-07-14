using System;
using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

/// <summary>
/// Composite formatting silently ignores a format specifier on a type that is not
/// <see cref="IFormattable"/> — <c>$"{price:C}"</c> compiles, never throws, and prints the
/// unformatted number. These tests pin the specifier and the culture actually reaching the
/// underlying value, for every numeric wrapper the generator emits.
/// </summary>
public class NumericFormattingTests
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");
    private static readonly CultureInfo American = CultureInfo.GetCultureInfo("en-US");

    // ── IFormattable ────────────────────────────────────────────────────

    [Fact]
    public void FormatSpecifier_IsHonouredInInterpolation()
    {
        var price = Positive<decimal>.Create(1234.5m);

        Assert.Equal("1,234.50", string.Format(American, "{0:N2}", price));
        Assert.Equal("$1,234.50", string.Format(American, "{0:C}", price));
        Assert.Equal("1.234,50", string.Format(German, "{0:N2}", price));
    }

    [Fact]
    public void FormatProvider_IsHonouredWithoutASpecifier() =>
        Assert.Equal("1234,5", string.Format(German, "{0}", Positive<decimal>.Create(1234.5m)));

    [Theory]
    [InlineData("N2", "1,234.50")]
    [InlineData("C", "$1,234.50")]
    [InlineData("F0", "1235")]
    [InlineData("E2", "1.23E+003")]
    [InlineData(null, "1234.5")]
    public void ToString_MatchesTheUnderlyingValue(string? format, string expected)
    {
        var price = Positive<decimal>.Create(1234.5m);

        Assert.Equal(expected, price.ToString(format, American));
        Assert.Equal(price.Value.ToString(format, American), price.ToString(format, American));
    }

    /// <summary>The wrapper must never disagree with the value it wraps, for any format the underlying type accepts.</summary>
    [Property]
    public void ToString_NeverDivergesFromTheUnderlyingValue(int value)
    {
        if (value <= 0) return;

        var positive = Positive<int>.Create(value);
        foreach (var format in new[] { "D", "N0", "X", "G", null })
        {
            Assert.Equal(value.ToString(format, American), positive.ToString(format, American));
        }
    }

    [Fact]
    public void EveryWrapper_IsFormattable()
    {
        Assert.Equal("1,234.50", string.Format(American, "{0:N2}", Positive<decimal>.Create(1234.5m)));
        Assert.Equal("1,234.50", string.Format(American, "{0:N2}", NonNegative<decimal>.Create(1234.5m)));
        Assert.Equal("-1,234.50", string.Format(American, "{0:N2}", Negative<decimal>.Create(-1234.5m)));
        Assert.Equal("-1,234.50", string.Format(American, "{0:N2}", NonPositive<decimal>.Create(-1234.5m)));
    }

    // ── ISpanFormattable ────────────────────────────────────────────────

    [Fact]
    public void TryFormat_WritesIntoTheDestination()
    {
        Span<char> destination = stackalloc char[32];

        Assert.True(Positive<decimal>.Create(1234.5m).TryFormat(destination, out var written, "N2", American));
        Assert.Equal("1,234.50", destination[..written].ToString());
    }

    [Fact]
    public void TryFormat_DestinationTooShort_ReturnsFalse()
    {
        Span<char> destination = stackalloc char[2];

        Assert.False(Positive<decimal>.Create(1234.5m).TryFormat(destination, out var written, "N2", American));
        Assert.Equal(0, written);
    }

    // ── ISpanParsable ───────────────────────────────────────────────────

    [Fact]
    public void Parse_ReadsASliceWithoutMaterialisingIt()
    {
        const string line = "sku-1,42,widget";

        Assert.Equal(42, Positive<int>.Parse(line.AsSpan(6, 2), CultureInfo.InvariantCulture).Value);
    }

    [Property]
    public void TryParse_Span_MatchesTryParse_String(int value)
    {
        var text = value.ToString(CultureInfo.InvariantCulture);

        var fromString = Positive<int>.TryParse(text, CultureInfo.InvariantCulture, out var parsedFromString);
        var fromSpan = Positive<int>.TryParse(text.AsSpan(), CultureInfo.InvariantCulture, out var parsedFromSpan);

        Assert.Equal(fromString, fromSpan);
        Assert.Equal(parsedFromString, parsedFromSpan);
        Assert.Equal(value > 0, fromSpan);
    }

    [Fact]
    public void Parse_Span_StillEnforcesTheInvariant() =>
        Assert.Throws<ArgumentException>(() => Positive<int>.Parse("-5".AsSpan(), CultureInfo.InvariantCulture));

    [Fact]
    public void TryParse_Span_BreachedInvariant_ReturnsFalse()
    {
        Assert.False(Positive<int>.TryParse("-5".AsSpan(), CultureInfo.InvariantCulture, out var result));
        Assert.Equal(default, result);
    }

    // ── Generic constraints ─────────────────────────────────────────────
    //
    // The reason the span interfaces earn their place: without them a wrapper
    // cannot be passed to generic code constrained on them, and no amount of
    // reaching for .Value at the call site fixes that for the caller.

    private static string Render<T>(T value, string format) where T : IFormattable =>
        value.ToString(format, American);

    private static T Read<T>(ReadOnlySpan<char> text) where T : ISpanParsable<T> =>
        T.Parse(text, CultureInfo.InvariantCulture);

    private static string RenderIntoBuffer<T>(T value, string format) where T : ISpanFormattable
    {
        Span<char> destination = stackalloc char[64];
        return value.TryFormat(destination, out var written, format, American) ? destination[..written].ToString() : "";
    }

    [Fact]
    public void SatisfiesTheIFormattableConstraint() =>
        Assert.Equal("$1,234.50", Render(Positive<decimal>.Create(1234.5m), "C"));

    [Fact]
    public void SatisfiesTheISpanFormattableConstraint() =>
        Assert.Equal("1,234.50", RenderIntoBuffer(Positive<decimal>.Create(1234.5m), "N2"));

    [Fact]
    public void SatisfiesTheISpanParsableConstraint()
    {
        Assert.Equal(42, Read<Positive<int>>("42").Value);
        Assert.Equal(0, Read<NonNegative<int>>("0").Value);
        Assert.Equal(-42, Read<Negative<int>>("-42").Value);
        Assert.Equal(-42, Read<NonPositive<int>>("-42").Value);
    }
}
