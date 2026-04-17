#nullable enable

using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be greater than or equal to <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <see cref="Create"/>. Unlike
/// <see cref="Positive{T}"/>, <c>default(NonNegative&lt;T&gt;)</c> wraps
/// <c>T.Zero</c> and therefore <em>does</em> satisfy the invariant, though the
/// factories remain the intended entry point.
/// </remarks>
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly struct NonNegative<T> :
    IEquatable<NonNegative<T>>,
    IEquatable<T>,
    IComparable<NonNegative<T>>,
    IComparable<T>,
    IComparable
    where T : INumber<T>
{
    private NonNegative(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public static implicit operator T(NonNegative<T> value) => value.Value;

    public static explicit operator NonNegative<T>(T value) => Create(value);

    /// <summary>
    /// Returns a <see cref="NonNegative{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is less than zero.
    /// </summary>
    public static NonNegative<T>? TryCreate(T value)
    {
        return value >= T.Zero ? new NonNegative<T>(value) : null;
    }

    /// <summary>
    /// Returns a <see cref="NonNegative{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is less
    /// than zero.
    /// </summary>
    public static NonNegative<T> Create(T value)
    {
        return TryCreate(value)
            ?? throw new ArgumentException($"Value must be non-negative, but was '{value}'.", nameof(value));
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) =>
        obj switch
        {
            NonNegative<T> other => Equals(other),
            T other => Equals(other),
            _ => false
        };

    public bool Equals(NonNegative<T> other) => Value.Equals(other.Value);

    public bool Equals(T? other) => other is not null && Value.Equals(other);

    public static bool operator ==(NonNegative<T> left, NonNegative<T> right) => left.Equals(right);
    public static bool operator !=(NonNegative<T> left, NonNegative<T> right) => !left.Equals(right);
    public static bool operator ==(NonNegative<T> left, T right) => left.Value.Equals(right);
    public static bool operator !=(NonNegative<T> left, T right) => !left.Value.Equals(right);
    public static bool operator ==(T left, NonNegative<T> right) => right.Value.Equals(left);
    public static bool operator !=(T left, NonNegative<T> right) => !right.Value.Equals(left);

    public int CompareTo(NonNegative<T> other) => Value.CompareTo(other.Value);

    public int CompareTo(T? other) => other is null ? 1 : Value.CompareTo(other);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            NonNegative<T> other => CompareTo(other),
            T other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(NonNegative<T>)} or {typeof(T).Name}.", nameof(obj))
        };

    public static bool operator <(NonNegative<T> left, NonNegative<T> right) => left.CompareTo(right) < 0;
    public static bool operator <=(NonNegative<T> left, NonNegative<T> right) => left.CompareTo(right) <= 0;
    public static bool operator >(NonNegative<T> left, NonNegative<T> right) => left.CompareTo(right) > 0;
    public static bool operator >=(NonNegative<T> left, NonNegative<T> right) => left.CompareTo(right) >= 0;
    public static bool operator <(NonNegative<T> left, T right) => left.Value.CompareTo(right) < 0;
    public static bool operator <=(NonNegative<T> left, T right) => left.Value.CompareTo(right) <= 0;
    public static bool operator >(NonNegative<T> left, T right) => left.Value.CompareTo(right) > 0;
    public static bool operator >=(NonNegative<T> left, T right) => left.Value.CompareTo(right) >= 0;
    public static bool operator <(T left, NonNegative<T> right) => left.CompareTo(right.Value) < 0;
    public static bool operator <=(T left, NonNegative<T> right) => left.CompareTo(right.Value) <= 0;
    public static bool operator >(T left, NonNegative<T> right) => left.CompareTo(right.Value) > 0;
    public static bool operator >=(T left, NonNegative<T> right) => left.CompareTo(right.Value) >= 0;

    public override string ToString() => Value.ToString() ?? string.Empty;
}
