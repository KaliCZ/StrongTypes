using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonPositiveDoubleEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonPositiveDoubleEntityTests, NonPositiveDoubleEntity, NonPositive<double>, NonPositive<double>?, double>(factory),
      IEntityTestData<double>
{
    protected override string RoutePrefix => "non-positive-double-entities";
    protected override NonPositive<double> Create(double raw) => NonPositive<double>.Create(raw);
    protected override double FirstValid => 0d;
    protected override double UpdatedValid => -100d;

    public static TheoryData<double> ValidInputs => new() { 0d, -5d, -42d, -10d, -1.5d, double.MinValue };
    public static TheoryData<double> InvalidInputs => new() { 1d, 100d, 1.5d, double.MaxValue };
}
