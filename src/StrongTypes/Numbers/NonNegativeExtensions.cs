#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace StrongTypes;

public static class NonNegativeExtensions
{
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
}
