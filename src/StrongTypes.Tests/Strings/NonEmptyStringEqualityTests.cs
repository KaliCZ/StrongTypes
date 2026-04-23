using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyStringEqualityTests
{
    [Property]
    public void Equals_NonEmptyString_Reflexive(NonEmptyString s) =>
        Assert.True(s.Equals(s));

    [Property]
    public void Equals_NonEmptyString_SameValue_AreEqual(NonEmptyString s)
    {
        var copy = NonEmptyString.Create(s.Value);
        Assert.True(s.Equals(copy));
        Assert.True(copy.Equals(s));
    }

    [Property]
    public void Equals_NonEmptyString_Null_IsFalse(NonEmptyString s) =>
        Assert.False(s.Equals((NonEmptyString?)null));

    [Property]
    public void Equals_NonEmptyString_DifferentValue_IsFalse(NonEmptyString a, NonEmptyString b)
    {
        if (a.Value == b.Value) return;
        Assert.False(a.Equals(b));
    }

    [Property]
    public void Equals_String_MirrorsValueEquality(NonEmptyString s)
    {
        Assert.True(s.Equals(s.Value));
        Assert.False(s.Equals(s.Value + "_suffix"));
        Assert.False(s.Equals((string?)null));
    }

    [Property]
    public void Equals_Object_HandlesNonEmptyString(NonEmptyString s)
    {
        object boxed = NonEmptyString.Create(s.Value);
        Assert.True(s.Equals(boxed));
    }

    [Property]
    public void Equals_Object_HandlesString(NonEmptyString s)
    {
        object boxed = s.Value;
        Assert.True(s.Equals(boxed));
    }

    [Property]
    public void Equals_Object_Null_IsFalse(NonEmptyString s) =>
        Assert.False(s.Equals((object?)null));

    [Property]
    public void Equals_Object_ForeignType_IsFalse(NonEmptyString s) =>
        Assert.False(s.Equals((object)42));

    [Property]
    public void GetHashCode_AgreesWithEquals(NonEmptyString s)
    {
        var copy = NonEmptyString.Create(s.Value);
        Assert.Equal(s.GetHashCode(), copy.GetHashCode());
    }

    [Property]
    public void GetHashCode_MatchesValueHashCode(NonEmptyString s) =>
        Assert.Equal(s.Value.GetHashCode(), s.GetHashCode());

    [Property]
    public void OperatorEquals_SameValue_IsTrue(NonEmptyString s)
    {
        var copy = NonEmptyString.Create(s.Value);
        Assert.True(s == copy);
        Assert.False(s != copy);
    }

    [Property]
    public void OperatorEquals_DifferentValue_IsFalse(NonEmptyString a, NonEmptyString b)
    {
        if (a.Value == b.Value) return;
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Fact]
    public void OperatorEquals_BothNull_IsTrue()
    {
        NonEmptyString? a = null;
        NonEmptyString? b = null;
        Assert.True(a == b);
        Assert.False(a != b);
    }

    [Property]
    public void OperatorEquals_OneNull_IsFalse(NonEmptyString s)
    {
        NonEmptyString? nil = null;
        Assert.False(s == nil);
        Assert.False(nil == s);
        Assert.True(s != nil);
        Assert.True(nil != s);
    }
}
