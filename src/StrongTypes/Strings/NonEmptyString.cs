using System;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>A string guaranteed to be non-null, non-empty, and not consisting solely of whitespace.</summary>
/// <remarks>Comparison uses the current culture (it delegates to <see cref="string.CompareTo(string?)"/>). Exposes <c>Count</c> and a char indexer for parity with <see cref="string"/>; <c>Count</c> in particular makes the BCL <c>[MaxLength]</c> attribute work without a custom shim.</remarks>
[JsonConverter(typeof(NonEmptyStringJsonConverter))]
public sealed class NonEmptyString :
    IEquatable<NonEmptyString>,
    IEquatable<string>,
    IComparable<NonEmptyString>,
    IComparable<string>,
    IComparable
{
    private NonEmptyString(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public int Length => Value.Length;

    public int Count => Value.Length;

    public char this[int index] => Value[index];

    public static implicit operator string(NonEmptyString s) => s.Value;

    public static explicit operator NonEmptyString(string s) => Create(s);

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is null, empty, or whitespace.</summary>
    /// <param name="value">The string to validate.</param>
    [Pure]
    public static NonEmptyString? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return new NonEmptyString(value);
    }

    /// <summary>Wraps <paramref name="value"/>.</summary>
    /// <param name="value">The string to validate.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is null, empty, or whitespace.</exception>
    [Pure]
    public static NonEmptyString Create(string? value)
    {
        return TryCreate(value)
            ?? throw new ArgumentException("You cannot create NonEmptyString from whitespace, empty string or null.", nameof(value));
    }

    #region Proxy methods to string

    [Pure]
    public NonEmptyString ToLower() => Create(Value.ToLower());
    [Pure]
    public NonEmptyString ToLower(CultureInfo culture) => Create(Value.ToLower(culture));
    [Pure]
    public NonEmptyString ToLowerInvariant() => Create(Value.ToLowerInvariant());

    [Pure]
    public NonEmptyString ToUpper() => Create(Value.ToUpper());
    [Pure]
    public NonEmptyString ToUpper(CultureInfo culture) => Create(Value.ToUpper(culture));
    [Pure]
    public NonEmptyString ToUpperInvariant() => Create(Value.ToUpperInvariant());

    [Pure]
    public bool Contains(string s) => Value.Contains(s);
    [Pure]
    public bool Contains(string s, StringComparison comparisonType) => Value.Contains(s, comparisonType);
    [Pure]
    public bool Contains(char c) => Value.Contains(c);
    [Pure]
    public bool Contains(char c, StringComparison comparisonType) => Value.Contains(c, comparisonType);

    [Pure]
    public string Replace(char oldChar, char newChar) => Value.Replace(oldChar, newChar);
    [Pure]
    public string Replace(string oldString, string newString) => Value.Replace(oldString, newString);
    [Pure]
    public string Replace(string oldString, string newString, StringComparison comparisonType) => Value.Replace(oldString, newString, comparisonType);

    [Pure]
    public string Trim() => Value.Trim();

    [Pure]
    public int IndexOf(string s) => Value.IndexOf(s);
    [Pure]
    public int IndexOf(string s, int startIndex) => Value.IndexOf(s, startIndex);
    [Pure]
    public int IndexOf(string s, StringComparison comparisonType) => Value.IndexOf(s, comparisonType);
    [Pure]
    public int IndexOf(string s, int startIndex, StringComparison comparisonType) => Value.IndexOf(s, startIndex, comparisonType);
    [Pure]
    public int IndexOf(char c) => Value.IndexOf(c);
    [Pure]
    public int IndexOf(char c, int startIndex) => Value.IndexOf(c, startIndex);
    [Pure]
    public int IndexOf(char c, StringComparison comparisonType) => Value.IndexOf(c, comparisonType);

    [Pure]
    public int LastIndexOf(string s) => Value.LastIndexOf(s);
    [Pure]
    public int LastIndexOf(string s, int startIndex) => Value.LastIndexOf(s, startIndex);
    [Pure]
    public int LastIndexOf(string s, StringComparison comparisonType) => Value.LastIndexOf(s, comparisonType);
    [Pure]
    public int LastIndexOf(string s, int startIndex, StringComparison comparisonType) => Value.LastIndexOf(s, startIndex, comparisonType);
    [Pure]
    public int LastIndexOf(char c) => Value.LastIndexOf(c);
    [Pure]
    public int LastIndexOf(char c, int startIndex) => Value.LastIndexOf(c, startIndex);

    [Pure]
    public string Substring(int startIndex) => Value.Substring(startIndex);
    [Pure]
    public string Substring(int startIndex, int length) => Value.Substring(startIndex, length);

    [Pure]
    public bool StartsWith(string s) => Value.StartsWith(s);
    [Pure]
    public bool StartsWith(string s, StringComparison comparisonType) => Value.StartsWith(s, comparisonType);
    [Pure]
    public bool StartsWith(string s, bool ignoreCase, CultureInfo culture) => Value.StartsWith(s, ignoreCase, culture);
    [Pure]
    public bool StartsWith(char c) => Value.StartsWith(c);

    [Pure]
    public bool EndsWith(string s) => Value.EndsWith(s);
    [Pure]
    public bool EndsWith(string s, StringComparison comparisonType) => Value.EndsWith(s, comparisonType);
    [Pure]
    public bool EndsWith(string s, bool ignoreCase, CultureInfo culture) => Value.EndsWith(s, ignoreCase, culture);
    [Pure]
    public bool EndsWith(char c) => Value.EndsWith(c);

    #endregion Proxy methods to string

    #region Equality

    [Pure]
    public override int GetHashCode() => Value.GetHashCode();

    [Pure]
    public override bool Equals(object? obj) =>
        obj is NonEmptyString otherNonEmpty ? Equals(otherNonEmpty)
        : obj is string otherString && Equals(otherString);

    [Pure]
    public bool Equals(NonEmptyString? other) => other is not null && Value == other.Value;

    [Pure]
    public bool Equals(string? other) => other is not null && Value == other;

    public static bool operator ==(NonEmptyString? left, NonEmptyString? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(NonEmptyString? left, NonEmptyString? right) => !(left == right);

    public static bool operator ==(NonEmptyString? left, string? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(NonEmptyString? left, string? right) => !(left == right);

    public static bool operator ==(string? left, NonEmptyString? right) => right == left;

    public static bool operator !=(string? left, NonEmptyString? right) => !(right == left);

    #endregion Equality

    #region Comparison

    [Pure]
    public int CompareTo(NonEmptyString? other) =>
        other is null ? 1 : Value.CompareTo(other.Value);

    [Pure]
    public int CompareTo(string? other) => Value.CompareTo(other);

    int IComparable.CompareTo(object? obj) =>
        obj switch
        {
            null => 1,
            NonEmptyString other => CompareTo(other),
            string other => CompareTo(other),
            _ => throw new ArgumentException($"Object must be of type {nameof(NonEmptyString)} or {nameof(String)}.", nameof(obj))
        };

    public static bool operator <(NonEmptyString? left, NonEmptyString? right) =>
        left is null ? right is not null : left.CompareTo(right) < 0;

    public static bool operator <=(NonEmptyString? left, NonEmptyString? right) =>
        left is null || left.CompareTo(right) <= 0;

    public static bool operator >(NonEmptyString? left, NonEmptyString? right) =>
        left is not null && left.CompareTo(right) > 0;

    public static bool operator >=(NonEmptyString? left, NonEmptyString? right) =>
        left is null ? right is null : left.CompareTo(right) >= 0;

    #endregion Comparison

    [Pure]
    public override string ToString() => Value;
}
