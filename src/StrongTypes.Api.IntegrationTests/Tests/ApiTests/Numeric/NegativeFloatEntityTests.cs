using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NegativeFloatEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NegativeFloatEntityTests, NegativeFloatEntity, Negative<float>, Negative<float>?, float>(factory),
      IEntityTestData<float>
{
    protected override string RoutePrefix => "negative-float-entities";
    protected override Negative<float> Create(float raw) => Negative<float>.Create(raw);
    protected override float FirstValid => -5f;
    protected override float UpdatedValid => -100f;

    public static TheoryData<float> ValidInputs => new() { -5f, -42f, -10f, -1f, -1.5f, float.MinValue };
    public static TheoryData<float> InvalidInputs => new() { 0f, 1f, 100f, 1.5f, float.MaxValue };
}
