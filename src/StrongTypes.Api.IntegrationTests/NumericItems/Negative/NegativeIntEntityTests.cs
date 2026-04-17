using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.Negative;

[Collection(IntegrationTestCollection.Name)]
public sealed class NegativeIntEntityTests(TestWebApplicationFactory factory)
    : NumericEntityTests<NegativeIntEntityTests, NegativeIntEntity, Negative<int>>(factory), INumericTestData
{
    protected override string RoutePrefix => "negative-int-entities";
    protected override Negative<int> Create(int raw) => Negative<int>.Create(raw);
    protected override int FirstValid => -5;
    protected override int UpdatedValid => -100;

    public static TheoryData<int> ValidInputs => new() { -5, -42, -10, -1, int.MinValue };
    public static TheoryData<int> InvalidInputs => new() { 0, 1, 100, int.MaxValue };
}
