using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NegativeShortEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NegativeShortEntityTests, NegativeShortEntity, Negative<short>, Negative<short>?, short>(factory),
      IEntityTestData<short>
{
    protected override string RoutePrefix => "negative-short-entities";
    protected override Negative<short> Create(short raw) => Negative<short>.Create(raw);
    protected override short FirstValid => -5;
    protected override short UpdatedValid => -100;

    public static TheoryData<short> ValidInputs => new() { (short)-5, (short)-42, (short)-10, (short)-1, short.MinValue };
    public static TheoryData<short> InvalidInputs => new() { (short)0, (short)1, (short)100, short.MaxValue };
}
