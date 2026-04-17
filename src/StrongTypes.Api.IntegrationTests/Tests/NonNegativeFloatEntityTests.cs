using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeFloatEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonNegativeFloatEntityTests, NonNegativeFloatEntity, NonNegative<float>, NonNegative<float>?, float>(factory),
      IEntityTestData<float>
{
    protected override string RoutePrefix => "non-negative-float-entities";
    protected override NonNegative<float> Create(float raw) => NonNegative<float>.Create(raw);
    protected override float FirstValid => 0f;
    protected override float UpdatedValid => 100f;

    public static TheoryData<float> ValidInputs => new() { 0f, 5f, 42f, 10f, 1.5f, float.MaxValue };
    public static TheoryData<float> InvalidInputs => new() { -1f, -100f, -1.5f, float.MinValue };
}
