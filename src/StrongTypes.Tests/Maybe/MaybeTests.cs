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
        var m = Maybe<int>.None;
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
        var m = Maybe<int>.None;
        var matched = m.Value is { };
        Assert.False(matched);
    }

    [Fact]
    public void IsPattern_EmptyReferenceType_DoesNotMatch()
    {
        var m = Maybe<string>.None;
        var matched = m.Value is { };
        Assert.False(matched);
    }

    // ── Value-null-iff-None invariant (one per extension class) ─────────
    //
    // These property tests pin down the single most important guarantee of
    // the extension-property design: Value is null exactly when the Maybe
    // is None, non-null exactly when it is Some. Covers both extension
    // classes (struct T and class T).

    [Property]
    public void MaybeStruct_Value_NullIffNone(Maybe<int> m)
    {
        if (m.IsNone) Assert.Null(m.Value);
        else Assert.NotNull(m.Value);
    }

    [Property]
    public void MaybeClass_Value_NullIffNone(Maybe<NonEmptyString> m)
    {
        if (m.IsNone) Assert.Null(m.Value);
        else Assert.NotNull(m.Value);
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
        Maybe<int> g(int a) => a > 0 ? Maybe<int>.Some(a * 10) : Maybe<int>.None;

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
        Assert.Equal(Maybe<int>.None, m.Where(_ => false));
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
        var result = Maybe<int>.None.Match(x => x + 1, () => -1);
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
        Maybe<int>.None.Match(_ => someHits++, () => noneHits++);
        Assert.Equal(0, someHits);
        Assert.Equal(1, noneHits);
    }

    [Fact]
    public void Match_Void_NullActionsAreSkipped()
    {
        Maybe<int>.Some(1).Match(ifNone: () => Assert.Fail());
        Maybe<int>.None.Match(ifSome: _ => Assert.Fail());
    }

    // ── Enumeration / `..` spread / foreach ─────────────────────────────

    [Fact]
    public void Spread_Empty_YieldsNoElements()
    {
        var maybe = Maybe<int>.None;
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
        foreach (var _ in Maybe<int>.None) count++;
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
        Assert.Equal(Maybe<int>.None, default);
        Assert.Equal(Maybe<string>.None, default);
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
        Assert.False(Maybe<int>.None.Equals(0));
    }

    [Fact]
    public void Equality_HashCodesMatch_WhenEqual()
    {
        Assert.Equal(Maybe<int>.Some(5).GetHashCode(), Maybe<int>.Some(5).GetHashCode());
        Assert.Equal(Maybe<int>.None.GetHashCode(), Maybe<int>.None.GetHashCode());
    }

    // Cross-closed-generic pairs (e.g. Maybe<string> vs Maybe<NonEmptyString>)
    // always compare unequal through Equals(object). Keeping Equals(object) strict
    // preserves its symmetry contract — we can't teach bare `string` or bare `int`
    // to see a Maybe<T> on the other side, so we don't pretend the relationship
    // exists in one direction. Typed IEquatable<T> and operator == remain available
    // for ergonomic same-T comparisons.
    [Fact]
    public void Equality_DifferentClosedGenerics_AlwaysUnequal()
    {
        var str = Maybe<string>.Some("abc");
        var nes = Maybe<NonEmptyString>.Some(NonEmptyString.Create("abc"));
        Assert.False(str.Equals((object)nes));
        Assert.False(nes.Equals((object)str));
        Assert.False(Maybe<string>.None.Equals((object)Maybe<NonEmptyString>.None));
        Assert.False(Maybe<NonEmptyString>.None.Equals((object)Maybe<string>.None));
    }

    // ── Comparison ──────────────────────────────────────────────────────

    [Fact]
    public void CompareTo_EmptyLessThanAnyValue()
    {
        Assert.True(Maybe<int>.None.CompareTo(Maybe<int>.Some(int.MinValue)) < 0);
        Assert.True(Maybe<int>.None.CompareTo(Maybe<int>.Some(0)) < 0);
    }

    [Fact]
    public void CompareTo_TwoEmpty_Equal()
    {
        Assert.Equal(0, Maybe<int>.None.CompareTo(Maybe<int>.None));
    }

    [Fact]
    public void CompareTo_SomeGreaterThanEmpty()
    {
        Assert.True(Maybe<int>.Some(0).CompareTo(Maybe<int>.None) > 0);
    }

    [Fact]
    public void CompareTo_DirectTValue_EmptyLessThan()
    {
        Assert.True(Maybe<int>.None.CompareTo(0) < 0);
    }

    [Property]
    public void CompareTo_SomeToSome_MatchesUnderlying(int a, int b)
    {
        var result = Maybe<int>.Some(a).CompareTo(Maybe<int>.Some(b));
        Assert.Equal(System.Math.Sign(a.CompareTo(b)), System.Math.Sign(result));
    }

    // ── Operators (==, !=, <, <=, >, >= agree with Equals/CompareTo) ────

    [Property]
    public void Operators_Equality_AgreesWithEquals(Maybe<int> a, Maybe<int> b)
    {
        Assert.Equal(a.Equals(b), a == b);
        Assert.Equal(!a.Equals(b), a != b);
    }

    [Property]
    public void Operators_EqualityAgainstT_AgreesWithEquals(Maybe<int> m, int t)
    {
        Assert.Equal(m.Equals(t), m == t);
        Assert.Equal(m.Equals(t), t == m);
        Assert.Equal(!m.Equals(t), m != t);
        Assert.Equal(!m.Equals(t), t != m);
    }

    [Property]
    public void Operators_Comparison_AgreesWithCompareTo(Maybe<int> a, Maybe<int> b)
    {
        var c = a.CompareTo(b);
        Assert.Equal(c < 0, a < b);
        Assert.Equal(c <= 0, a <= b);
        Assert.Equal(c > 0, a > b);
        Assert.Equal(c >= 0, a >= b);
    }

    [Property]
    public void Operators_ComparisonAgainstT_AgreesWithCompareTo(Maybe<int> m, int t)
    {
        var c = m.CompareTo(t);
        Assert.Equal(c < 0, m < t);
        Assert.Equal(c <= 0, m <= t);
        Assert.Equal(c > 0, m > t);
        Assert.Equal(c >= 0, m >= t);
        // (T, Maybe<T>) flips the sign because both delegate to CompareTo on the Maybe side.
        Assert.Equal(c > 0, t < m);
        Assert.Equal(c >= 0, t <= m);
        Assert.Equal(c < 0, t > m);
        Assert.Equal(c <= 0, t >= m);
    }

    [Fact]
    public void Operators_NoneOrdersBeforeAnySome()
    {
        Assert.True(Maybe<int>.None < Maybe<int>.Some(int.MinValue));
        Assert.True(Maybe<int>.None < 0);
        Assert.True(0 > Maybe<int>.None);
        Assert.False(Maybe<int>.None > 0);
    }

    // ── ToString ────────────────────────────────────────────────────────

    [Fact]
    public void ToString_Some() => Assert.Equal("Some(5)", Maybe<int>.Some(5).ToString());

    [Fact]
    public void ToString_None() => Assert.Equal("None", Maybe<int>.None.ToString());
}
