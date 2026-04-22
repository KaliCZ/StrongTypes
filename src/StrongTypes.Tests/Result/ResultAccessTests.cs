#nullable enable

using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultAccessTests
{
    // ── Branch accessors across the struct/class matrix ────────────────
    //
    // Four tests cover each combination of struct/class in the T and
    // TError slots, so every closed instantiation of the extension
    // classes (ResultSuccess{Struct,Class}Extensions / ResultError…) is
    // exercised.

    [Property]
    public void SuccessAccess_ValueType_ReturnsValue(int value)
    {
        Result<int, string> r = value;
        Assert.NotNull(r.Success);
        Assert.Equal(value, r.Success!.Value);
    }

    [Property]
    public void SuccessAccess_ReferenceType_ReturnsValue(string value)
    {
        Result<string, int> r = value;
        Assert.NotNull(r.Success);
        Assert.Equal(value, r.Success);
    }

    [Property]
    public void ErrorAccess_ReferenceType_ReturnsValue(string error)
    {
        Result<int, string> r = error;
        Assert.NotNull(r.Error);
        Assert.Equal(error, r.Error);
    }

    [Property]
    public void ErrorAccess_ValueType_ReturnsValue(int error)
    {
        Result<string, int> r = error;
        Assert.NotNull(r.Error);
        Assert.Equal(error, r.Error!.Value);
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
