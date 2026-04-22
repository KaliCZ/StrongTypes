#nullable enable

using System;
using System.Runtime.CompilerServices;

namespace StrongTypes;

public static class ResultFromNullableExtensions
{
    // ── Reference type receivers ───────────────────────────────────────

    /// <summary>
    /// Lifts a nullable reference into a <see cref="Result{T}"/>. A non-null value
    /// becomes a success; a null value becomes an <see cref="ArgumentNullException"/>
    /// whose parameter name is captured from the caller's expression.
    /// </summary>
    public static Result<T> ToResult<T>(
        this T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
        => value is not null
            ? Result.Success(value)
            : new ArgumentNullException(paramName);

    /// <summary>
    /// Lifts a nullable reference into a <see cref="Result{T}"/>. A non-null value
    /// becomes a success; a null value becomes the exception produced by
    /// <paramref name="error"/>.
    /// <para>
    /// Callers whose factory returns a specific <see cref="Exception"/> subtype
    /// (e.g. <c>() =&gt; new InvalidOperationException(...)</c>) will instead bind
    /// to the two-parameter <see cref="ToResult{T, TError}(T?, Func{TError})"/>
    /// overload and get back <c>Result&lt;T, InvalidOperationException&gt;</c>.
    /// To force the single-parameter form, cast the lambda result to
    /// <see cref="Exception"/>: <c>() =&gt; (Exception)new InvalidOperationException(...)</c>.
    /// </para>
    /// </summary>
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error)
        where T : class
        => value is not null ? Result.Success(value) : error();

    /// <summary>
    /// Lifts a nullable reference into a <see cref="Result{T, TError}"/>. A non-null
    /// value becomes a success; a null value becomes the error produced by
    /// <paramref name="error"/>.
    /// </summary>
    public static Result<T, TError> ToResult<T, TError>(this T? value, Func<TError> error)
        where T : class
        where TError : notnull
        => value is not null ? Result.Success<T, TError>(value) : error();

    // ── Value type receivers ───────────────────────────────────────────

    /// <inheritdoc cref="ToResult{T}(T?, string?)"/>
    public static Result<T> ToResult<T>(
        this T? value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct
        => value.HasValue
            ? Result.Success(value.Value)
            : new ArgumentNullException(paramName);

    /// <inheritdoc cref="ToResult{T}(T?, Func{Exception})"/>
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error)
        where T : struct
        => value.HasValue ? Result.Success(value.Value) : error();

    /// <inheritdoc cref="ToResult{T, TError}(T?, Func{TError})"/>
    public static Result<T, TError> ToResult<T, TError>(this T? value, Func<TError> error)
        where T : struct
        where TError : notnull
        => value.HasValue ? Result.Success<T, TError>(value.Value) : error();
}
