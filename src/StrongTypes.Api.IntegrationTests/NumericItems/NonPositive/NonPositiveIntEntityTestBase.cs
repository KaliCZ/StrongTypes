using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonPositive;

public abstract class NonPositiveIntEntityTestBase(TestWebApplicationFactory factory)
    : IntegrationTestBase<NonPositiveIntEntity, NonPositive<int>, NonPositive<int>?>(factory)
{
    protected override string RoutePrefix => "non-positive-int-entities";
}
