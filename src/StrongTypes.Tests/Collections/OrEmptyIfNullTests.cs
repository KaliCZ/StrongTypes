using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class OrEmptyIfNullTests
{
    [Fact]
    public void Enumerable_Null_ReturnsEmpty()
    {
        IEnumerable<int>? source = null;
        Assert.Empty(source.OrEmptyIfNull());
    }

    [Property]
    public void Enumerable_NonNull_ReturnsSameSequence(int[] items)
    {
        IEnumerable<int>? source = items;
        Assert.Equal(items, source.OrEmptyIfNull());
    }

    [Fact]
    public void Array_Null_ReturnsEmpty()
    {
        int[]? source = null;
        var result = source.OrEmptyIfNull();
        Assert.Empty(result);
        Assert.Same(System.Array.Empty<int>(), result);
    }

    [Property]
    public void Array_NonNull_ReturnsSameReference(int[] items)
    {
        int[]? source = items;
        Assert.Same(items, source.OrEmptyIfNull());
    }

    [Fact]
    public void List_Null_ReturnsEmpty()
    {
        List<int>? source = null;
        Assert.Empty(source.OrEmptyIfNull());
    }

    [Property]
    public void List_NonNull_ReturnsSameReference(int[] items)
    {
        var list = items.ToList();
        List<int>? source = list;
        Assert.Same(list, source.OrEmptyIfNull());
    }

    [Fact]
    public void ReadOnlyList_Null_ReturnsEmpty()
    {
        IReadOnlyList<int>? source = null;
        Assert.Empty(source.OrEmptyIfNull());
    }

    [Property]
    public void ReadOnlyList_NonNull_ReturnsSameReference(int[] items)
    {
        IReadOnlyList<int>? source = items;
        Assert.Same(source, source.OrEmptyIfNull());
    }

    [Fact]
    public void Collection_Null_ReturnsEmpty()
    {
        ICollection<int>? source = null;
        Assert.Empty(source.OrEmptyIfNull());
    }

    [Property]
    public void Collection_NonNull_ReturnsSameReference(int[] items)
    {
        ICollection<int> list = items.ToList();
        ICollection<int>? source = list;
        Assert.Same(list, source.OrEmptyIfNull());
    }
}
