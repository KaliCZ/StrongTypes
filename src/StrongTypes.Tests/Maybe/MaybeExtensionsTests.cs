using System;
using System.Linq;
using System.Threading.Tasks;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class MaybeExtensionsTests
{
    // ── LINQ aliases ────────────────────────────────────────────────────

    [Property]
    public void Select_IsMap(Maybe<int> m)
    {
        Assert.Equal(m.Map(x => x + 1), m.Select(x => x + 1));
    }

    [Property]
    public void SelectMany_IsFlatMap(Maybe<int> m)
    {
        Maybe<int> f(int x) => x > 0 ? Maybe<int>.Some(x * 2) : Maybe<int>.None;
        Assert.Equal(m.FlatMap(f), m.SelectMany(f));
    }

    [Fact]
    public void SelectMany_ThreeArg_BuildsCompositeValue()
    {
        var result =
            from a in Maybe<int>.Some(2)
            from b in Maybe<int>.Some(3)
            select a + b;

        Assert.True(result.HasValue);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void SelectMany_ThreeArg_ShortCircuitsOnEmpty()
    {
        var result =
            from a in Maybe<int>.Some(2)
            from b in Maybe<int>.None
            select a + b;

        Assert.False(result.HasValue);
    }

    // ── Async ───────────────────────────────────────────────────────────

    [Fact]
    public async Task MapAsync_Some_AwaitsAndWraps()
    {
        var result = await Maybe<int>.Some(3).MapAsync(async x =>
        {
            await Task.Yield();
            return x * 10;
        });
        Assert.Equal(Maybe<int>.Some(30), result);
    }

    [Fact]
    public async Task MapAsync_Empty_DoesNotInvokeFunc()
    {
        var invoked = false;
        var result = await Maybe<int>.None.MapAsync(async x =>
        {
            invoked = true;
            await Task.Yield();
            return x;
        });
        Assert.False(invoked);
        Assert.Equal(Maybe<int>.None, result);
    }

    [Fact]
    public async Task FlatMapAsync_Some_AwaitsAndBinds()
    {
        var result = await Maybe<int>.Some(3).FlatMapAsync(async x =>
        {
            await Task.Yield();
            return x > 0 ? Maybe<int>.Some(x * x) : Maybe<int>.None;
        });
        Assert.Equal(Maybe<int>.Some(9), result);
    }

    [Fact]
    public async Task Match_WithAsyncLambdas_Some()
    {
        // Match<R> with R = Task<int> covers what MatchAsync used to do — the
        // async lambdas just return Task<int>, Match returns Task<int>, await unwraps.
        var result = await Maybe<int>.Some(5).Match(
            async x => { await Task.Yield(); return x + 1; },
            async () => { await Task.Yield(); return -1; });
        Assert.Equal(6, result);
    }

    [Fact]
    public async Task Match_WithAsyncLambdas_Empty()
    {
        var result = await Maybe<int>.None.Match(
            async x => { await Task.Yield(); return x + 1; },
            async () => { await Task.Yield(); return -1; });
        Assert.Equal(-1, result);
    }

    // ── ToMaybe ─────────────────────────────────────────────────────────

    [Property]
    public void ToMaybe_Struct_Roundtrips(int? value)
    {
        var m = value.ToMaybe();
        if (value.HasValue)
        {
            Assert.True(m.HasValue);
            Assert.Equal(value.Value, m.Value);
        }
        else
        {
            Assert.False(m.HasValue);
        }
    }

    [Property]
    public void ToMaybe_Class_Roundtrips(NonEmptyString? value)
    {
        var m = value.ToMaybe();
        if (value is null)
        {
            Assert.False(m.HasValue);
        }
        else
        {
            Assert.True(m.HasValue);
            Assert.Equal(value, m.Value);
        }
    }

    // ── IEnumerable<T> → Maybe<T> (Safe* family) ────────────────────────

    [Fact]
    public void SafeFirst_Empty_IsEmpty() =>
        Assert.Equal(Maybe<int>.None, Array.Empty<int>().SafeFirst());

    [Fact]
    public void SafeFirst_NonEmpty_ReturnsFirst() =>
        Assert.Equal(Maybe<int>.Some(1), new[] { 1, 2, 3 }.SafeFirst());

    [Fact]
    public void SafeFirst_Predicate_FiltersFirst() =>
        Assert.Equal(Maybe<int>.Some(4), new[] { 1, 4, 7 }.SafeFirst(x => x > 2));

    [Fact]
    public void SafeFirst_Predicate_NoMatch_IsEmpty() =>
        Assert.Equal(Maybe<int>.None, new[] { 1, 2, 3 }.SafeFirst(x => x > 10));

    [Fact]
    public void SafeLast_NonEmpty_ReturnsLast() =>
        Assert.Equal(Maybe<int>.Some(3), new[] { 1, 2, 3 }.SafeLast());

    [Fact]
    public void SafeLast_Empty_IsEmpty() =>
        Assert.Equal(Maybe<int>.None, Array.Empty<int>().SafeLast());

    [Fact]
    public void SafeSingle_SingleElement_ReturnsIt() =>
        Assert.Equal(Maybe<int>.Some(5), new[] { 5 }.SafeSingle());

    [Fact]
    public void SafeSingle_Empty_IsEmpty() =>
        Assert.Equal(Maybe<int>.None, Array.Empty<int>().SafeSingle());

    [Fact]
    public void SafeSingle_MultipleElements_IsEmpty() =>
        Assert.Equal(Maybe<int>.None, new[] { 1, 2 }.SafeSingle());

    [Fact]
    public void SafeMax_Empty_IsEmpty() =>
        Assert.Equal(Maybe<int>.None, Array.Empty<int>().SafeMax());

    [Fact]
    public void SafeMax_NonEmpty_ReturnsMax() =>
        Assert.Equal(Maybe<int>.Some(9), new[] { 1, 9, 3, 7 }.SafeMax());

    [Fact]
    public void SafeMax_WithSelector_PicksByProjection() =>
        Assert.Equal(Maybe<int>.Some(5),
            new[] { "a", "ccccc", "bb" }.SafeMax(s => s.Length));

    [Fact]
    public void SafeMin_NonEmpty_ReturnsMin() =>
        Assert.Equal(Maybe<int>.Some(1), new[] { 9, 1, 3, 7 }.SafeMin());

    // ── Values (IEnumerable<Maybe<T>> → IEnumerable<T>) ─────────────────

    [Fact]
    public void Values_FiltersEmptiesAndUnwrapsSome()
    {
        Maybe<int>[] input = [1, Maybe.None, 2, Maybe.None];
        Assert.Equal([1, 2], input.Values().ToArray());
    }

    [Fact]
    public void Values_AllEmpty_YieldsNothing()
    {
        Maybe<int>[] input = [Maybe<int>.None, Maybe<int>.None];
        Assert.Empty(input.Values());
    }
}
