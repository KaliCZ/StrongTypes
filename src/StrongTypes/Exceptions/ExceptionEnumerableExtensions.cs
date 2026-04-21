#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

/// <summary>
/// Extensions that collapse a sequence of exceptions into a single one.
/// A single exception passes through unwrapped; multiple exceptions are wrapped
/// in an <see cref="AggregateException"/>.
/// </summary>
public static class ExceptionEnumerableExtensions
{
    /// <summary>
    /// Returns a single <see cref="Exception"/> aggregating <paramref name="source"/>,
    /// or <c>null</c> when the sequence is empty.
    /// </summary>
    public static Exception? Aggregate(this IEnumerable<Exception> source)
        => source switch
        {
            IReadOnlyList<Exception> list => Aggregate(list),
            ICollection<Exception> { Count: 0 } => null,
            ICollection<Exception> { Count: 1 } c => c.First(),
            ICollection<Exception> c => new AggregateException(c),
            _ => Aggregate((IReadOnlyList<Exception>)source.ToArray())
        };

    /// <summary>
    /// Returns a single <see cref="Exception"/> aggregating <paramref name="source"/>,
    /// or <c>null</c> when the list is empty.
    /// </summary>
    [Pure]
    public static Exception? Aggregate(this IReadOnlyList<Exception> source)
        => source.Count switch
        {
            0 => null,
            1 => source[0],
            _ => new AggregateException(source)
        };

    [Pure]
    public static Exception Aggregate(this INonEmptyEnumerable<Exception> source)
        => source.Count switch
        {
            1 => source[0],
            _ => new AggregateException(source)
        };
}
