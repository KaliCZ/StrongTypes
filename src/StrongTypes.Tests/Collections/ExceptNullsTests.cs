using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrongTypes.Tests;

public class ExceptNullsTests
{
    [Fact]
    public void ExceptNulls_NullableStruct()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var list = new List<Guid?> { null, guid1, null, guid2, null };

        Guid[] result = list.ExceptNulls().ToArray();

        Assert.Equal(new[] { guid1, guid2 }, result);
    }

    [Fact]
    public void ExceptNulls_NullableReference()
    {
        var list = new List<string?> { null, "1 potato", null, "2 potatoes", null };

        string[] result = list.ExceptNulls().ToArray();

        Assert.Equal(new[] { "1 potato", "2 potatoes" }, result);
    }

    [Fact]
    public void ExceptNulls_AllNull_ReturnsEmpty()
    {
        Assert.Empty(new int?[] { null, null, null }.ExceptNulls());
        Assert.Empty(new string?[] { null, null, null }.ExceptNulls());
    }

    [Fact]
    public void ExceptNulls_NoNulls_ReturnsAll()
    {
        Assert.Equal(new[] { 1, 2, 3 }, new int?[] { 1, 2, 3 }.ExceptNulls());
        Assert.Equal(new[] { "a", "b" }, new string?[] { "a", "b" }.ExceptNulls());
    }

    [Fact]
    public void ExceptNulls_ReferenceResult_IsStronglyTyped()
    {
        IEnumerable<string?> source = new[] { "x", null, "y" };
        IEnumerable<string> result = source.ExceptNulls();
        Assert.Equal(new[] { "x", "y" }, result);
    }
}
