using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeDoubleEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonNegativeDoubleEntityTests, NonNegativeDoubleEntity, NonNegative<double>, NonNegative<double>?, double>(factory),
      IEntityTestData<double>
{
    protected override string RoutePrefix => "non-negative-double-entities";
    protected override NonNegative<double> Create(double raw) => NonNegative<double>.Create(raw);
    protected override double FirstValid => 0d;
    protected override double UpdatedValid => 100d;

    public static TheoryData<double> ValidInputs => new() { 0d, 5d, 42d, 10d, 1.5d, double.MaxValue };
    public static TheoryData<double> InvalidInputs => new() { -1d, -100d, -1.5d, double.MinValue };
}
