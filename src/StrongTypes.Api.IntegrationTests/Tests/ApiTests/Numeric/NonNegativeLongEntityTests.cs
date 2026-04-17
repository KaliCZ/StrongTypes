using StrongTypes.Api.Entities;
using StrongTypes.Api.IntegrationTests.Infrastructure;
using Xunit;

namespace StrongTypes.Api.IntegrationTests.Tests;

[Collection(IntegrationTestCollection.Name)]
public sealed class NonNegativeLongEntityTests(TestWebApplicationFactory factory)
    : EntityTests<NonNegativeLongEntityTests, NonNegativeLongEntity, NonNegative<long>, NonNegative<long>?, long>(factory),
      IEntityTestData<long>
{
    protected override string RoutePrefix => "non-negative-long-entities";
    protected override NonNegative<long> Create(long raw) => NonNegative<long>.Create(raw);
    protected override long FirstValid => 0L;
    protected override long UpdatedValid => 100L;

    public static TheoryData<long> ValidInputs => new() { 0L, 5L, 42L, 10L, long.MaxValue };
    public static TheoryData<long> InvalidInputs => new() { -1L, -100L, long.MinValue };
}
