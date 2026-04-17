#nullable enable

using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be less than or equal to <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <see cref="Create"/>. Unlike
/// <see cref="Negative{T}"/>, <c>default(NonPositive&lt;T&gt;)</c> wraps
/// <c>T.Zero</c> and therefore <em>does</em> satisfy the invariant, though the
/// factories remain the intended entry point.
/// </remarks>
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly struct NonPositive<T> :
    IEquatable<NonPositive<T>>,
    IEquatable<T>,
    IComparable<NonPositive<T>>,
    IComparable<T>,
    IComparable
    where T : INumber<T>
{
    private NonPositive(T value)
    {
        Value = value;
    }

    public T Value { get; }

    public static implicit operator T(NonPositive<T> value) => value.Value;

    public static explicit operator NonPositive<T>(T value) => Create(value);

    /// <summary>
    /// Returns a <see cref="NonPositive{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is greater than zero.
    /// </summary>
    public static NonPositive<T>? TryCreate(T value)
    {
        return value <= T.Zero ? new NonPositive<T>(value) : null;
    }

    /// <summary>
    /// Returns a <see cref="NonPositive{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is greater
    /// than zero.
    /// </summary>
    public static NonPositive<T> Create(T value)
    {
        return TryCreate(value)
            ?? throw new ArgumentException($"Value must be non-positive, but was '{value}'.", nameof(value));
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) =>
        obj switch
        {
            NonPositive<T> other => Equals(other),
            T other => Equals(other),
            _ => false
        };

    public bool Equals(NonPositive<T> other) => Value.Equals(other.Value);

    public bool Equals(T? other) => other is not null && Value.Equals(other);

    public static bool operator ==(NonPositive<T> left, NonPositive<T> right) => left.Equals(right);
    public static bool operator !=(NonPositive<T> left, NonPositive<T> right) => !left.Equals(right);
    public static bool operator ==(NonPositive<T> left, T right) => left.Value.Equals(right);
    public static bool operator !=(NonPositive<T> left, T right) => !left.Value.Equals(right);
    public static bool operator ==(T left, NonPositive<T> right) => right.Value.Equals(left);
    public static bool operator !=(T left, NonPositive<T> right) => !right.Value.Equals(left);

    public int CompareTo(NonPositive<T> other) => Value.CompareTo(other.Value);

    public int CompareTo(T? other) => other is null ? 1 : Value.CompareTo(other);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            NonPositive<T> other => CompareTo(other),
            T other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(NonPositive<T>)} or {typeof(T).Name}.", nameof(obj))
        };

    public static bool operator <(NonPositive<T> left, NonPositive<T> right) => left.CompareTo(right) < 0;
    public static bool operator <=(NonPositive<T> left, NonPositive<T> right) => left.CompareTo(right) <= 0;
    public static bool operator >(NonPositive<T> left, NonPositive<T> right) => left.CompareTo(right) > 0;
    public static bool operator >=(NonPositive<T> left, NonPositive<T> right) => left.CompareTo(right) >= 0;
    public static bool operator <(NonPositive<T> left, T right) => left.Value.CompareTo(right) < 0;
    public static bool operator <=(NonPositive<T> left, T right) => left.Value.CompareTo(right) <= 0;
    public static bool operator >(NonPositive<T> left, T right) => left.Value.CompareTo(right) > 0;
    public static bool operator >=(NonPositive<T> left, T right) => left.Value.CompareTo(right) >= 0;
    public static bool operator <(T left, NonPositive<T> right) => left.CompareTo(right.Value) < 0;
    public static bool operator <=(T left, NonPositive<T> right) => left.CompareTo(right.Value) <= 0;
    public static bool operator >(T left, NonPositive<T> right) => left.CompareTo(right.Value) > 0;
    public static bool operator >=(T left, NonPositive<T> right) => left.CompareTo(right.Value) >= 0;

    public override string ToString() => Value.ToString() ?? string.Empty;
}
