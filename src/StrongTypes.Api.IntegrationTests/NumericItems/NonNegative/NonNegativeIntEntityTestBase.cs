using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonNegative;

public abstract class NonNegativeIntEntityTestBase(TestWebApplicationFactory factory)
    : IntegrationTestBase<NonNegativeIntEntity, NonNegative<int>, NonNegative<int>?>(factory)
{
    protected override string RoutePrefix => "non-negative-int-entities";
}
