#nullable enable

using System;
using System.Numerics;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>
/// A numeric value guaranteed to be strictly less than <c>T.Zero</c>.
/// </summary>
/// <remarks>
/// Construct via <see cref="TryCreate"/> or <see cref="Create"/>. Internally the
/// value is stored as an offset from <c>-T.One</c> so that <c>default(Negative&lt;T&gt;)</c>
/// represents <c>-T.One</c> — i.e. the zero-initialized struct still satisfies
/// the negativity invariant.
/// </remarks>
[JsonConverter(typeof(NumericStrongTypeJsonConverterFactory))]
public readonly struct Negative<T> :
    IEquatable<Negative<T>>,
    IEquatable<T>,
    IComparable<Negative<T>>,
    IComparable<T>,
    IComparable
    where T : INumber<T>
{
    // Stored as (Value - (-T.One)) == (Value + T.One); default represents Value == -T.One.
    private readonly T _offset;

    private Negative(T offset)
    {
        _offset = offset;
    }

    public T Value => _offset - T.One;

    public static implicit operator T(Negative<T> value) => value.Value;

    public static explicit operator Negative<T>(T value) => Create(value);

    /// <summary>
    /// Returns a <see cref="Negative{T}"/> wrapping <paramref name="value"/>, or
    /// <c>null</c> if <paramref name="value"/> is not strictly less than zero.
    /// </summary>
    public static Negative<T>? TryCreate(T value)
    {
        return value < T.Zero ? new Negative<T>(value + T.One) : null;
    }

    /// <summary>
    /// Returns a <see cref="Negative{T}"/> wrapping <paramref name="value"/>.
    /// Throws <see cref="ArgumentException"/> if <paramref name="value"/> is not
    /// strictly less than zero.
    /// </summary>
    public static Negative<T> Create(T value)
    {
        return TryCreate(value)
            ?? throw new ArgumentException($"Value must be negative, but was '{value}'.", nameof(value));
    }

    public override int GetHashCode() => Value.GetHashCode();

    public override bool Equals(object? obj) =>
        obj switch
        {
            Negative<T> other => Equals(other),
            T other => Equals(other),
            _ => false
        };

    public bool Equals(Negative<T> other) => Value.Equals(other.Value);

    public bool Equals(T? other) => other is not null && Value.Equals(other);

    public static bool operator ==(Negative<T> left, Negative<T> right) => left.Equals(right);
    public static bool operator !=(Negative<T> left, Negative<T> right) => !left.Equals(right);
    public static bool operator ==(Negative<T> left, T right) => left.Value.Equals(right);
    public static bool operator !=(Negative<T> left, T right) => !left.Value.Equals(right);
    public static bool operator ==(T left, Negative<T> right) => right.Value.Equals(left);
    public static bool operator !=(T left, Negative<T> right) => !right.Value.Equals(left);

    public int CompareTo(Negative<T> other) => Value.CompareTo(other.Value);

    public int CompareTo(T? other) => other is null ? 1 : Value.CompareTo(other);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            Negative<T> other => CompareTo(other),
            T other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(Negative<T>)} or {typeof(T).Name}.", nameof(obj))
        };

    public static bool operator <(Negative<T> left, Negative<T> right) => left.CompareTo(right) < 0;
    public static bool operator <=(Negative<T> left, Negative<T> right) => left.CompareTo(right) <= 0;
    public static bool operator >(Negative<T> left, Negative<T> right) => left.CompareTo(right) > 0;
    public static bool operator >=(Negative<T> left, Negative<T> right) => left.CompareTo(right) >= 0;
    public static bool operator <(Negative<T> left, T right) => left.Value.CompareTo(right) < 0;
    public static bool operator <=(Negative<T> left, T right) => left.Value.CompareTo(right) <= 0;
    public static bool operator >(Negative<T> left, T right) => left.Value.CompareTo(right) > 0;
    public static bool operator >=(Negative<T> left, T right) => left.Value.CompareTo(right) >= 0;
    public static bool operator <(T left, Negative<T> right) => left.CompareTo(right.Value) < 0;
    public static bool operator <=(T left, Negative<T> right) => left.CompareTo(right.Value) <= 0;
    public static bool operator >(T left, Negative<T> right) => left.CompareTo(right.Value) > 0;
    public static bool operator >=(T left, Negative<T> right) => left.CompareTo(right.Value) >= 0;

    public override string ToString() => Value.ToString() ?? string.Empty;
}
