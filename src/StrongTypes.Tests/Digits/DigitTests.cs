using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class DigitTests
{
    [Theory]
    [InlineData('0', (byte)0)]
    [InlineData('1', (byte)1)]
    [InlineData('2', (byte)2)]
    [InlineData('3', (byte)3)]
    [InlineData('4', (byte)4)]
    [InlineData('5', (byte)5)]
    [InlineData('6', (byte)6)]
    [InlineData('7', (byte)7)]
    [InlineData('8', (byte)8)]
    [InlineData('9', (byte)9)]
    public void TryCreate_Succeeds_ForAsciiDigits(char input, byte expected)
    {
        var digit = Digit.TryCreate(input);
        Assert.NotNull(digit);
        Assert.Equal(expected, digit!.Value.Value);
    }

    [Theory]
    [InlineData('a')]
    [InlineData('z')]
    [InlineData('B')]
    [InlineData(' ')]
    [InlineData('.')]
    [InlineData(char.MinValue)]
    [InlineData(char.MaxValue)]
    public void TryCreate_ReturnsNull_ForNonDigits(char input)
    {
        Assert.Null(Digit.TryCreate(input));
    }

    [Fact]
    public void Create_Throws_ForNonDigit()
    {
        Assert.Throws<ArgumentException>(() => Digit.Create('a'));
    }

    [Fact]
    public void Create_Succeeds_ForDigit()
    {
        Assert.Equal((byte)7, Digit.Create('7').Value);
    }

    [Fact]
    public void Default_IsZero()
    {
        Assert.Equal((byte)0, default(Digit).Value);
    }

    [Property]
    public void TryCreate_AgreesWith_CharIsDigit(char c)
    {
        var result = Digit.TryCreate(c);
        Assert.Equal(char.IsDigit(c), result.HasValue);
    }

    [Property]
    public void AllIntegerFirstDigits_Succeed(int number)
    {
        var firstDigit = Math.Abs(number).ToString()[0];
        Assert.NotNull(Digit.TryCreate(firstDigit));
    }

    [Fact]
    public void Equality_Digit()
    {
        Assert.Equal(Digit.Create('5'), Digit.Create('5'));
        Assert.NotEqual(Digit.Create('5'), Digit.Create('6'));
        Assert.True(Digit.Create('5') == Digit.Create('5'));
        Assert.True(Digit.Create('5') != Digit.Create('6'));
    }

    [Fact]
    public void Equality_Byte()
    {
        var digit = Digit.Create('5');
        Assert.True(digit.Equals((byte)5));
        Assert.False(digit.Equals((byte)6));
        Assert.True(digit.Equals((object)(byte)5));

        Assert.True(digit == (byte)5);
        Assert.True((byte)5 == digit);
        Assert.True(digit != (byte)6);
        Assert.True((byte)6 != digit);
    }

    [Fact]
    public void Equality_Int()
    {
        var digit = Digit.Create('5');
        Assert.True(digit.Equals(5));
        Assert.False(digit.Equals(6));
        Assert.False(digit.Equals(500));
        Assert.True(digit.Equals((object)5));

        Assert.True(digit == 5);
        Assert.True(5 == digit);
        Assert.True(digit != 500);
        Assert.True(500 != digit);
    }

    [Property]
    public void GetHashCode_MatchesByteHashCode(char c)
    {
        var digit = Digit.TryCreate(c);
        if (digit is null)
        {
            return;
        }
        Assert.Equal(digit.Value.Value.GetHashCode(), digit.Value.GetHashCode());
    }

    [Fact]
    public void Comparison_Digit()
    {
        var three = Digit.Create('3');
        var seven = Digit.Create('7');

        Assert.True(three.CompareTo(seven) < 0);
        Assert.True(seven.CompareTo(three) > 0);
        Assert.Equal(0, three.CompareTo(three));

        Assert.True(three < seven);
        Assert.True(three <= seven);
        Assert.True(seven > three);
        Assert.True(seven >= three);
        var threeAgain = Digit.Create('3');
        Assert.True(three <= threeAgain);
        Assert.True(three >= threeAgain);
    }

    [Fact]
    public void Comparison_Byte()
    {
        var digit = Digit.Create('5');
        Assert.True(digit.CompareTo((byte)4) > 0);
        Assert.True(digit.CompareTo((byte)6) < 0);
        Assert.Equal(0, digit.CompareTo((byte)5));

        Assert.True(digit > (byte)4);
        Assert.True(digit >= (byte)5);
        Assert.True(digit < (byte)6);
        Assert.True(digit <= (byte)5);

        Assert.True((byte)4 < digit);
        Assert.True((byte)5 <= digit);
        Assert.True((byte)6 > digit);
        Assert.True((byte)5 >= digit);
    }

    [Fact]
    public void Comparison_Int()
    {
        var digit = Digit.Create('5');
        Assert.True(digit.CompareTo(-1) > 0);
        Assert.True(digit.CompareTo(1000) < 0);
        Assert.Equal(0, digit.CompareTo(5));

        Assert.True(digit > -1);
        Assert.True(digit >= 5);
        Assert.True(digit < 1000);
        Assert.True(digit <= 5);

        Assert.True(-1 < digit);
        Assert.True(5 <= digit);
        Assert.True(1000 > digit);
        Assert.True(5 >= digit);
    }

    [Fact]
    public void Comparison_NonGeneric()
    {
        IComparable digit = Digit.Create('5');
        Assert.True(digit.CompareTo(null) > 0);
        Assert.True(digit.CompareTo(Digit.Create('3')) > 0);
        Assert.True(digit.CompareTo((byte)3) > 0);
        Assert.True(digit.CompareTo(3) > 0);
        Assert.Throws<ArgumentException>(() => digit.CompareTo("not a digit"));
    }

    [Fact]
    public void ImplicitConversions()
    {
        byte b = Digit.Create('5');
        int i = Digit.Create('9');

        Assert.Equal((byte)5, b);
        Assert.Equal(9, i);
    }

    [Fact]
    public void ToString_IsDecimalValue()
    {
        Assert.Equal("0", Digit.Create('0').ToString());
        Assert.Equal("9", Digit.Create('9').ToString());
    }

    // ── IParsable ───────────────────────────────────────────────────────

    [Theory]
    [InlineData("0", (byte)0)]
    [InlineData("5", (byte)5)]
    [InlineData("9", (byte)9)]
    public void TryParse_SingleDigitString_ReturnsTrueAndWrapsValue(string input, byte expected)
    {
        Assert.True(Digit.TryParse(input, provider: null, out var parsed));
        Assert.Equal(expected, parsed.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("a")]
    [InlineData("12")]
    [InlineData("99")]
    public void TryParse_InvalidInput_ReturnsFalse(string? input)
    {
        Assert.False(Digit.TryParse(input, provider: null, out var parsed));
        Assert.Equal(default, parsed);
    }

    [Fact]
    public void Parse_SingleDigit_WrapsValue() =>
        Assert.Equal((byte)7, Digit.Parse("7", provider: null).Value);

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("42")]
    public void Parse_Invalid_Throws(string input) =>
        Assert.Throws<ArgumentException>(() => Digit.Parse(input, provider: null));
}
