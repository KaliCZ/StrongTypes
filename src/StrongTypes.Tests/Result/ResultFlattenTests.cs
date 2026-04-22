#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultFlattenTests
{
    // ── Generic Flatten (two-param form) ───────────────────────────────

    [Property]
    public void Flatten_OuterSuccess_InnerSuccess_ReturnsInnerValue(int value)
    {
        Result<int, string> inner = value;
        Result<Result<int, string>, string> outer = inner;
        var flat = outer.Flatten();
        Assert.True(flat.IsSuccess);
        Assert.Equal(value, flat.Success);
    }

    [Property]
    public void Flatten_OuterSuccess_InnerError_ReturnsInnerError(string innerError)
    {
        Result<int, string> inner = innerError;
        Result<Result<int, string>, string> outer = inner;
        var flat = outer.Flatten();
        Assert.True(flat.IsError);
        Assert.Equal(innerError, flat.Error);
    }

    [Property]
    public void Flatten_OuterError_ReturnsOuterError(string outerError)
    {
        Result<Result<int, string>, string> outer = outerError;
        var flat = outer.Flatten();
        Assert.True(flat.IsError);
        Assert.Equal(outerError, flat.Error);
    }

    // ── Single-param Flatten (Result<Result<T>>) ───────────────────────

    [Property]
    public void Flatten_SingleParam_OuterSuccess_InnerSuccess(int value)
    {
        Result<int> inner = value;
        Result<Result<int>> outer = inner;
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Equal(value, flat.Success);
    }

    [Fact]
    public void Flatten_SingleParam_OuterSuccess_InnerError_PreservesException()
    {
        var innerEx = new InvalidOperationException("inner");
        Result<int> inner = innerEx;
        Result<Result<int>> outer = inner;
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Same(innerEx, flat.Error);
    }

    [Fact]
    public void Flatten_SingleParam_OuterError_PreservesException()
    {
        var outerEx = new InvalidOperationException("outer");
        Result<Result<int>> outer = outerEx;
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Same(outerEx, flat.Error);
    }

    // ── Mismatched exception subtypes (TInner != TOuter) ───────────────

    [Fact]
    public void Flatten_MismatchedExceptions_OuterError_UpcastsToException()
    {
        var outerEx = new InvalidOperationException("outer");
        Result<Result<int, FormatException>, InvalidOperationException> outer = outerEx;
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Same(outerEx, flat.Error);
    }

    [Fact]
    public void Flatten_MismatchedExceptions_InnerError_UpcastsToException()
    {
        var innerEx = new FormatException("inner");
        Result<int, FormatException> inner = innerEx;
        Result<Result<int, FormatException>, InvalidOperationException> outer = inner;
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Same(innerEx, flat.Error);
    }

    [Property]
    public void Flatten_MismatchedExceptions_Success_ReturnsInnerValue(int value)
    {
        Result<int, FormatException> inner = value;
        Result<Result<int, FormatException>, InvalidOperationException> outer = inner;
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Equal(value, flat.Success);
    }
}
