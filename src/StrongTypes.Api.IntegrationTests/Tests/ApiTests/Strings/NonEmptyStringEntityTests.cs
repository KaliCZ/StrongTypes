using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonEmptyStringEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonEmptyStringEntityTests, NonEmptyStringEntity, NonEmptyString, NonEmptyString?, string>(factory),
      IEntityTestData<string>
{
    protected override string RoutePrefix => "non-empty-string-entities";
    protected override NonEmptyString Create(string raw) => NonEmptyString.Create(raw);
    protected override string FirstValid => "Alice";
    protected override string UpdatedValid => "Updated";

    public static TheoryData<string> ValidInputs => new() { "Alice", "Bob", "a", "long value with spaces" };
    public static TheoryData<string> InvalidInputs => new() { "", "   ", "\t" };
}
