using System;
using System.Net.Mail;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class EmailExtensionsTests
{
    // ── string.AsEmail ────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    public void AsEmail_Invalid_ReturnsNull(string? input) =>
        Assert.Null(input.AsEmail());

    [Property]
    public void AsEmail_Valid_ReturnsMailAddressWithSameAddress(Email email)
    {
        var ma = email.Address.AsEmail();
        Assert.NotNull(ma);
        Assert.Equal(email.Address, ma!.Address);
    }

    [Fact]
    public void AsEmail_OverEmailMaxLength_ReturnsNull()
    {
        // Build a string that BCL accepts but Email's 254 cap rejects.
        var tooLong = new string('a', Email.MaxLength) + "@x.com";
        Assert.Null(tooLong.AsEmail());
    }

    // ── string.ToEmail ────────────────────────────────────────────────────

    [Property]
    public void ToEmail_Valid_ReturnsMailAddressWithSameAddress(Email email)
    {
        var ma = email.Address.ToEmail();
        Assert.Equal(email.Address, ma.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void ToEmail_Invalid_Throws(string? input) =>
        Assert.Throws<ArgumentException>(() => input.ToEmail());

    [Fact]
    public void ToEmail_OverEmailMaxLength_Throws()
    {
        var tooLong = new string('a', Email.MaxLength) + "@x.com";
        Assert.Throws<ArgumentException>(() => tooLong.ToEmail());
    }

    // ── MailAddress.Create (extension block) ──────────────────────────────

    [Fact]
    public void MailAddressCreate_Valid_WrapsAddress()
    {
        var ma = MailAddress.Create("alice@example.com");
        Assert.Equal("alice@example.com", ma.Address);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    public void MailAddressCreate_Invalid_Throws(string input) =>
        Assert.Throws<FormatException>(() => MailAddress.Create(input));

    [Fact]
    public void MailAddressCreate_DoesNotEnforceEmailMaxLength()
    {
        // The MailAddress.Create alias is a BCL shim — it must NOT enforce
        // Email's 254 cap. Callers that want the cap go through Email.Create
        // or string.ToEmail.
        var overCap = new string('a', Email.MaxLength) + "@x.com";
        var ma = MailAddress.Create(overCap);
        Assert.Equal(overCap, ma.Address);
    }

    // ── MailAddress.TryCreate (extension block) ───────────────────────────

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
}
