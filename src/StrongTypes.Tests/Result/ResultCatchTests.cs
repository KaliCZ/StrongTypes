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
    public void Catch_OperationCanceledException_IsNotCaptured()
    {
        Assert.Throws<OperationCanceledException>(() =>
            Result.Catch<int>(() => throw new OperationCanceledException()));
    }

    [Fact]
    public void Catch_NestedOperationCanceledException_IsNotCaptured()
    {
        // The helper walks InnerException — wrapped cancellations propagate too.
        var wrapped = new InvalidOperationException("outer", new OperationCanceledException());
        Assert.Throws<InvalidOperationException>(() =>
            Result.Catch<int>(() => throw wrapped));
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
    public async Task CatchAsync_OperationCanceledException_IsNotCaptured()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await Result.CatchAsync<int>(async () =>
            {
                await Task.Delay(Timeout.Infinite, cts.Token);
                return 0;
            }));
    }
}
