#nullable enable

using System;
using System.Globalization;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringExtensionsTests
{
    private enum Day { Mon, Tue }

    // NonEmptyString parsing just delegates to string parsing. Check one
    // value of each overload is enough to catch a wiring regression.

    [Property]
    public void AsInt_RoundTrips(int value) =>
        Assert.Equal(value, NonEmptyString.Create(value.ToString(CultureInfo.InvariantCulture))
            .AsInt(CultureInfo.InvariantCulture));

    [Property]
    public void AsLong_RoundTrips(long value) =>
        Assert.Equal(value, NonEmptyString.Create(value.ToString(CultureInfo.InvariantCulture))
            .AsLong(CultureInfo.InvariantCulture));

    [Property]
    public void AsDecimal_RoundTrips(decimal value) =>
        Assert.Equal(value, NonEmptyString.Create(value.ToString(CultureInfo.InvariantCulture))
            .AsDecimal(CultureInfo.InvariantCulture));

    [Property]
    public void AsGuid_RoundTrips(Guid value) =>
        Assert.Equal(value, NonEmptyString.Create(value.ToString()).AsGuid());

    [Fact]
    public void AsInt_ReturnsNull_OnGarbage() =>
        Assert.Null(NonEmptyString.Create("ASDF").AsInt());

    [Fact]
    public void AsBool_RoundTrips()
    {
        Assert.True(NonEmptyString.Create("true").AsBool());
        Assert.False(NonEmptyString.Create("false").AsBool());
        Assert.Null(NonEmptyString.Create("ASDF").AsBool());
    }

    [Fact]
    public void AsEnum_ParsesDefinedNames() =>
        Assert.Equal(Day.Mon, NonEmptyString.Create("Mon").AsEnum<Day>());

    [Fact]
    public void AsEnum_RejectsUndefinedNames() =>
        Assert.Null(NonEmptyString.Create("Fri").AsEnum<Day>());
}
