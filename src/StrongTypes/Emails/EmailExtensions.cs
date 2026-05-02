using System;
using System.Diagnostics.Contracts;
using System.Net.Mail;

namespace StrongTypes;

public static class EmailExtensions
{
    /// <summary>Parses <paramref name="s"/> under the full <see cref="Email"/> contract (non-blank, at most <see cref="Email.MaxLength"/> characters, parseable as an addr-spec) and returns the resulting <see cref="MailAddress"/>, or <c>null</c> on failure.</summary>
    /// <param name="s">The candidate address.</param>
    [Pure]
    public static MailAddress? AsEmail(this string? s) => Email.TryCreate(s)?.Value;

    /// <summary>Parses <paramref name="s"/> under the full <see cref="Email"/> contract and returns the resulting <see cref="MailAddress"/>.</summary>
    /// <param name="s">The candidate address.</param>
    /// <exception cref="ArgumentException"><paramref name="s"/> is null, blank, longer than <see cref="Email.MaxLength"/>, or not a valid addr-spec.</exception>
    [Pure]
    public static MailAddress ToEmail(this string? s) => Email.Create(s).Value;

    // C# 14 extension block: lets callers write MailAddress.Create / MailAddress.TryCreate
    // alongside the BCL's existing static surface, matching the Create / TryCreate shape
    // every StrongTypes wrapper uses.
    extension(MailAddress)
    {
        /// <summary>Constructs a <see cref="MailAddress"/> from <paramref name="address"/>. Thin alias for <c>new MailAddress(address)</c>.</summary>
        /// <param name="address">The candidate address.</param>
        /// <exception cref="ArgumentNullException"><paramref name="address"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="address"/> is empty.</exception>
        /// <exception cref="FormatException"><paramref name="address"/> is not a valid addr-spec.</exception>
        [Pure]
        public static MailAddress Create(string address) => new(address);

        /// <summary>Wraps the BCL <see cref="MailAddress.TryCreate(string?, out MailAddress?)"/> as a <c>TryCreate</c> returning <c>null</c> on failure.</summary>
        /// <param name="address">The candidate address.</param>
        [Pure]
        public static MailAddress? TryCreate(string? address) =>
            MailAddress.TryCreate(address, out var result) ? result : null;
    }
}
