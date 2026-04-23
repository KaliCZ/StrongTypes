using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace StrongTypes;

public static class ResultThrowIfErrorExtensions
{
    /// <summary>
    /// Returns the success value, or rethrows the captured exception preserving
    /// its original stack trace via <see cref="ExceptionDispatchInfo"/>. Applies
    /// whenever <typeparamref name="TError"/> is already an <see cref="Exception"/>
    /// — including the default <see cref="Result{T}"/> form and results whose error
    /// type is a concrete exception subclass (e.g. <c>Result&lt;T, InvalidOperationException&gt;</c>).
    /// </summary>
    public static T ThrowIfError<T, TError>(this Result<T, TError> r)
        where T : notnull
        where TError : Exception
    {
        if (r.IsSuccess) return r.InternalValue;
        ExceptionDispatchInfo.Capture(r.InternalError).Throw();
        throw new UnreachableException();
    }

    /// <summary>
    /// Returns the success value, or throws the exception produced by
    /// <paramref name="toException"/>. Use when <typeparamref name="TError"/>
    /// is not an <see cref="Exception"/>.
    /// </summary>
    public static T ThrowIfError<T, TError>(
        this Result<T, TError> r,
        Func<TError, Exception> toException)
        where T : notnull
        where TError : notnull
    {
        if (r.IsSuccess) return r.InternalValue;
        ExceptionDispatchInfo.Capture(toException(r.InternalError)).Throw();
        throw new UnreachableException();
    }

    /// <summary>
    /// Returns the success value, or throws. If the error list contains a single
    /// exception it is rethrown directly (stack preserved); otherwise the list is
    /// wrapped in an <see cref="AggregateException"/>.
    /// </summary>
    public static T ThrowIfError<T>(this Result<T, IReadOnlyList<Exception>> r)
        where T : notnull
    {
        if (r.IsSuccess) return r.InternalValue;
        var errors = r.InternalError;
        if (errors.Count == 1)
        {
            ExceptionDispatchInfo.Capture(errors[0]).Throw();
        }
        throw new AggregateException(errors);
    }
}
