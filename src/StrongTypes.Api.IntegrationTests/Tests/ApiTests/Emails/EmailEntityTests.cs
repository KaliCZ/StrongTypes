using System.Net.Mail;
using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class EmailEntityTests(TestWebApplicationFactory factory)
    : EntityTests<EmailEntityTests, EmailEntity, MailAddress, MailAddress?, string>(factory),
      IEntityTestData<string>
{
    protected override string RoutePrefix => "email-entities";
    protected override MailAddress Create(string raw) => new(raw);
    protected override string FirstValid => "alice@example.com";
    protected override string UpdatedValid => "bob@example.org";

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
