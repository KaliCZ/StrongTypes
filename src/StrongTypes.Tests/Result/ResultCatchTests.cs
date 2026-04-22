#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultCatchTests
{
    // ── Catch ──────────────────────────────────────────────────────────

    [Property]
    public void Catch_NoThrow_ReturnsSuccess(int value)
    {
        var r = Result.Catch(() => value);
        Assert.True(r.IsSuccess);
        Assert.Equal(value, r.Success);
    }

    [Fact]
    public void Catch_Throws_CapturesExceptionAsError()
    {
        var thrown = new InvalidOperationException("boom");
        var r = Result.Catch<int>(() => throw thrown);
        Assert.True(r.IsError);
        Assert.Same(thrown, r.Error);
    }

    [Fact]
    public void Catch_OperationCanceledException_IsCaptured()
    {
        // Default Catch is symmetric with a plain try/catch — OCE is captured
        // like any other exception. Cancellation-aware pipelines use the typed
        // Catch<T, TException> overload with a non-OCE type to let OCE propagate.
        var oce = new OperationCanceledException();
        var r = Result.Catch<int>(() => throw oce);
        Assert.True(r.IsError);
        Assert.Same(oce, r.Error);
    }

    // ── Typed Catch<T, TException> ─────────────────────────────────────

    [Fact]
    public void CatchTyped_NoThrow_ReturnsSuccess()
    {
        var r = Result.Catch<int, InvalidOperationException>(() => 42);
        Assert.True(r.IsSuccess);
        Assert.Equal(42, r.Success);
    }

    [Fact]
    public void CatchTyped_MatchingException_IsCaptured()
    {
        var thrown = new InvalidOperationException("boom");
        var r = Result.Catch<int, InvalidOperationException>(() => throw thrown);
        Assert.True(r.IsError);
        Assert.Same(thrown, r.Error);
    }

    [Fact]
    public void CatchTyped_NonMatchingException_Propagates()
    {
        Assert.Throws<ArgumentException>(() =>
            Result.Catch<int, InvalidOperationException>(() => throw new ArgumentException("bad")));
    }

    [Fact]
    public void CatchTyped_WithNonOceType_LetsOceEscape()
    {
        // The typed variant is the opt-in for cancellation-aware pipelines:
        // picking any non-OCE type means OperationCanceledException propagates.
        Assert.Throws<OperationCanceledException>(() =>
            Result.Catch<int, InvalidOperationException>(() => throw new OperationCanceledException()));
    }

    // ── CatchAsync ─────────────────────────────────────────────────────

    [Property]
    public async Task CatchAsync_NoThrow_ReturnsSuccess(int value)
    {
        var r = await Result.CatchAsync(async () => { await Task.Yield(); return value; });
        Assert.True(r.IsSuccess);
        Assert.Equal(value, r.Success);
    }

    [Fact]
    public async Task CatchAsync_Throws_CapturesExceptionAsError()
    {
        var thrown = new InvalidOperationException("boom");
        var r = await Result.CatchAsync<int>(async () =>
        {
            await Task.Yield();
            throw thrown;
        });
        Assert.True(r.IsError);
        Assert.Same(thrown, r.Error);
    }

    [Fact]
    public async Task CatchAsync_OperationCanceledException_IsCaptured()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await Result.CatchAsync<int>(async () =>
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
            return 0;
        });
        Assert.True(r.IsError);
        Assert.IsAssignableFrom<OperationCanceledException>(r.Error);
    }

    // ── Typed CatchAsync<T, TException> ────────────────────────────────

    [Fact]
    public async Task CatchAsyncTyped_MatchingException_IsCaptured()
    {
        var thrown = new InvalidOperationException("boom");
        var r = await Result.CatchAsync<int, InvalidOperationException>(async () =>
        {
            await Task.Yield();
            throw thrown;
        });
        Assert.True(r.IsError);
        Assert.Same(thrown, r.Error);
    }

    [Fact]
    public async Task CatchAsyncTyped_WithNonOceType_LetsOceEscape()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await Result.CatchAsync<int, InvalidOperationException>(async () =>
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
                return 0;
            }));
    }
}
