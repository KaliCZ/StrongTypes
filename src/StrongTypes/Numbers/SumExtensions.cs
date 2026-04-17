#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace StrongTypes;

/// <summary>
/// <c>Sum</c> extensions for every numeric strong type. Kept together as a single
/// feature slice — adding a new wrapper adds one method here rather than a new file.
/// </summary>
public static class SumExtensions
{
    /// <summary>
    /// Sums the values of a sequence of <see cref="Positive{T}"/>.
    /// </summary>
    /// <exception cref="OverflowException">The accumulated sum overflows <typeparamref name="T"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty (the sum is <c>T.Zero</c>, which is not positive).</exception>
    public static Positive<T> Sum<T>(this IEnumerable<Positive<T>> values) where T : INumber<T>
    {
        T sum = T.Zero;
        foreach (var value in values)
        {
            sum = checked(sum + value.Value);
        }
        return Positive<T>.Create(sum);
    }

    /// <summary>
    /// Sums the values of a sequence of <see cref="NonNegative{T}"/>. An empty
    /// sequence yields <c>T.Zero</c>, which is a valid non-negative value.
    /// </summary>
    /// <exception cref="OverflowException">The accumulated sum overflows <typeparamref name="T"/>.</exception>
    public static NonNegative<T> Sum<T>(this IEnumerable<NonNegative<T>> values) where T : INumber<T>
    {
        T sum = T.Zero;
        foreach (var value in values)
        {
            sum = checked(sum + value.Value);
        }
        return NonNegative<T>.Create(sum);
    }

    /// <summary>
    /// Sums the values of a sequence of <see cref="Negative{T}"/>.
    /// </summary>
    /// <exception cref="OverflowException">The accumulated sum overflows <typeparamref name="T"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="values"/> is empty (the sum is <c>T.Zero</c>, which is not negative).</exception>
    public static Negative<T> Sum<T>(this IEnumerable<Negative<T>> values) where T : INumber<T>
    {
        T sum = T.Zero;
        foreach (var value in values)
        {
            sum = checked(sum + value.Value);
        }
        return Negative<T>.Create(sum);
    }

    /// <summary>
    /// Sums the values of a sequence of <see cref="NonPositive{T}"/>. An empty
    /// sequence yields <c>T.Zero</c>, which is a valid non-positive value.
    /// </summary>
    /// <exception cref="OverflowException">The accumulated sum overflows <typeparamref name="T"/>.</exception>
    public static NonPositive<T> Sum<T>(this IEnumerable<NonPositive<T>> values) where T : INumber<T>
    {
        T sum = T.Zero;
        foreach (var value in values)
        {
            sum = checked(sum + value.Value);
        }
        return NonPositive<T>.Create(sum);
    }
}
