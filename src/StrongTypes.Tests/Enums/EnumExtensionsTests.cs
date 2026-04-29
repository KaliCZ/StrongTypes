using System;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class EnumExtensionsTests
{
    private enum Color { Red, Green, Blue }

    [Flags]
    private enum Perm
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        All = Read | Write | Execute,
    }

    [Flags]
    private enum SignedFlags : sbyte
    {
        Negative = -128,
        None = 0,
        A = 1,
        B = 2,
    }

    [Flags]
    private enum LongFlags : long
    {
        None = 0,
        Low = 1L,
        High = 1L << 62,
    }

    // ------- Parse / TryParse / Create / TryCreate -------

    [Fact]
    public void Parse_ReturnsMember() =>
        Assert.Equal(Color.Green, Color.Parse("Green"));

    [Fact]
    public void Parse_IgnoreCase() =>
        Assert.Equal(Color.Green, Color.Parse("green", ignoreCase: true));

    [Fact]
    public void Parse_ThrowsOnUnknown() =>
        Assert.ThrowsAny<ArgumentException>(() => Color.Parse("Purple"));

    [Fact]
    public void TryParse_ReturnsMember() =>
        Assert.Equal(Color.Green, Color.TryParse("Green"));

    [Fact]
    public void TryParse_ReturnsNullOnUnknown() =>
        Assert.Null(Color.TryParse("Purple"));

    [Fact]
    public void TryParse_ReturnsNullOnNull() =>
        Assert.Null(Color.TryParse(null));

    [Fact]
    public void TryParse_CaseSensitiveByDefault() =>
        Assert.Null(Color.TryParse("green"));

    [Fact]
    public void TryParse_IgnoreCase() =>
        Assert.Equal(Color.Green, Color.TryParse("green", ignoreCase: true));

    [Fact]
    public void Create_BehavesLikeParse() =>
        Assert.Equal(Color.Blue, Color.Create("Blue"));

    [Fact]
    public void Create_ThrowsOnUnknown() =>
        Assert.ThrowsAny<ArgumentException>(() => Color.Create("Purple"));

    [Fact]
    public void TryCreate_BehavesLikeTryParse()
    {
        Assert.Equal(Color.Blue, Color.TryCreate("Blue"));
        Assert.Null(Color.TryCreate("Purple"));
        Assert.Null(Color.TryCreate(null));
    }

    // ------- AllValues (works for any enum, flag or not) -------

    [Fact]
    public void AllValues_ReturnsDeclaredValues() =>
        Assert.Equal(new[] { Color.Red, Color.Green, Color.Blue }, Color.AllValues);

    [Fact]
    public void AllValues_IsCached() =>
        Assert.Same(Color.AllValues, Color.AllValues);

    // ------- AllFlagValues (flag enums only) -------

    [Fact]
    public void AllFlagValues_ContainsOnlyPowersOfTwo() =>
        Assert.Equal(new[] { Perm.Read, Perm.Write, Perm.Execute }, Perm.AllFlagValues);

    [Fact]
    public void AllFlagValues_ExcludesZero_AndAggregateMembers()
    {
        var flags = Perm.AllFlagValues;
        Assert.DoesNotContain(Perm.None, flags);
        Assert.DoesNotContain(Perm.All, flags);
    }

    [Fact]
    public void AllFlagValues_HandlesLongHighBit() =>
        Assert.Equal(new[] { LongFlags.Low, LongFlags.High }, LongFlags.AllFlagValues);

    [Fact]
    public void AllFlagValues_ExcludesNegativeMembers_OnSignedUnderlying() =>
        Assert.Equal(new[] { SignedFlags.A, SignedFlags.B }, SignedFlags.AllFlagValues);

    [Fact]
    public void AllFlagValues_IsCached() =>
        Assert.Same(Perm.AllFlagValues, Perm.AllFlagValues);

    [Fact]
    public void AllFlagValues_ThrowsOnNonFlagEnum() =>
        Assert.Throws<InvalidOperationException>(() => Color.AllFlagValues);

    // ------- AllFlagsCombined (flag enums only) -------

    [Fact]
    public void AllFlagsCombined_IsBitwiseOrOfFlags() =>
        Assert.Equal(Perm.Read | Perm.Write | Perm.Execute, Perm.AllFlagsCombined);

    [Fact]
    public void AllFlagsCombined_ThrowsOnNonFlagEnum() =>
        Assert.Throws<InvalidOperationException>(() => Color.AllFlagsCombined);

    // ------- GetFlags (flag enums only) -------

    [Fact]
    public void GetFlags_DecomposesCombinedValue() =>
        Assert.Equal(new[] { Perm.Read, Perm.Execute }, (Perm.Read | Perm.Execute).GetFlags());

    [Fact]
    public void GetFlags_ReturnsEmpty_ForZero() =>
        Assert.Empty(Perm.None.GetFlags());

    [Fact]
    public void GetFlags_ReturnsSingleton_ForSingleFlag() =>
        Assert.Equal(new[] { Perm.Write }, Perm.Write.GetFlags());

    [Fact]
    public void GetFlags_PreservesDeclarationOrder() =>
        Assert.Equal(new[] { Perm.Read, Perm.Write, Perm.Execute }, Perm.All.GetFlags());

    [Fact]
    public void GetFlags_HandlesLongHighBit() =>
        Assert.Equal(new[] { LongFlags.Low, LongFlags.High }, (LongFlags.Low | LongFlags.High).GetFlags());

    [Fact]
    public void GetFlags_ThrowsOnNonFlagEnum() =>
        Assert.Throws<InvalidOperationException>(() => Color.Green.GetFlags());

    // ------- Round-trip property: combining then decomposing is the identity on the flag set -------

    [Property]
    public void GetFlags_RoundTripsFromAnySubsetOfFlags(int mask)
    {
        var allFlags = Perm.AllFlagValues;
        var chosen = allFlags.Where((_, i) => (mask & (1 << i)) != 0).ToArray();

        var combined = chosen.Aggregate((Perm)0, (acc, f) => acc | f);
        Assert.Equal(chosen, combined.GetFlags());
    }
}
