using System;
using System.Diagnostics.Contracts;

namespace StrongTypes;

public static class ResultFromNullableExtensions
{
    // ── Reference type receivers ───────────────────────────────────────

    /// <summary>Lifts a nullable reference into a <see cref="Result{T}"/>. A null value becomes an <see cref="ArgumentNullException"/> with no <c>ParamName</c>.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    [Pure]
    public static Result<T> ToResult<T>(this T? value)
        where T : class
        => value is { } v ? v : new ArgumentNullException();

    /// <summary>Lifts a nullable reference into a <see cref="Result{T}"/>. A null value becomes <paramref name="error"/>.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="error">The exception to emit when <paramref name="value"/> is null.</param>
    [Pure]
    public static Result<T> ToResult<T>(this T? value, Exception error)
        where T : class
        => value is { } v ? v : error;

    /// <summary>Lifts a nullable reference into a <see cref="Result{T}"/>. A null value invokes <paramref name="error"/>.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="error">Produces the exception when <paramref name="value"/> is null.</param>
    [Pure]
    public static Result<T> ToResult<T>(this T? value, Func<Exception> error)
        where T : class
        => value is { } v ? v : error();

    /// <summary>Lifts a nullable reference into a <see cref="Result{T, TError}"/> with an eager custom error.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="error">The error to emit when <paramref name="value"/> is null.</param>
    /// <remarks>A lambda returning a specific <see cref="Exception"/> subtype binds here rather than to <see cref="ToResult{T}(T?, Func{Exception})"/>. Cast to <see cref="Exception"/> to force the single-parameter form.</remarks>
    [Pure]
    public static Result<T, TError> ToResult<T, TError>(this T? value, TError error)
        where T : class
        where TError : notnull
        => value is { } v ? v : error;

    /// <summary>Lifts a nullable reference into a <see cref="Result{T, TError}"/> with a lazy custom error.</summary>
    /// <typeparam name="T">The reference type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="value">The nullable input.</param>
    /// <param name="error">Produces the error when <paramref name="value"/> is null.</param>
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
