using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonPositiveIntEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonPositiveIntEntityTests, NonPositiveIntEntity, NonPositive<int>, NonPositive<int>?, int>(factory),
      IEntityTestData<int>
{
    protected override string RoutePrefix => "non-positive-int-entities";
    protected override NonPositive<int> Create(int raw) => NonPositive<int>.Create(raw);
    protected override int FirstValid => 0;
    protected override int UpdatedValid => -100;

    public static TheoryData<int> ValidInputs => new() { 0, -5, -42, -10, int.MinValue };
    public static TheoryData<int> InvalidInputs => new() { 1, 100, int.MaxValue };
}
