using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class FlattenTests
{
    [Property]
    public void Flatten_ConcatsInOrder(int[][] nested)
    {
        var result = nested.Flatten().ToArray();
        var expected = nested.SelectMany(x => x).ToArray();
        Assert.Equal(expected, result);
    }

    [Property]
    public void Flatten_PreservesTotalCount(int[][] nested)
    {
        Assert.Equal(nested.Sum(x => x.Length), nested.Flatten().Count());
    }

    [Fact]
    public void Flatten_EmptyOuter_ReturnsEmpty()
    {
        Assert.Empty(System.Array.Empty<IEnumerable<int>>().Flatten());
    }

    [Fact]
    public void Flatten_AllInnersEmpty_ReturnsEmpty()
    {
        IEnumerable<IEnumerable<int>> nested = new[]
        {
            Enumerable.Empty<int>(),
            Enumerable.Empty<int>(),
            Enumerable.Empty<int>(),
        };
        Assert.Empty(nested.Flatten());
    }
}
