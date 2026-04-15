#nullable enable

using System;

namespace StrongTypes;

/// <summary>
/// Bridges between nullable reference types and the result-shaped types in
/// this library.
/// </summary>
public static class NullableExtensions
{
    /// <summary>
    /// Turns a nullable reference into a <see cref="Try{A, E}"/>: a non-null
    /// value becomes a success, a null becomes the error produced by
    /// <paramref name="error"/>.
    /// </summary>
    public static Try<A, E> ToTry<A, E>(this A? value, Func<E> error)
        where A : class
    {
        return value is null
            ? Try.Error<A, E>(error())
            : Try.Success<A, E>(value);
    }

    /// <summary>
    /// Turns a nullable value type into a <see cref="Try{A, E}"/>: a non-null
    /// value becomes a success, a null becomes the error produced by
    /// <paramref name="error"/>.
    /// </summary>
    public static Try<A, E> ToTry<A, E>(this A? value, Func<E> error)
        where A : struct
    {
        return value is null
            ? Try.Error<A, E>(error())
            : Try.Success<A, E>(value.Value);
    }
}
