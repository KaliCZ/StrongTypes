using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeIntEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonNegativeIntEntityTests, NonNegativeIntEntity, NonNegative<int>, NonNegative<int>?, int>(factory),
      IEntityTestData<int>
{
    protected override string RoutePrefix => "non-negative-int-entities";
    protected override NonNegative<int> Create(int raw) => NonNegative<int>.Create(raw);
    protected override int FirstValid => 0;
    protected override int UpdatedValid => 100;

    public static TheoryData<int> ValidInputs => new() { 0, 5, 42, 10, int.MaxValue };
    public static TheoryData<int> InvalidInputs => new() { -1, -100, int.MinValue };
}
