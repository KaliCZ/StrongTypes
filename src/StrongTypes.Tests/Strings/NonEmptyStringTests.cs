#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringTests
{
    [Theory, InlineData(null), InlineData(""), InlineData(" "), InlineData("\t\n")]
    public void TryCreate_NullOrWhitespace_ReturnsNull(string? input) =>
        Assert.Null(NonEmptyString.TryCreate(input));

    [Property]
    public void TryCreate_ValidInput_WrapsValue(NonEmptyString seed)
    {
        var created = NonEmptyString.TryCreate(seed.Value);
        Assert.NotNull(created);
        Assert.Equal(seed.Value, created!.Value);
    }

    [Theory, InlineData(null), InlineData(""), InlineData(" "), InlineData("\t\n")]
    public void Create_NullOrWhitespace_Throws(string? input) =>
        Assert.Throws<ArgumentException>(() => NonEmptyString.Create(input));

    [Property]
    public void Create_ValidInput_WrapsValue(NonEmptyString seed) =>
        Assert.Equal(seed.Value, NonEmptyString.Create(seed.Value).Value);

    [Property]
    public void Create_And_TryCreate_AgreeOnValue(NonEmptyString seed) =>
        Assert.Equal(NonEmptyString.Create(seed.Value).Value, NonEmptyString.TryCreate(seed.Value)!.Value);

    [Property]
    public void Length_MatchesValueLength(NonEmptyString s) =>
        Assert.Equal(s.Value.Length, s.Length);

    [Property]
    public void ToString_ReturnsValue(NonEmptyString s) =>
        Assert.Equal(s.Value, s.ToString());

    [Property]
    public void ImplicitConversionToString_ReturnsValue(NonEmptyString s)
    {
        string asString = s;
        Assert.Same(s.Value, asString);
    }

    [Property]
    public void ExplicitConversionFromString_WrapsValue(NonEmptyString seed)
    {
        var converted = (NonEmptyString)seed.Value;
        Assert.Equal(seed.Value, converted.Value);
    }

    [Theory, InlineData(null), InlineData(""), InlineData(" ")]
    public void ExplicitConversionFromString_Throws_OnInvalid(string? input) =>
        Assert.Throws<ArgumentException>(() => (NonEmptyString)input!);
}
