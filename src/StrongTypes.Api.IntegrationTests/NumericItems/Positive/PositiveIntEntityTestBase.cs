using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Positive;

public abstract class PositiveIntEntityTestBase(TestWebApplicationFactory factory)
    : IntegrationTestBase<PositiveIntEntity, Positive<int>, Positive<int>?>(factory)
{
    protected override string RoutePrefix => "positive-int-entities";
}
