using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

// Values are constrained to two decimal places so they round-trip exactly
// through EF Core's default SQL Server decimal(18,2) mapping.
[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveDecimalEntityTests(TestWebApplicationFactory factory)
    : EntityTests<PositiveDecimalEntityTests, PositiveDecimalEntity, Positive<decimal>, Positive<decimal>?, decimal>(factory),
      IEntityTestData<decimal>
{
    protected override string RoutePrefix => "positive-decimal-entities";
    protected override Positive<decimal> Create(decimal raw) => Positive<decimal>.Create(raw);
    protected override decimal FirstValid => 5m;
    protected override decimal UpdatedValid => 100m;

    public static TheoryData<decimal> ValidInputs => new() { 5m, 42m, 10m, 1m, 1.5m, 0.01m };
    public static TheoryData<decimal> InvalidInputs => new() { 0m, -1m, -100m, -1.5m };
}
