#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyEnumerableExtensionsTests
{
    [Fact]
    public void AsNonEmpty_EmptyReturnsNull()
    {
        Assert.Null(Array.Empty<int>().AsNonEmpty());
    }

    [Fact]
    public void AsNonEmpty_PopulatedReturnsWrapped()
    {
        var list = new[] { 1, 2, 3 }.AsNonEmpty();
        Assert.NotNull(list);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void ToNonEmpty_EmptyThrows()
    {
        Assert.Throws<ArgumentException>(() => Array.Empty<int>().ToNonEmpty());
    }

    [Fact]
    public void ToNonEmpty_NullThrows()
    {
        Assert.Throws<ArgumentException>(() => ((int[]?)null).ToNonEmpty());
    }

    [Fact]
    public void ToNonEmpty_PopulatedReturnsWrapped()
    {
        var list = new[] { 1, 2, 3 }.ToNonEmpty();
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Property]
    public void Select_PreservesLengthAndMapsElements(NonEmptyEnumerable<int> list)
    {
        var mapped = list.Select(i => i * 2);
        Assert.Equal(list.Count, mapped.Count);
        for (var i = 0; i < list.Count; i++)
            Assert.Equal(list[i] * 2, mapped[i]);
    }

    [Property]
    public void Select_WithIndex_SeesZeroBasedIndex(NonEmptyEnumerable<int> list)
    {
        var indices = list.Select((_, i) => i);
        Assert.Equal(Enumerable.Range(0, list.Count), indices);
    }

    [Property]
    public void Select_InterfaceAndConcreteOverloads_Agree(NonEmptyEnumerable<int> list)
    {
        // Concrete overload vs. interface overload with a concrete backing — same result.
        INonEmptyEnumerable<int> asInterface = list;
        Assert.Equal(list.Select(i => i * 2), asInterface.Select(i => i * 2));
        Assert.Equal(list.Select((x, i) => x + i), asInterface.Select((x, i) => x + i));
    }

    [Fact]
    public void Select_InterfaceOverload_HandlesNonConcreteImplementation()
    {
        // A hand-rolled INonEmptyEnumerable<T> forces the interface overload's fallback
        // path (no NonEmptyEnumerable<T> to dispatch to).
        INonEmptyEnumerable<int> custom = new CustomNonEmpty<int>([10, 20, 30]);
        Assert.Equal(new[] { 11, 21, 31 }, custom.Select(i => i + 1));
        Assert.Equal(new[] { 10, 21, 32 }, custom.Select((x, i) => x + i));
    }

    [Property]
    public void Distinct_MatchesLinqDistinct(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(Enumerable.Distinct(list), list.Distinct());
    }

    [Property]
    public void SelectMany_EmitsAtLeastHeadSequence(NonEmptyEnumerable<int> list)
    {
        // Each input element produces a 2-element non-empty sequence — the
        // flattened length is therefore exactly 2 * input length.
        var flattened = list.SelectMany(i => NonEmptyEnumerable.Create(i, i));
        Assert.Equal(list.Count * 2, flattened.Count);
    }

    [Property]
    public void Concat_Params_AppendsItems(NonEmptyEnumerable<int> list)
    {
        var extended = list.Concat(99, 100);
        Assert.Equal(list.Count + 2, extended.Count);
        Assert.Equal(list, extended.Take(list.Count));
        Assert.Equal(new[] { 99, 100 }, extended.Skip(list.Count));
    }

    [Property]
    public void Concat_Params_InterfaceAndConcreteOverloads_Agree(NonEmptyEnumerable<int> list)
    {
        INonEmptyEnumerable<int> asInterface = list;
        Assert.Equal(list.Concat(99, 100), asInterface.Concat(99, 100));
    }

    [Fact]
    public void Concat_Params_InterfaceOverload_HandlesNonConcreteImplementation()
    {
        INonEmptyEnumerable<int> custom = new CustomNonEmpty<int>([1, 2, 3]);
        Assert.Equal(new[] { 1, 2, 3, 99, 100 }, custom.Concat(99, 100));
    }

    [Property]
    public void Concat_Enumerable_AppendsItems(NonEmptyEnumerable<int> list)
    {
        var extra = Enumerable.Range(100, 3);
        var extended = list.Concat(extra);
        Assert.Equal(list.Count + 3, extended.Count);
        Assert.Equal(extra, extended.Skip(list.Count));
    }

    [Fact]
    public void Flatten_SingleInner_ReturnsInner()
    {
        var nested = NonEmptyEnumerable.Create(NonEmptyEnumerable.Create(1, 2, 3));
        var flat = nested.Flatten();
        Assert.Equal(new[] { 1, 2, 3 }, flat);
    }

    [Fact]
    public void Flatten_MultipleInners_ConcatsInOrder()
    {
        var nested = NonEmptyEnumerable.Create(
            NonEmptyEnumerable.Create(1, 2),
            NonEmptyEnumerable.Create(3),
            NonEmptyEnumerable.Create(4, 5, 6));
        var flat = nested.Flatten();
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6 }, flat);
    }

    [Property]
    public void Flatten_MatchesLinqSelectMany(NonEmptyEnumerable<int> list)
    {
        // Wrap every element in a singleton non-empty list and flatten back —
        // should round-trip the original elements exactly.
        var nested = list.Select(x => NonEmptyEnumerable.Create(x));
        Assert.Equal(list, nested.Flatten());
    }

    [Property]
    public void Flatten_LengthEqualsSumOfInnerCounts(NonEmptyEnumerable<int> list)
    {
        // Duplicate each element into a pair — flat length is exactly 2x input length.
        var nested = list.Select(x => NonEmptyEnumerable.Create(x, x));
        var flat = nested.Flatten();
        Assert.Equal(list.Count * 2, flat.Count);
    }

    // ── Concat(this T head, params IEnumerable<T>[] tails) ──────────────

    [Fact]
    public void Concat_HeadOnly_ReturnsSingleton()
    {
        var result = 42.Concat();
        Assert.Single(result);
        Assert.Equal(42, result.Head);
    }

    [Fact]
    public void Concat_SingleArrayTail_PrependsHead()
    {
        var result = 1.Concat(new[] { 2, 3, 4 });
        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void Concat_MultipleTails_ConcatsInOrder()
    {
        var result = 1.Concat(new[] { 2, 3 }, new[] { 4, 5, 6 }, new[] { 7 });
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, result);
    }

    [Fact]
    public void Concat_NullTails_AreSkipped()
    {
        // Null tails are treated as empty — useful when callers assemble tails from
        // optional sources (e.g. `maybeExtras?.ToArray()` that may be null).
        var result = 1.Concat(null!, new[] { 2, 3 }, null!, new[] { 4 });
        Assert.Equal(new[] { 1, 2, 3, 4 }, result);
    }

    [Fact]
    public void Concat_MixedTailTypes_AllDispatchCorrectly()
    {
        // Each tail here lands in a different CopyInto branch — validates the
        // polymorphic dispatch end-to-end.
        IEnumerable<int> array = new[] { 2, 3 };
        IEnumerable<int> list = new List<int> { 4, 5 };
        IEnumerable<int> nonEmpty = NonEmptyEnumerable.Create(6, 7);
        IEnumerable<int> readOnlyColl = new HashSet<int> { 8, 9 }; // ICollection<T> but not List<T>/array

        var result = 1.Concat(array, list, nonEmpty, readOnlyColl);
        // HashSet ordering isn't guaranteed — sort the last-two slice for a stable assert.
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, result.Take(7));
        Assert.Equal(new[] { 8, 9 }, result.Skip(7).OrderBy(x => x));
    }

    [Fact]
    public void Concat_LazyIteratorTail_ForcesListFallback()
    {
        // Enumerable.Where doesn't implement TryGetNonEnumeratedCount, so the fast path
        // bails and we fall through to the List<T> growth path.
        IEnumerable<int> lazy = new[] { 10, 20, 30 }.Where(_ => true);
        var result = 1.Concat(lazy);
        Assert.Equal(new[] { 1, 10, 20, 30 }, result);
    }

    [Fact]
    public void Concat_NullTailsArray_Throws()
    {
        IEnumerable<int>[] tails = null!;
        Assert.Throws<ArgumentNullException>(() => 1.Concat(tails));
    }

    [Property]
    public void Concat_MatchesPrependFollowedByLinqConcat(int head, int[] tail1, int[] tail2)
    {
        var expected = new[] { head }.Concat(tail1).Concat(tail2);
        var actual = head.Concat(tail1, tail2);
        Assert.Equal(expected, actual);
    }

    [Property]
    public void Concat_ResultCountEqualsOnePlusSumOfTails(int head, int[] tail1, int[] tail2, int[] tail3)
    {
        var result = head.Concat(tail1, tail2, tail3);
        Assert.Equal(1 + tail1.Length + tail2.Length + tail3.Length, result.Count);
        Assert.Equal(head, result.Head);
    }

    /// <summary>
    /// Minimal <see cref="INonEmptyEnumerable{T}"/> implementation backed by an array —
    /// lets the interface-overload tests force the fallback path that doesn't dispatch to
    /// <see cref="NonEmptyEnumerable{T}"/>.
    /// </summary>
    private sealed class CustomNonEmpty<T>(T[] items) : INonEmptyEnumerable<T>
    {
        public T this[int index] => items[index];
        public int Count => items.Length;
        public T Head => items[0];
        public System.Collections.Generic.IReadOnlyList<T> Tail => new ArraySegment<T>(items, 1, items.Length - 1);
        public System.Collections.Generic.IEnumerator<T> GetEnumerator()
            => ((System.Collections.Generic.IEnumerable<T>)items).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
