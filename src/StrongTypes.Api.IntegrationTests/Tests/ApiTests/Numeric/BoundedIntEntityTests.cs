using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class BoundedIntEntityTests(TestWebApplicationFactory factory)
    : EntityTests<BoundedIntEntityTests, BoundedIntEntity, BoundedInt<PageSizeBounds>, BoundedInt<PageSizeBounds>?, int>(factory),
      IEntityTestData<int>
{
    protected override string RoutePrefix => "bounded-int-entities";
    protected override BoundedInt<PageSizeBounds> Create(int raw) => BoundedInt<PageSizeBounds>.Create(raw);
    protected override int FirstValid => 5;
    protected override int UpdatedValid => 50;

    // Both ends of the inclusive 1..100 range plus interior values.
    public static TheoryData<int> ValidInputs => new() { 1, 5, 50, 99, 100 };

    // Just-outside both bounds and the extremes.
    public static TheoryData<int> InvalidInputs => new() { 0, 101, -1, int.MinValue, int.MaxValue };
}
