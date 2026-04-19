using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrongTypes.Tests.Collections.INonEmptyEnumerable;

public class AsNonEmptyTests
{
    [Fact]
    public void AsNonEmpty_null()
    {
        IEnumerable<string> enumerableNull = null;
        string[] arrayNull = null;

        Assert.Null(enumerableNull.AsNonEmpty());
        Assert.Null(arrayNull.AsNonEmpty());
    }

    [Fact]
    public void AsNonEmpty_Empty()
    {
        IEnumerable<string> enumerableEmpty = Enumerable.Empty<string>();
        string[] arrayEmpty = new string[] { };

        Assert.Null(enumerableEmpty.AsNonEmpty());
        Assert.Null(arrayEmpty.AsNonEmpty());
    }

    [Fact]
    public void AsNonEmpty_Single()
    {
        IEnumerable<string> enumerableSingle = Enumerable.Repeat("A potato", 1);
        string[] arraySingle = new[] { "A potato" };
        var expected = NonEmptyEnumerable.Create("A potato");

        Assert.Equal(expected, enumerableSingle.AsNonEmpty());
        Assert.Equal(expected, arraySingle.AsNonEmpty());
    }

    [Fact]
    public void AsNonEmpty_Multiple()
    {
        IEnumerable<string> enumerableMultiple = Enumerable.Range(0, 4).Select(i => $"{i} potatoes");
        string[] arrayMultiple = Enumerable.Range(0, 4).Select(i => $"{i} potatoes").ToArray();
        var expected = NonEmptyEnumerable.Create("0 potatoes", "1 potatoes", "2 potatoes", "3 potatoes");

        Assert.Equal(expected, enumerableMultiple.AsNonEmpty());
        Assert.Equal(expected, arrayMultiple.AsNonEmpty());
    }
}
