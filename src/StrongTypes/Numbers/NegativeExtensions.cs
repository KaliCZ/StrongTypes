#nullable enable

using System;
using System.Collections.Generic;
using System.Numerics;

namespace StrongTypes;

public static class NegativeExtensions
{
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
}
