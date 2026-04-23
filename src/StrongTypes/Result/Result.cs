using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace StrongTypes;

/// <summary>A value that is either a success carrying <typeparamref name="T"/> or an error carrying <typeparamref name="TError"/>.</summary>
/// <typeparam name="T">The success type.</typeparam>
/// <typeparam name="TError">The error type.</typeparam>
/// <remarks>Construct through the implicit conversions (<c>return value;</c> / <c>return error;</c>) or the <see cref="Result"/> factory. Unwrap with the extension-property pattern: <c>if (result.Success is {} s)</c> or <c>if (result.Error is {} e)</c>.</remarks>
// Note: we can't also declare IEquatable<T> and IEquatable<TError> — the C#
// compiler rejects that combination (CS0695) because the two interfaces unify
// when T equals TError at a closed type. The Equals(T) / Equals(TError)
// methods below still provide the same call-site ergonomic without the
// interface implementations.
public class Result<T, TError> : IEquatable<Result<T, TError>>
    where T : notnull
    where TError : notnull
{
    internal readonly T InternalValue;
    internal readonly TError InternalError;

    [Pure]
    public bool IsSuccess { get; }
    [Pure]
    public bool IsError => !IsSuccess;

    internal Result(T value)
    {
        InternalValue = value;
        InternalError = default!;
        IsSuccess = true;
    }

    internal Result(TError error)
    {
        InternalValue = default!;
        InternalError = error;
        IsSuccess = false;
    }

    public static implicit operator Result<T, TError>(T value) => new(value);
    public static implicit operator Result<T, TError>(TError error) => new(error);

    #region Match

    [Pure]
    public R Match<R>(Func<T, R> success, Func<TError, R> error) =>
        IsSuccess ? success(InternalValue) : error(InternalError);

    public void Match(Action<T>? success = null, Action<TError>? error = null)
    {
        if (IsSuccess) success?.Invoke(InternalValue);
        else error?.Invoke(InternalError);
    }

    [Pure]
    public async Task<R> MatchAsync<R>(Func<T, Task<R>> success, Func<TError, Task<R>> error) =>
        IsSuccess ? await success(InternalValue) : await error(InternalError);

    public async Task MatchAsync(Func<T, Task>? success = null, Func<TError, Task>? error = null)
    {
        if (IsSuccess && success is not null) await success(InternalValue);
        else if (IsError && error is not null) await error(InternalError);
    }

    #endregion

    #region Map (success branch)

    [Pure]
    public Result<U, TError> Map<U>(Func<T, U> f) where U : notnull =>
        IsSuccess ? f(InternalValue) : InternalError;

    [Pure]
    public async Task<Result<U, TError>> MapAsync<U>(Func<T, Task<U>> f) where U : notnull =>
        IsSuccess ? await f(InternalValue) : InternalError;

    /// <summary>Transforms both branches in one call (bimap).</summary>
    /// <typeparam name="U">The mapped success type.</typeparam>
    /// <typeparam name="UError">The mapped error type.</typeparam>
    /// <param name="success">Applied when this is a success.</param>
    /// <param name="error">Applied when this is an error.</param>
    [Pure]
    public Result<U, UError> Map<U, UError>(Func<T, U> success, Func<TError, UError> error)
        where U : notnull
        where UError : notnull =>
        IsSuccess ? success(InternalValue) : error(InternalError);

    [Pure]
    public async Task<Result<U, UError>> MapAsync<U, UError>(Func<T, Task<U>> success, Func<TError, Task<UError>> error)
        where U : notnull
        where UError : notnull =>
        IsSuccess ? await success(InternalValue) : await error(InternalError);

    #endregion

    #region MapError (error branch)

    [Pure]
    public Result<T, UError> MapError<UError>(Func<TError, UError> f) where UError : notnull =>
        IsError ? f(InternalError) : InternalValue;

    [Pure]
    public async Task<Result<T, UError>> MapErrorAsync<UError>(Func<TError, Task<UError>> f) where UError : notnull =>
        IsError ? await f(InternalError) : InternalValue;

    #endregion

    #region FlatMap

    [Pure]
    public Result<U, TError> FlatMap<U>(Func<T, Result<U, TError>> f) where U : notnull =>
        IsSuccess ? f(InternalValue) : InternalError;

    [Pure]
    public async Task<Result<U, TError>> FlatMapAsync<U>(Func<T, Task<Result<U, TError>>> f) where U : notnull =>
        IsSuccess ? await f(InternalValue) : InternalError;

    #endregion

    #region Equality / ToString

    [Pure]
    public bool Equals(Result<T, TError>? other)
    {
        if (other is null) return false;
        if (IsSuccess != other.IsSuccess) return false;
        return IsSuccess
            ? EqualityComparer<T>.Default.Equals(InternalValue, other.InternalValue)
            : EqualityComparer<TError>.Default.Equals(InternalError, other.InternalError);
    }

    /// <summary>Returns <c>true</c> when this is a success whose value equals <paramref name="other"/>.</summary>
    /// <param name="other">The value to compare against.</param>
    [Pure]
    public bool Equals(T? other) =>
        IsSuccess && other is not null && EqualityComparer<T>.Default.Equals(InternalValue, other);

    /// <summary>Returns <c>true</c> when this is an error whose value equals <paramref name="other"/>.</summary>
    /// <param name="other">The error to compare against.</param>
    [Pure]
    public bool Equals(TError? other) =>
        IsError && other is not null && EqualityComparer<TError>.Default.Equals(InternalError, other);

    [Pure]
    public override bool Equals(object? obj) => obj is Result<T, TError> r && Equals(r);

    [Pure]
    public override int GetHashCode() => HashCode.Combine(IsSuccess, InternalValue, InternalError);

    public static bool operator ==(Result<T, TError>? left, Result<T, TError>? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(Result<T, TError>? left, Result<T, TError>? right) => !(left == right);

    [Pure]
    public override string ToString() => IsSuccess
        ? $"Success({InternalValue})"
        : $"Error({InternalError})";

    #endregion
}

/// <summary>Shorthand for <c>Result&lt;T, Exception&gt;</c>; chained <c>Map</c>/<c>FlatMap</c> calls stay as <see cref="Result{T}"/> rather than decaying to the two-parameter form.</summary>
/// <typeparam name="T">The success type.</typeparam>
public sealed class Result<T> : Result<T, Exception>
    where T : notnull
{
    internal Result(T value) : base(value) { }
    internal Result(Exception error) : base(error) { }

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Exception error) => new(error);

    [Pure]
    public new Result<U> Map<U>(Func<T, U> f) where U : notnull =>
        IsSuccess ? f(InternalValue) : InternalError;

    [Pure]
    public new async Task<Result<U>> MapAsync<U>(Func<T, Task<U>> f) where U : notnull =>
        IsSuccess ? await f(InternalValue) : InternalError;

    // FlatMap's parameter matches the base signature so `new` can hide it; the
    // callback may return any `Result<U, Exception>` (including `Result<U>`
    // itself via inheritance). The inner value is re-wrapped as `Result<U>`
    // because the callback may hand back a base-class instance.
    [Pure]
    public new Result<U> FlatMap<U>(Func<T, Result<U, Exception>> f) where U : notnull
    {
        if (IsError) return InternalError;
        var inner = f(InternalValue);
        return inner.IsSuccess ? inner.InternalValue : inner.InternalError;
    }

    [Pure]
    public new async Task<Result<U>> FlatMapAsync<U>(Func<T, Task<Result<U, Exception>>> f) where U : notnull
    {
        if (IsError) return InternalError;
        var inner = await f(InternalValue);
        return inner.IsSuccess ? inner.InternalValue : inner.InternalError;
    }
}

