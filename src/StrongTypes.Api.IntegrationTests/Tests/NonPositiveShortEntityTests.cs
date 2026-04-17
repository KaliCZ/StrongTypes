using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonPositiveShortEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonPositiveShortEntityTests, NonPositiveShortEntity, NonPositive<short>, NonPositive<short>?, short>(factory),
      IEntityTestData<short>
{
    protected override string RoutePrefix => "non-positive-short-entities";
    protected override NonPositive<short> Create(short raw) => NonPositive<short>.Create(raw);
    protected override short FirstValid => 0;
    protected override short UpdatedValid => -100;

    public static TheoryData<short> ValidInputs => new() { (short)0, (short)-5, (short)-42, (short)-10, short.MinValue };
    public static TheoryData<short> InvalidInputs => new() { (short)1, (short)100, short.MaxValue };
}
