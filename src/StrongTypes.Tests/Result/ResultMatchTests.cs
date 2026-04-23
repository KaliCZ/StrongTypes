using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultMatchTests
{
    // ── Match<R> ────────────────────────────────────────────────────────

    [Property]
    public void Match_Success_InvokesSuccessBranch(int value)
    {
        Result<int, string> r = value;
        var output = r.Match(success: s => s + 1, error: _ => -1);
        Assert.Equal(value + 1, output);
    }

    [Property]
    public void Match_Error_InvokesErrorBranch(string error)
    {
        Result<int, string> r = error;
        var output = r.Match(success: _ => "ok", error: e => e);
        Assert.Equal(error, output);
    }

    // ── Match (void) ───────────────────────────────────────────────────

    [Property]
    public void Match_Void_Success_InvokesOnlySuccessBranch(int value)
    {
        Result<int, string> r = value;
        int? successInvoked = null;
        string? errorInvoked = null;
        r.Match(success: s => successInvoked = s, error: e => errorInvoked = e);
        Assert.Equal(value, successInvoked);
        Assert.Null(errorInvoked);
    }

    [Property]
    public void Match_Void_Error_InvokesOnlyErrorBranch(string error)
    {
        Result<int, string> r = error;
        int? successInvoked = null;
        string? errorInvoked = null;
        r.Match(success: s => successInvoked = s, error: e => errorInvoked = e);
        Assert.Null(successInvoked);
        Assert.Equal(error, errorInvoked);
    }

    [Fact]
    public void Match_Void_NullCallbacks_AreIgnored()
    {
        Result<int, string> success = 1;
        Result<int, string> error = "e";
        success.Match();
        error.Match();
    }

    // ── MatchAsync ──────────────────────────────────────────────────────

    [Property]
    public async Task MatchAsync_Success_InvokesSuccessBranch(int value)
    {
        Result<int, string> r = value;
        var output = await r.MatchAsync(
            success: async s => { await Task.Yield(); return s + 1; },
            error: _ => Task.FromResult(-1));
        Assert.Equal(value + 1, output);
    }

    [Property]
    public async Task MatchAsync_Error_InvokesErrorBranch(string error)
    {
        Result<int, string> r = error;
        var output = await r.MatchAsync(
            success: _ => Task.FromResult("ok"),
            error: async e => { await Task.Yield(); return e; });
        Assert.Equal(error, output);
    }

    [Property]
    public async Task MatchAsync_Void_Success_InvokesOnlySuccessBranch(int value)
    {
        Result<int, string> r = value;
        int? successInvoked = null;
        string? errorInvoked = null;
        await r.MatchAsync(
            success: s => { successInvoked = s; return Task.CompletedTask; },
            error: e => { errorInvoked = e; return Task.CompletedTask; });
        Assert.Equal(value, successInvoked);
        Assert.Null(errorInvoked);
    }
}
