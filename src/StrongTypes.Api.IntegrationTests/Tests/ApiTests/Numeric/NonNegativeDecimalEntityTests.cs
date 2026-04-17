using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeDecimalEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonNegativeDecimalEntityTests, NonNegativeDecimalEntity, NonNegative<decimal>, NonNegative<decimal>?, decimal>(factory),
      IEntityTestData<decimal>
{
    protected override string RoutePrefix => "non-negative-decimal-entities";
    protected override NonNegative<decimal> Create(decimal raw) => NonNegative<decimal>.Create(raw);
    protected override decimal FirstValid => 0m;
    protected override decimal UpdatedValid => 100m;

    public static TheoryData<decimal> ValidInputs => new() { 0m, 5m, 42m, 10m, 1.5m, 0.01m };
    public static TheoryData<decimal> InvalidInputs => new() { -1m, -100m, -1.5m };
}
