using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveIntEntityTests(TestWebApplicationFactory factory)
    : EntityTests<PositiveIntEntityTests, PositiveIntEntity, Positive<int>, Positive<int>?, int>(factory),
      IEntityTestData<int>
{
    protected override string RoutePrefix => "positive-int-entities";
    protected override Positive<int> Create(int raw) => Positive<int>.Create(raw);
    protected override int FirstValid => 5;
    protected override int UpdatedValid => 100;

    public static TheoryData<int> ValidInputs => new() { 5, 42, 10, 1, int.MaxValue };
    public static TheoryData<int> InvalidInputs => new() { 0, -1, -100, int.MinValue };
}
