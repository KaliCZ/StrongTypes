#nullable enable

using System;
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
    public void Concat_Enumerable_AppendsItems(NonEmptyEnumerable<int> list)
    {
        var extra = Enumerable.Range(100, 3);
        var extended = list.Concat(extra);
        Assert.Equal(list.Count + 3, extended.Count);
        Assert.Equal(extra, extended.Skip(list.Count));
    }
}
