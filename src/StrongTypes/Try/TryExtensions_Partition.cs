#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace StrongTypes;

public static partial class TryExtensions
{
    /// <summary>
    /// Splits a sequence of <see cref="Try{TSuccess, TError}"/> into successes and
    /// errors. Relative order is preserved within each partition.
    /// </summary>
    public static (IReadOnlyList<TSuccess> Successes, IReadOnlyList<TError> Errors) Partition<TSuccess, TError>(
        this IEnumerable<Try<TSuccess, TError>> source)
        where TSuccess : notnull
        where TError : notnull
    {
        var capacity = source is ICollection<Try<TSuccess, TError>> c ? c.Count : 0;
        var successes = new List<TSuccess>(capacity);
        var errors = new List<TError>(capacity);

        foreach (var value in source)
        {
            if (value.IsSuccess) successes.Add(value.Success.InternalValue);
            else errors.Add(value.Error.InternalValue);
        }

        return (successes, errors);
    }

    /// <summary>
    /// Partitions the sequence into successes and errors, then invokes
    /// <paramref name="success"/> on the successes and <paramref name="error"/>
    /// on the errors. Both callbacks are invoked unconditionally, even when the
    /// corresponding partition is empty.
    /// </summary>
    public static void PartitionMatch<TSuccess, TError>(
        this IEnumerable<Try<TSuccess, TError>> source,
        Action<IReadOnlyList<TSuccess>> success,
        Action<IReadOnlyList<TError>> error)
        where TSuccess : notnull
        where TError : notnull
    {
        var (successes, errors) = source.Partition();
        success(successes);
        error(errors);
    }

    /// <summary>
    /// Partitions the sequence into successes and errors, projects each partition
    /// through the matching callback, and returns the concatenated results in
    /// successes-then-errors order.
    /// </summary>
    public static IReadOnlyList<TResult> PartitionMatch<TSuccess, TError, TResult>(
        this IEnumerable<Try<TSuccess, TError>> source,
        Func<IReadOnlyList<TSuccess>, IEnumerable<TResult>> success,
        Func<IReadOnlyList<TError>, IEnumerable<TResult>> error)
        where TSuccess : notnull
        where TError : notnull
    {
        var (successes, errors) = source.Partition();
        return success(successes).Concat(error(errors)).ToArray();
    }
}
