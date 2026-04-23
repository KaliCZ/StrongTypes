using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static class ResultPartitionExtensions
{
    /// <summary>Splits a sequence of <see cref="Result{T, TError}"/> into successes and errors. Relative order is preserved within each partition.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="source">The sequence of results.</param>
    [Pure]
    public static (IReadOnlyList<T> Successes, IReadOnlyList<TError> Errors) Partition<T, TError>(
        this IEnumerable<Result<T, TError>> source)
        where T : notnull
        where TError : notnull
    {
        var capacity = source is ICollection<Result<T, TError>> c ? c.Count : 0;
        var successes = new List<T>(capacity);
        var errors = new List<TError>(capacity);

        foreach (var r in source)
        {
            if (r.IsSuccess) successes.Add(r.InternalValue);
            else errors.Add(r.InternalError);
        }

        return (successes, errors);
    }

    /// <summary>Partitions the sequence, then invokes <paramref name="success"/> on the successes and <paramref name="error"/> on the errors. Both callbacks are invoked even when their partition is empty.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="source">The sequence of results.</param>
    /// <param name="success">Invoked with all successes.</param>
    /// <param name="error">Invoked with all errors.</param>
    public static void PartitionMatch<T, TError>(
        this IEnumerable<Result<T, TError>> source,
        Action<IReadOnlyList<T>> success,
        Action<IReadOnlyList<TError>> error)
        where T : notnull
        where TError : notnull
    {
        var (successes, errors) = source.Partition();
        success(successes);
        error(errors);
    }

    /// <summary>Partitions the sequence, projects each partition through the matching callback, and returns the concatenated results in successes-then-errors order.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <typeparam name="R">The projected element type.</typeparam>
    /// <param name="source">The sequence of results.</param>
    /// <param name="success">Projects the successes.</param>
    /// <param name="error">Projects the errors.</param>
    [Pure]
    public static R[] PartitionMatch<T, TError, R>(
        this IEnumerable<Result<T, TError>> source,
        Func<IReadOnlyList<T>, IEnumerable<R>> success,
        Func<IReadOnlyList<TError>, IEnumerable<R>> error)
        where T : notnull
        where TError : notnull
    {
        var (successes, errors) = source.Partition();
        return success(successes).Concat(error(errors)).ToArray();
    }
}
