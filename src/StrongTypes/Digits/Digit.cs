using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace StrongTypes;

/// <summary>A decimal digit in the range <c>0</c>–<c>9</c>, parsed from a single character.</summary>
/// <remarks>A character is accepted when <see cref="char.IsDigit(char)"/> returns <c>true</c>, which includes non-ASCII Unicode decimal digits whose numeric value folds into 0–9. <c>default(Digit)</c> represents <c>0</c>.</remarks>
public readonly struct Digit :
    IEquatable<Digit>,
    IEquatable<byte>,
    IEquatable<int>,
    IComparable<Digit>,
    IComparable<byte>,
    IComparable<int>,
    IComparable,
    IParsable<Digit>
{
    private Digit(byte value)
    {
        Value = value;
    }

    [Pure]
    public byte Value { get; }

    public static implicit operator byte(Digit d) => d.Value;
    public static implicit operator int(Digit d) => d.Value;

    /// <summary>Wraps the decimal value of <paramref name="value"/>, or returns <c>null</c> when it is not a decimal digit character.</summary>
    /// <param name="value">The character to parse.</param>
    [Pure]
    public static Digit? TryCreate(char value)
    {
        if (!char.IsDigit(value))
        {
            return null;
        }

        return new Digit((byte)char.GetNumericValue(value));
    }

    /// <summary>Wraps the decimal value of <paramref name="value"/>.</summary>
    /// <param name="value">The character to parse.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is not a decimal digit character.</exception>
    [Pure]
    public static Digit Create(char value)
    {
        return TryCreate(value)
            ?? throw new ArgumentException($"Value must be a decimal digit character, but was '{value}'.", nameof(value));
    }

    /// <summary>Parses <paramref name="s"/> into a <see cref="Digit"/>. The string must consist of exactly one decimal digit character; the format provider is unused.</summary>
    /// <exception cref="ArgumentException"><paramref name="s"/> is not a single decimal digit character.</exception>
    [Pure]
    public static Digit Parse(string s, IFormatProvider? provider)
        => TryParse(s, provider, out var result)
            ? result
            : throw new ArgumentException($"Value must be a single decimal digit character, but was '{s}'.", nameof(s));

    /// <summary>Tries to parse <paramref name="s"/> into a <see cref="Digit"/>. The string must consist of exactly one decimal digit character; the format provider is unused.</summary>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, out Digit result)
    {
        if (s is { Length: 1 } && TryCreate(s[0]) is { } digit)
        {
            result = digit;
            return true;
        }
        result = default;
        return false;
    }

    #region Equality

    [Pure]
    public override int GetHashCode() => Value.GetHashCode();

    [Pure]
    public override bool Equals(object? obj) =>
        obj switch
        {
            Digit other => Equals(other),
            byte otherByte => Equals(otherByte),
            int otherInt => Equals(otherInt),
            _ => false
        };

    [Pure]
    public bool Equals(Digit other) => Value == other.Value;

    [Pure]
    public bool Equals(byte other) => Value == other;

    [Pure]
    public bool Equals(int other) => Value == other;

    public static bool operator ==(Digit left, Digit right) => left.Equals(right);
    public static bool operator !=(Digit left, Digit right) => !left.Equals(right);

    public static bool operator ==(Digit left, byte right) => left.Equals(right);
    public static bool operator !=(Digit left, byte right) => !left.Equals(right);
    public static bool operator ==(byte left, Digit right) => right.Equals(left);
    public static bool operator !=(byte left, Digit right) => !right.Equals(left);

    public static bool operator ==(Digit left, int right) => left.Equals(right);
    public static bool operator !=(Digit left, int right) => !left.Equals(right);
    public static bool operator ==(int left, Digit right) => right.Equals(left);
    public static bool operator !=(int left, Digit right) => !right.Equals(left);

    #endregion Equality

    #region Comparison

    [Pure]
    public int CompareTo(Digit other) => Value.CompareTo(other.Value);

    [Pure]
    public int CompareTo(byte other) => Value.CompareTo(other);

    [Pure]
    public int CompareTo(int other) => ((int)Value).CompareTo(other);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            Digit other => CompareTo(other),
            byte otherByte => CompareTo(otherByte),
            int otherInt => CompareTo(otherInt),
            _ => throw new ArgumentException($"Object must be of type {nameof(Digit)}, {nameof(Byte)}, or {nameof(Int32)}.", nameof(obj))
        };

    public static bool operator <(Digit left, Digit right) => left.CompareTo(right) < 0;
    public static bool operator <=(Digit left, Digit right) => left.CompareTo(right) <= 0;
    public static bool operator >(Digit left, Digit right) => left.CompareTo(right) > 0;
    public static bool operator >=(Digit left, Digit right) => left.CompareTo(right) >= 0;

    public static bool operator <(Digit left, byte right) => left.CompareTo(right) < 0;
    public static bool operator <=(Digit left, byte right) => left.CompareTo(right) <= 0;
    public static bool operator >(Digit left, byte right) => left.CompareTo(right) > 0;
    public static bool operator >=(Digit left, byte right) => left.CompareTo(right) >= 0;
    public static bool operator <(byte left, Digit right) => right.CompareTo(left) > 0;
    public static bool operator <=(byte left, Digit right) => right.CompareTo(left) >= 0;
    public static bool operator >(byte left, Digit right) => right.CompareTo(left) < 0;
    public static bool operator >=(byte left, Digit right) => right.CompareTo(left) <= 0;

    public static bool operator <(Digit left, int right) => left.CompareTo(right) < 0;
    public static bool operator <=(Digit left, int right) => left.CompareTo(right) <= 0;
    public static bool operator >(Digit left, int right) => left.CompareTo(right) > 0;
    public static bool operator >=(Digit left, int right) => left.CompareTo(right) >= 0;
    public static bool operator <(int left, Digit right) => right.CompareTo(left) > 0;
    public static bool operator <=(int left, Digit right) => right.CompareTo(left) >= 0;
    public static bool operator >(int left, Digit right) => right.CompareTo(left) < 0;
    public static bool operator >=(int left, Digit right) => right.CompareTo(left) <= 0;

    #endregion Comparison

    [Pure]
    public override string ToString() => Value.ToString();
}
