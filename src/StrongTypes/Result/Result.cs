#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StrongTypes;

/// <summary>
/// A value that is either a success carrying a <typeparamref name="T"/> or an error
/// carrying a <typeparamref name="TError"/>. Construct through the implicit conversions
/// (<c>return value;</c> / <c>return error;</c>) or the <see cref="Result"/> factory.
/// <para>
/// The typical branch check is <c>if (result.Success is {} s)</c> / <c>if (result.Error is {} e)</c>,
/// which unwraps to the underlying value in a single expression. <c>Success</c> and
/// <c>Error</c> are surfaced via extension members so the returned nullable type is
/// <see cref="Nullable{T}"/> for value types and <c>T?</c> for reference types.
/// </para>
/// </summary>
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

    public bool IsSuccess { get; }
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

    public R Match<R>(Func<T, R> success, Func<TError, R> error) =>
        IsSuccess ? success(InternalValue) : error(InternalError);

    public void Match(Action<T>? success = null, Action<TError>? error = null)
    {
        if (IsSuccess) success?.Invoke(InternalValue);
        else error?.Invoke(InternalError);
    }

    public async Task<R> MatchAsync<R>(Func<T, Task<R>> success, Func<TError, Task<R>> error) =>
        IsSuccess ? await success(InternalValue) : await error(InternalError);

    public async Task MatchAsync(Func<T, Task>? success = null, Func<TError, Task>? error = null)
    {
        if (IsSuccess && success is not null) await success(InternalValue);
        else if (IsError && error is not null) await error(InternalError);
    }

    #endregion

    #region Map (success branch)

    public Result<U, TError> Map<U>(Func<T, U> f) where U : notnull =>
        IsSuccess ? f(InternalValue) : InternalError;

    public async Task<Result<U, TError>> MapAsync<U>(Func<T, Task<U>> f) where U : notnull =>
        IsSuccess ? await f(InternalValue) : InternalError;

    /// <summary>
    /// Bimap: transforms both branches in one call. Equivalent to
    /// <c>r.Map(success).MapError(error)</c> but traverses the value once.
    /// </summary>
    public Result<U, UError> Map<U, UError>(Func<T, U> success, Func<TError, UError> error)
        where U : notnull
        where UError : notnull =>
        IsSuccess ? success(InternalValue) : error(InternalError);

    #endregion

    #region MapError (error branch)

    public Result<T, UError> MapError<UError>(Func<TError, UError> f) where UError : notnull =>
        IsError ? f(InternalError) : InternalValue;

    public async Task<Result<T, UError>> MapErrorAsync<UError>(Func<TError, Task<UError>> f) where UError : notnull =>
        IsError ? await f(InternalError) : InternalValue;

    #endregion

    #region FlatMap

    public Result<U, TError> FlatMap<U>(Func<T, Result<U, TError>> f) where U : notnull =>
        IsSuccess ? f(InternalValue) : InternalError;

    public async Task<Result<U, TError>> FlatMapAsync<U>(Func<T, Task<Result<U, TError>>> f) where U : notnull =>
        IsSuccess ? await f(InternalValue) : InternalError;

    #endregion

    #region FlatMapError

    public Result<T, UError> FlatMapError<UError>(Func<TError, Result<T, UError>> f) where UError : notnull =>
        IsError ? f(InternalError) : InternalValue;

    public async Task<Result<T, UError>> FlatMapErrorAsync<UError>(Func<TError, Task<Result<T, UError>>> f) where UError : notnull =>
        IsError ? await f(InternalError) : InternalValue;

    #endregion

    #region Equality / ToString

    public bool Equals(Result<T, TError>? other)
    {
        if (other is null) return false;
        if (IsSuccess != other.IsSuccess) return false;
        return IsSuccess
            ? EqualityComparer<T>.Default.Equals(InternalValue, other.InternalValue)
            : EqualityComparer<TError>.Default.Equals(InternalError, other.InternalError);
    }

    /// <summary>
    /// Returns <see langword="true"/> when this Result is a success whose value
    /// equals <paramref name="other"/>.
    /// </summary>
    public bool Equals(T? other) =>
        IsSuccess && other is not null && EqualityComparer<T>.Default.Equals(InternalValue, other);

    /// <summary>
    /// Returns <see langword="true"/> when this Result is an error whose value
    /// equals <paramref name="other"/>.
    /// </summary>
    public bool Equals(TError? other) =>
        IsError && other is not null && EqualityComparer<TError>.Default.Equals(InternalError, other);

    public override bool Equals(object? obj) => obj is Result<T, TError> r && Equals(r);

    public override int GetHashCode() => HashCode.Combine(IsSuccess, InternalValue, InternalError);

    public static bool operator ==(Result<T, TError>? left, Result<T, TError>? right) =>
        ReferenceEquals(left, right) || (left is not null && left.Equals(right));

    public static bool operator !=(Result<T, TError>? left, Result<T, TError>? right) => !(left == right);

    public override string ToString() => IsSuccess
        ? $"Success({InternalValue})"
        : $"Error({InternalError})";

    #endregion
}

/// <summary>
/// Shorthand for <c>Result&lt;T, Exception&gt;</c>. Method signatures can read as
/// <c>Result&lt;T&gt;</c> without naming the error type. Shadows <c>Map</c>,
/// <c>FlatMap</c>, and their async counterparts to narrow the return type — so
/// chained expressions stay as <c>Result&lt;T&gt;</c> instead of decaying to the
/// two-parameter form.
/// </summary>
public sealed class Result<T> : Result<T, Exception>
    where T : notnull
{
    internal Result(T value) : base(value) { }
    internal Result(Exception error) : base(error) { }

    public static implicit operator Result<T>(T value) => new(value);
    public static implicit operator Result<T>(Exception error) => new(error);

    public new Result<U> Map<U>(Func<T, U> f) where U : notnull =>
        IsSuccess ? f(InternalValue) : InternalError;

    public new async Task<Result<U>> MapAsync<U>(Func<T, Task<U>> f) where U : notnull =>
        IsSuccess ? await f(InternalValue) : InternalError;

    // FlatMap's parameter matches the base signature so `new` can hide it; the
    // callback may return any `Result<U, Exception>` (including `Result<U>`
    // itself via inheritance). The inner value is re-wrapped as `Result<U>`
    // because the callback may hand back a base-class instance.
    public new Result<U> FlatMap<U>(Func<T, Result<U, Exception>> f) where U : notnull
    {
        if (IsError) return InternalError;
        var inner = f(InternalValue);
        return inner.IsSuccess ? inner.InternalValue : inner.InternalError;
    }

    public new async Task<Result<U>> FlatMapAsync<U>(Func<T, Task<Result<U, Exception>>> f) where U : notnull
    {
        if (IsError) return InternalError;
        var inner = await f(InternalValue);
        return inner.IsSuccess ? inner.InternalValue : inner.InternalError;
    }
}

/// <summary>
/// Factory helpers for <see cref="Result{T}"/> and <see cref="Result{T, TError}"/>. Prefer the
/// implicit conversions (<c>return value;</c>) in callers; use these when type inference
/// needs a nudge or when explicit intent reads better.
/// </summary>
public static partial class Result
{
    public static Result<T> Success<T>(T value) where T : notnull => new(value);
    public static Result<T> Error<T>(Exception error) where T : notnull => new(error);

    public static Result<T, TError> Success<T, TError>(T value)
        where T : notnull where TError : notnull => new(value);

    public static Result<T, TError> Error<T, TError>(TError error)
        where T : notnull where TError : notnull => new(error);
}
