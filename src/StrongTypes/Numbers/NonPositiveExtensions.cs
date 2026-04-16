#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace StrongTypes;

public static class NonPositiveExtensions
{
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
