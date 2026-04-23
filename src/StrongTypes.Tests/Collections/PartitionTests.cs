using System;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class PartitionTests
{
    [Property]
    public void Partition_PassingMatchesPredicate(int[] source)
    {
        var (passing, _) = source.Partition(i => i % 2 == 0);
        Assert.All(passing, i => Assert.True(i % 2 == 0));
    }

    [Property]
    public void Partition_ViolatingFailsPredicate(int[] source)
    {
        var (_, violating) = source.Partition(i => i % 2 == 0);
        Assert.All(violating, i => Assert.False(i % 2 == 0));
    }

    [Property]
    public void Partition_TotalCountMatchesSource(int[] source)
    {
        var (passing, violating) = source.Partition(i => i > 0);
        Assert.Equal(source.Length, passing.Count + violating.Count);
    }

    [Property]
    public void Partition_PreservesRelativeOrder(int[] source)
    {
        var (passing, violating) = source.Partition(i => i > 0);
        Assert.Equal(source.Where(i => i > 0), passing);
        Assert.Equal(source.Where(i => !(i > 0)), violating);
    }

    [Fact]
    public void Partition_Empty_ReturnsTwoEmptyLists()
    {
        var (passing, violating) = Array.Empty<int>().Partition(_ => true);
        Assert.Empty(passing);
        Assert.Empty(violating);
    }

    [Fact]
    public void Partition_AllPassing_ViolatingEmpty()
    {
        var (passing, violating) = new[] { 1, 2, 3 }.Partition(_ => true);
        Assert.Equal(new[] { 1, 2, 3 }, passing);
        Assert.Empty(violating);
    }

    [Fact]
    public void Partition_AllViolating_PassingEmpty()
    {
        var (passing, violating) = new[] { 1, 2, 3 }.Partition(_ => false);
        Assert.Empty(passing);
        Assert.Equal(new[] { 1, 2, 3 }, violating);
    }

    [Fact]
    public void Partition_LazyEnumerable_IsMaterializedOnce()
    {
        var enumerations = 0;
        var source = Enumerate();

        var (passing, violating) = source.Partition(i => i > 0);

        Assert.Equal(1, enumerations);
        Assert.Equal(new[] { 1, 2 }, passing);
        Assert.Equal(new[] { -1 }, violating);

        System.Collections.Generic.IEnumerable<int> Enumerate()
        {
            enumerations++;
            yield return 1;
            yield return -1;
            yield return 2;
        }
    }
}
