using System.Diagnostics.Contracts;
using System.Numerics;

namespace StrongTypes;

public static class NumberExtensions
{
    /// <summary>
    /// Returns a <see cref="Positive{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> when <paramref name="value"/> is not strictly greater than zero.
    /// </summary>
    [Pure]
    public static Positive<T>? AsPositive<T>(this T value) where T : INumber<T>
        => Positive<T>.TryCreate(value);

    /// <summary>
    /// Returns a <see cref="NonNegative{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> when <paramref name="value"/> is less than zero.
    /// </summary>
    [Pure]
    public static NonNegative<T>? AsNonNegative<T>(this T value) where T : INumber<T>
        => NonNegative<T>.TryCreate(value);

    /// <summary>
    /// Returns a <see cref="Negative{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> when <paramref name="value"/> is not strictly less than zero.
    /// </summary>
    [Pure]
    public static Negative<T>? AsNegative<T>(this T value) where T : INumber<T>
        => Negative<T>.TryCreate(value);

    /// <summary>
    /// Returns a <see cref="NonPositive{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> when <paramref name="value"/> is greater than zero.
    /// </summary>
    [Pure]
    public static NonPositive<T>? AsNonPositive<T>(this T value) where T : INumber<T>
        => NonPositive<T>.TryCreate(value);

    /// <summary>
    /// Returns a <see cref="Positive{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="System.ArgumentException"/> when <paramref name="value"/>
    /// is not strictly greater than zero.
    /// </summary>
    [Pure]
    public static Positive<T> ToPositive<T>(this T value) where T : INumber<T>
        => Positive<T>.Create(value);

    /// <summary>
    /// Returns a <see cref="NonNegative{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="System.ArgumentException"/> when <paramref name="value"/>
    /// is less than zero.
    /// </summary>
    [Pure]
    public static NonNegative<T> ToNonNegative<T>(this T value) where T : INumber<T>
        => NonNegative<T>.Create(value);

    /// <summary>
    /// Returns a <see cref="Negative{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="System.ArgumentException"/> when <paramref name="value"/>
    /// is not strictly less than zero.
    /// </summary>
    [Pure]
    public static Negative<T> ToNegative<T>(this T value) where T : INumber<T>
        => Negative<T>.Create(value);

    /// <summary>
    /// Returns a <see cref="NonPositive{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="System.ArgumentException"/> when <paramref name="value"/>
    /// is greater than zero.
    /// </summary>
    [Pure]
    public static NonPositive<T> ToNonPositive<T>(this T value) where T : INumber<T>
        => NonPositive<T>.Create(value);

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or <c>null</c>
    /// when <paramref name="b"/> is zero.
    /// </summary>
    [Pure]
    public static decimal? Divide(this int a, decimal b)
        => b == 0 ? null : a / b;

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or <c>null</c>
    /// when <paramref name="b"/> is zero.
    /// </summary>
    [Pure]
    public static decimal? Divide(this decimal a, decimal b)
        => b == 0 ? null : a / b;

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or
    /// <paramref name="otherwise"/> when <paramref name="b"/> is zero.
    /// </summary>
    [Pure]
    public static decimal SafeDivide(this int a, decimal b, decimal otherwise = 0)
        => a.Divide(b) ?? otherwise;

    /// <summary>
    /// Returns <paramref name="a"/> divided by <paramref name="b"/>, or
    /// <paramref name="otherwise"/> when <paramref name="b"/> is zero.
    /// </summary>
    [Pure]
    public static decimal SafeDivide(this decimal a, decimal b, decimal otherwise = 0)
        => a.Divide(b) ?? otherwise;
}
