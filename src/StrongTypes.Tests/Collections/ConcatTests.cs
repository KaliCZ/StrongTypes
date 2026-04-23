using System.Collections.Generic;
using System.Linq;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class ConcatTests
{
    [Property]
    public void Concat_ParamsItems_AppendsInOrder(int[] first, int[] items)
    {
        var result = first.Concat(items).ToArray();
        Assert.Equal(first.Concat((IEnumerable<int>)items), result);
    }

    [Property]
    public void Concat_ParamsItems_EmptyItems_ReturnsFirst(int[] first)
    {
        var result = first.Concat(System.Array.Empty<int>()).ToArray();
        Assert.Equal(first, result);
    }

    [Fact]
    public void Concat_ParamsItems_EmptyFirst_ReturnsItems()
    {
        var result = Enumerable.Empty<string>().Concat("a", "b", "c").ToArray();
        Assert.Equal(new[] { "a", "b", "c" }, result);
    }

    [Property]
    public void Concat_ParamsEnumerables_FlattensInOrder(int[] first, int[] a, int[] b, int[] c)
    {
        var result = first.Concat(a, b, c).ToArray();
        var expected = first.Concat(a).Concat(b).Concat(c).ToArray();
        Assert.Equal(expected, result);
    }

    [Property]
    public void Concat_ParamsEnumerables_NoOthers_ReturnsFirst(int[] first)
    {
        var result = first.Concat(System.Array.Empty<IEnumerable<int>>()).ToArray();
        Assert.Equal(first, result);
    }

    [Fact]
    public void Concat_ParamsEnumerables_AllEmpty_ReturnsEmpty()
    {
        var result = Enumerable.Empty<string>()
            .Concat(Enumerable.Empty<string>(), Enumerable.Empty<string>())
            .ToArray();
        Assert.Empty(result);
    }
}
