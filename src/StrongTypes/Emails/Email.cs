using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An email address validated by <see cref="MailAddress.TryCreate(string?, out MailAddress?)"/> and capped at the RFC 5321 deliverable length of 254 characters.</summary>
/// <remarks>The <see cref="Value"/> property hands the caller a <see cref="MailAddress"/> directly so it can be passed straight into APIs that take one (mailers, validators). The wire form on JSON, EF Core columns, and route arguments is the underlying address string.</remarks>
[JsonConverter(typeof(EmailJsonConverter))]
public sealed class Email :
    IEquatable<Email>,
    IEquatable<MailAddress>,
    IEquatable<string>,
    IParsable<Email>
{
    /// <summary>RFC 5321 deliverable cap. The forwarder path can be longer; this is the addr-spec limit clients should enforce on input.</summary>
    public const int MaxLength = 254;

    /// <summary>Wraps an already-parsed <paramref name="value"/> without re-validating. Skips the <see cref="MaxLength"/> check — callers that need the cap enforced should go through <see cref="Create(string?)"/> or <see cref="TryCreate(string?)"/>.</summary>
    /// <param name="value">The mail address to wrap.</param>
    public Email(MailAddress value)
    {
        Value = value;
    }

    /// <summary>The parsed address. Use <c>email.Value.Address</c> for the raw string, <c>email.Value.User</c> for the local-part, and <c>email.Value.Host</c> for the domain.</summary>
    public MailAddress Value { get; }

    /// <summary>The address as it appeared on the wire.</summary>
    public string Address => Value.Address;

    /// <summary>Wraps <paramref name="value"/>, or returns <c>null</c> when it is null, blank, longer than <see cref="MaxLength"/>, or rejected by <see cref="MailAddress.TryCreate(string?, out MailAddress?)"/>.</summary>
    /// <param name="value">The candidate address.</param>
    [Pure]
    public static Email? TryCreate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (value.Length > MaxLength) return null;
        if (!MailAddress.TryCreate(value, out var address) || address is null) return null;
        return new Email(address);
    }

    /// <summary>Wraps <paramref name="value"/>.</summary>
    /// <param name="value">The candidate address.</param>
    /// <exception cref="ArgumentException"><paramref name="value"/> is null, blank, longer than <see cref="MaxLength"/>, or not a valid email address.</exception>
    [Pure]
    public static Email Create(string? value) =>
        TryCreate(value) ?? throw new ArgumentException(
            $"'{value}' is not a valid email address (must be non-empty, at most {MaxLength} characters, and parseable as an addr-spec).",
            nameof(value));

    /// <summary>Parses <paramref name="s"/> into an <see cref="Email"/>. Equivalent to <see cref="Create(string?)"/>; the format provider is unused.</summary>
    /// <exception cref="ArgumentException"><paramref name="s"/> is null, blank, longer than <see cref="MaxLength"/>, or not a valid email address.</exception>
    [Pure]
    public static Email Parse(string s, IFormatProvider? provider) => Create(s);

    /// <summary>Tries to parse <paramref name="s"/> into an <see cref="Email"/>. Equivalent to <see cref="TryCreate(string?)"/>; the format provider is unused.</summary>
    public static bool TryParse(string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Email result)
    {
        result = TryCreate(s);
        return result is not null;
    }

    public static implicit operator string(Email email) => email.Address;

    public static implicit operator MailAddress(Email email) => email.Value;

    public static implicit operator Email(MailAddress value) => new(value);

    public static explicit operator Email(string value) => Create(value);

    [Pure]
    public bool Equals(Email? other) => other is not null && string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase);

    [Pure]
    public bool Equals(MailAddress? other) => other is not null && string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase);

    [Pure]
    public bool Equals(string? other) => other is not null && string.Equals(Address, other, StringComparison.OrdinalIgnoreCase);

    [Pure]
    public override bool Equals(object? obj) => obj switch
    {
        Email otherEmail => Equals(otherEmail),
        MailAddress otherMailAddress => Equals(otherMailAddress),
        string otherString => Equals(otherString),
        _ => false,
    };

    [Pure]
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Address);

    [Pure]
    public override string ToString() => Address;

    public static bool operator ==(Email? left, Email? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Email? left, Email? right) => !(left == right);

    public static bool operator ==(Email? left, MailAddress? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Email? left, MailAddress? right) => !(left == right);

    public static bool operator ==(MailAddress? left, Email? right) => right == left;

    public static bool operator !=(MailAddress? left, Email? right) => !(right == left);

    public static bool operator ==(Email? left, string? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Email? left, string? right) => !(left == right);

    public static bool operator ==(string? left, Email? right) => right == left;

    public static bool operator !=(string? left, Email? right) => !(right == left);

    public static bool operator ==(Email? left, NonEmptyString? right) =>
        left == (right is null ? null : right.Value);

    public static bool operator !=(Email? left, NonEmptyString? right) => !(left == right);

    public static bool operator ==(NonEmptyString? left, Email? right) => right == left;

    public static bool operator !=(NonEmptyString? left, Email? right) => !(right == left);
}
