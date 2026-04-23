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
        Assert.True(r.IsSuccess);
        Assert.False(r.IsError);
        Assert.Equal(value, r.Success!.Value);
        Assert.Null(r.Error);
    }

    [Property]
    public void SuccessAccess_ReferenceType_ReturnsValue(string value)
    {
        Result<string, int> r = value;
        Assert.True(r.IsSuccess);
        Assert.False(r.IsError);
        Assert.Equal(value, r.Success);
        Assert.Null(r.Error);
    }

    [Property]
    public void ErrorAccess_ReferenceType_ReturnsValue(string error)
    {
        Result<int, string> r = error;
        Assert.False(r.IsSuccess);
        Assert.True(r.IsError);
        Assert.Null(r.Success);
        Assert.Equal(error, r.Error);
    }

    [Property]
    public void ErrorAccess_ValueType_ReturnsValue(int error)
    {
        Result<string, int> r = error;
        Assert.False(r.IsSuccess);
        Assert.True(r.IsError);
        Assert.Null(r.Success);
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

        Assert.True(asSuccess.IsSuccess);
        Assert.False(asSuccess.IsError);
        Assert.Equal(successValue, asSuccess.Success);
        Assert.Null(asSuccess.Error);

        Assert.False(asError.IsSuccess);
        Assert.True(asError.IsError);
        Assert.Null(asError.Success);
        Assert.Equal(errorValue, asError.Error);
    }

    [Property]
    public void SuccessAccess_ClassT_NonNullOnSuccess_NullOnError(NonEmptyString successValue, int errorValue)
    {
        Result<string, int> asSuccess = successValue.Value;
        Result<string, int> asError = errorValue;

        Assert.True(asSuccess.IsSuccess);
        Assert.False(asSuccess.IsError);
        Assert.Equal(successValue.Value, asSuccess.Success);
        Assert.Null(asSuccess.Error);

        Assert.False(asError.IsSuccess);
        Assert.True(asError.IsError);
        Assert.Null(asError.Success);
        Assert.Equal(errorValue, asError.Error!.Value);
    }

    [Property]
    public void ErrorAccess_StructError_NonNullOnError_NullOnSuccess(NonEmptyString successValue, int errorValue)
    {
        Result<string, int> asSuccess = successValue.Value;
        Result<string, int> asError = errorValue;

        Assert.True(asSuccess.IsSuccess);
        Assert.False(asSuccess.IsError);
        Assert.Equal(successValue.Value, asSuccess.Success);
        Assert.Null(asSuccess.Error);

        Assert.False(asError.IsSuccess);
        Assert.True(asError.IsError);
        Assert.Null(asError.Success);
        Assert.Equal(errorValue, asError.Error!.Value);
    }

    [Property]
    public void ErrorAccess_ClassError_NonNullOnError_NullOnSuccess(int successValue, NonEmptyString errorValue)
    {
        Result<int, string> asSuccess = successValue;
        Result<int, string> asError = errorValue.Value;

        Assert.True(asSuccess.IsSuccess);
        Assert.False(asSuccess.IsError);
        Assert.Equal(successValue, asSuccess.Success);
        Assert.Null(asSuccess.Error);

        Assert.False(asError.IsSuccess);
        Assert.True(asError.IsError);
        Assert.Null(asError.Success);
        Assert.Equal(errorValue.Value, asError.Error);
    }

    // ── Invariants over an arbitrary Result<int, string> ──────────────
    //
    // These exercise the generator directly: the test takes whatever
    // Result the arbitrary produces and asserts the invariants that
    // hold on every instance regardless of which branch it landed on.

    [Property]
    public void BranchInvariants_HoldOnArbitraryResult(Result<int, string> r)
    {
        // Every Result is in exactly one branch; the four accessors
        // agree on which branch that is.
        Assert.NotEqual(r.IsSuccess, r.IsError);
        Assert.Equal(r.IsSuccess, r.Success.HasValue);
        Assert.Equal(r.IsError, r.Error is not null);
        Assert.NotEqual(r.Success.HasValue, r.Error is not null);
    }

    // ── Subclass Result<T> inherits the same extension members ─────────

    [Fact]
    public void ResultOfT_SuccessAccess_WorksThroughSubclass()
    {
        Result<int> r = 7;
        Assert.True(r.IsSuccess);
        Assert.False(r.IsError);
        Assert.Equal(7, r.Success);
        Assert.Null(r.Error);
    }

    [Fact]
    public void ResultOfT_ErrorAccess_WorksThroughSubclass()
    {
        var ex = new InvalidOperationException("boom");
        Result<int> r = ex;
        Assert.False(r.IsSuccess);
        Assert.True(r.IsError);
        Assert.Null(r.Success);
        Assert.Same(ex, r.Error);
    }
}
