using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrongTypes.Tests.Exceptions;

public class ExceptionEnumerableExtensionsTests
{
    private sealed class CustomException(string message) : Exception(message);

    [Fact]
    public void Aggregate_Empty()
    {
        IEnumerable<Exception> enumerable = Enumerable.Empty<Exception>();
        Exception[] array = [];

        Assert.Null(enumerable.Aggregate());
        Assert.Null(array.Aggregate());
    }

    [Fact]
    public void Aggregate_Single()
    {
        var singleException = new CustomException("A potato here.");
        IEnumerable<Exception> enumerable = Enumerable.Repeat<Exception>(singleException, 1);
        CustomException[] array = [singleException];
        var nonEmpty = array.AsNonEmpty()!;

        Assert.Same(singleException, enumerable.Aggregate());
        Assert.Same(singleException, array.Aggregate());
        Assert.IsType<CustomException>(nonEmpty.Aggregate());
    }

    [Fact]
    public void Aggregate_Multiple()
    {
        CustomException[] array = Enumerable.Range(0, 10).Select(i => new CustomException($"{i} potatoes")).ToArray();
        IEnumerable<Exception> enumerable = array;
        INonEmptyEnumerable<Exception> nonEmpty = array.AsNonEmpty()!;

        Assert.Equal(array, ((AggregateException)enumerable.Aggregate()!).InnerExceptions);
        Assert.Equal(array, ((AggregateException)array.Aggregate()!).InnerExceptions);
        Assert.Equal(array, ((AggregateException)nonEmpty.Aggregate()).InnerExceptions);
    }
}
