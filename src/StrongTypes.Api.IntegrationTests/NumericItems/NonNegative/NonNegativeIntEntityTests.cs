using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonNegative;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeIntEntityTests(TestWebApplicationFactory factory)
    : NumericEntityTests<NonNegativeIntEntityTests, NonNegativeIntEntity, NonNegative<int>>(factory), INumericTestData
{
    protected override string RoutePrefix => "non-negative-int-entities";
    protected override NonNegative<int> Create(int raw) => NonNegative<int>.Create(raw);
    protected override int FirstValid => 0;
    protected override int UpdatedValid => 100;

    public static TheoryData<int> ValidInputs => new() { 0, 5, 42, 10, int.MaxValue };
    public static TheoryData<int> InvalidInputs => new() { -1, -100, int.MinValue };
}
