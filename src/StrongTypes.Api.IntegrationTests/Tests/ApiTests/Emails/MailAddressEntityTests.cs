using System.Net.Mail;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class MailAddressEntityTests(TestWebApplicationFactory factory)
    : EntityTests<MailAddressEntityTests, MailAddressEntity, MailAddress, MailAddress?, string>(factory),
      IEntityTestData<string>
{
    protected override string RoutePrefix => "mail-address-entities";
    protected override MailAddress Create(string raw) => new(raw);
    protected override string FirstValid => "alice@example.com";
    protected override string UpdatedValid => "bob@example.org";

    // MailAddress isn't IComparable; the column stores the address string, so the
    // database orders by that. Mirror it here for the OrderBy assertion.
    protected override IComparer<MailAddress> ValueComparer =>
        Comparer<MailAddress>.Create((left, right) => string.CompareOrdinal(left.Address, right.Address));

    public static TheoryData<string> ValidInputs => new()
    {
        "alice@example.com",
        "user.name+tag@sub.example.co.uk",
        "x@y.io",
    };

    public static TheoryData<string> InvalidInputs => new()
    {
        "",
        "   ",
        "not-an-email",
        "@no-local.com",
        "no-at-sign.com",
    };
}
