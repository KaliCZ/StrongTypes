using System;
using System.Net.Mail;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class EmailTests
{
    // ── Validation contract — every Email entry point agrees ──────────────
    // Email.Create / Email.TryCreate / string.ToEmail / string.AsEmail are
    // four faces of the same validation rule. Asserting all four per case
    // keeps them lock-stepped and stops a wrapper from drifting away from
    // the canonical contract.

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    [InlineData("no-at-sign.com")]
    public void Invalid_AllEntryPointsReject(string? input)
    {
        Assert.Null(Email.TryCreate(input));
        Assert.Null(input.AsEmail());
        Assert.Throws<ArgumentException>(() => Email.Create(input));
        Assert.Throws<ArgumentException>(() => input.ToEmail());
    }

    [Fact]
    public void MaxLength_BoundaryAcceptedJustOverRejected()
    {
        // Local-part of 244 chars + "@x.com" (6 chars) = 250 chars, valid.
        var ok = new string('a', Email.MaxLength - 6) + "@x.com";
        Assert.NotNull(Email.TryCreate(ok));
        Assert.NotNull(ok.AsEmail());
        Assert.Equal(ok, Email.Create(ok).Address);
        Assert.Equal(ok, ok.ToEmail().Address);

        // One past the cap.
        var tooLong = new string('a', Email.MaxLength) + "@x.com";
        Assert.Null(Email.TryCreate(tooLong));
        Assert.Null(tooLong.AsEmail());
        Assert.Throws<ArgumentException>(() => Email.Create(tooLong));
        Assert.Throws<ArgumentException>(() => tooLong.ToEmail());
    }

    [Property]
    public void Valid_AllEntryPointsWrapAddress(Email seed)
    {
        var address = seed.Address;

        Assert.Equal(address, Email.Create(address).Address);
        Assert.Equal(address, Email.TryCreate(address)!.Value.Address);
        Assert.Equal(address, address.ToEmail().Address);
        Assert.Equal(address, address.AsEmail()!.Address);
    }

    // ── MailAddress.Create / TryCreate (extension block) ──────────────────
    // BCL shims, deliberately distinct from the Email entry points: they
    // throw FormatException and do NOT enforce Email.MaxLength. Callers
    // that want the cap go through Email.Create or string.ToEmail.

    [Fact]
    public void MailAddressCreate_Valid_WrapsAddress() =>
        Assert.Equal("alice@example.com", MailAddress.Create("alice@example.com").Address);

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    public void MailAddressCreate_Invalid_Throws(string input) =>
        Assert.Throws<FormatException>(() => MailAddress.Create(input));

    [Fact]
    public void MailAddressCreate_DoesNotEnforceEmailMaxLength()
    {
        var overCap = new string('a', Email.MaxLength) + "@x.com";
        Assert.Equal(overCap, MailAddress.Create(overCap).Address);
    }

    [Fact]
    public void MailAddressTryCreate_Valid_ReturnsMailAddress()
    {
        var ma = MailAddress.TryCreate("alice@example.com");
        Assert.NotNull(ma);
        Assert.Equal("alice@example.com", ma!.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    public void MailAddressTryCreate_Invalid_ReturnsNull(string? input) =>
        Assert.Null(MailAddress.TryCreate(input));

    [Fact]
    public void MailAddressTryCreate_DoesNotEnforceEmailMaxLength()
    {
        var overCap = new string('a', Email.MaxLength) + "@x.com";
        var ma = MailAddress.TryCreate(overCap);
        Assert.NotNull(ma);
        Assert.Equal(overCap, ma!.Address);
    }

    // ── Value / Address / default(Email) ──────────────────────────────────

    [Property]
    public void Value_ExposesParsedMailAddress(Email email)
    {
        var ma = email.Value;
        Assert.Equal(email.Address, ma.Address);
        Assert.Contains("@", ma.Address);
        Assert.False(string.IsNullOrEmpty(ma.User));
        Assert.False(string.IsNullOrEmpty(ma.Host));
    }

    // ── String / MailAddress conversions ──────────────────────────────────

    [Property]
    public void ToString_ReturnsAddress(Email email) =>
        Assert.Equal(email.Address, email.ToString());

    [Property]
    public void ImplicitConversion_ToString_ReturnsAddress(Email email)
    {
        string asString = email;
        Assert.Equal(email.Address, asString);
    }

    [Property]
    public void ImplicitConversion_ToMailAddress_ReturnsUnderlyingValue(Email email)
    {
        MailAddress asMailAddress = email;
        Assert.Same(email.Value, asMailAddress);
    }

    [Property]
    public void ExplicitConversion_FromString_WrapsValid(Email seed)
    {
        var converted = (Email)seed.Address;
        Assert.Equal(seed.Address, converted.Address);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void ExplicitConversion_FromString_ThrowsOnInvalid(string input) =>
        Assert.Throws<ArgumentException>(() => (Email)input);

    // ── Equality ──────────────────────────────────────────────────────────

    [Property]
    public void Equals_SameAddress_True(Email email)
    {
        var copy = Email.Create(email.Address);
        Assert.True(email.Equals(copy));
        Assert.True(email == copy);
        Assert.False(email != copy);
        Assert.Equal(email.GetHashCode(), copy.GetHashCode());
    }

    [Property]
    public void Equals_CaseInsensitive(Email email)
    {
        var upper = Email.Create(email.Address.ToUpperInvariant());
        Assert.True(email.Equals(upper));
        Assert.Equal(email.GetHashCode(), upper.GetHashCode());
    }

    [Property]
    public void Equals_DifferentAddress_False(Email a, Email b)
    {
        if (string.Equals(a.Address, b.Address, StringComparison.OrdinalIgnoreCase)) return;
        Assert.False(a.Equals(b));
        Assert.False(a == b);
        Assert.True(a != b);
    }

    [Property]
    public void Equals_Object_Boxed_True(Email email)
    {
        object boxed = Email.Create(email.Address);
        Assert.True(email.Equals(boxed));
    }

    [Property]
    public void Equals_Object_ForeignType_False(Email email) =>
        Assert.False(email.Equals((object)42));

    [Property]
    public void Equals_Object_Null_False(Email email) =>
        Assert.False(email.Equals((object?)null));

    [Property]
    public void Equals_MailAddress_SameAddress_True(Email email)
    {
        var ma = new MailAddress(email.Address);
        Assert.True(email.Equals(ma));
        Assert.True(email.Equals((object)ma));
        Assert.True(email == ma);
        Assert.True(ma == email);
        Assert.False(email != ma);
        Assert.False(ma != email);
    }

    [Property]
    public void Equals_MailAddress_CaseInsensitive(Email email)
    {
        var ma = new MailAddress(email.Address.ToUpperInvariant());
        Assert.True(email.Equals(ma));
    }

    [Property]
    public void Equals_MailAddress_DifferentAddress_False(Email a, Email b)
    {
        if (string.Equals(a.Address, b.Address, StringComparison.OrdinalIgnoreCase)) return;
        var ma = new MailAddress(b.Address);
        Assert.False(a.Equals(ma));
    }

    [Property]
    public void Equals_MailAddress_Null_False(Email email) =>
        Assert.False(email.Equals((MailAddress?)null));

    [Property]
    public void Equals_String_SameAddress_True(Email email)
    {
        Assert.True(email.Equals(email.Address));
        Assert.True(email.Equals((object)email.Address));
        Assert.True(email == email.Address);
        Assert.True(email.Address == email);
        Assert.False(email != email.Address);
        Assert.False(email.Address != email);
    }

    [Property]
    public void Equals_String_CaseInsensitive(Email email) =>
        Assert.True(email.Equals(email.Address.ToUpperInvariant()));

    [Property]
    public void Equals_String_DifferentAddress_False(Email a, Email b)
    {
        if (string.Equals(a.Address, b.Address, StringComparison.OrdinalIgnoreCase)) return;
        Assert.False(a.Equals(b.Address));
    }

    [Property]
    public void Equals_String_Null_False(Email email) =>
        Assert.False(email.Equals((string?)null));

    // ── Cross-type operators with NonEmptyString ──────────────────────────

    [Property]
    public void OperatorEquals_NonEmptyString_SameValue_True(Email email)
    {
        var nes = NonEmptyString.Create(email.Address);
        Assert.True(email == nes);
        Assert.True(nes == email);
        Assert.False(email != nes);
        Assert.False(nes != email);
    }

    [Property]
    public void OperatorEquals_NonEmptyString_CaseInsensitive(Email email)
    {
        var nes = NonEmptyString.Create(email.Address.ToUpperInvariant());
        Assert.True(email == nes);
        Assert.True(nes == email);
    }

    [Property]
    public void OperatorEquals_NonEmptyString_DifferentValue_False(Email email)
    {
        var nes = NonEmptyString.Create(email.Address + "_suffix");
        Assert.False(email == nes);
        Assert.False(nes == email);
        Assert.True(email != nes);
        Assert.True(nes != email);
    }

    [Property]
    public void OperatorEquals_NonEmptyString_Null_False(Email email)
    {
        NonEmptyString? nesNull = null;
        Assert.False(email == nesNull);
        Assert.False(nesNull == email);
        Assert.True(email != nesNull);
        Assert.True(nesNull != email);
    }

    [Fact]
    public void OperatorEquals_NonEmptyString_BothNull_True()
    {
        Email? emailNull = null;
        NonEmptyString? nesNull = null;
        Assert.True(emailNull == nesNull);
        Assert.True(nesNull == emailNull);
        Assert.False(emailNull != nesNull);
        Assert.False(nesNull != emailNull);
    }

    // ── IParsable ─────────────────────────────────────────────────────────

    [Property]
    public void TryParse_ValidInput_ReturnsTrueAndWrapsAddress(Email seed)
    {
        Assert.True(Email.TryParse(seed.Address, provider: null, out var parsed));
        Assert.Equal(seed.Address, parsed!.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    public void TryParse_Invalid_ReturnsFalse(string? input)
    {
        Assert.False(Email.TryParse(input, provider: null, out var parsed));
        Assert.Null(parsed);
    }

    [Property]
    public void Parse_ValidInput_WrapsAddress(Email seed) =>
        Assert.Equal(seed.Address, Email.Parse(seed.Address, provider: null).Address);

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Parse_Invalid_Throws(string input) =>
        Assert.Throws<ArgumentException>(() => Email.Parse(input, provider: null));
}
