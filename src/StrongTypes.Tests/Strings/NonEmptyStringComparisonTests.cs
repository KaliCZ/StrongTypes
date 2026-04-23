using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringComparisonTests
{
    private static int Sign(int n) => Math.Sign(n);

    [Property]
    public void CompareTo_NonEmptyString_MirrorsStringCompareTo(NonEmptyString a, NonEmptyString b) =>
        Assert.Equal(Sign(a.Value.CompareTo(b.Value)), Sign(a.CompareTo(b)));

    [Property]
    public void CompareTo_NonEmptyString_Null_ReturnsOne(NonEmptyString s) =>
        Assert.Equal(1, s.CompareTo((NonEmptyString?)null));

    [Property]
    public void CompareTo_String_MirrorsStringCompareTo(NonEmptyString s, NonEmptyString other) =>
        Assert.Equal(Sign(s.Value.CompareTo(other.Value)), Sign(s.CompareTo(other.Value)));

    [Property]
    public void CompareTo_String_Null_MatchesStringCompareToNull(NonEmptyString s) =>
        Assert.Equal(s.Value.CompareTo(null), s.CompareTo((string?)null));

    [Property]
    public void IComparable_CompareTo_Object_Null_ReturnsOne(NonEmptyString s) =>
        Assert.Equal(1, ((IComparable)s).CompareTo(null));

    [Property]
    public void IComparable_CompareTo_Object_NonEmptyString(NonEmptyString a, NonEmptyString b) =>
        Assert.Equal(Sign(a.CompareTo(b)), Sign(((IComparable)a).CompareTo(b)));

    [Property]
    public void IComparable_CompareTo_Object_String(NonEmptyString a, NonEmptyString b) =>
        Assert.Equal(Sign(a.CompareTo(b.Value)), Sign(((IComparable)a).CompareTo(b.Value)));

    [Property]
    public void IComparable_CompareTo_Object_ForeignType_Throws(NonEmptyString s) =>
        Assert.Throws<ArgumentException>(() => ((IComparable)s).CompareTo(42));

    [Property]
    public void Operators_AgreeWithCompareTo(NonEmptyString a, NonEmptyString b)
    {
        var cmp = a.CompareTo(b);
        Assert.Equal(cmp < 0, a < b);
        Assert.Equal(cmp <= 0, a <= b);
        Assert.Equal(cmp > 0, a > b);
        Assert.Equal(cmp >= 0, a >= b);
    }

    [Property]
    public void Operators_LeftNull_SortsBeforeNonNull(NonEmptyString s)
    {
        NonEmptyString? nil = null;
        Assert.True(nil < s);
        Assert.True(nil <= s);
        Assert.False(nil > s);
        Assert.False(nil >= s);
    }

    [Property]
    public void Operators_RightNull_NonNullSortsAfter(NonEmptyString s)
    {
        NonEmptyString? nil = null;
        Assert.False(s < nil);
        Assert.False(s <= nil);
        Assert.True(s > nil);
        Assert.True(s >= nil);
    }

    [Fact]
    public void Operators_BothNull()
    {
        NonEmptyString? a = null;
        NonEmptyString? b = null;
        Assert.False(a < b);
        Assert.True(a <= b);
        Assert.False(a > b);
        Assert.True(a >= b);
    }
}
