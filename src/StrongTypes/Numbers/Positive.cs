#nullable enable

using System;
using System.Numerics;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be strictly greater than <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <see cref="Create"/>. Internally the
/// value is stored as an offset from <c>T.One</c> so that <c>default(Positive&lt;T&gt;)</c>
/// represents <c>T.One</c> — i.e. the zero-initialized struct still satisfies
/// the positivity invariant.
/// </remarks>
public readonly struct Positive<T> :
    IEquatable<Positive<T>>,
    IComparable<Positive<T>>,
    IComparable
    where T : INumber<T>
{
    // Stored as (Value - T.One); default(Positive<T>) therefore represents Value == T.One.
    private readonly T _offset;

    private Positive(T offset)
    {
        _offset = offset;
    }

    public T Value => _offset + T.One;

    public static implicit operator T(Positive<T> value) => value.Value;

    public static explicit operator Positive<T>(T value) => Create(value);

    /// <summary>
    /// Returns a <see cref="Positive{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is not strictly greater than zero.
    /// </summary>
    public static Positive<T>? TryCreate(T value)
    {
        return value > T.Zero ? new Positive<T>(value - T.One) : null;
    }

    /// <summary>
    /// Returns a <see cref="Positive{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is not
    /// strictly greater than zero.
    /// </summary>
    public static Positive<T> Create(T value)
    {
        return TryCreate(value)
            ?? throw new ArgumentException($"Value must be positive, but was '{value}'.", nameof(value));
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) => obj is Positive<T> other && Equals(other);

    public bool Equals(Positive<T> other) => Value.Equals(other.Value);

    public static bool operator ==(Positive<T> left, Positive<T> right) => left.Equals(right);

    public static bool operator !=(Positive<T> left, Positive<T> right) => !left.Equals(right);

    public int CompareTo(Positive<T> other) => Value.CompareTo(other.Value);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            Positive<T> other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(Positive<T>)}.", nameof(obj))
        };

    public static bool operator <(Positive<T> left, Positive<T> right) => left.CompareTo(right) < 0;
    public static bool operator <=(Positive<T> left, Positive<T> right) => left.CompareTo(right) <= 0;
    public static bool operator >(Positive<T> left, Positive<T> right) => left.CompareTo(right) > 0;
    public static bool operator >=(Positive<T> left, Positive<T> right) => left.CompareTo(right) >= 0;

    public override string ToString() => Value.ToString() ?? string.Empty;
}
