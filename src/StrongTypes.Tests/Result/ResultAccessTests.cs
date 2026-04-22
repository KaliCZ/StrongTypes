#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultAccessTests
{
    // ── `is {} s` pattern — the motivating ergonomic ───────────────────

    [Property]
    public void SuccessAccess_ValueType_UnwrapsInIsPattern(int value)
    {
        Result<int, string> r = value;
        if (r.Success is { } s)
        {
            int unwrapped = s;
            Assert.Equal(value, unwrapped);
        }
        else
        {
            Assert.Fail("Expected success branch to match.");
        }
    }

    [Property]
    public void SuccessAccess_ReferenceType_UnwrapsInIsPattern(string value)
    {
        Result<string, int> r = value;
        if (r.Success is { } s)
        {
            string unwrapped = s;
            Assert.Equal(value, unwrapped);
        }
        else
        {
            Assert.Fail("Expected success branch to match.");
        }
    }

    [Property]
    public void ErrorAccess_ReferenceType_UnwrapsInIsPattern(string error)
    {
        Result<int, string> r = error;
        if (r.Error is { } e)
        {
            string unwrapped = e;
            Assert.Equal(error, unwrapped);
        }
        else
        {
            Assert.Fail("Expected error branch to match.");
        }
    }

    [Property]
    public void ErrorAccess_ValueType_UnwrapsInIsPattern(int error)
    {
        Result<string, int> r = error;
        if (r.Error is { } e)
        {
            int unwrapped = e;
            Assert.Equal(error, unwrapped);
        }
        else
        {
            Assert.Fail("Expected error branch to match.");
        }
    }

    [Property]
    public void SuccessAccess_OnErrorResult_IsNull(string error)
    {
        Result<int, string> r = error;
        Assert.Null(r.Success);
    }

    [Property]
    public void ErrorAccess_OnSuccessResult_IsNull(int value)
    {
        Result<int, string> r = value;
        Assert.Null(r.Error);
    }

    // ── Subclass Result<T> inherits the same extension members ─────────

    [Fact]
    public void ResultOfT_SuccessAccess_WorksThroughSubclass()
    {
        Result<int> r = 7;
        Assert.Equal(7, r.Success);
        Assert.Null(r.Error);
    }

    [Fact]
    public void ResultOfT_ErrorAccess_WorksThroughSubclass()
    {
        var ex = new InvalidOperationException("boom");
        Result<int> r = ex;
        Assert.Null(r.Success);
        Assert.Same(ex, r.Error);
    }
}
