using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NegativeDecimalEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NegativeDecimalEntityTests, NegativeDecimalEntity, Negative<decimal>, Negative<decimal>?, decimal>(factory),
      IEntityTestData<decimal>
{
    protected override string RoutePrefix => "negative-decimal-entities";
    protected override Negative<decimal> Create(decimal raw) => Negative<decimal>.Create(raw);
    protected override decimal FirstValid => -5m;
    protected override decimal UpdatedValid => -100m;

    public static TheoryData<decimal> ValidInputs => new() { -5m, -42m, -10m, -1m, -1.5m, -0.01m };
    public static TheoryData<decimal> InvalidInputs => new() { 0m, 1m, 100m, 1.5m };
}
