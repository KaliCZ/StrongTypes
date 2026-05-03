using System;
using System.Diagnostics.Contracts;
using System.Net.Mail;
using System.Text.Json.Serialization;

namespace StrongTypes;

/// <summary>An email address validated by <see cref="MailAddress.TryCreate(string?, out MailAddress?)"/> and capped at the RFC 5321 deliverable length of 254 characters.</summary>
/// <remarks>The <see cref="Value"/> property hands the caller a <see cref="MailAddress"/> directly so it can be passed straight into APIs that take one (mailers, validators). The wire form on JSON, EF Core columns, and route arguments is the underlying address string. <c>default(Email)</c> is invalid; reading any property on it throws.</remarks>
[JsonConverter(typeof(EmailJsonConverter))]
public readonly struct Email : IEquatable<Email>
{
    /// <summary>RFC 5321 deliverable cap. The forwarder path can be longer; this is the addr-spec limit clients should enforce on input.</summary>
    public const int MaxLength = 254;

    private readonly MailAddress? _value;

    private Email(MailAddress value)
    {
        _value = value;
    }

    /// <summary>The parsed address. Use <c>email.Value.Address</c> for the raw string, <c>email.Value.User</c> for the local-part, and <c>email.Value.Host</c> for the domain.</summary>
    /// <exception cref="InvalidOperationException">The instance is <c>default(Email)</c> and was never initialised.</exception>
    public MailAddress Value =>
        _value ?? throw new InvalidOperationException($"{nameof(Email)} is uninitialised; construct it via {nameof(Create)} or {nameof(TryCreate)}.");

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

    public static implicit operator string(Email email) => email.Address;

    public static implicit operator MailAddress(Email email) => email.Value;

    public static explicit operator Email(string value) => Create(value);

    [Pure]
    public bool Equals(Email other) => string.Equals(Address, other.Address, StringComparison.OrdinalIgnoreCase);

    [Pure]
    public override bool Equals(object? obj) => obj is Email other && Equals(other);

    [Pure]
    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Address);

    [Pure]
    public override string ToString() => Address;

    public static bool operator ==(Email left, Email right) => left.Equals(right);
    public static bool operator !=(Email left, Email right) => !left.Equals(right);
}
