#nullable enable

using System;
using System.Threading.Tasks;

namespace StrongTypes;

public static partial class Result
{
    /// <summary>
    /// Runs <paramref name="f"/> and returns its value as a success; any thrown
    /// <see cref="Exception"/> (including <see cref="OperationCanceledException"/>)
    /// is captured as an error. Use <see cref="Catch{T, TException}(Func{T})"/>
    /// when you need to restrict which exceptions are captured.
    /// </summary>
    public static Result<T> Catch<T>(Func<T> f) where T : notnull
    {
        try { return f(); }
        catch (Exception e) { return e; }
    }

    /// <summary>
    /// Runs <paramref name="f"/> and returns its value as a success; only
    /// exceptions assignable to <typeparamref name="TException"/> are captured
    /// as errors, others propagate. This is the opt-in for cancellation-aware
    /// pipelines: pick a non-cancellation exception type and
    /// <see cref="OperationCanceledException"/> will flow past.
    /// </summary>
    public static Result<T> Catch<T, TException>(Func<T> f)
        where T : notnull
        where TException : Exception
    {
        try { return f(); }
        catch (TException e) { return e; }
    }

    /// <summary>
    /// Awaits <paramref name="f"/> and returns its value as a success; any
    /// thrown exception (including <see cref="OperationCanceledException"/>) is
    /// captured as an error.
    /// </summary>
    public static async Task<Result<T>> CatchAsync<T>(Func<Task<T>> f) where T : notnull
    {
        try { return await f(); }
        catch (Exception e) { return e; }
    }

    /// <summary>
    /// Awaits <paramref name="f"/> and returns its value as a success; only
    /// exceptions assignable to <typeparamref name="TException"/> are captured
    /// as errors, others propagate.
    /// </summary>
    public static async Task<Result<T>> CatchAsync<T, TException>(Func<Task<T>> f)
        where T : notnull
        where TException : Exception
    {
        try { return await f(); }
        catch (TException e) { return e; }
    }
}
