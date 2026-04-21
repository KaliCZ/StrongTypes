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
    public void Concat_NullTail_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => 1.Concat(null!, new[] { 2, 3 }));
        Assert.Throws<ArgumentNullException>(() => 1.Concat(new[] { 2, 3 }, null!));
    }

    [Fact]
    public void Concat_MixedTailTypes_AllWork()
    {
        // Arrays, lists, NonEmptyEnumerable, and other ICollection<T>-shaped sources
        // are all valid tails — verify the common combinations end-to-end.
        IEnumerable<int> array = new[] { 2, 3 };
        IEnumerable<int> list = new List<int> { 4, 5 };
        IEnumerable<int> nonEmpty = NonEmptyEnumerable.Create(6, 7);
        IEnumerable<int> hashSet = new HashSet<int> { 8, 9 };

        var result = 1.Concat(array, list, nonEmpty, hashSet);
        // HashSet ordering isn't guaranteed — sort the last-two slice for a stable assert.
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7 }, result.Take(7));
        Assert.Equal(new[] { 8, 9 }, result.Skip(7).OrderBy(x => x));
    }

    [Fact]
    public void Concat_LazyIteratorTail_Works()
    {
        // Enumerable.Where is a LINQ iterator with no pre-computable count —
        // still expands correctly into the output.
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

    // ── Concat(IEnumerable<T>) ──────────────────────────────────────────

    [Fact]
    public void Concat_Enumerable_LazyIterator_Works()
    {
        var list = NonEmptyEnumerable.Create(1, 2, 3);
        IEnumerable<int> lazy = new[] { 10, 20, 30 }.Where(_ => true);
        Assert.Equal(new[] { 1, 2, 3, 10, 20, 30 }, list.Concat(lazy));
    }

    // ── Reverse ─────────────────────────────────────────────────────────

    [Property]
    public void Reverse_ReversesOrder(NonEmptyEnumerable<int> list)
    {
        var reversed = list.Reverse();
        Assert.Equal(list.Count, reversed.Count);
        Assert.Equal(list.AsEnumerable().Reverse(), reversed);
    }

    [Property]
    public void Reverse_Twice_RoundTrips(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(list, list.Reverse().Reverse());
    }

    [Fact]
    public void Reverse_InterfaceOverload_HandlesNonConcreteImplementation()
    {
        INonEmptyEnumerable<int> custom = new CustomNonEmpty<int>([1, 2, 3]);
        Assert.Equal(new[] { 3, 2, 1 }, custom.Reverse());
    }

    // ── Prepend / Append ────────────────────────────────────────────────

    [Property]
    public void Prepend_AddsItemAtFront(NonEmptyEnumerable<int> list, int item)
    {
        var result = list.Prepend(item);
        Assert.Equal(list.Count + 1, result.Count);
        Assert.Equal(item, result.Head);
        Assert.Equal(list, result.Skip(1));
    }

    [Property]
    public void Append_AddsItemAtBack(NonEmptyEnumerable<int> list, int item)
    {
        var result = list.Append(item);
        Assert.Equal(list.Count + 1, result.Count);
        Assert.Equal(item, result.Last());
        Assert.Equal(list, result.Take(list.Count));
    }

    [Fact]
    public void Append_InterfaceOverload_HandlesNonConcreteImplementation()
    {
        INonEmptyEnumerable<int> custom = new CustomNonEmpty<int>([1, 2, 3]);
        Assert.Equal(new[] { 1, 2, 3, 99 }, custom.Append(99));
    }

    // ── Take ────────────────────────────────────────────────────────────

    [Property]
    public void Take_Positive_TakesFirstN(NonEmptyEnumerable<int> list)
    {
        var n = Math.Min(2, list.Count);
        var taken = list.Take(Positive<int>.Create(n));
        Assert.Equal(n, taken.Count);
        Assert.Equal(list.AsEnumerable().Take(n), taken);
    }

    [Fact]
    public void Take_ExceedingCount_ReturnsWholeSource()
    {
        var list = NonEmptyEnumerable.Create(1, 2, 3);
        Assert.Equal(list, list.Take(99));
    }

    [Fact]
    public void Take_IntOverload_ValidCount_Works()
    {
        var list = NonEmptyEnumerable.Create(1, 2, 3, 4, 5);
        Assert.Equal(new[] { 1, 2, 3 }, list.Take(3));
    }

    [Fact]
    public void Take_IntOverload_ZeroOrNegative_Throws()
    {
        var list = NonEmptyEnumerable.Create(1, 2, 3);
        Assert.Throws<ArgumentException>(() => list.Take(0));
        Assert.Throws<ArgumentException>(() => list.Take(-1));
    }

    [Fact]
    public void Take_InterfaceOverload_HandlesNonConcreteImplementation()
    {
        INonEmptyEnumerable<int> custom = new CustomNonEmpty<int>([1, 2, 3, 4, 5]);
        Assert.Equal(new[] { 1, 2 }, custom.Take(2));
    }

    // ── Aggregation (total functions) ───────────────────────────────────

    [Property]
    public void Max_MatchesLinq(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(Enumerable.Max(list), list.Max());
    }

    [Property]
    public void Min_MatchesLinq(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(Enumerable.Min(list), list.Min());
    }

    [Property]
    public void MaxBy_MatchesLinq(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(Enumerable.MaxBy(list, x => -x), list.MaxBy(x => -x));
    }

    [Property]
    public void MinBy_MatchesLinq(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(Enumerable.MinBy(list, x => -x), list.MinBy(x => -x));
    }

    [Property]
    public void Last_ReturnsLastElement(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(list[list.Count - 1], list.Last());
    }

    [Property]
    public void Aggregate_MatchesLinq(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(Enumerable.Aggregate(list, (a, b) => a + b), list.Aggregate((a, b) => a + b));
    }

    [Property]
    public void Average_MatchesLinq(NonEmptyEnumerable<int> list)
    {
        // Non-empty guarantees Average is defined — implementation uses INumber<T>
        // which does integer division for int, so compare against the same arithmetic.
        // Skip inputs whose sum would overflow int: Average throws checked, but the
        // comparison baseline `Sum()` also throws, so the generator already shrinks
        // around that. Use long for the expected to sidestep it cleanly.
        long expected = 0;
        foreach (var v in list) expected += v;
        if (expected > int.MaxValue || expected < int.MinValue) return;
        Assert.Equal((int)(expected / list.Count), list.Average());
    }

    [Fact]
    public void Average_Overflow_Throws()
    {
        var list = NonEmptyEnumerable.Create(int.MaxValue, int.MaxValue);
        Assert.Throws<OverflowException>(() => list.Average());
    }

    [Fact]
    public void Average_Double_DoesNotThrow()
    {
        // Floating-point has no overflow concept — `checked` is a no-op, Infinity
        // is the defined answer. Guard the behavior so nobody "fixes" this later.
        var list = NonEmptyEnumerable.Create(double.MaxValue, double.MaxValue);
        Assert.Equal(double.PositiveInfinity, list.Average());
    }

    [Fact]
    public void Aggregation_InterfaceOverload_HandlesNonConcreteImplementation()
    {
        INonEmptyEnumerable<int> custom = new CustomNonEmpty<int>([3, 1, 4, 1, 5, 9, 2, 6]);
        Assert.Equal(9, custom.Max());
        Assert.Equal(1, custom.Min());
        Assert.Equal(6, custom.Last());
        Assert.Equal(31, custom.Aggregate((a, b) => a + b));
        Assert.Equal(3, custom.Average());
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
