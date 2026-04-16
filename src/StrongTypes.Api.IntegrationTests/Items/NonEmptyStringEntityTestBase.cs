using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.Items;

public abstract class NonEmptyStringEntityTestBase(TestWebApplicationFactory factory)
    : IntegrationTestBase<NonEmptyStringEntity, NonEmptyString>(factory)
{
    protected override string RoutePrefix => "non-empty-string-entities";
}
