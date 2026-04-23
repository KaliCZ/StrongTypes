#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static partial class Result
{
    /// <summary>Runs <paramref name="f"/>, returning its value as a success or any thrown exception as an error.</summary>
    /// <param name="f">The operation to invoke.</param>
    /// <param name="propagateCancellation">When <c>true</c> (default), <see cref="OperationCanceledException"/> is rethrown rather than captured.</param>
    public static Result<T> Catch<T>(Func<T> f, bool propagateCancellation = true) where T : notnull
    {
        try { return f(); }
        catch (OperationCanceledException) when (propagateCancellation) { throw; }
        catch (Exception e) { return e; }
    }

    /// <summary>Runs <paramref name="f"/>; only exceptions assignable to <typeparamref name="TException"/> are captured, others propagate.</summary>
    /// <param name="f">The operation to invoke.</param>
    /// <param name="propagateCancellation">When <c>true</c> (default), <see cref="OperationCanceledException"/> is rethrown even if it would otherwise match <typeparamref name="TException"/>.</param>
    public static Result<T, TException> Catch<T, TException>(Func<T> f, bool propagateCancellation = true)
        where T : notnull
        where TException : Exception
    {
        try { return f(); }
        catch (OperationCanceledException) when (propagateCancellation) { throw; }
        catch (TException e) { return e; }
    }

    /// <summary>Awaits <paramref name="f"/>, returning its value as a success or any thrown exception as an error.</summary>
    /// <param name="f">The async operation to await.</param>
    /// <param name="propagateCancellation">When <c>true</c> (default), <see cref="OperationCanceledException"/> is rethrown rather than captured.</param>
    public static async Task<Result<T>> CatchAsync<T>(Func<Task<T>> f, bool propagateCancellation = true) where T : notnull
    {
        try { return await f(); }
        catch (OperationCanceledException) when (propagateCancellation) { throw; }
        catch (Exception e) { return e; }
    }

    /// <summary>Awaits <paramref name="f"/>; only exceptions assignable to <typeparamref name="TException"/> are captured, others propagate.</summary>
    /// <param name="f">The async operation to await.</param>
    /// <param name="propagateCancellation">When <c>true</c> (default), <see cref="OperationCanceledException"/> is rethrown even if it would otherwise match <typeparamref name="TException"/>.</param>
    public static async Task<Result<T, TException>> CatchAsync<T, TException>(Func<Task<T>> f, bool propagateCancellation = true)
        where T : notnull
        where TException : Exception
    {
        try { return await f(); }
        catch (OperationCanceledException) when (propagateCancellation) { throw; }
        catch (TException e) { return e; }
    }
}
