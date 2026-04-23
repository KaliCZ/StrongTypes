using System;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static class ResultFlattenExtensions
{
    /// <summary>Collapses a nested <see cref="Result{T, TError}"/> when both levels share the same error type.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <typeparam name="TError">The shared error type.</typeparam>
    /// <param name="nested">The nested result.</param>
    [Pure]
    public static Result<T, TError> Flatten<T, TError>(this Result<Result<T, TError>, TError> nested)
        where T : notnull
        where TError : notnull
        => nested.IsSuccess ? nested.InternalValue : nested.InternalError;

    /// <summary>Collapses a <see cref="Result{T}"/> of <see cref="Result{T}"/>, preserving the single-parameter form.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="nested">The nested result.</param>
    [Pure]
    public static Result<T> Flatten<T>(this Result<Result<T>, Exception> nested)
        where T : notnull
        => nested.IsSuccess ? nested.InternalValue : nested.InternalError;

    /// <summary>Collapses a nested <see cref="Result{T, TError}"/> whose inner and outer error types are different <see cref="Exception"/> subtypes. Both errors upcast to <see cref="Exception"/>.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <typeparam name="TInnerException">The inner exception type.</typeparam>
    /// <typeparam name="TOuterException">The outer exception type.</typeparam>
    /// <param name="nested">The nested result.</param>
    [Pure]
    public static Result<T> Flatten<T, TInnerException, TOuterException>(
        this Result<Result<T, TInnerException>, TOuterException> nested)
        where T : notnull
        where TInnerException : Exception
        where TOuterException : Exception
    {
        if (nested.IsError) return nested.InternalError;
        var inner = nested.InternalValue;
        return inner.IsSuccess ? (Result<T>)inner.InternalValue : (Exception)inner.InternalError;
    }
}
