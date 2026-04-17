using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.NumericItems.NonPositive;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonPositiveIntEntityTests(TestWebApplicationFactory factory)
    : NumericEntityTests<NonPositiveIntEntity, NonPositive<int>>(factory)
{
    protected override string RoutePrefix => "non-positive-int-entities";
    protected override NonPositive<int> Create(int raw) => NonPositive<int>.Create(raw);
    protected override int FirstValid => 0;
    protected override int UpdatedValid => -100;

    public static TheoryData<int> ValidInputs => new() { 0, -5, -42, -10, int.MinValue };
    public static TheoryData<int> InvalidInputs => new() { 1, 100, int.MaxValue };
}
