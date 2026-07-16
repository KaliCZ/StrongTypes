using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace StrongTypes;

public static class ResultPartitionExtensions
{
    /// <summary>Splits a sequence of <see cref="Result{T, TError}"/> into successes and errors. Relative order is preserved within each partition.</summary>
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

    /// <summary>Partitions the sequence, then invokes <paramref name="successes"/> on the successes and <paramref name="errors"/> on the errors. Both callbacks are invoked even when their partition is empty.</summary>
    public static void PartitionMatch<T, TError>(
        this IEnumerable<Result<T, TError>> source,
        Action<IReadOnlyList<T>> successes,
        Action<IReadOnlyList<TError>> errors)
        where T : notnull
        where TError : notnull
    {
        var (successList, errorList) = source.Partition();
        successes(successList);
        errors(errorList);
    }

    /// <summary>Partitions the sequence, projects each partition through the matching callback, and returns the concatenated results in successes-then-errors order.</summary>
    [Pure]
    public static R[] PartitionMatch<T, TError, R>(
        this IEnumerable<Result<T, TError>> source,
        Func<IReadOnlyList<T>, IEnumerable<R>> successes,
        Func<IReadOnlyList<TError>, IEnumerable<R>> errors)
        where T : notnull
        where TError : notnull
    {
        var (successList, errorList) = source.Partition();
        return successes(successList).Concat(errors(errorList)).ToArray();
    }
}
