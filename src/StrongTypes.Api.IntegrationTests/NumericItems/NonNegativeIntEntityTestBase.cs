using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

public abstract class NonNegativeIntEntityTestBase(TestWebApplicationFactory factory)
    : ValueIntegrationTestBase<NonNegativeIntEntity, NonNegative<int>>(factory)
{
    protected override string RoutePrefix => "non-negative-int-entities";
}
