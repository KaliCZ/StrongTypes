#nullable enable

using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NullableMatchExtensionsTests
{
    // ── struct receiver ───────────────────────────────────────────────

    [Property]
    public void Match_Struct_SomeInvokesIfSome(int raw)
    {
        int? value = raw;
        var noneInvoked = false;
        var result = value.Match(
            ifSome: x => x + 1,
            ifNone: () => { noneInvoked = true; return -1; });
        Assert.Equal(raw + 1, result);
        Assert.False(noneInvoked);
    }

    [Fact]
    public void Match_Struct_NullInvokesIfNone()
    {
        int? value = null;
        var someInvoked = false;
        var result = value.Match(
            ifSome: x => { someInvoked = true; return x + 1; },
            ifNone: () => -1);
        Assert.Equal(-1, result);
        Assert.False(someInvoked);
    }

    // ── class receiver ────────────────────────────────────────────────

    [Property]
    public void Match_Class_SomeInvokesIfSome(NonEmptyString value)
    {
        string? s = value.Value;
        var noneInvoked = false;
        var result = s.Match(
            ifSome: x => x.Length,
            ifNone: () => { noneInvoked = true; return -1; });
        Assert.Equal(value.Value.Length, result);
        Assert.False(noneInvoked);
    }

    [Fact]
    public void Match_Class_NullInvokesIfNone()
    {
        string? value = null;
        var someInvoked = false;
        var result = value.Match(
            ifSome: x => { someInvoked = true; return x.Length; },
            ifNone: () => -1);
        Assert.Equal(-1, result);
        Assert.False(someInvoked);
    }

    // ── result type can itself be anything ─────────────────────────────

    [Fact]
    public void Match_ReturnsReferenceType()
    {
        int? value = 7;
        string result = value.Match(ifSome: x => x.ToString(), ifNone: () => "none");
        Assert.Equal("7", result);
    }

    [Fact]
    public void Match_ReturnsNullableReferenceType()
    {
        string? value = null;
        string? result = value.Match<string, string?>(
            ifSome: x => x,
            ifNone: () => null);
        Assert.Null(result);
    }
}