/// <summary>Factory helpers for <see cref="Result{T}"/> and <see cref="Result{T, TError}"/>.</summary>
public static partial class Result
{
    /// <summary>Wraps <paramref name="value"/> as a successful <see cref="Result{T}"/>.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="value">The success payload.</param>
    [Pure]
    public static Result<T> Success<T>(T value) where T : notnull => new(value);

    /// <summary>Wraps <paramref name="error"/> as a failed <see cref="Result{T}"/>.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <param name="error">The captured exception.</param>
    [Pure]
    public static Result<T> Error<T>(Exception error) where T : notnull => new(error);

    /// <summary>Wraps <paramref name="value"/> as a successful <see cref="Result{T, TError}"/>.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="value">The success payload.</param>
    [Pure]
    public static Result<T, TError> Success<T, TError>(T value)
        where T : notnull where TError : notnull => new(value);

    /// <summary>Wraps <paramref name="error"/> as a failed <see cref="Result{T, TError}"/>.</summary>
    /// <typeparam name="T">The success type.</typeparam>
    /// <typeparam name="TError">The error type.</typeparam>
    /// <param name="error">The error payload.</param>
    [Pure]
    public static Result<T, TError> Error<T, TError>(TError error)
        where T : notnull where TError : notnull => new(error);
}
