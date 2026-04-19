#nullable enable

using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MaybeTests
{
    // ── Construction ────────────────────────────────────────────────────

    [Fact]
    public void Some_HasValueAndExposesUnderlyingValue()
    {
        var m = Maybe<int>.Some(42);
        Assert.True(m.HasValue);
        Assert.Equal(42, m.Value);
    }

    [Fact]
    public void Empty_IsEmpty()
    {
        var m = Maybe<int>.Empty;
        Assert.False(m.HasValue);
        Assert.Null(m.Value);
    }

    [Fact]
    public void Default_InstanceIsEmpty()
    {
        Maybe<int> m = default;
        Assert.False(m.HasValue);
    }

    [Fact]
    public void NonGenericFactory_Some()
    {
        var m = Maybe.Some("hello");
        Assert.True(m.HasValue);
        Assert.Equal("hello", m.Value);
    }

    // ── `is {} v` pattern (the whole point of the extension-property design) ──

    [Fact]
    public void IsPattern_ValueType_UnwrapsToUnderlyingInt()
    {
        var m = Maybe<int>.Some(7);
        if (m.Value is { } v)
        {
            // v is int (not int?), per Nullable<int>'s is-pattern behaviour.
            int unwrapped = v;
            Assert.Equal(7, unwrapped);
        }
        else
        {
            Assert.Fail("Expected pattern match on populated Maybe<int>.");
        }
    }

    [Fact]
    public void IsPattern_ReferenceType_UnwrapsToNonNullString()
    {
        var m = Maybe<string>.Some("hello");
        if (m.Value is { } v)
        {
            string unwrapped = v;
            Assert.Equal("hello", unwrapped);
        }
        else
        {
            Assert.Fail("Expected pattern match on populated Maybe<string>.");
        }
    }

    [Fact]
    public void IsPattern_EmptyValueType_DoesNotMatch()
    {
        var m = Maybe<int>.Empty;
        var matched = m.Value is { };
        Assert.False(matched);
    }

    [Fact]
    public void IsPattern_EmptyReferenceType_DoesNotMatch()
    {
        var m = Maybe<string>.Empty;
        var matched = m.Value is { };
        Assert.False(matched);
    }

    // ── Map / FlatMap / Where algebra ───────────────────────────────────

    [Property]
    public void Map_Identity(Maybe<int> m)
    {
        Assert.Equal(m, m.Map(x => x));
    }

    [Property]
    public void Map_Composition(Maybe<int> m)
    {
        var lhs = m.Map(x => x + 1).Map(x => x * 2);
        var rhs = m.Map(x => (x + 1) * 2);
        Assert.Equal(lhs, rhs);
    }

    [Property]
    public void FlatMap_LeftIdentity(int x)
    {
        Maybe<int> f(int a) => Maybe<int>.Some(a * 3);
        Assert.Equal(f(x), Maybe<int>.Some(x).FlatMap(f));
    }

    [Property]
    public void FlatMap_RightIdentity(Maybe<int> m)
    {
        Assert.Equal(m, m.FlatMap(Maybe<int>.Some));
    }

    [Property]
    public void FlatMap_Associativity(Maybe<int> m)
    {
        Maybe<int> f(int a) => Maybe<int>.Some(a + 1);
        Maybe<int> g(int a) => a > 0 ? Maybe<int>.Some(a * 10) : Maybe<int>.Empty;

        Assert.Equal(m.FlatMap(f).FlatMap(g), m.FlatMap(a => f(a).FlatMap(g)));
    }

    [Property]
    public void Where_True_Identity(Maybe<int> m)
    {
        Assert.Equal(m, m.Where(_ => true));
    }

    [Property]
    public void Where_False_Empty(Maybe<int> m)
    {
        Assert.Equal(Maybe<int>.Empty, m.Where(_ => false));
    }

    // ── Match ───────────────────────────────────────────────────────────

    [Fact]
    public void Match_Some_InvokesIfSome()
    {
        var result = Maybe<int>.Some(5).Match(x => x + 1, () => -1);
        Assert.Equal(6, result);
    }

    [Fact]
    public void Match_Empty_InvokesIfNone()
    {
        var result = Maybe<int>.Empty.Match(x => x + 1, () => -1);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void Match_Void_Some_OnlyInvokesSomeAction()
    {
        var someHits = 0;
        var noneHits = 0;
        Maybe<int>.Some(3).Match(_ => someHits++, () => noneHits++);
        Assert.Equal(1, someHits);
        Assert.Equal(0, noneHits);
    }

    [Fact]
    public void Match_Void_Empty_OnlyInvokesNoneAction()
    {
        var someHits = 0;
        var noneHits = 0;
        Maybe<int>.Empty.Match(_ => someHits++, () => noneHits++);
        Assert.Equal(0, someHits);
        Assert.Equal(1, noneHits);
    }

    [Fact]
    public void Match_Void_NullActionsAreSkipped()
    {
        Maybe<int>.Some(1).Match(ifNone: () => Assert.Fail());
        Maybe<int>.Empty.Match(ifSome: _ => Assert.Fail());
    }

    // ── Enumeration / `..` spread / foreach ─────────────────────────────

    [Fact]
    public void Spread_Empty_YieldsNoElements()
    {
        var maybe = Maybe<int>.Empty;
        int[] spread = [.. maybe];
        Assert.Empty(spread);
    }

    [Fact]
    public void Spread_Some_YieldsOneElement()
    {
        var maybe = Maybe<int>.Some(42);
        int[] spread = [.. maybe];
        Assert.Equal([42], spread);
    }

    [Fact]
    public void Foreach_Some_IteratesOnce()
    {
        var count = 0;
        foreach (var _ in Maybe<int>.Some(1)) count++;
        Assert.Equal(1, count);
    }

    [Fact]
    public void Foreach_Empty_IteratesZeroTimes()
    {
        var count = 0;
        foreach (var _ in Maybe<int>.Empty) count++;
        Assert.Equal(0, count);
    }

    // ── Equality ────────────────────────────────────────────────────────

    [Property]
    public void Equality_Reflexive(Maybe<int> m)
    {
        // Not "m == m" — CS1718 warns on self-comparison via operator. The
        // Equals/Assert.Equal paths go through IEquatable<T>.Equals directly.
        Assert.True(m.Equals(m));
        Assert.Equal(m, m);
    }

    [Property]
    public void Equality_Symmetric(Maybe<int> a, Maybe<int> b)
    {
        Assert.Equal(a.Equals(b), b.Equals(a));
    }

    [Fact]
    public void Equality_EmptyEqualsEmpty_AcrossInstances()
    {
        Assert.Equal(Maybe<int>.Empty, default);
        Assert.Equal(Maybe<string>.Empty, default);
    }

    [Fact]
    public void Equality_DirectTValue_Some()
    {
        var m = Maybe<int>.Some(10);
        Assert.True(m.Equals(10));
        Assert.False(m.Equals(11));
    }

    [Fact]
    public void Equality_DirectTValue_Empty()
    {
        Assert.False(Maybe<int>.Empty.Equals(0));
    }

    [Fact]
    public void Equality_HashCodesMatch_WhenEqual()
    {
        Assert.Equal(Maybe<int>.Some(5).GetHashCode(), Maybe<int>.Some(5).GetHashCode());
        Assert.Equal(Maybe<int>.Empty.GetHashCode(), Maybe<int>.Empty.GetHashCode());
    }

    // ── Cross-type equality: Maybe<string> <-> Maybe<NonEmptyString> ────

    [Fact]
    public void CrossTypeEquality_StringEqualsNonEmptyString_WhenValuesMatch()
    {
        var str = Maybe<string>.Some("abc");
        var nes = Maybe<NonEmptyString>.Some(NonEmptyString.Create("abc"));
        Assert.True(str.Equals((object)nes));
        Assert.True(nes.Equals((object)str));
    }

    [Fact]
    public void CrossTypeEquality_StringDiffersFromNonEmptyString_WhenValuesDiffer()
    {
        var str = Maybe<string>.Some("abc");
        var nes = Maybe<NonEmptyString>.Some(NonEmptyString.Create("xyz"));
        Assert.False(str.Equals((object)nes));
        Assert.False(nes.Equals((object)str));
    }

    [Fact]
    public void CrossTypeEquality_BothEmpty_Equal()
    {
        Assert.True(Maybe<string>.Empty.Equals((object)Maybe<NonEmptyString>.Empty));
        Assert.True(Maybe<NonEmptyString>.Empty.Equals((object)Maybe<string>.Empty));
    }

    [Fact]
    public void CrossTypeEquality_HashCodesMatch()
    {
        var str = Maybe<string>.Some("abc");
        var nes = Maybe<NonEmptyString>.Some(NonEmptyString.Create("abc"));
        Assert.Equal(str.GetHashCode(), nes.GetHashCode());
    }

    // ── Comparison ──────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_EmptyLessThanAnyValue()
    {
        Assert.True(Maybe<int>.Empty.CompareTo(Maybe<int>.Some(int.MinValue)) < 0);
        Assert.True(Maybe<int>.Empty.CompareTo(Maybe<int>.Some(0)) < 0);
    }

    [Fact]
    public void CompareTo_TwoEmpty_Equal()
    {
        Assert.Equal(0, Maybe<int>.Empty.CompareTo(Maybe<int>.Empty));
    }

    [Fact]
    public void CompareTo_SomeGreaterThanEmpty()
    {
        Assert.True(Maybe<int>.Some(0).CompareTo(Maybe<int>.Empty) > 0);
    }

    [Fact]
    public void CompareTo_DirectTValue_EmptyLessThan()
    {
        Assert.True(Maybe<int>.Empty.CompareTo(0) < 0);
    }

    [Property]
    public void CompareTo_SomeToSome_MatchesUnderlying(int a, int b)
    {
        var result = Maybe<int>.Some(a).CompareTo(Maybe<int>.Some(b));
        Assert.Equal(System.Math.Sign(a.CompareTo(b)), System.Math.Sign(result));
    }

    // ── ToString ────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Some() => Assert.Equal("Some(5)", Maybe<int>.Some(5).ToString());

    [Fact]
    public void ToString_Empty() => Assert.Equal("Empty", Maybe<int>.Empty.ToString());
}
