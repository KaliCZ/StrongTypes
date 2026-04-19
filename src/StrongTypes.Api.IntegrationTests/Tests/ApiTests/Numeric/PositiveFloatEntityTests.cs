using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveFloatEntityTests(TestWebApplicationFactory factory)
    : EntityTests<PositiveFloatEntityTests, PositiveFloatEntity, Positive<float>, Positive<float>?, float>(factory),
      IEntityTestData<float>
{
    protected override string RoutePrefix => "positive-float-entities";
    protected override Positive<float> Create(float raw) => Positive<float>.Create(raw);
    protected override float FirstValid => 5f;
    protected override float UpdatedValid => 100f;

    public static TheoryData<float> ValidInputs => new() { 5f, 42f, 10f, 1f, 1.5f, float.MaxValue };
    public static TheoryData<float> InvalidInputs => new() { 0f, -1f, -100f, -1.5f, float.MinValue };
}
