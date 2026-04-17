#nullable enable

using System;
using System.Linq;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class EnumExtensionsTests
{
    private enum Color { Red, Green, Blue }

    // Three consecutive non-power-of-two values, used for tests that need an
    // enum whose declared members contain no single-bit flags.
    private enum Step { Low = 3, Mid = 5, High = 6 }

    [Flags]
    private enum Perm : int
    {
        None = 0,
        Read = 1,
        Write = 2,
        Execute = 4,
        All = Read | Write | Execute,
    }

    [Flags]
    private enum BigFlags : ulong
    {
        None = 0,
        Low = 1UL,
        High = 1UL << 63,
    }

    [Flags]
    private enum SignedFlags : sbyte
    {
        Negative = -128,
        None = 0,
        A = 1,
        B = 2,
    }

    // ------- AllValues -------

    [Fact]
    public void AllValues_ReturnsDeclaredValues() =>
        Assert.Equal(new[] { Color.Red, Color.Green, Color.Blue }, EnumExtensions.AllValues<Color>());

    [Fact]
    public void AllValues_IsCached() =>
        Assert.Same(EnumExtensions.AllValues<Color>(), EnumExtensions.AllValues<Color>());

    // ------- AllFlagValues -------

    [Fact]
    public void AllFlagValues_ContainsOnlyPowersOfTwo() =>
        Assert.Equal(new[] { Perm.Read, Perm.Write, Perm.Execute }, EnumExtensions.AllFlagValues<Perm>());

    [Fact]
    public void AllFlagValues_ExcludesZero_AndAggregateMembers()
    {
        var flags = EnumExtensions.AllFlagValues<Perm>();
        Assert.DoesNotContain(Perm.None, flags);
        Assert.DoesNotContain(Perm.All, flags);
    }

    [Fact]
    public void AllFlagValues_HandlesHighBit_OnUInt64() =>
        Assert.Equal(new[] { BigFlags.Low, BigFlags.High }, EnumExtensions.AllFlagValues<BigFlags>());

    [Fact]
    public void AllFlagValues_HandlesSignedUnderlyingType() =>
        // Only positive single-bit values are flags. Negative = -128 is a single
        // bit in its own width but sign-extends to a negative Int64, so it's not
        // a power of two in the wider representation and is excluded.
        Assert.Equal(new[] { SignedFlags.A, SignedFlags.B }, EnumExtensions.AllFlagValues<SignedFlags>());

    [Fact]
    public void AllFlagValues_IsCached() =>
        Assert.Same(EnumExtensions.AllFlagValues<Perm>(), EnumExtensions.AllFlagValues<Perm>());

    // ------- AllFlagsCombined -------

    [Fact]
    public void AllFlagsCombined_ForPerm_IsAll() =>
        Assert.Equal(Perm.Read | Perm.Write | Perm.Execute, EnumExtensions.AllFlagsCombined<Perm>());

    [Fact]
    public void AllFlagsCombined_ForEnumWithoutFlags_IsZero() =>
        Assert.Equal(default, EnumExtensions.AllFlagsCombined<Step>());

    // ------- GetFlags -------

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
    public void GetFlags_WorksForHighBitOnUInt64() =>
        Assert.Equal(new[] { BigFlags.Low, BigFlags.High }, (BigFlags.Low | BigFlags.High).GetFlags());

    // ------- Round-trip property: combining then decomposing is the identity on the flag set -------

    [Property]
    public void GetFlags_RoundTripsFromAnySubsetOfFlags(int mask)
    {
        var allFlags = EnumExtensions.AllFlagValues<Perm>();
        var chosen = allFlags.Where((_, i) => (mask & (1 << i)) != 0).ToArray();

        var combined = chosen.Aggregate((Perm)0, (acc, f) => acc | f);
        Assert.Equal(chosen, combined.GetFlags());
    }
}
