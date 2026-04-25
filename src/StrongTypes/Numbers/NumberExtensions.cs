using System.Diagnostics.Contracts;
using System.Numerics;

namespace StrongTypes;

public static class NumberExtensions
{
    /// <summary>Wraps <paramref name="value"/> as <see cref="Positive{T}"/>, or returns <c>null</c> when it is not strictly greater than zero.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static Positive<T>? AsPositive<T>(this T value) where T : INumber<T>
        => Positive<T>.TryCreate(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="NonNegative{T}"/>, or returns <c>null</c> when it is less than zero.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static NonNegative<T>? AsNonNegative<T>(this T value) where T : INumber<T>
        => NonNegative<T>.TryCreate(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="Negative{T}"/>, or returns <c>null</c> when it is not strictly less than zero.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static Negative<T>? AsNegative<T>(this T value) where T : INumber<T>
        => Negative<T>.TryCreate(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="NonPositive{T}"/>, or returns <c>null</c> when it is greater than zero.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static NonPositive<T>? AsNonPositive<T>(this T value) where T : INumber<T>
        => NonPositive<T>.TryCreate(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="Positive{T}"/>.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    /// <exception cref="System.ArgumentException"><paramref name="value"/> is not strictly greater than zero.</exception>
    [Pure]
    public static Positive<T> ToPositive<T>(this T value) where T : INumber<T>
        => Positive<T>.Create(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="NonNegative{T}"/>.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    /// <exception cref="System.ArgumentException"><paramref name="value"/> is less than zero.</exception>
    [Pure]
    public static NonNegative<T> ToNonNegative<T>(this T value) where T : INumber<T>
        => NonNegative<T>.Create(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="Negative{T}"/>.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    /// <exception cref="System.ArgumentException"><paramref name="value"/> is not strictly less than zero.</exception>
    [Pure]
    public static Negative<T> ToNegative<T>(this T value) where T : INumber<T>
        => Negative<T>.Create(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="NonPositive{T}"/>.</summary>
    /// <typeparam name="T">The underlying numeric type.</typeparam>
    /// <param name="value">The number to validate.</param>
    /// <exception cref="System.ArgumentException"><paramref name="value"/> is greater than zero.</exception>
    [Pure]
    public static NonPositive<T> ToNonPositive<T>(this T value) where T : INumber<T>
        => NonPositive<T>.Create(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="BoundedInt{TBounds}"/>, or returns <c>null</c> when it is outside <c>[TBounds.Min, TBounds.Max]</c>.</summary>
    /// <typeparam name="TBounds">A witness type carrying the inclusive lower and upper bounds.</typeparam>
    /// <param name="value">The number to validate.</param>
    [Pure]
    public static BoundedInt<TBounds>? AsBounded<TBounds>(this int value) where TBounds : IBounds<int>
        => BoundedInt<TBounds>.TryCreate(value);

    /// <summary>Wraps <paramref name="value"/> as <see cref="BoundedInt{TBounds}"/>.</summary>
    /// <typeparam name="TBounds">A witness type carrying the inclusive lower and upper bounds.</typeparam>
    /// <param name="value">The number to validate.</param>
    /// <exception cref="System.ArgumentException"><paramref name="value"/> is outside <c>[TBounds.Min, TBounds.Max]</c>.</exception>
    [Pure]
    public static BoundedInt<TBounds> ToBounded<TBounds>(this int value) where TBounds : IBounds<int>
        => BoundedInt<TBounds>.Create(value);

    /// <summary>Divides <paramref name="a"/> by <paramref name="b"/>, or returns <c>null</c> when <paramref name="b"/> is zero.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    [Pure]
    public static decimal? Divide(this int a, decimal b)
        => b == 0 ? null : a / b;

    /// <summary>Divides <paramref name="a"/> by <paramref name="b"/>, or returns <c>null</c> when <paramref name="b"/> is zero.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    [Pure]
    public static decimal? Divide(this decimal a, decimal b)
        => b == 0 ? null : a / b;

    /// <summary>Divides <paramref name="a"/> by <paramref name="b"/>, or returns <paramref name="otherwise"/> when <paramref name="b"/> is zero.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <param name="otherwise">Fallback returned when <paramref name="b"/> is zero.</param>
    [Pure]
    public static decimal SafeDivide(this int a, decimal b, decimal otherwise = 0)
        => a.Divide(b) ?? otherwise;

    /// <summary>Divides <paramref name="a"/> by <paramref name="b"/>, or returns <paramref name="otherwise"/> when <paramref name="b"/> is zero.</summary>
    /// <param name="a">The dividend.</param>
    /// <param name="b">The divisor.</param>
    /// <param name="otherwise">Fallback returned when <paramref name="b"/> is zero.</param>
    [Pure]
    public static decimal SafeDivide(this decimal a, decimal b, decimal otherwise = 0)
        => a.Divide(b) ?? otherwise;
}
