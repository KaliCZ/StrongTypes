using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

public class ExceptTests
{
    [Property]
    public void Except_MatchesLinqExcept(int[] source, int[] excluded)
    {
        Assert.Equal(source.Except((System.Collections.Generic.IEnumerable<int>)excluded), source.Except(excluded));
    }

    [Property]
    public void Except_ResultContainsNoExcludedItem(int[] source, int[] excluded)
    {
        Assert.DoesNotContain(source.Except(excluded), i => excluded.Contains(i));
    }

    [Property]
    public void Except_Idempotent(int[] source, int[] excluded)
    {
        var once = source.Except(excluded).ToArray();
        var twice = once.Except(excluded).ToArray();
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Except_RemovesMatchingItemsIncludingDuplicates()
    {
        var source = new[] { "1 potato", "2 potatoes", "1 potato", "3 potatoes" };
        var result = source.Except("1 potato").ToArray();
        Assert.Equal(new[] { "2 potatoes", "3 potatoes" }, result);
    }

    [Fact]
    public void Except_DedupsSource_SetSemantics()
    {
        var source = new[] { 1, 2, 2, 3, 3, 3 };
        var result = source.Except(4).ToArray();
        Assert.Equal(new[] { 1, 2, 3 }, result);
    }

    [Fact]
    public void Except_NoExcludedItems_StillDedups()
    {
        var source = new[] { 1, 1, 2 };
        Assert.Equal(new[] { 1, 2 }, source.Except().ToArray());
    }

    [Fact]
    public void Except_EmptySource_ReturnsEmpty()
    {
        Assert.Empty(System.Array.Empty<int>().Except(1, 2, 3));
    }
}
