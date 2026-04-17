#nullable enable

using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class DigitExtensionsTests
{
    [Property]
    public void AsDigit_DelegatesToTryCreate(char c)
    {
        Assert.Equal(Digit.TryCreate(c), c.AsDigit());
    }

    [Fact]
    public void FilterDigits_ExtractsDigitsInOrder()
    {
        Assert.Equal(
            new byte[] { 1, 2, 3, 8, 7, 6, 5, 9 },
            "ASD 1 some spaces 2 with numbers 38 7 in between .6 ?:`'!@(#*&$%&^!@)$_  them59"
                .FilterDigits()
                .Select(d => d.Value));
    }

    [Fact]
    public void FilterDigits_ReturnsEmpty_ForNull()
    {
        Assert.Empty(((string?)null).FilterDigits());
    }

    [Fact]
    public void FilterDigits_ReturnsEmpty_ForEmptyString()
    {
        Assert.Empty("".FilterDigits());
    }

    [Fact]
    public void FilterDigits_ReturnsEmpty_ForNonDigitsOnly()
    {
        Assert.Empty("abc XYZ !@#".FilterDigits());
    }
}
