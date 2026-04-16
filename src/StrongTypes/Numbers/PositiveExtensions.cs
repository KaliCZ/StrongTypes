#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace StrongTypes;

public static class PositiveExtensions
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
}
