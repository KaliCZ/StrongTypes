using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Negative;

public abstract class NegativeIntEntityTestBase(TestWebApplicationFactory factory)
    : IntegrationTestBase<NegativeIntEntity, Negative<int>, Negative<int>?>(factory)
{
    protected override string RoutePrefix => "negative-int-entities";
}
