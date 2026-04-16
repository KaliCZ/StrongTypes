using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

public abstract class NonPositiveIntEntityTestBase(TestWebApplicationFactory factory)
    : ValueIntegrationTestBase<NonPositiveIntEntity, NonPositive<int>>(factory)
{
    protected override string RoutePrefix => "non-positive-int-entities";
}
