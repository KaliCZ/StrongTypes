#nullable enable

using System;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultFlatMapErrorTests
{
    [Property]
    public void FlatMapError_Error_AppliesContinuation(string error)
    {
        Result<int, string> r = error;
        // Factory used here because T (int) and UError (int) share the same
        // type — implicit int→Result<int,int> would be ambiguous.
        var bound = r.FlatMapError(e => Result.Success<int, int>(e.Length));
        Assert.True(bound.IsSuccess);
        Assert.Equal(error.Length, bound.Success);
    }

    [Property]
    public void FlatMapError_Error_ContinuationCanReturnError(string error)
    {
        Result<int, string> r = error;
        var bound = r.FlatMapError(_ => Result.Error<int, int>(-1));
        Assert.True(bound.IsError);
        Assert.Equal(-1, bound.Error);
    }

    [Property]
    public void FlatMapError_Success_ShortCircuits(int value)
    {
        Result<int, string> r = value;
        var bound = r.FlatMapError<int>(_ => throw new Exception("should not run"));
        Assert.True(bound.IsSuccess);
        Assert.Equal(value, bound.Success);
    }

    [Property]
    public async Task FlatMapErrorAsync_Error_AppliesContinuation(string error)
    {
        Result<int, string> r = error;
        var bound = await r.FlatMapErrorAsync(async e =>
        {
            await Task.Yield();
            return Result.Success<int, int>(e.Length);
        });
        Assert.Equal(error.Length, bound.Success);
    }

    [Property]
    public async Task FlatMapErrorAsync_Success_ShortCircuits(int value)
    {
        Result<int, string> r = value;
        var bound = await r.FlatMapErrorAsync<int>(_ => throw new Exception("should not run"));
        Assert.Equal(value, bound.Success);
    }
}
