using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveDoubleEntityTests(TestWebApplicationFactory factory)
    : EntityTests<PositiveDoubleEntityTests, PositiveDoubleEntity, Positive<double>, Positive<double>?, double>(factory),
      IEntityTestData<double>
{
    protected override string RoutePrefix => "positive-double-entities";
    protected override Positive<double> Create(double raw) => Positive<double>.Create(raw);
    protected override double FirstValid => 5d;
    protected override double UpdatedValid => 100d;

    public static TheoryData<double> ValidInputs => new() { 5d, 42d, 10d, 1d, 1.5d, double.MaxValue };
    public static TheoryData<double> InvalidInputs => new() { 0d, -1d, -100d, -1.5d, double.MinValue };
}
