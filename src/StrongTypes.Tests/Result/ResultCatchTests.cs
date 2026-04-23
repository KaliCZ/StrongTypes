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
    public void Catch_OperationCanceledException_PropagatesByDefault()
    {
        Assert.Throws<OperationCanceledException>(() =>
            Result.Catch<int>(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void Catch_OperationCanceledException_CapturedWhenPropagateFalse()
    {
        var oce = new OperationCanceledException();
        var r = Result.Catch<int>(() => throw oce, propagateCancellation: false);
        Assert.True(r.IsError);
        Assert.Same(oce, r.Error);
    }

    [Fact]
    public void Catch_TaskCanceledException_PropagatesByDefault()
    {
        // TaskCanceledException derives from OperationCanceledException, so
        // the same propagation rule covers it.
        Assert.Throws<TaskCanceledException>(() =>
            Result.Catch<int>(() => throw new TaskCanceledException()));
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
    public void CatchTyped_ReturnsNarrowedResultType()
    {
        // Return type flows the chosen TException through — caller gets
        // Result<int, InvalidOperationException>, not the base Result<int>.
        Result<int, InvalidOperationException> r =
            Result.Catch<int, InvalidOperationException>(() => 1);
        Assert.True(r.IsSuccess);
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
        // Double-guarded: OCE doesn't match InvalidOperationException anyway,
        // and the default propagateCancellation=true would rethrow it first.
        Assert.Throws<OperationCanceledException>(() =>
            Result.Catch<int, InvalidOperationException>(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void CatchTyped_WithExceptionType_PropagatesOceByDefault()
    {
        // TException = Exception would normally catch OCE, but
        // propagateCancellation=true rethrows it first.
        Assert.Throws<OperationCanceledException>(() =>
            Result.Catch<int, Exception>(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void CatchTyped_WithExceptionType_CapturesOceWhenPropagateFalse()
    {
        var oce = new OperationCanceledException();
        var r = Result.Catch<int, Exception>(() => throw oce, propagateCancellation: false);
        Assert.True(r.IsError);
        Assert.Same(oce, r.Error);
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
    public async Task CatchAsync_OperationCanceledException_PropagatesByDefault()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await Result.CatchAsync<int>(async () =>
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
                return 0;
            }));
    }

    [Fact]
    public async Task CatchAsync_OperationCanceledException_CapturedWhenPropagateFalse()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var r = await Result.CatchAsync<int>(async () =>
        {
            await Task.Delay(Timeout.Infinite, cts.Token);
            return 0;
        }, propagateCancellation: false);
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
    public async Task CatchAsyncTyped_ReturnsNarrowedResultType()
    {
        Result<int, InvalidOperationException> r =
            await Result.CatchAsync<int, InvalidOperationException>(async () =>
            {
                await Task.Yield();
                return 1;
            });
        Assert.True(r.IsSuccess);
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
