using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class PositiveShortEntityTests(TestWebApplicationFactory factory)
    : EntityTests<PositiveShortEntityTests, PositiveShortEntity, Positive<short>, Positive<short>?, short>(factory),
      IEntityTestData<short>
{
    protected override string RoutePrefix => "positive-short-entities";
    protected override Positive<short> Create(short raw) => Positive<short>.Create(raw);
    protected override short FirstValid => 5;
    protected override short UpdatedValid => 100;

    public static TheoryData<short> ValidInputs => new() { (short)5, (short)42, (short)10, (short)1, short.MaxValue };
    public static TheoryData<short> InvalidInputs => new() { (short)0, (short)-1, (short)-100, short.MinValue };
}
