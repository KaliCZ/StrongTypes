using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

public abstract class PositiveIntEntityTestBase(TestWebApplicationFactory factory)
    : ValueIntegrationTestBase<PositiveIntEntity, Positive<int>>(factory)
{
    protected override string RoutePrefix => "positive-int-entities";
}
