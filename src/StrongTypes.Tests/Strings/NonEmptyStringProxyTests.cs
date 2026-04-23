using System;
using System.Globalization;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringProxyTests
{
    [Property]
    public void ToLower_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.ToLower(), s.ToLower().Value);

    [Property]
    public void ToLower_Culture_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.ToLower(CultureInfo.InvariantCulture), s.ToLower(CultureInfo.InvariantCulture).Value);

    [Property]
    public void ToLowerInvariant_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.ToLowerInvariant(), s.ToLowerInvariant().Value);

    [Property]
    public void ToUpper_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.ToUpper(), s.ToUpper().Value);

    [Property]
    public void ToUpper_Culture_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.ToUpper(CultureInfo.InvariantCulture), s.ToUpper(CultureInfo.InvariantCulture).Value);

    [Property]
    public void ToUpperInvariant_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.ToUpperInvariant(), s.ToUpperInvariant().Value);

    [Property]
    public void Contains_String_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.Contains(s.Value), s.Contains(s.Value));

    [Property]
    public void Contains_String_Comparison_DelegatesToString(NonEmptyString s)
    {
        var needle = s.Value.ToLowerInvariant();
        Assert.Equal(
            s.Value.Contains(needle, StringComparison.OrdinalIgnoreCase),
            s.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    [Property]
    public void Contains_Char_DelegatesToString(NonEmptyString s)
    {
        var c = s.Value[0];
        Assert.Equal(s.Value.Contains(c), s.Contains(c));
    }

    [Property]
    public void Contains_Char_Comparison_DelegatesToString(NonEmptyString s)
    {
        var c = char.ToLowerInvariant(s.Value[0]);
        Assert.Equal(
            s.Value.Contains(c, StringComparison.OrdinalIgnoreCase),
            s.Contains(c, StringComparison.OrdinalIgnoreCase));
    }

    [Property]
    public void Replace_CharChar_DelegatesToString(NonEmptyString s)
    {
        var first = s.Value[0];
        Assert.Equal(s.Value.Replace(first, 'Z'), s.Replace(first, 'Z'));
    }

    [Property]
    public void Replace_StringString_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.Replace(s.Value, "X"), s.Replace(s.Value, "X"));

    [Property]
    public void Replace_StringString_Comparison_DelegatesToString(NonEmptyString s)
    {
        var needle = s.Value.ToUpperInvariant();
        Assert.Equal(
            s.Value.Replace(needle, "X", StringComparison.OrdinalIgnoreCase),
            s.Replace(needle, "X", StringComparison.OrdinalIgnoreCase));
    }

    [Property]
    public void Trim_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.Trim(), s.Trim());

    [Property]
    public void IndexOf_String_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.IndexOf(s.Value), s.IndexOf(s.Value));

    [Property]
    public void IndexOf_String_Start_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.IndexOf(s.Value, 0), s.IndexOf(s.Value, 0));

    [Property]
    public void IndexOf_String_Comparison_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.IndexOf(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase),
            s.IndexOf(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));

    [Property]
    public void IndexOf_String_Start_Comparison_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.IndexOf(s.Value.ToUpperInvariant(), 0, StringComparison.OrdinalIgnoreCase),
            s.IndexOf(s.Value.ToUpperInvariant(), 0, StringComparison.OrdinalIgnoreCase));

    [Property]
    public void IndexOf_Char_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.IndexOf(s.Value[0]), s.IndexOf(s.Value[0]));

    [Property]
    public void IndexOf_Char_Start_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.IndexOf(s.Value[0], 0), s.IndexOf(s.Value[0], 0));

    [Property]
    public void IndexOf_Char_Comparison_DelegatesToString(NonEmptyString s)
    {
        var c = char.ToUpperInvariant(s.Value[0]);
        Assert.Equal(
            s.Value.IndexOf(c, StringComparison.OrdinalIgnoreCase),
            s.IndexOf(c, StringComparison.OrdinalIgnoreCase));
    }

    [Property]
    public void LastIndexOf_String_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.LastIndexOf(s.Value), s.LastIndexOf(s.Value));

    [Property]
    public void LastIndexOf_String_Start_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.LastIndexOf(s.Value, s.Value.Length - 1), s.LastIndexOf(s.Value, s.Value.Length - 1));

    [Property]
    public void LastIndexOf_String_Comparison_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.LastIndexOf(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase),
            s.LastIndexOf(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));

    [Property]
    public void LastIndexOf_String_Start_Comparison_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.LastIndexOf(s.Value.ToUpperInvariant(), s.Value.Length - 1, StringComparison.OrdinalIgnoreCase),
            s.LastIndexOf(s.Value.ToUpperInvariant(), s.Value.Length - 1, StringComparison.OrdinalIgnoreCase));

    [Property]
    public void LastIndexOf_Char_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.LastIndexOf(s.Value[0]), s.LastIndexOf(s.Value[0]));

    [Property]
    public void LastIndexOf_Char_Start_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.LastIndexOf(s.Value[0], s.Value.Length - 1), s.LastIndexOf(s.Value[0], s.Value.Length - 1));

    [Property]
    public void Substring_Start_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.Substring(0), s.Substring(0));

    [Property]
    public void Substring_Start_Length_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.Substring(0, s.Value.Length), s.Substring(0, s.Value.Length));

    [Property]
    public void StartsWith_String_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.StartsWith(s.Value), s.StartsWith(s.Value));

    [Property]
    public void StartsWith_String_Comparison_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.StartsWith(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase),
            s.StartsWith(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));

    [Property]
    public void StartsWith_String_IgnoreCase_Culture_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.StartsWith(s.Value.ToUpperInvariant(), ignoreCase: true, CultureInfo.InvariantCulture),
            s.StartsWith(s.Value.ToUpperInvariant(), ignoreCase: true, CultureInfo.InvariantCulture));

    [Property]
    public void StartsWith_Char_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.StartsWith(s.Value[0]), s.StartsWith(s.Value[0]));

    [Property]
    public void EndsWith_String_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.EndsWith(s.Value), s.EndsWith(s.Value));

    [Property]
    public void EndsWith_String_Comparison_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.EndsWith(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase),
            s.EndsWith(s.Value.ToUpperInvariant(), StringComparison.OrdinalIgnoreCase));

    [Property]
    public void EndsWith_String_IgnoreCase_Culture_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(
            s.Value.EndsWith(s.Value.ToUpperInvariant(), ignoreCase: true, CultureInfo.InvariantCulture),
            s.EndsWith(s.Value.ToUpperInvariant(), ignoreCase: true, CultureInfo.InvariantCulture));

    [Property]
    public void EndsWith_Char_DelegatesToString(NonEmptyString s) =>
        Assert.Equal(s.Value.EndsWith(s.Value[s.Value.Length - 1]), s.EndsWith(s.Value[s.Value.Length - 1]));
}
