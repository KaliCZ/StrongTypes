using System;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static class ResultFromNullableExtensions
{
    // ── Reference type receivers ───────────────────────────────────────

    /// <summary>
    /// Lifts a nullable reference into a <see cref="Result{T}"/>. A non-null
    /// value becomes a success; a null value becomes an
    /// <see cref="ArgumentNullException"/> with no <c>ParamName</c>. Use the
    /// eager or lazy overloads when you want a specific exception or a custom
    /// error value.
    /// </summary>
    [Pure]
    public static Result<T> ToResult<T>(this T? value)
        where T : class
        => value is { } v ? v : new ArgumentNullException();

    /// <summary>
    /// Eager exception overload. A non-null <paramref name="value"/> becomes a
    /// success; a null value becomes <paramref name="error"/>.
    /// </summary>
    [Pure]
    public static Result<T> ToResult<T>(this T? value, Exception error)
        where T : class
        => value is { } v ? v : error;

    /// <summary>
    /// Lazy exception overload — prefer when the exception is expensive to
    /// construct (e.g. deep stack capture).
    /// </summary>
    [Pure]
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error)
        where T : class
        => value is { } v ? v : error();

    /// <summary>
    /// Eager custom-error overload. Use for cheap error values (enums, captured
    /// singletons, small records) where lambda allocation is unwanted overhead.
    /// <para>
    /// A lambda returning a specific <see cref="Exception"/> subtype binds here
    /// rather than to <see cref="ToResult{T}(T?, Func{Exception})"/>. To force
    /// the single-parameter form, cast to <see cref="Exception"/>.
    /// </para>
    /// </summary>
    [Pure]
    public static Result<T, TError> ToResult<T, TError>(this T? value, TError error)
        where T : class
        where TError : notnull
        => value is { } v ? v : error;

    /// <summary>
    /// Lazy custom-error overload.
    /// </summary>
    [Pure]
    public static Result<T, TError> ToResult<T, TError>(this T? value, Func<TError> error)
        where T : class
        where TError : notnull
        => value is { } v ? v : error();

    // ── Value type receivers ───────────────────────────────────────────

    /// <inheritdoc cref="ToResult{T}(T?)"/>
    [Pure]
    public static Result<T> ToResult<T>(this T? value)
        where T : struct
        => value is { } v ? v : new ArgumentNullException();

    /// <inheritdoc cref="ToResult{T}(T?, Exception)"/>
    [Pure]
    public static Result<T> ToResult<T>(this T? value, Exception error)
        where T : struct
        => value is { } v ? v : error;

    /// <inheritdoc cref="ToResult{T}(T?, Func{Exception})"/>
    [Pure]
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error)
        where T : struct
        => value is { } v ? v : error();

    /// <inheritdoc cref="ToResult{T, TError}(T?, TError)"/>
    [Pure]
    public static Result<T, TError> ToResult<T, TError>(this T? value, TError error)
        where T : struct
        where TError : notnull
        => value is { } v ? v : error;

    /// <inheritdoc cref="ToResult{T, TError}(T?, Func{TError})"/>
    [Pure]
    public static Result<T, TError> ToResult<T, TError>(this T? value, Func<TError> error)
        where T : struct
        where TError : notnull
        => value is { } v ? v : error();
}
