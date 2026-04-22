#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static partial class Result
{
    /// <summary>
    /// Runs <paramref name="f"/> and returns its value as a success; any exception
    /// other than <see cref="OperationCanceledException"/> (and its inheritors) is
    /// captured as an error. Cancellation exceptions propagate.
    /// </summary>
    public static Result<T> Catch<T>(Func<T> f) where T : notnull
    {
        try
        {
            return f();
        }
        catch (Exception e) when (!IsCancellation(e))
        {
            return e;
        }
    }

    /// <summary>
    /// Awaits <paramref name="f"/> and returns its value as a success; any exception
    /// other than <see cref="OperationCanceledException"/> (and its inheritors) is
    /// captured as an error. Cancellation exceptions propagate.
    /// </summary>
    public static async Task<Result<T>> CatchAsync<T>(Func<Task<T>> f) where T : notnull
    {
        try
        {
            return await f();
        }
        catch (Exception e) when (!IsCancellation(e))
        {
            return e;
        }
    }

    private static bool IsCancellation(Exception e)
    {
        for (var current = e; current is not null; current = current.InnerException)
        {
            if (current is OperationCanceledException) return true;
        }
        return false;
    }
}
