#nullable enable

using System;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultMapTests
{
    // ── Map (two-param) ────────────────────────────────────────────────

    [Property]
    public void Map_Success_AppliesFunction(int value)
    {
        Result<int, string> r = value;
        var mapped = r.Map(x => x + 1);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(value + 1, mapped.Success);
    }

    [Property]
    public void Map_Error_PropagatesUnchanged(string error)
    {
        Result<int, string> r = error;
        var mapped = r.Map(x => x + 1);
        Assert.True(mapped.IsError);
        Assert.Equal(error, mapped.Error);
    }

    // ── Map on Result<T> — the "no error-type leakage" ergonomic ───────

    [Property]
    public void Map_OnResultOfT_Success_ReturnsResultOfT(int value)
    {
        Result<int> r = value;
        var mapped = r.Map(x => x.ToString());
        Assert.IsType<Result<string>>(mapped);
        Assert.Equal(value.ToString(), mapped.Success);
    }

    [Fact]
    public void Map_OnResultOfT_Error_ReturnsResultOfT_WithSameException()
    {
        var ex = new InvalidOperationException("boom");
        Result<int> r = ex;
        var mapped = r.Map(x => x.ToString());
        Assert.IsType<Result<string>>(mapped);
        Assert.Same(ex, mapped.Error);
    }

    [Fact]
    public void Map_ChainOnResultOfT_StaysAsResultOfT()
    {
        Result<int> r = 2;
        var chained = r.Map(x => x + 1).Map(x => x * 10);
        Assert.IsType<Result<int>>(chained);
        Assert.Equal(30, chained.Success);
    }

    // ── MapAsync ───────────────────────────────────────────────────────

    [Property]
    public async Task MapAsync_Success_AppliesFunction(int value)
    {
        Result<int, string> r = value;
        var mapped = await r.MapAsync(async x => { await Task.Yield(); return x + 1; });
        Assert.Equal(value + 1, mapped.Success);
    }

    [Property]
    public async Task MapAsync_Error_Propagates(string error)
    {
        Result<int, string> r = error;
        var mapped = await r.MapAsync(async x => { await Task.Yield(); return x + 1; });
        Assert.Equal(error, mapped.Error);
    }

    [Fact]
    public async Task MapAsync_OnResultOfT_ReturnsResultOfT()
    {
        Result<int> r = 5;
        var mapped = await r.MapAsync(async x => { await Task.Yield(); return x.ToString(); });
        Assert.IsType<Result<string>>(mapped);
        Assert.Equal("5", mapped.Success);
    }

    // ── MapError ───────────────────────────────────────────────────────

    [Property]
    public void MapError_Error_AppliesFunction(string error)
    {
        Result<int, string> r = error;
        var mapped = r.MapError(e => e.Length);
        Assert.True(mapped.IsError);
        Assert.Equal(error.Length, mapped.Error);
    }

    [Property]
    public void MapError_Success_PropagatesUnchanged(int value)
    {
        Result<int, string> r = value;
        var mapped = r.MapError(e => e.Length);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(value, mapped.Success);
    }

    [Property]
    public async Task MapErrorAsync_Error_AppliesFunction(string error)
    {
        Result<int, string> r = error;
        var mapped = await r.MapErrorAsync(async e => { await Task.Yield(); return e.Length; });
        Assert.Equal(error.Length, mapped.Error);
    }

    // ── Bimap: Map<U, UError>(success, error) ──────────────────────────

    [Property]
    public void Bimap_Success_InvokesOnlySuccessBranch(int value)
    {
        Result<int, string> r = value;
        var mapped = r.Map(
            success: x => x + 1,
            error: e => e.Length);
        Assert.True(mapped.IsSuccess);
        Assert.Equal(value + 1, mapped.Success);
    }

    [Property]
    public void Bimap_Error_InvokesOnlyErrorBranch(string error)
    {
        Result<int, string> r = error;
        var mapped = r.Map(
            success: x => x + 1,
            error: e => e.Length);
        Assert.True(mapped.IsError);
        Assert.Equal(error.Length, mapped.Error);
    }

    [Property]
    public void Bimap_EquivalentToMapThenMapError(int value, string error)
    {
        Result<int, string> success = value;
        Result<int, string> failure = error;

        Assert.Equal(
            success.Map(x => x + 1).MapError(e => e.Length),
            success.Map(x => x + 1, e => e.Length));
        Assert.Equal(
            failure.Map(x => x + 1).MapError(e => e.Length),
            failure.Map(x => x + 1, e => e.Length));
    }
}
