using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems;

public abstract class NegativeIntEntityTestBase(TestWebApplicationFactory factory)
    : ValueIntegrationTestBase<NegativeIntEntity, Negative<int>>(factory)
{
    protected override string RoutePrefix => "negative-int-entities";
}
