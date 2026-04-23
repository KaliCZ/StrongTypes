using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultTests
{
    // ── Construction via factories ──────────────────────────────────────

    [Property]
    public void Success_Factory_IsSuccess(int value)
    {
        var r = Result.Success<int, string>(value);
        Assert.True(r.IsSuccess);
        Assert.False(r.IsError);
    }

    [Property]
    public void Error_Factory_IsError(string error)
    {
        var r = Result.Error<int, string>(error);
        Assert.False(r.IsSuccess);
        Assert.True(r.IsError);
    }

    [Property]
    public void SingleParam_SuccessFactory_ReturnsResultOfT(int value)
    {
        Result<int> r = Result.Success(value);
        Assert.IsType<Result<int>>(r);
        Assert.True(r.IsSuccess);
    }

    [Property]
    public void SingleParam_ErrorFactory_ReturnsResultOfT(string message)
    {
        Result<int> r = Result.Error<int>(new InvalidOperationException(message));
        Assert.IsType<Result<int>>(r);
        Assert.True(r.IsError);
    }

    // ── Implicit conversions ────────────────────────────────────────────

    [Property]
    public void ImplicitFromValue_CreatesSuccess(int value)
    {
        Result<int, string> r = value;
        Assert.True(r.IsSuccess);
    }

    [Property]
    public void ImplicitFromError_CreatesError(string error)
    {
        Result<int, string> r = error;
        Assert.True(r.IsError);
    }

    [Property]
    public void ImplicitFromValue_OnSingleParam_ReturnsResultOfT(int value)
    {
        Result<int> r = value;
        Assert.IsType<Result<int>>(r);
        Assert.True(r.IsSuccess);
    }

    [Fact]
    public void ImplicitFromException_OnSingleParam_ReturnsResultOfT()
    {
        Result<int> r = new InvalidOperationException("boom");
        Assert.IsType<Result<int>>(r);
        Assert.True(r.IsError);
    }

    [Fact]
    public void ImplicitConversions_EnableBareReturnStatements()
    {
        // Demonstrates the ergonomic the whole design targets: method bodies
        // return the raw value or the raw error, no factory call required.
        static Result<int> ParseInt(string s) =>
            int.TryParse(s, out var n) ? n : new FormatException(s);

        Assert.True(ParseInt("42").IsSuccess);
        Assert.True(ParseInt("nope").IsError);
    }

    // ── Equality ────────────────────────────────────────────────────────

    [Property]
    public void Equality_SameSuccess_AreEqual(int value)
    {
        Result<int, string> a = value;
        Result<int, string> b = value;
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Property]
    public void Equality_SameError_AreEqual(string error)
    {
        Result<int, string> a = error;
        Result<int, string> b = error;
        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Property]
    public void Equality_DifferentBranches_AreNotEqual(int value, string error)
    {
        Result<int, string> success = value;
        Result<int, string> failure = error;
        Assert.NotEqual(success, failure);
    }

    [Fact]
    public void Equality_ResultOfT_AndTwoParam_CompareStructurally()
    {
        // Both carry the same (success, 5) state; equality walks the branch
        // payloads regardless of which closed generic the instance came from.
        Result<int> a = 5;
        Result<int, Exception> b = 5;
        Assert.True(a.Equals(b));
    }

    // ── ToString ────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Success_FormatsPayload()
    {
        Result<int, string> r = 42;
        Assert.Equal("Success(42)", r.ToString());
    }

    [Fact]
    public void ToString_Error_FormatsPayload()
    {
        Result<int, string> r = "boom";
        Assert.Equal("Error(boom)", r.ToString());
    }
}
