#nullable enable

using System;

namespace StrongTypes;

/// <summary>
/// Extensions that produce <see cref="Try{A, E}"/> values from other shapes.
/// </summary>
public static class TryExtensions
{
    /// <summary>
    /// Turns a nullable reference into a <see cref="Try{A, E}"/>: a non-null
    /// value becomes a success, a null becomes the error produced by
    /// <paramref name="error"/>.
    /// </summary>
    public static Try<A, E> ToTry<A, E>(this A? value, Func<E> error)
        where A : class
        where E : notnull
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
        where E : notnull
    {
        return value is null
            ? Try.Error<A, E>(error())
            : Try.Success<A, E>(value.Value);
    }

    /// <summary>
    /// Turns a <see cref="Maybe{T}"/> into a <see cref="Try{T, E}"/>: a populated
    /// Maybe becomes a success; an empty Maybe becomes the error produced by
    /// <paramref name="error"/>.
    /// </summary>
    public static Try<T, E> ToTry<T, E>(this Maybe<T> m, Func<E> error)
        where T : notnull
        where E : notnull
        => m.HasValue ? Try.Success<T, E>(m.InternalValue) : Try.Error<T, E>(error());
}
