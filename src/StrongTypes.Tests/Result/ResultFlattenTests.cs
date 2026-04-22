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
        var inner = Result.Success<int, string>(value);
        var outer = Result.Success<Result<int, string>, string>(inner);
        var flat = outer.Flatten();
        Assert.True(flat.IsSuccess);
        Assert.Equal(value, flat.Success);
    }

    [Property]
    public void Flatten_OuterSuccess_InnerError_ReturnsInnerError(string innerError)
    {
        var inner = Result.Error<int, string>(innerError);
        var outer = Result.Success<Result<int, string>, string>(inner);
        var flat = outer.Flatten();
        Assert.True(flat.IsError);
        Assert.Equal(innerError, flat.Error);
    }

    [Property]
    public void Flatten_OuterError_ReturnsOuterError(string outerError)
    {
        var outer = Result.Error<Result<int, string>, string>(outerError);
        var flat = outer.Flatten();
        Assert.True(flat.IsError);
        Assert.Equal(outerError, flat.Error);
    }

    // ── Single-param Flatten (Result<Result<T>>) ───────────────────────

    [Property]
    public void Flatten_SingleParam_OuterSuccess_InnerSuccess(int value)
    {
        Result<Result<int>> outer = Result.Success(Result.Success(value));
        var flat = outer.Flatten();
        Assert.IsType<Result<int>>(flat);
        Assert.Equal(value, flat.Success);
    }

    [Fact]
    public void Flatten_SingleParam_OuterSuccess_InnerError_PreservesException()
    {
        var innerEx = new InvalidOperationException("inner");
        Result<Result<int>> outer = Result.Success<Result<int>>(innerEx);
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

    // ── FlattenErrors (chained-Aggregate unwrap) ───────────────────────

    [Fact]
    public void FlattenErrors_Success_LeavesValueUnchanged()
    {
        Result<int, string[][]> r = 42;
        var flat = r.FlattenErrors();
        Assert.Equal(42, flat.Success);
    }

    [Fact]
    public void FlattenErrors_Error_ConcatsAllInnerArraysInOrder()
    {
        Result<int, string[][]> r = new[]
        {
            new[] { "a", "b" },
            new[] { "c" },
            new[] { "d", "e" },
        };
        var flat = r.FlattenErrors();
        Assert.Equal(new[] { "a", "b", "c", "d", "e" }, flat.Error);
    }

    [Fact]
    public void FlattenErrors_ComposesWithChainedAggregate()
    {
        Result<int, string> r1 = 1, r2 = 2, r3 = "e3", r4 = 4;
        var group1 = Result.Aggregate(r1, r2);
        var group2 = Result.Aggregate(r3, r4);
        var chained = Result.Aggregate(group1, group2).FlattenErrors();
        Assert.Equal(new[] { "e3" }, chained.Error);
    }
}
