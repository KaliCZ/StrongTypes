using System;
using System.Collections.Generic;

namespace StrongTypes;

public static partial class IEnumerableExtensions
{
    /// <summary>
    /// Splits a collection of tries into a collection of success results and a collection of errors.
    /// </summary>
    public static (IReadOnlyList<TSuccess>, IReadOnlyList<TError>) Partition<TSuccess, TError>(this IEnumerable<Try<TSuccess, TError>> values)
        where TSuccess : notnull
        where TError : notnull
    {
        var successes = new List<TSuccess>();
        var errors = new List<TError>();
        foreach (var value in values)
        {
            if (value.IsSuccess)
            {
                successes.Add(value.Success.InternalValue);
            }
            else
            {
                errors.Add(value.Error.InternalValue);
            }
        }
        return (successes, errors);
    }

    /// <summary>
    /// Splits a collection of tries into a collection of success results and a collection of errors and executes an action for those.
    /// </summary>
    public static void PartitionMatch<TSuccess, TError>(this IEnumerable<Try<TSuccess, TError>> values, Action<IReadOnlyList<TSuccess>> success, Action<IReadOnlyList<TError>> error)
        where TSuccess : notnull
        where TError : notnull
    {
        var successes = new List<TSuccess>();
        var errors = new List<TError>();
        foreach (var item in values)
        {
            if (item.IsSuccess)
            {
                successes.Add(item.Success.InternalValue);
            }
            else
            {
                errors.Add(item.Error.InternalValue);
            }
        }

        success(successes);
        error(errors);
    }

    public static IReadOnlyList<TResult> PartitionMatch<TSuccess, TError, TResult>(
        this IEnumerable<Try<TSuccess, TError>> values,
        Func<IReadOnlyList<TSuccess>, IEnumerable<TResult>> success,
        Func<IReadOnlyList<TError>, IEnumerable<TResult>> error)
        where TSuccess : notnull
        where TError : notnull
    {
        var successes = new List<TSuccess>();
        var errors = new List<TError>();
        foreach (var item in values)
        {
            if (item.IsSuccess)
            {
                successes.Add(item.Success.InternalValue);
            }
            else
            {
                errors.Add(item.Error.InternalValue);
            }
        }

        return ReadOnlyList.CreateFlat(success(successes), error(errors));
    }
}
