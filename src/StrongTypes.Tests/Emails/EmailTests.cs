using System;
using FsCheck.Xunit;
using Xunit;

namespace StrongTypes.Tests;

[Properties(Arbitrary = new[] { typeof(Generators) })]
public class EmailTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("not-an-email")]
    [InlineData("@no-local.com")]
    [InlineData("no-at-sign.com")]
    public void TryCreate_Invalid_ReturnsNull(string? input) =>
        Assert.Null(Email.TryCreate(input));

    [Fact]
    public void TryCreate_TooLong_ReturnsNull()
    {
        // Local-part of 244 chars + "@x.com" (6 chars) = 250 chars, valid.
        var ok = new string('a', Email.MaxLength - 6) + "@x.com";
        Assert.NotNull(Email.TryCreate(ok));

        // One past the cap.
        var tooLong = new string('a', Email.MaxLength) + "@x.com";
        Assert.Null(Email.TryCreate(tooLong));
    }

    [Property]
    public void TryCreate_Valid_WrapsAddress(Email seed)
    {
        var created = Email.TryCreate(seed.Address);
        Assert.NotNull(created);
        Assert.Equal(seed.Address, created!.Value.Address);
    }

    [Property]
    public void Create_Valid_WrapsAddress(Email seed) =>
        Assert.Equal(seed.Address, Email.Create(seed.Address).Address);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Create_Invalid_Throws(string? input) =>
        Assert.Throws<ArgumentException>(() => Email.Create(input));

    [Property]
    public void Value_ExposesParsedMailAddress(Email email)
    {
        var ma = email.Value;
        Assert.Equal(email.Address, ma.Address);
        Assert.Contains("@", ma.Address);
        Assert.False(string.IsNullOrEmpty(ma.User));
        Assert.False(string.IsNullOrEmpty(ma.Host));
    }

    [Fact]
    public void Default_Value_Throws()
    {
        Email defaulted = default;
        Assert.Throws<InvalidOperationException>(() => defaulted.Value);
    }

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
        System.Net.Mail.MailAddress asMailAddress = email;
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
}
