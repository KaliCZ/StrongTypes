#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace StrongTypes;

public static class ResultThrowIfErrorExtensions
{
    /// <summary>Returns the success value, or rethrows the captured exception preserving its original stack trace.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error type, constrained to <see cref="Exception"/>.</typeparam>
    /// <param name="r">The result to unwrap.</param>
    public static T ThrowIfError<T, TError>(this Result<T, TError> r)
        where T : notnull
        where TError : Exception
    {
        if (r.IsSuccess) return r.InternalValue;
        ExceptionDispatchInfo.Capture(r.InternalError).Throw();
        throw new UnreachableException();
    }

    /// <summary>Returns the success value, or throws the exception produced by <paramref name="toException"/>.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <typeparam name="TError">The error value type.</typeparam>
    /// <param name="r">The result to unwrap.</param>
    /// <param name="toException">Maps the error to an <see cref="Exception"/>.</param>
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

    /// <summary>Returns the success value, or throws. A single captured exception is rethrown (stack preserved); multiple exceptions are wrapped in an <see cref="AggregateException"/>.</summary>
    /// <typeparam name="T">The success value type.</typeparam>
    /// <param name="r">The result to unwrap.</param>
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
