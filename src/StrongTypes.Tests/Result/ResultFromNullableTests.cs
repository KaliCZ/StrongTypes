#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultFromNullableTests
{
    // ── Reference type: default ArgumentNullException form ─────────────

    [Property]
    public void ToResult_RefType_NonNull_IsSuccess(string value)
    {
        Assert.True(((string?)value).ToResult().IsSuccess);
    }

    [Fact]
    public void ToResult_RefType_Null_BecomesArgumentNullException_WithCallerName()
    {
        string? myValue = null;
        var r = myValue.ToResult();
        Assert.True(r.IsError);
        var ex = Assert.IsType<ArgumentNullException>(r.Error);
        Assert.Equal(nameof(myValue), ex.ParamName);
    }

    [Fact]
    public void ToResult_RefType_Null_WithCustomExceptionFactory()
    {
        string? val = null;
        var r = val.ToResult(() => new InvalidOperationException("custom"));
        Assert.IsType<InvalidOperationException>(r.Error);
        Assert.Equal("custom", r.Error!.Message);
    }

    // ── Reference type: custom TError ──────────────────────────────────

    [Fact]
    public void ToResult_RefType_Null_WithCustomErrorType()
    {
        string? val = null;
        Result<string, int> r = val.ToResult(() => 42);
        Assert.Equal(42, r.Error);
    }

    [Fact]
    public void ToResult_RefType_NonNull_WithCustomErrorType_Succeeds()
    {
        string? val = "hi";
        Result<string, int> r = val.ToResult(() => 42);
        Assert.Equal("hi", r.Success);
    }

    // ── Value type: default form ───────────────────────────────────────

    [Property]
    public void ToResult_StructType_HasValue_IsSuccess(int value)
    {
        int? nv = value;
        Assert.True(nv.ToResult().IsSuccess);
        Assert.Equal(value, nv.ToResult().Success);
    }

    [Fact]
    public void ToResult_StructType_Null_BecomesArgumentNullException_WithCallerName()
    {
        int? maybeInt = null;
        var r = maybeInt.ToResult();
        var ex = Assert.IsType<ArgumentNullException>(r.Error);
        Assert.Equal(nameof(maybeInt), ex.ParamName);
    }

    [Fact]
    public void ToResult_StructType_Null_WithCustomErrorType()
    {
        int? nv = null;
        Result<int, string> r = nv.ToResult(() => "bad");
        Assert.Equal("bad", r.Error);
    }

    // ── The ambiguity concern: Func returning Exception ────────────────
    //
    // Both overloads could match — the non-generic `Func<Exception>` form
    // and the generic `Func<TError>` form with TError=Exception. The
    // former is more specific on the parameter type, so overload resolution
    // picks it and the return is Result<T>, not Result<T, Exception>.

    [Fact]
    public void ToResult_RefType_FuncReturningException_PicksSingleParamOverload()
    {
        string? val = null;
        var r = val.ToResult(() => (Exception)new InvalidOperationException("x"));
        Assert.IsType<Result<string>>(r);
    }

    [Fact]
    public void ToResult_StructType_FuncReturningException_PicksSingleParamOverload()
    {
        int? val = null;
        var r = val.ToResult(() => (Exception)new InvalidOperationException("x"));
        Assert.IsType<Result<int>>(r);
    }

    [Fact]
    public void ToResult_RefType_FuncReturningExceptionSubclass_PicksTwoParamOverload()
    {
        // Documented overload-resolution behaviour: Func<InvalidOperationException>
        // is a more specific parameter type than Func<Exception>, so the two-param
        // overload (E = InvalidOperationException) wins. See the XML doc on
        // ResultFromNullableExtensions.ToResult for the workaround.
        string? val = null;
        var r = val.ToResult(() => new InvalidOperationException("x"));
        Assert.IsType<Result<string, InvalidOperationException>>(r);
    }

    [Fact]
    public void ToResult_RefType_ExceptionCast_PicksSingleParamOverload()
    {
        // Workaround: casting the lambda result to Exception makes the lambda's
        // inferred return type Exception, so V1 (Func<Exception>) and V2
        // (Func<E> with E=Exception) have identical parameter types and the
        // non-generic V1 is preferred.
        string? val = null;
        var r = val.ToResult(() => (Exception)new InvalidOperationException("x"));
        Assert.IsType<Result<string>>(r);
    }
}
