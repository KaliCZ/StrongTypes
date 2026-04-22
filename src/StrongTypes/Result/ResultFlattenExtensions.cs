#nullable enable

using System;

namespace StrongTypes;

public static class ResultFlattenExtensions
{
    /// <summary>
    /// Collapses a nested Result into a single Result when both levels share
    /// the same error type. The outer error propagates when the outer result
    /// is an error; otherwise the inner Result is returned as-is.
    /// </summary>
    public static Result<T, TError> Flatten<T, TError>(this Result<Result<T, TError>, TError> nested)
        where T : notnull
        where TError : notnull
        => nested.IsSuccess ? nested.InternalValue : nested.InternalError;

    /// <summary>
    /// Collapses a <see cref="Result{T}"/> of <see cref="Result{T}"/> into a single
    /// <see cref="Result{T}"/>. Exists so the flattened value stays on the single-parameter
    /// form instead of decaying to <c>Result&lt;T, Exception&gt;</c>.
    /// </summary>
    public static Result<T> Flatten<T>(this Result<Result<T>, Exception> nested)
        where T : notnull
        => nested.IsSuccess ? nested.InternalValue : nested.InternalError;
}
