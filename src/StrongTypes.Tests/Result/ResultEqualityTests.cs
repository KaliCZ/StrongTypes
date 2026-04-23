#nullable enable

using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ResultEqualityTests
{
    // ── == / != operators ──────────────────────────────────────────────

    [Property]
    public void OperatorEquals_SameSuccess_IsTrue(int value)
    {
        Result<int, string> a = value;
        Result<int, string> b = value;
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Property]
    public void OperatorEquals_SameError_IsTrue(string error)
    {
        Result<int, string> a = error;
        Result<int, string> b = error;
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Property]
    public void OperatorEquals_DifferentBranches_IsFalse(int value, string error)
    {
        Result<int, string> success = value;
        Result<int, string> failure = error;
        Assert.False(success == failure);
        Assert.True(success != failure);
    }

    [Fact]
    public void OperatorEquals_BothNull_IsTrue()
    {
        Result<int, string>? a = null;
        Result<int, string>? b = null;
        Assert.True(a == b);
    }

    [Fact]
    public void OperatorEquals_OneNull_IsFalse()
    {
        Result<int, string>? a = 1;
        Result<int, string>? b = null;
        Assert.False(a == b);
        Assert.False(b == a);
    }

    [Fact]
    public void OperatorEquals_ReferenceEqualInstance_IsTrue()
    {
        Result<int, string> r = 1;
        var alias = r;
        Assert.True(r == alias);
    }

    // ── Equals(T) — raw success value ──────────────────────────────────

    [Property]
    public void EqualsT_SuccessWithMatchingValue_IsTrue(int value)
    {
        Result<int, string> r = value;
        Assert.True(r.Equals(value));
    }

    [Property]
    public void EqualsT_SuccessWithDifferentValue_IsFalse(int value)
    {
        Result<int, string> r = value;
        Assert.False(r.Equals(value + 1));
    }

    [Property]
    public void EqualsT_ErrorResult_IsFalse(string error, int probe)
    {
        Result<int, string> r = error;
        Assert.False(r.Equals(probe));
    }

    // ── Equals(TError) — raw error value ───────────────────────────────

    [Property]
    public void EqualsTError_ErrorWithMatchingValue_IsTrue(string error)
    {
        Result<int, string> r = error;
        Assert.True(r.Equals(error));
    }

    [Property]
    public void EqualsTError_ErrorWithDifferentValue_IsFalse(string error)
    {
        Result<int, string> r = error;
        Assert.False(r.Equals(error + "_different"));
    }

    [Property]
    public void EqualsTError_SuccessResult_IsFalse(int value, string probe)
    {
        Result<int, string> r = value;
        Assert.False(r.Equals(probe));
    }
}
