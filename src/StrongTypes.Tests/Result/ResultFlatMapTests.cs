#nullable enable

using System;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultFlatMapTests
{
    // ── FlatMap (two-param) ────────────────────────────────────────────

    [Property]
    public void FlatMap_Success_AppliesContinuation(int value)
    {
        Result<int, string> r = value;
        var bound = r.FlatMap<int>(x => x + 1);
        Assert.True(bound.IsSuccess);
        Assert.Equal(value + 1, bound.Success);
    }

    [Property]
    public void FlatMap_Success_ContinuationCanReturnError(int value)
    {
        Result<int, string> r = value;
        var bound = r.FlatMap<int>(x => "rejected: " + x);
        Assert.True(bound.IsError);
        Assert.Equal("rejected: " + value, bound.Error);
    }

    [Property]
    public void FlatMap_Error_ShortCircuits(string error)
    {
        Result<int, string> r = error;
        var bound = r.FlatMap<int>(_ => throw new Exception("should not run"));
        Assert.True(bound.IsError);
        Assert.Equal(error, bound.Error);
    }

    // ── FlatMap on Result<T> — stays Result<T> through the chain ───────

    [Property]
    public void FlatMap_OnResultOfT_Success_ReturnsResultOfT(int value)
    {
        Result<int> r = value;
        var bound = r.FlatMap<int>(x => x + 1);
        Assert.IsType<Result<int>>(bound);
        Assert.Equal(value + 1, bound.Success);
    }

    [Fact]
    public void FlatMap_OnResultOfT_Error_ReturnsResultOfT_WithSameException()
    {
        var ex = new InvalidOperationException("boom");
        Result<int> r = ex;
        var bound = r.FlatMap<int>(_ => 99);
        Assert.IsType<Result<int>>(bound);
        Assert.Same(ex, bound.Error);
    }

    [Fact]
    public void FlatMap_ChainOnResultOfT_StaysAsResultOfT()
    {
        Result<int> r = 2;
        Result<int> chained = r
            .FlatMap<int>(x => x + 1)
            .FlatMap<int>(x => x * 10);
        Assert.IsType<Result<int>>(chained);
        Assert.Equal(30, chained.Success);
    }

    // ── FlatMapAsync ───────────────────────────────────────────────────

    [Property]
    public async Task FlatMapAsync_Success_AppliesContinuation(int value)
    {
        Result<int, string> r = value;
        var bound = await r.FlatMapAsync(async x =>
        {
            await Task.Yield();
            return Result.Success<int, string>(x + 1);
        });
        Assert.Equal(value + 1, bound.Success);
    }

    [Property]
    public async Task FlatMapAsync_Error_ShortCircuits(string error)
    {
        Result<int, string> r = error;
        var bound = await r.FlatMapAsync<int>(_ => throw new Exception("should not run"));
        Assert.Equal(error, bound.Error);
    }

    [Fact]
    public async Task FlatMapAsync_OnResultOfT_ReturnsResultOfT()
    {
        Result<int> r = 5;
        // Explicit <int> because Task<T> is invariant, so the lambda's
        // `Task<Result<int>>` can't be inferred against the shadow's
        // `Task<Result<U, Exception>>` parameter without help.
        var bound = await r.FlatMapAsync<int>(async x =>
        {
            await Task.Yield();
            return Result.Success(x + 1);
        });
        Assert.IsType<Result<int>>(bound);
        Assert.Equal(6, bound.Success);
    }
}
