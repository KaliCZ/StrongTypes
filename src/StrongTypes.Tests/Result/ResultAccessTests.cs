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

    // ── Null-iff-opposite-branch invariants (one per extension class) ───
    //
    // Each property test covers one of the four extension classes —
    // ResultSuccess{Struct,Class}Extensions and ResultError{Struct,Class}Extensions
    // — and asserts Value is non-null exactly when its branch is active, null
    // exactly when the other branch is. Done as property tests so branch
    // coverage isn't hostage to one hand-picked value.

    [Property]
    public void SuccessAccess_StructT_NonNullOnSuccess_NullOnError(int successValue, string errorValue)
    {
        Result<int, string> asSuccess = successValue;
        Result<int, string> asError = errorValue;

        Assert.Equal(successValue, asSuccess.Success);
        Assert.Null(asError.Success);
    }

    [Property]
    public void SuccessAccess_ClassT_NonNullOnSuccess_NullOnError(NonEmptyString successValue, int errorValue)
    {
        Result<string, int> asSuccess = successValue.Value;
        Result<string, int> asError = errorValue;

        Assert.Equal(successValue.Value, asSuccess.Success);
        Assert.Null(asError.Success);
    }

    [Property]
    public void ErrorAccess_StructError_NonNullOnError_NullOnSuccess(NonEmptyString successValue, int errorValue)
    {
        Result<string, int> asSuccess = successValue.Value;
        Result<string, int> asError = errorValue;

        Assert.Null(asSuccess.Error);
        Assert.Equal(errorValue, asError.Error);
    }

    [Property]
    public void ErrorAccess_ClassError_NonNullOnError_NullOnSuccess(int successValue, NonEmptyString errorValue)
    {
        Result<int, string> asSuccess = successValue;
        Result<int, string> asError = errorValue.Value;

        Assert.Null(asSuccess.Error);
        Assert.Equal(errorValue.Value, asError.Error);
    }

    // ── Invariants over an arbitrary Result<int, string> ──────────────
    //
    // These exercise the generator directly: the test takes whatever
    // Result the arbitrary produces and asserts the invariants that
    // hold on every instance regardless of which branch it landed on.

    [Property]
    public void BranchesAreMutuallyExclusive(Result<int, string> r)
    {
        Assert.NotEqual(r.IsSuccess, r.IsError);
    }

    [Property]
    public void SuccessIsNonNull_IffIsSuccess(Result<int, string> r)
    {
        Assert.Equal(r.IsSuccess, r.Success.HasValue);
    }

    [Property]
    public void ErrorIsNonNull_IffIsError(Result<int, string> r)
    {
        Assert.Equal(r.IsError, r.Error is not null);
    }

    [Property]
    public void SuccessAccess_PatternMatchesActiveBranch(Result<int, string> r)
    {
        if (r.Success is { } s)
        {
            Assert.True(r.IsSuccess);
            Assert.Equal(s, r.Success);
        }
        else
        {
            Assert.True(r.IsError);
        }
    }

    [Property]
    public void ErrorAccess_PatternMatchesActiveBranch(Result<int, string> r)
    {
        if (r.Error is { } e)
        {
            Assert.True(r.IsError);
            Assert.Equal(e, r.Error);
        }
        else
        {
            Assert.True(r.IsSuccess);
        }
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
