#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class NonEmptyEnumerableTests
{
    // ── Factory: head-plus-tail shapes ──────────────────────────────────

    [Fact]
    public void Of_HeadOnly()
    {
        var list = NonEmptyEnumerable.Of("solo");
        Assert.Single(list);
        Assert.Equal("solo", list.Head);
    }

    [Fact]
    public void Of_HeadPlusParamsTail()
    {
        var list = NonEmptyEnumerable.Of(1, 2, 3, 4);
        Assert.Equal(new[] { 1, 2, 3, 4 }, list);
    }

    [Fact]
    public void Of_HeadPlusIEnumerableTail()
    {
        IEnumerable<int> tail = Enumerable.Range(2, 3);
        var list = NonEmptyEnumerable.Of(1, tail);
        Assert.Equal(new[] { 1, 2, 3, 4 }, list);
    }

    // ── Factory: sequence overloads ─────────────────────────────────────

    [Fact]
    public void TryCreate_EmptySequence_ReturnsNull()
    {
        Assert.Null(NonEmptyEnumerable.TryCreate(Array.Empty<int>()));
        Assert.Null(NonEmptyEnumerable.TryCreate(new List<int>()));
        Assert.Null(NonEmptyEnumerable.TryCreate(Enumerable.Empty<int>()));
    }

    [Fact]
    public void TryCreate_NullSequence_ReturnsNull()
    {
        Assert.Null(NonEmptyEnumerable.TryCreate<int>(null));
    }

    [Fact]
    public void TryCreate_PopulatedSequence_ReturnsNonEmpty()
    {
        var list = NonEmptyEnumerable.TryCreate(new[] { 1, 2, 3 });
        Assert.NotNull(list);
        Assert.Equal(new[] { 1, 2, 3 }, list);
    }

    [Fact]
    public void Create_EmptySequence_Throws()
    {
        Assert.Throws<ArgumentException>(() => NonEmptyEnumerable.Create(Array.Empty<int>()));
        Assert.Throws<ArgumentException>(() => NonEmptyEnumerable.Create<int>(null));
    }

    [Fact]
    public void TryCreate_ReturnsSameInstance_ForExistingNonEmpty()
    {
        // Idempotent wrap: re-wrapping an already non-empty enumerable doesn't allocate.
        var original = NonEmptyEnumerable.Of(1, 2, 3);
        var wrapped = NonEmptyEnumerable.TryCreate((IEnumerable<int>)original);
        Assert.Same(original, wrapped);
    }

    // ── Read-only list surface ──────────────────────────────────────────

    [Property]
    public void Indexer_AgreesWithEnumeration(NonEmptyEnumerable<int> list)
    {
        var enumerated = list.ToArray();
        for (var i = 0; i < list.Count; i++)
            Assert.Equal(enumerated[i], list[i]);
    }

    [Property]
    public void HeadIsFirst(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(list[0], list.Head);
    }

    [Property]
    public void TailIsEverythingAfterHead(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(list.Skip(1), list.Tail);
        Assert.Equal(list.Count - 1, list.Tail.Count);
    }

    [Property]
    public void CountMatchesEnumeration(NonEmptyEnumerable<int> list)
    {
        Assert.Equal(list.ToArray().Length, list.Count);
        Assert.True(list.Count >= 1);
    }

    // ── Equality ────────────────────────────────────────────────────────

    [Property]
    public void Equality_SameContent(NonEmptyEnumerable<int> list)
    {
        var copy = NonEmptyEnumerable.Create(list.AsEnumerable());
        Assert.Equal(list, copy);
        Assert.Equal(list.GetHashCode(), copy.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentContent()
    {
        Assert.NotEqual(NonEmptyEnumerable.Of(1, 2), NonEmptyEnumerable.Of(1, 3));
        Assert.NotEqual(NonEmptyEnumerable.Of(1, 2), NonEmptyEnumerable.Of(1, 2, 3));
    }

    // ── Input isolation ─────────────────────────────────────────────────

    [Fact]
    public void Create_CopiesInputArray_SoMutationDoesNotLeak()
    {
        // If the factory kept the input buffer, later edits would mutate the list —
        // the whole point of a read-only list is that callers can't do that.
        var source = new[] { 1, 2, 3 };
        var list = NonEmptyEnumerable.Create(source);
        source[0] = 999;
        Assert.Equal(1, list.Head);
    }

    // ── ICollection<T> surface ──────────────────────────────────────────

    [Fact]
    public void ICollection_IsReadOnly()
    {
        var list = NonEmptyEnumerable.Of(1, 2, 3);
        Assert.True(((ICollection<int>)list).IsReadOnly);
    }

    [Fact]
    public void ICollection_Mutators_Throw()
    {
        var list = (ICollection<int>)NonEmptyEnumerable.Of(1, 2, 3);
        Assert.Throws<NotSupportedException>(() => list.Add(4));
        Assert.Throws<NotSupportedException>(() => list.Clear());
        Assert.Throws<NotSupportedException>(() => list.Remove(1));
    }

    [Fact]
    public void ICollection_Contains()
    {
        var list = NonEmptyEnumerable.Of(1, 2, 3);
        Assert.Contains(2, list);
        Assert.DoesNotContain(99, list);
    }

    [Fact]
    public void ICollection_CopyTo_WritesAtOffset()
    {
        var list = NonEmptyEnumerable.Of(1, 2, 3);
        var target = new int[5];
        list.CopyTo(target, 1);
        Assert.Equal(new[] { 0, 1, 2, 3, 0 }, target);
    }
}
